# Урок 01 — Composition Root і FSM стану гри

Мета: один вхід у гру (`GameBootstrapper`) і явна FSM, яка веде від запуску до ігрового
циклу: `BootstrapState → LoadLevelState → GameLoopState`. Геймплею ще немає — це чистий
скелет, яким користуватимуться всі наступні уроки.

Патерни: Composition Root і FSM — розділи 4.1 і 4.3 `ARCHITECTURE_REFERENCE.md`, а також
`Docs/ROADMAP.md` §3 (пункти 1 і 3).

---

## Що потрібно створити

Усе — чисті C#-класи в `Assets/CodeBase/Infrastructure/`, крім двох навмисних винятків:
`GameBootstrapper` (єдиний MonoBehaviour-вхід) і `LoadingCurtain` (другий MonoBehaviour —
візуальний індикатор завантаження).

### 1. Контракти стану — `Infrastructure/States/IState.cs`

Три інтерфейси (можна в одному файлі — вони завжди йдуть разом):

```
IExitableState { void Exit(); }
IState : IExitableState { void Enter(); }
IPayloadedState<TPayload> : IExitableState { void Enter(TPayload payload); }
```

**Чому три, а не один `IState` з опціональним payload?** Interface Segregation Principle:
стан без параметра (`GameLoopState`) не повинен бути змушений реалізовувати
`Enter(TPayload payload)`, який йому не потрібен і нічим не наповнюється.

### 2. `Infrastructure/States/GameStateMachine.cs`

Чистий C#-клас (**не** MonoBehaviour). Відповідальність — тримати всі стани як
`Dictionary<Type, IExitableState>`, створені один раз у конструкторі, і перемикати активний.

Конструктор: `GameStateMachine(SceneLoader sceneLoader, LoadingCurtain loadingCurtain)` —
усередині створює всі стани (`new BootstrapState(this, sceneLoader)`,
`new LoadLevelState(this, sceneLoader, loadingCurtain)`, `new GameLoopState(this)`) і кладе
у словник за `typeof(...)`.

Методи:
- `void Enter<TState>() where TState : class, IState` — викликає `Exit()` на поточному
  активному стані (якщо він є), дістає `TState` зі словника, робить його активним, викликає
  `Enter()`.
- `void Enter<TState, TPayload>(TPayload payload) where TState : class, IPayloadedState<TPayload>`
  — те саме, тільки викликає `Enter(payload)`.

**Чому стани створюються одразу в конструкторі, а не через `new BootstrapState()` щоразу
при вході?** Стан живе, поки жива гра (по одному екземпляру кожного типу), і його залежності
(які він отримує через конструктор) не повинні перестворюватись при кожному переході.

### 3. `Infrastructure/ICoroutineRunner.cs`

```
ICoroutineRunner { Coroutine StartCoroutine(IEnumerator routine); }
```

**Навіщо, якщо є `MonoBehaviour.StartCoroutine`?** `SceneLoader` (нижче) — чистий C#-клас, а
корутини Unity вміє запускати тільки `MonoBehaviour`. Цей інтерфейс — тонка абстракція, яка
дозволяє `SceneLoader` попросити "будь-кого, хто вміє запускати корутини" зробити це, не
знаючи, що це саме `GameBootstrapper`.

### 4. `Infrastructure/SceneLoader.cs`

Чистий C#-клас, конструктор приймає `ICoroutineRunner`. Метод
`void Load(string sceneName, Action onLoaded = null)` запускає корутину, яка:
- якщо активна сцена вже має цю назву — одразу викликає `onLoaded` і виходить (не варто
  вантажити те, що вже завантажено);
- інакше робить `SceneManager.LoadSceneAsync(sceneName)`, чекає, поки `isDone`, тоді
  викликає `onLoaded`.

### 5. `Infrastructure/Game.cs`

Кореневий (не MonoBehaviour) об'єкт гри. Тримає `GameStateMachine StateMachine`.
Конструктор: `Game(ICoroutineRunner coroutineRunner, LoadingCurtain curtain)` — усередині
створює `new SceneLoader(coroutineRunner)` і передає його разом із `curtain` у
`new GameStateMachine(...)`.

**Навмисно нічого не додавай сюди про сервіси чи інпут.** `AllServices`/`IAssetProvider`
з'являться в уроці 02, `IInputService` — в уроці 03. У цьому уроці `Game` відповідає рівно
за одне: тримати FSM живою.

### 6. `Infrastructure/GameBootstrapper.cs` — єдиний MonoBehaviour-вхід

Лежить на GameObject у сцені `Bootstrap`. Реалізує `ICoroutineRunner` (у `MonoBehaviour` вже
є метод `StartCoroutine` з потрібною сигнатурою — просто додай `: ICoroutineRunner` до
оголошення класу, компілятор сам перевірить відповідність).

Публічне серіалізоване поле `LoadingCurtain Curtain` (перетягнеш об'єкт в інспекторі —
дивись пункт 9).

`Awake()`:
1. `_game = new Game(this, Curtain)`.
2. `DontDestroyOnLoad(gameObject)` — без цього об'єкт (і жива FSM усередині) помре при
   переході на `Level_Arena`.
3. `_game.StateMachine.Enter<BootstrapState>()`.

**Чому саме `Awake`, а не `Start`?** `Awake` викликається одразу при завантаженні сцени, до
того як спрацює будь-який `Start()` на інших об'єктах — для composition root важливо, щоб
ініціалізація гри відбулась першою, до всього іншого.

### 7. `Infrastructure/States/BootstrapState.cs`

`BootstrapState : IState`. Конструктор: `(GameStateMachine stateMachine, SceneLoader sceneLoader)`.

`Enter()`: одразу переходить у `LoadLevelState` з payload — назвою сцени `"Level_Arena"`
(зроби це окремим `const string`, не magic string посеред коду).
`Exit()`: поки порожній тілом (`{}`) — і це нормально, не кожен стан має що прибирати.

Залиш тут `// TODO` — саме сюди в уроках 02-03 переїде `RegisterServices()` перед переходом
у `LoadLevelState`.

### 8. `Infrastructure/States/LoadLevelState.cs`

`LoadLevelState : IPayloadedState<string>`. Конструктор:
`(GameStateMachine stateMachine, SceneLoader sceneLoader, LoadingCurtain curtain)`.

- `Enter(string sceneName)`: `curtain.Show()`, потім `sceneLoader.Load(sceneName, onLoaded)`.
- приватний `onLoaded()`: просто `stateMachine.Enter<GameLoopState>()`.
- `Exit()`: `curtain.Hide()`.

Зверни увагу: ховає завісу саме `Exit()`, а не `onLoaded` напряму — тому що `Exit()`
викликається автоматично самою `GameStateMachine` рівно в момент переходу на наступний
стан, і це говорить само за себе без зайвого виклику в середині `onLoaded`.

**Навмисно нічого не спавниться тут ще** — ні гравець, ні HUD. Це з'явиться в уроках 04
(Construct-патерн для Player) і 10-11 (фабрики, UI). Зараз задача — довести, що перехід між
сценами через FSM працює сам по собі.

### 9. `Logic/LoadingCurtain.cs` — другий і останній навмисний MonoBehaviour

Найпростіша версія: повноекранний чорний `Image` на `Canvas` у сцені `Bootstrap`, дочірній
до об'єкта з `GameBootstrapper` (тоді `DontDestroyOnLoad(gameObject)` з пункту 6 збереже й
його теж). Методи `void Show()` / `void Hide()` — досить просто `gameObject.SetActive(...)`.
Плавний fade — не зараз, це polish для уроку 14.

### 10. `Infrastructure/States/GameLoopState.cs`

`GameLoopState : IState`. Конструктор `(GameStateMachine stateMachine)`. `Enter()`/`Exit()`
— обидва порожні. Це точка входу в геймплей, почне наповнюватись з уроку 04.

---

## Тимчасова діагностика (не архітектурний хак)

Оскільки геймплею ще немає, а FSM невидима, додай по одному
`Debug.Log($"[FSM] Enter {GetType().Name}")` на початок кожного `Enter()`. Це тимчасова
діагностика, а не архітектурне рішення — прибереш чи заміниш на щось корисне, коли стани
почнуть щось реально робити.

Очікуваний порядок у Console при натисканні Play в сцені `Bootstrap`:
1. `Enter BootstrapState`
2. `Enter LoadLevelState` (одразу, ще до видимого перемикання сцени — `BootstrapState.Enter`
   викликає перехід синхронно)
3. видиме перемикання сцени `Bootstrap → Level_Arena` (завіса `LoadingCurtain` в цей момент
   показана)
4. `Enter GameLoopState`, і тиша — все відпрацювало без помилок

---

## Перевірка

- [ ] Проєкт компілюється без помилок.
- [ ] У сцені `Bootstrap` є рівно один GameObject з `GameBootstrapper`, поле `Curtain`
      заповнене в інспекторі (не `None`).
- [ ] У сцені `Level_Arena` **немає** жодного `GameBootstrapper` — вона тільки приймає
      керування, сама нічого не ініціалізує.
- [ ] Play саме в сцені `Bootstrap` (не в `Level_Arena`!) дає послідовність логів з розділу
      вище, без винятків/помилок у Console.
- [ ] Жодного `FindObjectOfType`, жодного публічного статичного поля з посиланням на
      сервіс чи стан (навіть тимчасового) — усе, що потрібне класу, приходить через
      конструктор або `Construct()`.

Коли все компілюється і перехід виглядає саме так — пиши "готово", і перейдемо до уроку 02
(`IService`, `AllServices`, перший сервіс — `IAssetProvider`).
