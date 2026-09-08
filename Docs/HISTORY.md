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

```csharp
public class GameBootstrapper : MonoBehaviour, ICoroutineRunner
{
    [SerializeField] private LoadingCurtain _curtain;
    private Game _game;

    private void Awake()
    {
        _game = new Game(this, _curtain);
        DontDestroyOnLoad(this);
        _game.StateMachine.Enter<BootstrapState>();
    }
}
```

`_curtain` — серіалізована залежність, перетягнута в інспекторі Unity (компонент на
дочірньому GameObject `Image` під `Curtain`). Три рядки `Awake()` — це вся головна лінія
виконання уроку. Перший рядок створює `Game`, передаючи себе (`this`) як `ICoroutineRunner`.

**→ Занурюємось у `Game`** (`Infrastructure/Game.cs`) — кореневий не-`MonoBehaviour`
об'єкт гри, навмисно "тупий", без ігрової логіки:

```csharp
public class Game
{
    public GameStateMachine StateMachine { get; }

    public Game(ICoroutineRunner coroutineRunner, LoadingCurtain curtain)
    {
        SceneLoader sceneLoader = new SceneLoader(coroutineRunner);
        StateMachine = new GameStateMachine(sceneLoader, curtain);
    }
}
```

Конструктор `Game` створює дві залежності по черзі. Перша — `SceneLoader`.

**→ Занурюємось у `SceneLoader`** (`Infrastructure/SceneLoader.cs`) — відповідає рівно за
одне: асинхронно завантажити сцену Unity і повідомити колбеком, коли готово; не знає
нічого про стани, гру чи UI:

```csharp
public class SceneLoader
{
    private readonly ICoroutineRunner _coroutineRunner;
    public SceneLoader(ICoroutineRunner coroutineRunner) => _coroutineRunner = coroutineRunner;

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
```

`SceneLoader` сам залежить лише від `ICoroutineRunner`.

**→ Занурюємось у `ICoroutineRunner`** (`Infrastructure/ICoroutineRunner.cs`):

```csharp
public interface ICoroutineRunner { Coroutine StartCoroutine(IEnumerator coroutine); }
```

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

```csharp
public class GameStateMachine
{
    private readonly Dictionary<Type, IExitableState> _states;
    private IExitableState _currentState;

    public GameStateMachine(SceneLoader sceneLoader, LoadingCurtain loadingCurtain)
    {
        _states = new Dictionary<Type, IExitableState>();
        _states.Add(typeof(BootstrapState), new BootstrapState(this, sceneLoader));
        _states.Add(typeof(LoadLevelState), new LoadLevelState(this, sceneLoader, loadingCurtain));
        _states.Add(typeof(GameLoopState), new GameLoopState(this));
    }
    // Enter<TState>() / Enter<TState, TPayload>() — див. нижче, після виконавців
}
```

`sceneLoader` тут — уже знайомий об'єкт (той самий, щойно розглянутий вище, просто
прокинутий далі), повторно не занурюємось. Але тип значень словника,
`IExitableState`, і три стани, які тут одразу створюються, — нові. Спочатку контракти,
якими типізований словник.

**→ Занурюємось у контракти станів** (`IExitableState`/`IState`/`IPayloadedState<TPayload>`
— `Infrastructure/States/IState.cs` та поруч):

```csharp
public interface IExitableState { void Exit(); }
public interface IState : IExitableState { void Enter(); }
public interface IPayloadedState<TPayload> : IExitableState { void Enter(TPayload payload); }
```

`IExitableState` — найвужчий контракт, тільки `Exit()`; саме ним типізоване поле
`_currentState` у `GameStateMachine`, бо це єдине, що гарантовано є в усіх станах
одночасно. `IState` — для станів без вхідних даних. `IPayloadedState<TPayload>` — для
станів, яким потрібен параметр при вході. Залежностей у самих контрактів немає.

**← Повертаємось до `GameStateMachine`.** Конструктор по черзі створює три виконавці —
проходимо кожного.

**→ Занурюємось у `BootstrapState`** (`Infrastructure/States/BootstrapState.cs`) — перша
фаза гри:

```csharp
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
        _stateMachine.Enter<LoadLevelState, string>(SceneName);
    }

    public void Exit() { /* TODO: сюди в уроках 02-03 переїде RegisterServices() */ }
}
```

Обидві залежності конструктора — `GameStateMachine` (той самий, що його й створив) і
`SceneLoader` — уже знайомі з лінії вище, нового занурення не треба (`sceneLoader` тут
навіть не використовується всередині — прийнятий тільки для однорідності сигнатури з
іншими станами, знадобиться в уроках 02-03 для `RegisterServices()`). `Enter()` одразу
запускає перехід у `LoadLevelState` з payload — назвою сцени.

**← Повертаємось до `GameStateMachine`.** Другий виконавець — `LoadLevelState`.

**→ Занурюємось у `LoadLevelState`** (`Infrastructure/States/LoadLevelState.cs`) — показати
завісу, завантажити сцену, сховати завісу, перейти далі:

```csharp
public class LoadLevelState : IPayloadedState<string>
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

    private void onLoaded() => _stateMachine.Enter<GameLoopState>();

    public void Exit() => _loadingCurtain.Hide();
}
```

Перші дві залежності конструктора (`GameStateMachine`, `SceneLoader`) уже знайомі. Третя —
`LoadingCurtain` — нова.

**→ Занурюємось у `LoadingCurtain`** (`Logic/LoadingCurtain.cs`) — візуальна "завіса", що
ховає від гравця сам процес завантаження сцени, найдрібніший клас уроку:

```csharp
public class LoadingCurtain : MonoBehaviour
{
    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}
```

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

```csharp
public class GameLoopState : IState
{
    public GameLoopState(GameStateMachine parent) { }
    public void Enter() => Debug.Log($"[FSM] Enter {GetType().Name}");
    public void Exit() { }
}
```

Єдина залежність — `GameStateMachine`, уже знайомий, поки не використовується всередині
(той самий принцип однорідної сигнатури, що й у `BootstrapState`).

**← Повертаємось до `GameStateMachine`.** Усі три стани зареєстровано в словнику, конструктор
завершено. Лишились самі методи перемикання:

```csharp
public void Enter<TState>() where TState : class, IState
{
    _currentState?.Exit();
    TState newState = _states[typeof(TState)] as TState;
    _currentState = newState;
    newState?.Enter();
}

public void Enter<TState, TPayload>(TPayload payload) where TState : class, IPayloadedState<TPayload>
{
    _currentState?.Exit();
    TState newState = _states[typeof(TState)] as TState;
    _currentState = newState;
    newState?.Enter(payload);
}
```

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

```csharp
public class Game
{
    public GameStateMachine StateMachine {get;}

    public Game(ICoroutineRunner coroutineRunner, LoadingCurtain curtain)
    {
        SceneLoader sceneLoader = new SceneLoader(coroutineRunner);
        StateMachine = new GameStateMachine(sceneLoader, curtain, AllServices.Instance);
    }
}
```

Перший рядок конструктора (`SceneLoader`) — уже знайомий з уроку 01, повторно не
занурюємось. Другий рядок тепер передає в `GameStateMachine` третій аргумент —
`AllServices.Instance`. Це перше звернення до нового класу цього уроку.

**→ Занурюємось у `AllServices`** (`Infrastructure/Services/AllServices.cs`) — Service
Locator, центральний реєстр сервісів гри:

```csharp
public class AllServices
{
    private readonly Dictionary<Type, IService> _services;

    public static AllServices Instance { get; } = new AllServices();

    private AllServices()
    {
        _services = new Dictionary<Type, IService>();
    }

    public void RegisterService<TService>(TService service) where TService : class, IService
    {
        _services.Add(typeof(TService), service);
        Debug.Log($"[Services] Registered service of type {typeof(TService).Name}");
    }

    public TService GetService<TService>() where TService : class, IService
    {
        var service = _services[typeof(TService)] as TService;
        return service;
    }
}
```

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

```csharp
public interface IService { }
```

Жодного методу — сам факт реалізації цього інтерфейсу і є вся інформація, яку він несе:
"цей клас можна класти в `AllServices`". Без нього `RegisterService<TService>` довелось
би або приймати `object` (втрата типобезпеки), або обмежувати generic-параметр чимось
конкретним (втрата гнучкості для різних сервісів). Залежностей немає.

**← Повертаємось до `Game`.** Конструктор `Game` завершено: `StateMachine` зібраний,
переданий `AllServices.Instance` разом із уже знайомими `sceneLoader`/`curtain`.

**← Повертаємось до `GameBootstrapper.Awake()`**, тепер уже в `GameStateMachine` —

**→ Занурюємось у `GameStateMachine`** (`Infrastructure/States/GameStateMachine.cs`) —
сигнатура конструктора й тіло змінились відносно уроку 01:

```csharp
public GameStateMachine(SceneLoader sceneLoader, LoadingCurtain loadingCurtain, AllServices services)
{
    _states = new Dictionary<Type, IExitableState>();
    _states.Add(typeof(BootstrapState), new BootstrapState(this, services));
    _states.Add(typeof(LoadLevelState), new LoadLevelState(this, sceneLoader, loadingCurtain));
    IAssetProvider assetProvider = services.GetService<IAssetProvider>();
    _states.Add(typeof(GameLoopState), new GameLoopState(assetProvider));
    Debug.Log($"[FSM] {GetType().Name} created {_states[typeof(BootstrapState)].GetType().Name} " +
              $"{_states[typeof(LoadLevelState)].GetType().Name} " +
              $"{_states[typeof(GameLoopState)].GetType().Name} states");
}
```

`sceneLoader`/`loadingCurtain` — уже знайомі, просто прокинуті далі в `LoadLevelState`
(код `LoadLevelState` не змінився з уроку 01, не занурюємось повторно). Новий параметр
— `services`, той самий `AllServices.Instance`, щойно розглянутий вище. Перший рядок
створює `BootstrapState`, передаючи йому `this` (сам `GameStateMachine`) і `services`.

**→ Занурюємось у `BootstrapState`** (`Infrastructure/States/BootstrapState.cs`) —
конструктор і тіло цього уроку:

```csharp
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
        _stateMachine.Enter<LoadLevelState, string>(SceneName);
    }

    private void RegisterServices()
    {
        AssetProvider assetProvider = new AssetProvider();
        Debug.Log($"[FSM] {GetType().Name} initiated registration of a new service {assetProvider.GetType().Name}");
        _services.RegisterService<IAssetProvider>(assetProvider);
    }

    public void Exit() { /* TODO */ }
}
```

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

```csharp
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
        GameObject asset = Resources.Load<GameObject>(assetPath);
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
```

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

```csharp
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
```

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
