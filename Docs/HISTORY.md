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
