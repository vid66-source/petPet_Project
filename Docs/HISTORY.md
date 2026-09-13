# Історія проєкту

Детальний журнал того, що реально було збудовано, стадія за стадією — на відміну від
`Docs/PATTERNS.md` (тільки названі патерни, формат для співбесід), тут описується **кожен**
клас/інтерфейс, який урок вводить: за що відповідає, які має методи і що кожен робить,
навіщо існує, які має залежності (що приймає і звідки вони беруться) і як взаємодіє з
рештою коду. Пишеться завжди з реального фінального коду, не з початкової специфікації
уроку — вони можуть розходитись (наприклад, параметр конструктора лишили для однорідності,
хоч він і не використовується всередині).

Один розділ `## Урок NN — назва` на урок, додається по завершенню уроку в порядку
проходження. Заповнюється тільки після того, як код уроку пройшов рев'ю **і** студент
підтвердив, що реально запускав і перевіряв фічу в Play Mode — той самий гейт, що і в
`Docs/PATTERNS.md`.

**Формат розділу уроку — наскрізний наратив виконання, не список пласких описів.** Іду по
головній лінії виконання програми від точки входу; щойно на цій лінії трапляється
залежність іншого класу — одразу занурююсь у неї (`→ Занурюємось у X`), пояснюю її код і
її власні залежності (рекурсивно, тим самим правилом), тоді повертаюсь
(`← Повертаємось до Y`) до класу, для якого ця залежність була створена, і продовжую решту
його гілок/виконавців, аж поки не повернусь на саму головну лінію. Залежність, яка вже
зустрічалась раніше в цьому ж проході, повторно не розгортається — просто позначається як
вже знайома. Приклад — увесь розділ уроку 01 нижче.

---

## Урок 01 — Composition Root і FSM стану гри

Завершено й підтверджено в Play Mode 2026-09-05: запуск сцени `Bootstrap` дає повну
послідовність у Console `Enter BootstrapState → Enter LoadLevelState → Enter GameLoopState`,
без винятків, з реальним переходом сцен `Bootstrap → Level_Arena`.

Мета уроку: один вхід у гру і явна FSM, яка веде від запуску до ігрового циклу. Нижче —
хід виконання програми від точки входу, з зануренням у кожну залежність по черзі.

### Точка входу: `GameBootstrapper.Awake()`

`GameBootstrapper` (`Infrastructure/GameBootstrapper.cs`) — єдиний `MonoBehaviour`-вхід у
гру, фізичний Composition Root: точка, де Unity-світ торкається чистого C#-світу. Лежить
на GameObject `Bootstraper` у сцені `Bootstrap`.

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

`_curtain` — серіалізована залежність, перетягнута в інспекторі Unity (компонент на
дочірньому GameObject `Image` під `Curtain`). Три рядки `Awake()` — це вся головна лінія
виконання уроку. Перший рядок створює `Game`, передаючи себе (`this`) як `ICoroutineRunner`.

**→ Занурюємось у `Game`** (`Infrastructure/Game.cs`) — кореневий не-`MonoBehaviour`
об'єкт гри, навмисно "тупий", без ігрової логіки:

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

Конструктор `Game` створює дві залежності по черзі. Перша — `SceneLoader`.

**→ Занурюємось у `SceneLoader`** (`Infrastructure/SceneLoader.cs`) — відповідає рівно за
одне: асинхронно завантажити сцену Unity і повідомити колбеком, коли готово; не знає
нічого про стани, гру чи UI:

--------------------------- КОД ---------------------------
<pre>
public class SceneLoader
{
    private readonly ICoroutineRunner _coroutineRunner;
    public SceneLoader(ICoroutineRunner coroutineRunner) =&gt; _coroutineRunner = coroutineRunner;

    public void Load(string sceneName, Action onLoaded = null)
    {
        if (SceneManager.GetActiveScene().name == sceneName)
            onLoaded?.Invoke();
        else
            _coroutineRunner.StartCoroutine(LoadScene(sceneName, onLoaded));
    }

    private IEnumerator LoadScene(string sceneName, Action onLoaded)
    {
        AsyncOperation sceneLoadOperation = SceneManager.LoadSceneAsync(sceneName);
        while (!sceneLoadOperation.isDone) yield return null;
        onLoaded?.Invoke();
    }
}
</pre>
------------------------------------------------------------

`SceneLoader` сам залежить лише від `ICoroutineRunner`.

**→ Занурюємось у `ICoroutineRunner`** (`Infrastructure/ICoroutineRunner.cs`):

--------------------------- КОД ---------------------------
<pre>
public interface ICoroutineRunner { Coroutine StartCoroutine(IEnumerator coroutine); }
</pre>
------------------------------------------------------------

Це абстракція над "умінням запускати корутини" — щоб `SceneLoader` (сам не
`MonoBehaviour`) міг попросити про це, не знаючи, хто саме виконає. Реалізує його той
самий `GameBootstrapper`, з якого почалась уся ця лінія — гілка замкнулась, нового
занурення не треба.

**← Повертаємось до `SceneLoader`.** `Load(sceneName, onLoaded)` — якщо активна сцена вже
має цю назву, одразу викликає `onLoaded`; інакше запускає приватну корутину `LoadScene`,
яка робить `SceneManager.LoadSceneAsync(sceneName)` і чекає, поки `AsyncOperation.isDone`,
не блокуючи гру (`yield return null` — почекати кадр). Залежностей у `SceneLoader` більше
нема.

**← Повертаємось до `Game`.** Перший рядок конструктора (`SceneLoader`) розглянуто, другий
створює `GameStateMachine`, передаючи їй щойно створений `sceneLoader` і отриманий
`curtain`.

**→ Занурюємось у `GameStateMachine`** (`Infrastructure/States/GameStateMachine.cs`) —
Context у State pattern, тримає всі стани гри й перемикає активний:

--------------------------- КОД ---------------------------
<pre>
public class GameStateMachine
{
    private readonly Dictionary&lt;Type, IExitableState&gt; _states;
    private IExitableState _currentState;

    public GameStateMachine(SceneLoader sceneLoader, LoadingCurtain loadingCurtain)
    {
        _states = new Dictionary&lt;Type, IExitableState&gt;();
        _states.Add(typeof(BootstrapState), new BootstrapState(this, sceneLoader));
        _states.Add(typeof(LoadLevelState), new LoadLevelState(this, sceneLoader, loadingCurtain));
        _states.Add(typeof(GameLoopState), new GameLoopState(this));
    }
    // Enter&lt;TState&gt;() / Enter&lt;TState, TPayload&gt;() — див. нижче, після виконавців
}
</pre>
------------------------------------------------------------

`sceneLoader` тут — уже знайомий об'єкт (той самий, щойно розглянутий вище, просто
прокинутий далі), повторно не занурюємось. Але тип значень словника,
`IExitableState`, і три стани, які тут одразу створюються, — нові. Спочатку контракти,
якими типізований словник.

**→ Занурюємось у контракти станів** (`IExitableState`/`IState`/`IPayloadedState<TPayload>`
— `Infrastructure/States/IState.cs` та поруч):

--------------------------- КОД ---------------------------
<pre>
public interface IExitableState { void Exit(); }
public interface IState : IExitableState { void Enter(); }
public interface IPayloadedState&lt;TPayload&gt; : IExitableState { void Enter(TPayload payload); }
</pre>
------------------------------------------------------------

`IExitableState` — найвужчий контракт, тільки `Exit()`; саме ним типізоване поле
`_currentState` у `GameStateMachine`, бо це єдине, що гарантовано є в усіх станах
одночасно. `IState` — для станів без вхідних даних. `IPayloadedState<TPayload>` — для
станів, яким потрібен параметр при вході. Залежностей у самих контрактів немає.

**← Повертаємось до `GameStateMachine`.** Конструктор по черзі створює три виконавці —
проходимо кожного.

**→ Занурюємось у `BootstrapState`** (`Infrastructure/States/BootstrapState.cs`) — перша
фаза гри:

--------------------------- КОД ---------------------------
<pre>
public class BootstrapState : IState
{
    private const string SceneName = "Level_Arena";
    private GameStateMachine _stateMachine;

    public BootstrapState(GameStateMachine gameStateMachine, SceneLoader sceneLoader)
    {
        _stateMachine = gameStateMachine;
    }

    public void Enter()
    {
        Debug.Log($"[FSM] Enter {GetType().Name}");
        _stateMachine.Enter&lt;LoadLevelState, string&gt;(SceneName);
    }

    public void Exit() { /* TODO: сюди в уроках 02-03 переїде RegisterServices() */ }
}
</pre>
------------------------------------------------------------

Обидві залежності конструктора — `GameStateMachine` (той самий, що його й створив) і
`SceneLoader` — уже знайомі з лінії вище, нового занурення не треба (`sceneLoader` тут
навіть не використовується всередині — прийнятий тільки для однорідності сигнатури з
іншими станами, знадобиться в уроках 02-03 для `RegisterServices()`). `Enter()` одразу
запускає перехід у `LoadLevelState` з payload — назвою сцени.

**← Повертаємось до `GameStateMachine`.** Другий виконавець — `LoadLevelState`.

**→ Занурюємось у `LoadLevelState`** (`Infrastructure/States/LoadLevelState.cs`) — показати
завісу, завантажити сцену, сховати завісу, перейти далі:

--------------------------- КОД ---------------------------
<pre>
public class LoadLevelState : IPayloadedState&lt;string&gt;
{
    private readonly GameStateMachine _stateMachine;
    private readonly SceneLoader _sceneLoader;
    private readonly LoadingCurtain _loadingCurtain;

    public LoadLevelState(GameStateMachine gameStateMachine, SceneLoader sceneLoader, LoadingCurtain loadingCurtain)
    {
        _stateMachine = gameStateMachine;
        _sceneLoader = sceneLoader;
        _loadingCurtain = loadingCurtain;
    }

    public void Enter(string sceneName)
    {
        Debug.Log($"[FSM] Enter {GetType().Name}");
        _loadingCurtain.Show();
        _sceneLoader.Load(sceneName, onLoaded);
    }

    private void onLoaded() =&gt; _stateMachine.Enter&lt;GameLoopState&gt;();

    public void Exit() =&gt; _loadingCurtain.Hide();
}
</pre>
------------------------------------------------------------

Перші дві залежності конструктора (`GameStateMachine`, `SceneLoader`) уже знайомі. Третя —
`LoadingCurtain` — нова.

**→ Занурюємось у `LoadingCurtain`** (`Logic/LoadingCurtain.cs`) — візуальна "завіса", що
ховає від гравця сам процес завантаження сцени, найдрібніший клас уроку:

--------------------------- КОД ---------------------------
<pre>
public class LoadingCurtain : MonoBehaviour
{
    public void Show() =&gt; gameObject.SetActive(true);
    public void Hide() =&gt; gameObject.SetActive(false);
}
</pre>
------------------------------------------------------------

Залежностей немає (чистий `MonoBehaviour`). У сцені `Bootstrap` лежить на GameObject
`Image` (дочірньому до `Curtain` — `Canvas`/`CanvasScaler`/`GraphicRaycaster`,
повноекранний чорний спрайт). Практична пастка, знайдена й виправлена цього уроку:
спочатку `Curtain` був окремим коренем сцени, а не дитиною `Bootstraper` — через це
`DontDestroyOnLoad(this)` у `GameBootstrapper` (див. головну лінію нижче) не зберігав би
його при переході сцен, і `LoadLevelState.Exit()` впав би на знищеному об'єкті
(`MissingReferenceException`). Виправлено вручну в Hierarchy (перетягнуто `Curtain` під
`Bootstraper`), підтверджено в Play Mode.

**← Повертаємось до `LoadLevelState`.** `Enter(sceneName)` показує завісу
(`_loadingCurtain.Show()`) і викликає вже знайомий `_sceneLoader.Load(sceneName,
onLoaded)`, передаючи свій приватний метод `onLoaded` як колбек. Коли той спрацює (сцена
довантажилась), `onLoaded()` переходить у `GameLoopState`. `Exit()` (яка ховає завісу)
викликається автоматично самою `GameStateMachine` у момент переходу на наступний стан —
не з середини `onLoaded`.

**← Повертаємось до `GameStateMachine`.** Третій і останній виконавець — `GameLoopState`.

**→ Занурюємось у `GameLoopState`** (`Infrastructure/States/GameLoopState.cs`) — точка
входу в геймплей, навмисно порожня, наповниться з уроку 04:

--------------------------- КОД ---------------------------
<pre>
public class GameLoopState : IState
{
    public GameLoopState(GameStateMachine parent) { }
    public void Enter() =&gt; Debug.Log($"[FSM] Enter {GetType().Name}");
    public void Exit() { }
}
</pre>
------------------------------------------------------------

Єдина залежність — `GameStateMachine`, уже знайомий, поки не використовується всередині
(той самий принцип однорідної сигнатури, що й у `BootstrapState`).

**← Повертаємось до `GameStateMachine`.** Усі три стани зареєстровано в словнику, конструктор
завершено. Лишились самі методи перемикання:

--------------------------- КОД ---------------------------
<pre>
public void Enter&lt;TState&gt;() where TState : class, IState
{
    _currentState?.Exit();
    TState newState = _states[typeof(TState)] as TState;
    _currentState = newState;
    newState?.Enter();
}

public void Enter&lt;TState, TPayload&gt;(TPayload payload) where TState : class, IPayloadedState&lt;TPayload&gt;
{
    _currentState?.Exit();
    TState newState = _states[typeof(TState)] as TState;
    _currentState = newState;
    newState?.Enter(payload);
}
</pre>
------------------------------------------------------------

Обидва — `Exit()` на поточному активному (якщо є) → дістати `TState` зі словника за
`typeof(TState)` → зробити активним → `Enter()`/`Enter(payload)`. Ніяких нових залежностей
тут немає — усе, що потрібно, уже пройдено вище.

**← Повертаємось до `Game`.** Конструктор `Game` завершено: `StateMachine` зібраний і
збережений у властивості `StateMachine`. Більше в `Game` нічого немає.

**← Повертаємось до `GameBootstrapper.Awake()`** — головної лінії виконання. Перший рядок
(`new Game(...)`) щойно повністю розглянуто. Другий — `DontDestroyOnLoad(this)`, який
зберігає між сценами сам об'єкт `Bootstraper` і всю його дочірню ієрархію (звідси й
вимога, щоб `Curtain` був дитиною `Bootstraper`, — див. занурення в `LoadingCurtain`
вище). Третій — `_game.StateMachine.Enter<BootstrapState>()`: це виклик того самого
`GameStateMachine.Enter<TState>()`, який ми щойно розглянули, — виконання переходить у
`BootstrapState.Enter()` → (через `Enter<LoadLevelState, string>`) `LoadLevelState.Enter()`
→ (через `onLoaded` → `Enter<GameLoopState>`) `GameLoopState.Enter()`. Це і є та сама
послідовність, яку підтвердили логи в Play Mode на початку розділу.

---

## Урок 02 — Service Locator і перший сервіс (`IAssetProvider`)

Завершено й підтверджено в Play Mode 2026-09-08: повний ланцюжок у Console — реєстрація
сервісу, побудова всіх станів, `Enter BootstrapState → Enter LoadLevelState → Enter
GameLoopState`, і в `GameLoopState` реальні `Loaded TestObject` / `Spawning TestObject`
без винятків.

Мета уроку: центральний реєстр сервісів (`AllServices`) і перший сервіс у ньому —
провайдер над `Resources`. Той самий вхід, що й в уроці 01 (`GameBootstrapper.Awake()`),
але тепер конструктори по дорозі приймають і прокидують ще одну залежність —
`AllServices`. Класи, вже розглянуті в уроці 01, тут лише згадуються (без повторного
занурення), якщо їхній код не змінився по суті.

### Точка входу: та сама, `GameBootstrapper.Awake()`

Код не змінився з уроку 01. Перший рядок, як і раніше, створює `Game`.

**→ Занурюємось у `Game`** (`Infrastructure/Game.cs`) — код цього класу змінився:

--------------------------- КОД ---------------------------
<pre>
public class Game
{
    public GameStateMachine StateMachine {get;}

    public Game(ICoroutineRunner coroutineRunner, LoadingCurtain curtain)
    {
        SceneLoader sceneLoader = new SceneLoader(coroutineRunner);
        StateMachine = new GameStateMachine(sceneLoader, curtain, AllServices.Instance);
    }
}
</pre>
------------------------------------------------------------

Перший рядок конструктора (`SceneLoader`) — уже знайомий з уроку 01, повторно не
занурюємось. Другий рядок тепер передає в `GameStateMachine` третій аргумент —
`AllServices.Instance`. Це перше звернення до нового класу цього уроку.

**→ Занурюємось у `AllServices`** (`Infrastructure/Services/AllServices.cs`) — Service
Locator, центральний реєстр сервісів гри:

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

Конструктор приватний, а `Instance` — публічна `static`-властивість, ініціалізована
одразу при першому зверненні до класу (eager singleton): рівно один інстанс на все
життя гри, і ніхто інший не може створити другий. `RegisterService<TService>`/
`GetService<TService>` — обидва generic з обмеженням `where TService : class, IService`,
всередині — звичайний `Dictionary<Type, IService>` (той самий прийом "тип як ключ", що
й `_states` у `GameStateMachine` уроку 01, тільки тут ключем реєструється сервіс).
`Instance.` — це єдиний публічний статичний доступ, дозволений у всьому проєкті (свідомий,
вузький виняток із заборони на статичні синглтони з уроку 01) — і викликається лише в
трьох місцях: тут (`Game`), і далі нижче (`GameStateMachine`, `BootstrapState`).
Залежностей у самого `AllServices` більше немає.

**→ Занурюємось у `IService`** (`Infrastructure/Services/IService.cs`) — маркерний
інтерфейс, використаний як generic-обмеження вище:

--------------------------- КОД ---------------------------
<pre>
public interface IService { }
</pre>
------------------------------------------------------------

Жодного методу — сам факт реалізації цього інтерфейсу і є вся інформація, яку він несе:
"цей клас можна класти в `AllServices`". Без нього `RegisterService<TService>` довелось
би або приймати `object` (втрата типобезпеки), або обмежувати generic-параметр чимось
конкретним (втрата гнучкості для різних сервісів). Залежностей немає.

**← Повертаємось до `Game`.** Конструктор `Game` завершено: `StateMachine` зібраний,
переданий `AllServices.Instance` разом із уже знайомими `sceneLoader`/`curtain`.

**← Повертаємось до `GameBootstrapper.Awake()`**, тепер уже в `GameStateMachine` —

**→ Занурюємось у `GameStateMachine`** (`Infrastructure/States/GameStateMachine.cs`) —
сигнатура конструктора й тіло змінились відносно уроку 01:

--------------------------- КОД ---------------------------
<pre>
public GameStateMachine(SceneLoader sceneLoader, LoadingCurtain loadingCurtain, AllServices services)
{
    _states = new Dictionary&lt;Type, IExitableState&gt;();
    _states.Add(typeof(BootstrapState), new BootstrapState(this, services));
    _states.Add(typeof(LoadLevelState), new LoadLevelState(this, sceneLoader, loadingCurtain));
    IAssetProvider assetProvider = services.GetService&lt;IAssetProvider&gt;();
    _states.Add(typeof(GameLoopState), new GameLoopState(assetProvider));
    Debug.Log($"[FSM] {GetType().Name} created {_states[typeof(BootstrapState)].GetType().Name} " +
              $"{_states[typeof(LoadLevelState)].GetType().Name} " +
              $"{_states[typeof(GameLoopState)].GetType().Name} states");
}
</pre>
------------------------------------------------------------

`sceneLoader`/`loadingCurtain` — уже знайомі, просто прокинуті далі в `LoadLevelState`
(код `LoadLevelState` не змінився з уроку 01, не занурюємось повторно). Новий параметр
— `services`, той самий `AllServices.Instance`, щойно розглянутий вище. Перший рядок
створює `BootstrapState`, передаючи йому `this` (сам `GameStateMachine`) і `services`.

**→ Занурюємось у `BootstrapState`** (`Infrastructure/States/BootstrapState.cs`) —
конструктор і тіло цього уроку:

--------------------------- КОД ---------------------------
<pre>
public class BootstrapState : IState
{
    private const string SceneName  =  "Level_Arena";

    private readonly GameStateMachine _stateMachine;
    private readonly AllServices _services;

    public BootstrapState(GameStateMachine gameStateMachine, AllServices services)
    {
        _stateMachine = gameStateMachine;
        _services = services;
        RegisterServices();
    }

    public void Enter()
    {
        Debug.Log($"[FSM] Enter {GetType().Name}");
        Debug.Log($"[FSM] {GetType().Name} initiated enter LoadLevelState");
        _stateMachine.Enter&lt;LoadLevelState, string&gt;(SceneName);
    }

    private void RegisterServices()
    {
        AssetProvider assetProvider = new AssetProvider();
        Debug.Log($"[FSM] {GetType().Name} initiated registration of a new service {assetProvider.GetType().Name}");
        _services.RegisterService&lt;IAssetProvider&gt;(assetProvider);
    }

    public void Exit() { /* TODO */ }
}
</pre>
------------------------------------------------------------

`_stateMachine` — уже знайомий. `_services` — щойно розглянутий `AllServices`.
Конструктор одразу, у собі самому, викликає `RegisterServices()` — реєстрація
відбувається до того, як `Enter()` взагалі буде викликано, бо `GameStateMachine`
будує всі три стани одразу у своєму конструкторі (той самий порядок, що і в уроці 01),
і `BootstrapState` там перший — реєстрація встигає до того, як нижче по цьому ж
конструктору `GameStateMachine` спробує дістати `IAssetProvider` для `GameLoopState`.
`RegisterServices()` створює новий `AssetProvider` — перше звернення до нового класу.

**→ Занурюємось у `AssetProvider`/`IAssetProvider`**
(`Infrastructure/AssetManagement/AssetProvider.cs` та поруч) — Provider-патерн над
`Resources`:

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

    public GameObject SpawnAsset(GameObject asset)
    {
        Debug.Log($"[AssetProvider] Spawning {asset.name}");
        GameObject spawnAsset = Object.Instantiate(asset);
        return spawnAsset;
    }

    public GameObject SpawnAsset(GameObject asset, Vector3 position, Quaternion rotation)
    {
        Debug.Log($"[AssetProvider] Spawning {asset.name} with position {position} and rotation {rotation}");
        GameObject spawnAsset = Object.Instantiate(asset, position, rotation);
        return spawnAsset;
    }
}
</pre>
------------------------------------------------------------

`IAssetProvider : IService` — успадковує маркер, тому щойно створений `assetProvider`
можна зареєструвати як `IAssetProvider` без додаткового каста. `LoadAsset` — тонка
обгортка над `Resources.Load<GameObject>`, з логом успіху/невдачі. Обидва `SpawnAsset` —
обгортки над `Object.Instantiate`, повертають саме результат інстанціювання (а не
вхідний `asset`-референс — рання версія цього коду помилково повертала сам префаб,
виправлено під час рев'ю). Залежностей, крім `UnityEngine`, немає.

**← Повертаємось до `BootstrapState.RegisterServices()`.** `assetProvider` створено,
`_services.RegisterService<IAssetProvider>(assetProvider)` кладе його в реєстр —
конструктор `BootstrapState` завершено.

**← Повертаємось до `GameStateMachine`.** `BootstrapState` доданий у `_states`. Другий
рядок — `LoadLevelState`, код і залежності якого не змінились з уроку 01 (повторно не
занурюємось). Третій рядок — новий: `services.GetService<IAssetProvider>()` дістає з
реєстру щойно зареєстрований `assetProvider` і передає його в `GameLoopState`.

**→ Занурюємось у `GameLoopState`** (`Infrastructure/States/GameLoopState.cs`) — цей
урок уперше наповнює цей стан реальною логікою:

--------------------------- КОД ---------------------------
<pre>
public class GameLoopState : IState
{
    private readonly IAssetProvider _assetProvider;
    private readonly string _assetPath = "TestObject";

    public GameLoopState(IAssetProvider assetProvider)
    {
        _assetProvider = assetProvider;
    }

    public void Enter()
    {
        Debug.Log($"[FSM] Enter {GetType().Name}");
        var obj = _assetProvider.LoadAsset(_assetPath);
        _assetProvider.SpawnAsset(obj, Vector3.one, Quaternion.identity);
    }

    public void Exit() { }
}
</pre>
------------------------------------------------------------

Єдина залежність — `IAssetProvider`, уже знайомий (той самий `assetProvider`, щойно
зареєстрований і діставаний вище). В уроці 01 цей клас нічого не робив у `Enter()`,
крім логу; тепер він через уже знайомий `_assetProvider` завантажує тестовий префаб
(`Resources/TestObject.prefab`) і спавнить його в точці `Vector3.one`.

**← Повертаємось до `GameStateMachine`.** Усі три стани зареєстровано, конструктор
завершено новим логом (`created BootstrapState LoadLevelState GameLoopState states`).
Методи `Enter<TState>()`/`Enter<TState, TPayload>()` не змінились з уроку 01.

**← Повертаємось до `Game`, тоді до `GameBootstrapper.Awake()`.** Решта головної лінії —
`DontDestroyOnLoad(this)` і `_game.StateMachine.Enter<BootstrapState>()` — не змінилась
з уроку 01. Але тепер сам прохід `BootstrapState.Enter()` → `LoadLevelState.Enter()` →
`GameLoopState.Enter()` завершується реальною дією (спавн `TestObject`), а не тільки
логом переходу — це і підтвердили логи в Play Mode на початку розділу.

---

## Урок 03 — `IInputService`, другий сервіс і подія-переклад над Input System

Завершено й підтверджено в Play Mode 2026-09-12: Console показує реєстрацію обох
сервісів (`IAssetProvider`, `IInputService`) через `RegisterService<TService>` без
жодної зміни в `AllServices`, повний прохід `Enter BootstrapState → Enter
LoadLevelState → Enter GameLoopState`, і реальне натискання клавіші Jump викликає
підписаний `GameLoopState.TestJump()` через увесь ланцюжок Unity Input System →
`InputService.OnJumpPressed` → `GameLoopState`.

Той самий вхід, що й в уроках 01-02 (`GameBootstrapper.Awake()` → `Game` —обидва без
змін цього уроку). Зміни починаються в `GameStateMachine`.

**→ Занурюємось у `GameStateMachine`** (`Infrastructure/States/GameStateMachine.cs`) —
конструктор тепер додатково резолвить другий сервіс:

--------------------------- КОД ---------------------------
<pre>
public GameStateMachine(SceneLoader sceneLoader, LoadingCurtain loadingCurtain, AllServices services)
{
    _states = new Dictionary&lt;Type, IExitableState&gt;();
    _states.Add(typeof(BootstrapState), new BootstrapState(this, services));
    _states.Add(typeof(LoadLevelState), new LoadLevelState(this, sceneLoader, loadingCurtain));
    IAssetProvider assetProvider = services.GetService&lt;IAssetProvider&gt;();
    IInputService inputService = services.GetService&lt;IInputService&gt;();
    _states.Add(typeof(GameLoopState), new GameLoopState(assetProvider, inputService));

    Debug.Log($"[FSM] {GetType().Name} created {_states[typeof(BootstrapState)].GetType().Name} " +
              $"{_states[typeof(LoadLevelState)].GetType().Name} " +
              $"{_states[typeof(GameLoopState)].GetType().Name} states");
}
</pre>
------------------------------------------------------------

`sceneLoader`/`loadingCurtain`/`services` — уже знайомі з уроків 01-02. Перший рядок
створює `BootstrapState`, передаючи ті самі `this`/`services`, що й в уроці 02, але
тіло цього класу цього уроку змінилось.

**→ Занурюємось у `BootstrapState`** (`Infrastructure/States/BootstrapState.cs`) —
`RegisterServices()` тепер реєструє два сервіси:

--------------------------- КОД ---------------------------
<pre>
private void RegisterServices()
{
    AssetProvider assetProvider = new AssetProvider();
    InputService inputService = new InputService();
    Debug.Log($"[FSM] {GetType().Name} initiated registration of a new service {assetProvider.GetType().Name}");
    Debug.Log($"[FSM] {GetType().Name} initiated registration of a new service {inputService.GetType().Name}");
    _services.RegisterService&lt;IAssetProvider&gt;(assetProvider);
    _services.RegisterService&lt;IInputService&gt;(inputService);
}
</pre>
------------------------------------------------------------

`AssetProvider` — уже знайомий з уроку 02, повторно не занурюємось. `InputService` —
новий клас цього уроку, створений тим самим прийомом: `new`, потім
`RegisterService<TService>` — жодної правки в самому `AllServices` не знадобилось
(доказ OCP: контейнер відкритий для нового типу сервісу, закритий для модифікації).

**→ Занурюємось у `IInputService`/`InputService`**
(`Infrastructure/Input/IInputService.cs` та `InputService.cs`) — другий сервіс,
обгортка над новим Input System:

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

`IInputService : IService` — успадковує той самий маркер, що й `IAssetProvider`,
тому реєструється в `AllServices` без додаткового каста. Контракт навмисно вузький
(ISP): лише те, чим реально користується решта гри — опитування напрямку руху
(`GetDirection()`, `Vector2`, бо `Move` — безперервне значення, перевіряється "на
запит") і разова подія натискання (`OnJumpPressed`, `event Action`, бо натискання —
дискретний момент, а не значення).

Усередині `InputService` — приватне поле `_inputActions`, екземпляр згенерованого
(`Generate C# Class`) класу з Input Actions asset'у (`Assets/Input/InputActions.inputactions`).
Конструктор: створює екземпляр, викликає `.Player.Enable()` (без цього виклику новий
Input System мовчки нічого не зчитує — ні `ReadValue`, ні події), і підписується на
`_inputActions.Player.Jump.performed` лямбдою, яка одразу ретранслює подію у власну,
простішу `OnJumpPressed` (без жодного Unity-типу в сигнатурі). Це і є та сама
DIP-межа: `InputService` — єдине місце в проєкті, яке імпортує
`UnityEngine.InputSystem` (перевірено скриптом `Docs/tools/verify_lesson.sh`); решта
гри (зараз — `GameLoopState`, у майбутньому `PlayerController`) залежить лише від
`IInputService`. `GetDirection()` — тонка обгортка над `_inputActions.Player.Move.ReadValue<Vector2>()`,
той самий принцип делегування, що й `LoadAsset`/`Resources.Load` в уроці 02.

**← Повертаємось до `BootstrapState.RegisterServices()`.** `inputService` створено й
зареєстровано — конструктор `BootstrapState` завершено.

**← Повертаємось до `GameStateMachine`.** `BootstrapState` доданий у `_states`. Другий
рядок — `LoadLevelState`, не змінився з уроку 01, не занурюємось. Третій і четвертий
рядки — нові: `services.GetService<IAssetProvider>()` (уже знайомий виклик з уроку 02)
і поруч такий самий `services.GetService<IInputService>()` для щойно зареєстрованого
`inputService`. Обидва передаються в `GameLoopState`.

**→ Занурюємось у `GameLoopState`** (`Infrastructure/States/GameLoopState.cs`) —
другий параметр конструктора й нова логіка в `Enter()`/`Exit()`:

--------------------------- КОД ---------------------------
<pre>
public class GameLoopState : IState
{
    private readonly IAssetProvider _assetProvider;
    private readonly IInputService _inputService;
    private readonly string _assetPath = "TestObject";

    public GameLoopState(IAssetProvider assetProvider, IInputService inputService)
    {
        _assetProvider = assetProvider;
        _inputService = inputService;
    }

    public void Enter()
    {
        Debug.Log($"[FSM] Enter {GetType().Name}");
        var obj = _assetProvider.LoadAsset(_assetPath);
        _assetProvider.SpawnAsset(obj, Vector3.one, Quaternion.identity);
        _inputService.OnJumpPressed += TestJump;
    }

    private void TestJump()
    {
        Debug.Log($"Jump Action!");
    }

    public void Exit()
    {
        _inputService.OnJumpPressed -= TestJump;
    }
}
</pre>
------------------------------------------------------------

`_assetProvider`-частина `Enter()` не змінилась з уроку 02. Нове — `_inputService`,
уже знайомий (той самий, щойно зареєстрований і діставаний вище): `Enter()`
підписує приватний метод `TestJump` на `OnJumpPressed`, `Exit()` симетрично
відписує тим самим методом (той самий принцип, що вже застосований для завіси в
`LoadLevelState.Exit()` уроку 01, — підписка й відписка мають бути парними, інакше
повторний вхід у стан подвоїв би виклики). `TestJump` — навмисно тимчасовий
смок-тест уроку 03 (просто `Debug.Log`), не постійна ігрова логіка — та прийде разом
із `PlayerController` в уроці 04, який і замінить цей метод на реальний стрибок.

**← Повертаємось до `GameStateMachine`, тоді до `Game`, тоді до `GameBootstrapper.Awake()`.**
Решта головної лінії не змінилась з уроків 01-02. Але тепер `GameLoopState.Enter()`
завершується не лише спавном `TestObject`, а й живою підпискою на реальний ввід
гравця — натискання Jump у Play Mode доходить через увесь ланцюжок (Unity Input
System → `InputService` → `OnJumpPressed` → `GameLoopState.TestJump()`) до логу в
Console, що й підтвердили логи на початку розділу.
