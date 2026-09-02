# Урок 01 — Composition Root і FSM стану гри

Мета: один вхід у гру (`GameBootstrapper`) і явна FSM, яка веде від запуску до ігрового
циклу: `BootstrapState → LoadLevelState → GameLoopState`. Геймплею ще немає — це чистий
скелет, яким користуватимуться всі наступні уроки.

Патерни: Composition Root і FSM — розділи 4.1 і 4.3 `ARCHITECTURE_REFERENCE.md`, а також
`Docs/ROADMAP.md` §3 (пункти 1 і 3).

**Composition Root** — відповідь на питання "хто створює всі об'єкти гри і зв'язує їх
залежності?". Без нього кожен клас сам шукав би собі залежності (`FindObjectOfType`,
статичні синглтони) — граф залежностей розмазаний по коду, і його неможливо ні побачити
одним поглядом, ні протестувати. Рішення — один явний вхід (`GameBootstrapper`), де весь
граф збирається руками через конструктори. Це Dependency Injection вручну, без
DI-контейнера.

**FSM** — відповідь на "як описати послідовність фаз гри (завантаження → рівень →
геймплей), не плодячи `if (isLoading) ... else if (isPlaying) ...` по всьому коду?". Кожна
фаза — окремий об'єкт зі своєю `Enter()`/`Exit()`, перемикання між ними централізоване в
одному місці.

**Словничок термінів** (звідки взялись ці слова, не архітектура — просто англійська):

- **Bootstrap** — з виразу "pull yourself up by your own bootstraps" (витягнути себе за
  шнурки власних чобіт, тобто почати з нуля без сторонньої допомоги). У computer science —
  `bootstrapping`: комп'ютер не може завантажити ОС без програми, а програма лежить на
  диску, який ще нема чим прочитати — розв'язок: крихітна початкова програма, яка "витягує
  сама себе", завантажуючи щось більше. `GameBootstrapper` — та сама точка: єдиний клас,
  який створює все інше з нуля, коли більше нічого ще не існує.
- **Payload** — з логістики/авіації: вантаж ділиться на "накладні витрати" (паливо,
  упаковка) і **pay**-load — ту частину, заради якої взагалі летять (супутник у ракеті,
  наприклад — буквально "вантаж, за який платять"). У коді — "корисні дані, заради яких
  відбувається виклик", на відміну від службової обгортки. `IPayloadedState<TPayload>` —
  "стан, у який входять із вантажем"; для `LoadLevelState` цей вантаж — назва сцени.
- **Curtain** — буквально "театральна завіса". Метафора з театру: завісу закривають між
  актами, щоб глядач не бачив зміну декорацій. `LoadingCurtain` ховає від гравця сам процес
  завантаження сцени — `Show()` "закриває завісу", `Hide()` відкриває, коли все готово.

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

**Патерн:** контракти для учасників State pattern (GoF). **Принцип:** тут же прихований і
LSP — `GameStateMachine.Enter<TState>()` працює з будь-яким `IState`, а
`Enter<TState,TPayload>()` — з будь-яким `IPayloadedState<TPayload>`, і жодна конкретна
реалізація не може "здивувати" викликача — контракт однаковий для всіх.

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

**Патерн:** `GameStateMachine` — це "Context" у класичному State pattern: об'єкт, що тримає
посилання на поточний активний стан і делегує йому поведінку, замість того щоб сам
перевіряти прапорці. **Принцип:** SRP — єдина відповідальність: знати, який стан активний, і
коректно переключати (`Exit()` старого → `Enter()` нового); що саме відбувається всередині
`LoadLevelState` — не її справа. Тут же DIP: словник типізований як
`Dictionary<Type, IExitableState>` — машина працює з абстракцією, а не з конкретними класами
станів, тож завтрашній `PauseState` додасться без жодної правки в `GameStateMachine`.

Розберемо GameStateMachine покроково — це найскладніший клас уроку, варто зупинитись детальніше.

Чому саме Dictionary<Type, IExitableState>, а не окремі поля

Уяви альтернативу — окремі приватні поля BootstrapState _bootstrap, LoadLevelState _loadLevel, GameLoopState _gameLoop, і метод Enter<TState>() через switch/if по типу TState, що вручну підставляє потрібне поле. Це працює для трьох станів, але:
- кожен новий стан = правка вже написаного switch (порушення OCP);
- код перемикання дублюється для кожного типу.

Словник вирішує обидві проблеми: Enter<TState>() пише один раз, і він працює для будь-якої кількості станів, зареєстрованих у словнику — включно з тими, яких ще навіть не існує (майбутній PauseState).

Чому ключ — саме Type (typeof(BootstrapState)), а не enum StateId.Bootstrap? Бо тоді generic-метод Enter<TState>() може сам обчислити ключ як typeof(TState) — виклику stateMachine.Enter<BootstrapState>() не потрібен окремий enum-параметр, компілятор і так знає, що TState = BootstrapState. З enum довелося б писати Enter(StateId.Bootstrap) — на один рівень непрямоти більше, і легко забути додати новий enum-кейс.

Чому значення типізовані як IExitableState, а не одразу конкретний тип? Бо в словнику одночасно лежать і BootstrapState/GameLoopState (реалізують IState), і LoadLevelState (реалізує IPayloadedState<string>) — спільний знаменник між ними, той єдиний метод, який є в обох, це Exit() з IExitableState. Словник з одним типом значення просто не може зберігати різнорідні типи інакше, ніж через їхнього спільного предка.

Покроковий розбір Enter<TState>()

Виклик виглядає як stateMachine.Enter<BootstrapState>(). Усередині відбувається послідовно:

1. Перевірка, чи є активний стан. Перший виклик у GameBootstrapper.Awake() — активного стану ще немає (null), тож пропускаємо. Але на другому й наступних викликах (наприклад, коли BootstrapState.Enter() сам викликає перехід у LoadLevelState) активний стан є — і саме тут викликається .Exit() на ньому. Зверни увагу: Exit() доступний через IExitableState — саме тому тип поля активного стану саме IExitableState, а не object: не треба нічого кастити, щоб викликати Exit().
2. Дістати потрібний стан зі словника за ключем typeof(TState). Результат має тип IExitableState (бо це тип значення словника) — але щоб викликати Enter() (який є тільки в IState), треба привести цей результат до TState. Ось де generic-параметр і constraint працюють разом.
3. Generic-обмеження where TState : class, IState. Це обіцянка компілятору: "яким би конкретним типом не був TState, він точно реалізує IState, тобто має Enter()". Тому можна безпечно скастити значення зі словника (IExitableState) до TState і викликати .Enter() на результаті — без цього обмеження компілятор не дозволив би виклик .Enter(), бо не знав би, що такий метод у TState взагалі існує.
4. Присвоїти новий стан активним і викликати Enter() на ньому (вже приведеному до TState).

Другий метод — Enter<TState, TPayload>(TPayload payload)

Той самий алгоритм, тільки:
- constraint інший: where TState : class, IPayloadedState<TPayload> — обіцянка, що TState має Enter(TPayload), а не
  Enter();
- на кроці 4 викликається Enter(payload) замість Enter().

Чому не один метод з TPayload = default? Тоді довелося б викликати Enter<GameLoopState, object>(null) — безглуздий payload для стану, якому він не потрібен (саме те, від чого рятує ISP в інтерфейсах), або городити nullable-магію. Два окремі методи з різними constraint'ами — компілятор сам не дасть викликати неправильний метод для неправильного типу стану: спробуй Enter<GameLoopState, string>(...) — не скомпілюється, бо GameLoopState не реалізує IPayloadedState<string>. Це помилка виявляється на етапі компіляції, а не в рантаймі — і це головна причина, чому вся ця generic-механіка того варта.

На що звернути увагу, коли писатимеш сам

- Активний стан як поле повинен мати тип саме IExitableState (не object, не конкретний клас).
- Дістаючи значення зі словника, приведення до TState необхідне явно — компілятор не зробить це сам, бо статично знає лише про IExitableState.
- Порядок дій критичний: спочатку Exit() старого, потім Enter() нового (інакше новий стан може побачити сліди/побічні ефекти старого, який ще не встиг прибратись).

Спробуй написати цей клас з цим у голові — якщо десь compiler підкаже, що каста бракує чи типи не сходяться, це і буде ознакою, що саме тут constraint або приведення пропущені.

### 3. `Infrastructure/ICoroutineRunner.cs`

```
ICoroutineRunner { Coroutine StartCoroutine(IEnumerator routine); }
```

**Навіщо, якщо є `MonoBehaviour.StartCoroutine`?** `SceneLoader` (нижче) — чистий C#-клас, а
корутини Unity вміє запускати тільки `MonoBehaviour`. Цей інтерфейс — тонка абстракція, яка
дозволяє `SceneLoader` попросити "будь-кого, хто вміє запускати корутини" зробити це, не
знаючи, що це саме `GameBootstrapper`.

**Це найчистіший приклад DIP (Dependency Inversion) в уроці.** `SceneLoader` — логіка
високого рівня (оркеструє завантаження сцени), а `MonoBehaviour.StartCoroutine` — деталь
низького рівня, специфічна для Unity. Без цього інтерфейсу `SceneLoader` довелось би або
самому бути `MonoBehaviour` (зайва прив'язка до GameObject), або лізти в статичний
`GameBootstrapper.Instance` — Service Locator/Singleton-антипатерн, якого курс свідомо
уникає. Замість цього і `SceneLoader`, і `GameBootstrapper` залежать від абстракції
`ICoroutineRunner`, а не одне від одного.

### 4. `Infrastructure/SceneLoader.cs`

Чистий C#-клас, конструктор приймає `ICoroutineRunner`. Метод
`void Load(string sceneName, Action onLoaded = null)` запускає корутину, яка:
- якщо активна сцена вже має цю назву — одразу викликає `onLoaded` і виходить (не варто
  вантажити те, що вже завантажено);
- інакше робить `SceneManager.LoadSceneAsync(sceneName)`, чекає, поки `isDone`, тоді
  викликає `onLoaded`.

**Принцип:** SRP — уміє рівно одне: завантажити сцену асинхронно і повідомити, коли готово.
Не знає ні про стани, ні про завісу, ні про гру взагалі.

### 5. `Infrastructure/Game.cs`

Кореневий (не MonoBehaviour) об'єкт гри. Тримає `GameStateMachine StateMachine`.
Конструктор: `Game(ICoroutineRunner coroutineRunner, LoadingCurtain curtain)` — усередині
створює `new SceneLoader(coroutineRunner)` і передає його разом із `curtain` у
`new GameStateMachine(...)`.

**Навмисно нічого не додавай сюди про сервіси чи інпут.** `AllServices`/`IAssetProvider`
з'являться в уроці 02, `IInputService` — в уроці 03. У цьому уроці `Game` відповідає рівно
за одне: тримати FSM живою.

**Патерн:** корінь графу залежностей (частина Composition Root) — не-Unity об'єкт, що тримає
`GameStateMachine` живою. **Принцип:** знову SRP — рівно одна відповідальність: зібрати
`SceneLoader` + `GameStateMachine` і тримати їх живими, поки живе гра. Навмисно "тупий" — не
росте функціоналом, бо його роль — бути коренем, а не робочою логікою.

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
ініціалізація гри відбулась першою, до всього іншого. (Це про порядок ініціалізації Unity, а
не про SOLID.)

**Патерн:** це і є фізичний Composition Root — єдина точка, де Unity-світ
(GameObject/MonoBehaviour) торкається чистого C#-світу (`Game`, `GameStateMachine`, стани).
**Принцип:** SRP на рівні "хто відповідає за старт" — рівно один клас має право сказати "гра
почалась". Реалізація `ICoroutineRunner` тут — це DIP у дії: конкретний `MonoBehaviour`
підставляється туди, де очікується абстракція.

### 7. `Infrastructure/States/BootstrapState.cs`

`BootstrapState : IState`. Конструктор: `(GameStateMachine stateMachine, SceneLoader sceneLoader)`.

`Enter()`: одразу переходить у `LoadLevelState` з payload — назвою сцени `"Level_Arena"`
(зроби це окремим `const string`, не magic string посеред коду).
`Exit()`: поки порожній тілом (`{}`) — і це нормально, не кожен стан має що прибирати.

Залиш тут `// TODO` — саме сюди в уроках 02-03 переїде `RegisterServices()` перед переходом
у `LoadLevelState`.

**Принцип:** тут головний — OCP (Open/Closed). Ідея FSM саме в тому, що додавання нової фази
гри (наприклад, `PauseState` чи `GameOverState` пізніше) = новий клас + рядок реєстрації в
`GameStateMachine`, і жоден з існуючих станів при цьому не редагується. `BootstrapState`
відповідає (SRP) рівно за одне: "стартуй завантаження першого рівня".

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

**Принцип:** SRP — `LoadLevelState` відповідає рівно за одне: "покажи завісу, завантаж
сцену, сховай завісу".

### 9. `Logic/LoadingCurtain.cs` — другий і останній навмисний MonoBehaviour

Найпростіша версія: повноекранний чорний `Image` на `Canvas` у сцені `Bootstrap`, дочірній
до об'єкта з `GameBootstrapper` (тоді `DontDestroyOnLoad(gameObject)` з пункту 6 збереже й
його теж). Методи `void Show()` / `void Hide()` — досить просто `gameObject.SetActive(...)`.
Плавний fade — не зараз, це polish для уроку 14.

Другий (і навмисно останній) `MonoBehaviour`, бо це чисто візуальна річ (`Image` на
`Canvas`) — не можна відірвати від GameObject. Стани викликають `Show()`/`Hide()` через
звичайний публічний метод, а не через `FindObjectOfType` — тобто його теж інжектять через
конструктор (`LoadLevelState(..., curtain)`), а не шукають самі. Це той самий DIP, тільки
застосований до UI-об'єкта.

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

## SOLID — підсумок по уроку

- **S**RP — кожен клас має рівно одну причину змінюватись (`GameStateMachine` ≠
  `SceneLoader` ≠ конкретний стан).
- **O**CP — нові стани (`PauseState`, `GameOverState`...) додаються без правок існуючих
  класів.
- **L**SP — будь-який `IExitableState`/`IState`/`IPayloadedState<T>` можна підставити, не
  ламаючи `GameStateMachine`.
- **I**SP — три вузькі інтерфейси стану замість одного товстого.
- **D**IP — `SceneLoader`, `GameStateMachine`, `GameBootstrapper` залежать від абстракцій
  (`ICoroutineRunner`, `IExitableState`), а не від конкретних класів одне одного.

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
