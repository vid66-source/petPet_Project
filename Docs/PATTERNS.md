# Патерни в проєкті

Шпаргалка для повторення перед співбесідою: для кожного патерна — розлоге пояснення
(навіщо він тут, яку проблему вирішує) + реальні приклади коду з цього проєкту (не
абстрактні, а те, що справді написано і працює).

**Коли зʼявляється запис:** тільки після того, як код написаний, пройшов рев'ю, і
студент запустив/протестував фічу в Play Mode. Не наперед і не як заготовка — інакше
це просто інший спосіб списати, а не результат пророблення теми.

---

## Урок 01 — Composition Root і FSM стану гри

Підтверджено в Play Mode 2026-09-05: повна послідовність `Enter BootstrapState → Enter
LoadLevelState → Enter GameLoopState` у Console, без винятків.

### Composition Root

**Проблема, яку вирішує:** хтось повинен зібрати граф залежностей гри (хто кому що
передає в конструктор), і це має бути видно одним поглядом, а не розмазано по коду через
`FindObjectOfType`/статичні синглтони.

**Реалізація** — `Assets/CodeBase/Infrastructure/GameBootstrapper.cs`, єдина точка, де
Unity-світ (`MonoBehaviour`) торкається чистого C#-світу гри:

--------------------------- КОД ---------------------------
<pre>
public class GameBootstrapper : MonoBehaviour, ICoroutineRunner
{
    [SerializeField] private LoadingCurtain _curtain;
    private Game _game;

    private void Awake()
    {
        _game = new Game(this, _curtain);
        DontDestroyOnLoad(this);
        _game.StateMachine.Enter&lt;BootstrapState&gt;();
    }
}
</pre>
------------------------------------------------------------

`Game.cs` — кореневий не-MonoBehaviour об'єкт, який продовжує збирати граф далі:

--------------------------- КОД ---------------------------
<pre>
public class Game
{
    public GameStateMachine StateMachine { get; }

    public Game(ICoroutineRunner coroutineRunner, LoadingCurtain curtain)
    {
        SceneLoader sceneLoader = new SceneLoader(coroutineRunner);
        StateMachine = new GameStateMachine(sceneLoader, curtain);
    }
}
</pre>
------------------------------------------------------------

Усі залежності передаються через конструктор — жодного класу, який сам собі щось шукає.

### State (GoF) — FSM глобального стану гри

**Проблема:** описати послідовність фаз гри (завантаження → рівень → геймплей) без
розкиданих `if (isLoading) ... else if (isPlaying) ...` по всьому коду.

**Контракти** — `IState.cs`, `IPayloadedState.cs`, `IExitableState.cs`:

--------------------------- КОД ---------------------------
<pre>
public interface IExitableState { void Exit(); }
public interface IState : IExitableState { void Enter(); }
public interface IPayloadedState&lt;TPayload&gt; : IExitableState { void Enter(TPayload payload); }
</pre>
------------------------------------------------------------

**Context** — `GameStateMachine.cs`, тримає всі стани як
`Dictionary<Type, IExitableState>`, створені один раз у конструкторі:

--------------------------- КОД ---------------------------
<pre>
public GameStateMachine(SceneLoader sceneLoader, LoadingCurtain loadingCurtain)
{
    _states = new Dictionary&lt;Type, IExitableState&gt;();
    _states.Add(typeof(BootstrapState), new BootstrapState(this, sceneLoader));
    _states.Add(typeof(LoadLevelState), new LoadLevelState(this, sceneLoader, loadingCurtain));
    _states.Add(typeof(GameLoopState), new GameLoopState(this));
}

public void Enter&lt;TState&gt;() where TState : class, IState
{
    _currentState?.Exit();
    TState newState = _states[typeof(TState)] as TState;
    _currentState = newState;
    newState?.Enter();
}
</pre>
------------------------------------------------------------

**Конкретні стани** самі знають, куди переходять далі — наприклад
`BootstrapState.Enter()`:

--------------------------- КОД ---------------------------
<pre>
public void Enter()
{
    Debug.Log($"[FSM] Enter {GetType().Name}");
    _stateMachine.Enter&lt;LoadLevelState, string&gt;(SceneName);
}
</pre>
------------------------------------------------------------

### SRP (Single Responsibility)

Кожен клас має рівно одну причину змінюватись:
- `SceneLoader.cs` уміє рівно одне — асинхронно завантажити сцену й повідомити колбеком:
  --------------------------- КОД ---------------------------
  <pre>
  public void Load(string sceneName, Action onLoaded = null)
  {
      if (SceneManager.GetActiveScene().name == sceneName)
          onLoaded?.Invoke();
      else
          _coroutineRunner.StartCoroutine(LoadScene(sceneName, onLoaded));
  }
  </pre>
  ------------------------------------------------------------
- `LoadLevelState.cs` відповідає за один сценарій — "покажи завісу, завантаж сцену, сховай
  завісу", нічого про гравця чи UI:
  --------------------------- КОД ---------------------------
  <pre>
  public void Enter(string sceneName)
  {
      Debug.Log($"[FSM] Enter {GetType().Name}");
      _loadingCurtain.Show();
      _sceneLoader.Load(sceneName, onLoaded);
  }

  private void onLoaded() =&gt; _stateMachine.Enter&lt;GameLoopState&gt;();

  public void Exit() =&gt; _loadingCurtain.Hide();
  </pre>
  ------------------------------------------------------------

### OCP (Open/Closed)

`GameStateMachine.Enter<TState>()` написаний один раз і працює для будь-якої кількості
станів, зареєстрованих у словнику — включно з тими, яких ще немає (майбутній
`PauseState`). Додавання нового стану = новий клас + рядок реєстрації в конструкторі
`GameStateMachine`, жоден з існуючих станів не редагується.

### ISP (Interface Segregation)

Три вузькі інтерфейси стану (`IExitableState`/`IState`/`IPayloadedState<TPayload>`) замість
одного товстого — `BootstrapState`/`GameLoopState` реалізують тільки `Enter()` без
параметра, `LoadLevelState` окремо `Enter(string)`; жоден не змушений реалізовувати метод,
який йому не потрібен.

### LSP (Liskov Substitution)

`GameStateMachine.Enter<TState>()` працює однаково байдуже, чи `TState` — це
`BootstrapState`, чи `GameLoopState`, чи майбутній `PauseState`: жодного
`if (TState == typeof(...))`, жодних спецвипадків для конкретної реалізації.

### DIP (Dependency Inversion)

Найчистіший приклад — `ICoroutineRunner.cs`:

--------------------------- КОД ---------------------------
<pre>
public interface ICoroutineRunner { Coroutine StartCoroutine(IEnumerator coroutine); }
</pre>
------------------------------------------------------------

`SceneLoader` (логіка високого рівня) залежить від цієї абстракції, а не від конкретного
`MonoBehaviour`:

--------------------------- КОД ---------------------------
<pre>
public class SceneLoader
{
    private readonly ICoroutineRunner _coroutineRunner;
    public SceneLoader(ICoroutineRunner coroutineRunner) =&gt; _coroutineRunner = coroutineRunner;
    ...
}
</pre>
------------------------------------------------------------

`GameBootstrapper` реалізує `ICoroutineRunner` безкоштовно (у `MonoBehaviour` вже є
`StartCoroutine` з потрібною сигнатурою) — конкретна деталь підставляється туди, де
очікується абстракція, без жодного `GameBootstrapper.Instance`-звернення.

Далі читати: State pattern — refactoring.guru; Composition Root/ручний DI — блог Марка
Сімана (blog.ploeh.dk); SOLID — en.wikipedia.org/wiki/SOLID.

---

## Урок 02 — Service Locator і Provider над асетами

Підтверджено в Play Mode 2026-09-08: Console показує повний ланцюжок без винятків —
реєстрація `IAssetProvider` у `AllServices`, побудова всіх трьох станів, переходи
`BootstrapState → LoadLevelState → GameLoopState`, і в `GameLoopState.Enter()` реальне
`LoadAsset`/`SpawnAsset` тестового префабу `TestObject` з `Resources/`.

### Service Locator — `AllServices`

**Проблема, яку вирішує:** десь потрібен один центральний реєстр сервісів гри (зараз —
`IAssetProvider`, далі — `IInputService`, `IStaticDataService` тощо), щоб не тягнути
кожну залежність вручну через усі проміжні конструктори і не звертатись до конкретних
класів напряму.

**Свідомий виняток із заборони на статичні синглтони** (яку сам же курс і викладає —
див. DIP уроку 01, "жодного `GameBootstrapper.Instance`"): `AllServices.Instance` —
єдине місце в проєкті, де публічний статичний доступ дозволений, і звертається до нього
рівно три класи (`Game`, `GameStateMachine`, `BootstrapState`), ніде більше. Це
свідомий компроміс Service Locator-патерну, а не порушення DIP — сам реєстр не ховає
залежності всередині бізнес-логіки, він сам і є точкою збірки графу.

`Assets/CodeBase/Infrastructure/Services/AllServices.cs`:

--------------------------- КОД ---------------------------
<pre>
public class AllServices
{
    private readonly Dictionary&lt;Type, IService&gt; _services;

    public static AllServices Instance { get; } = new AllServices();

    private AllServices()
    {
        _services = new Dictionary&lt;Type, IService&gt;();
    }

    public void RegisterService&lt;TService&gt;(TService service) where TService : class, IService
    {
        _services.Add(typeof(TService), service);
        Debug.Log($"[Services] Registered service of type {typeof(TService).Name}");
    }

    public TService GetService&lt;TService&gt;() where TService : class, IService
    {
        var service = _services[typeof(TService)] as TService;
        return service;
    }
}
</pre>
------------------------------------------------------------

Приватний конструктор + публічна `static` властивість, ініціалізована одразу при
першому зверненні до класу (eager singleton) — інстанс гарантовано один на весь час
життя гри, і ніхто ззовні не може створити другий через `new AllServices()`.
`Dictionary<Type, IService>` — той самий прийом "тип як ключ", що й `GameStateMachine`
уроку 01, тільки тут ключем реєструється сервіс, а не стан.

`IService.cs` — маркерний інтерфейс без жодного методу:

--------------------------- КОД ---------------------------
<pre>
public interface IService { }
</pre>
------------------------------------------------------------

**Навіщо порожній інтерфейс:** він не описує поведінку, а обмежує, *що взагалі можна*
покласти в `AllServices` — generic-constraint `where TService : class, IService` не
дозволить зареєструвати будь-який випадковий об'єкт, тільки те, що явно позначено як
сервіс. Це `I` та частково `D` з SOLID: вузький маркер замість "здогадуватись" по типу.

### Provider — `IAssetProvider`/`AssetProvider`

**Проблема, яку вирішує:** решта коду не повинна знати, *звідки* фізично береться
асет (`Resources.Load`, а пізніше — Addressables, урок 15) — тільки що є спосіб
попросити асет за шляхом і заспавнити його.

--------------------------- КОД ---------------------------
<pre>
public interface IAssetProvider : IService
{
    public GameObject LoadAsset(string assetPath);
    public GameObject SpawnAsset(GameObject asset);
    public GameObject SpawnAsset(GameObject asset, Vector3 position, Quaternion rotation);
}

public class AssetProvider : IAssetProvider
{
    public GameObject LoadAsset(string assetPath)
    {
        GameObject asset = Resources.Load&lt;GameObject&gt;(assetPath);
        if (asset != null)
            Debug.Log($"[AssetProvider] Loaded {assetPath}");
        else
            Debug.LogError($"[AssetProvider] Failed to load {assetPath}");
        return asset;
    }

    public GameObject SpawnAsset(GameObject asset, Vector3 position, Quaternion rotation)
    {
        Debug.Log($"[AssetProvider] Spawning {asset.name} with position {position} and rotation {rotation}");
        GameObject spawnAsset = Object.Instantiate(asset, position, rotation);
        return spawnAsset;
    }
    // + беспараметрове SpawnAsset(GameObject asset) — той самий принцип
}
</pre>
------------------------------------------------------------

`IAssetProvider : IService` — інтерфейс сервісу одразу успадковує маркер, тому
`AllServices.RegisterService<IAssetProvider>(assetProvider)` компілюється без
додаткового каста. Споживач (`GameLoopState`) отримує саме інтерфейс, а не конкретний
`AssetProvider` — можна підмінити реалізацію (наприклад, на Addressables-провайдер в
уроці 15) без жодної зміни в коді, який ним користується (ще один приклад DIP і LSP).

### YAGNI — прибирання невикористаних параметрів конструктора

Під час рев'ю цього уроку в конструкторах `BootstrapState` і `GameLoopState` знайшлись
параметри, скопійовані по аналогії з сусідніх станів, але фактично ніде не використані
(`SceneLoader sceneLoader` у `BootstrapState`, `GameStateMachine stateMachine` у
`GameLoopState`) — обидва прибрані. Правило, яке з цього лишається: параметр
конструктора отримує стан лише тоді, коли **сам цей клас** ним реально користується
(наприклад, `_stateMachine` лишається в `BootstrapState`/`LoadLevelState`, бо вони самі
ініціюють перехід в наступний стан через `Enter<TState>()`) — а не "про запас", бо
"стан взагалі має вміти так". Незайнята залежність у сигнатурі — прихована брехня для
того, хто читає код: він думає, що клас цим користується, а він ні. `GameLoopState`
поверне собі `GameStateMachine` в уроці 13, коли реально почне сам ініціювати переходи
в `VictoryState`/`GameOverState`.

---

## Урок 03 — `IInputService`, другий сервіс над новим Input System

Підтверджено в Play Mode 2026-09-12: Console показує реєстрацію обох сервісів
(`IAssetProvider`, `IInputService`) через `RegisterService<TService>`, повний прохід
FSM, і реальне натискання Jump доходить через `InputService.OnJumpPressed` до
`GameLoopState.TestJump()`.

### Другий сервіс у контейнері — без жодної правки `AllServices` (OCP)

`Assets/CodeBase/Infrastructure/States/BootstrapState.cs`, `RegisterServices()`:

--------------------------- КОД ---------------------------
<pre>
private void RegisterServices()
{
    AssetProvider assetProvider = new AssetProvider();
    InputService inputService = new InputService();
    _services.RegisterService&lt;IAssetProvider&gt;(assetProvider);
    _services.RegisterService&lt;IInputService&gt;(inputService);
}
</pre>
------------------------------------------------------------

Той самий `RegisterService<TService>`, що й в уроці 02 для `IAssetProvider`, тепер
використаний для типу, який ще не існував на момент написання `AllServices`. Жодного
рядка в самому `AllServices` не довелось міняти — контейнер відкритий для нового
типу сервісу, закритий для модифікації.

### `IInputService` — вузький контракт (ISP) над новим Input System

--------------------------- КОД ---------------------------
<pre>
public interface IInputService : IService
{
    Vector2 GetDirection();

    event Action OnJumpPressed;
}

public class InputService : IInputService
{
    private readonly InputActions _inputActions;

    public event Action OnJumpPressed;

    public InputService()
    {
        _inputActions = new InputActions();
        _inputActions.Player.Enable();
        _inputActions.Player.Jump.performed += ctx =&gt; OnJumpPressed?.Invoke();
    }

    public Vector2 GetDirection()
    {
        Vector2 direction = _inputActions.Player.Move.ReadValue&lt;Vector2&gt;();
        return direction;
    }
}
</pre>
------------------------------------------------------------

**ISP:** контракт тримає лише те, чим реально користується решта гри — `GetDirection()`
(значення опитується "на запит", бо рух — безперервна величина) і `OnJumpPressed`
(разова подія, бо натискання — дискретний момент) — а не весь API згенерованого
Input System класу.

**DIP:** `InputService` — єдине місце в проєкті, яке імпортує
`UnityEngine.InputSystem` (перевірено `Docs/tools/verify_lesson.sh`). Решта гри
залежить лише від `IInputService`.

### Подія-переклад — `InputService` як міст між Unity-подією й подією проєкту

`InputService` одночасно підписник (на `_inputActions.Player.Jump.performed`,
Unity-подію) і видавець (власної `OnJumpPressed`) — переводить Unity-специфічну
подію (з параметром `InputAction.CallbackContext`) у просту, без жодного Unity-типу
в сигнатурі. Споживач (`GameLoopState`) ніколи не бачить `InputAction.CallbackContext`:

--------------------------- КОД ---------------------------
<pre>
public void Enter()
{
    ...
    _inputService.OnJumpPressed += TestJump;
}

public void Exit()
{
    _inputService.OnJumpPressed -= TestJump;
}
</pre>
------------------------------------------------------------

Підписка через **іменований** метод (`TestJump`), не інлайн-лямбду — саме тому
відписка (`-=`) в `Exit()` взагалі спрацьовує (той самий делегат-об'єкт при `+=` і
`-=`). Симетрична пара `Enter()`/`Exit()` — той самий принцип, що вже застосований
для завіси в `LoadLevelState.Exit()` уроку 01: без відписки повторний вхід у стан
подвоїв би виклики `TestJump`.

Далі читати: детальний розбір `.started`/`.performed`/`.canceled` і `CallbackContext`
— `Docs/Notes/InputAction_Events_and_CallbackContext.md`.
