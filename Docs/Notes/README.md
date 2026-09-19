# Конспекти

Довідник тих C#/Unity понять, з якими студент був мало знайомий, коли вони вперше
трапились у роботі над проєктом — не архітектурні патерни (для цього `Docs/PATTERNS.md`) і
не як побудований сам проєкт (для цього `Docs/HISTORY.md`), а базові мовні/платформні
речі: generics, делегати, конкретні API Unity тощо. Кожна тема — окремий файл, написаний
розгорнуто, із прикладами з реального коду цього проєкту там, де це можливо.

**Джерело тем:** реальні питання, які студент ставив у чаті (цієї чи попередніх сесій) —
**не** `Docs/CV_LOG.md` (це ширший лог навичок для резюме, включно з речами з інших
репозиторіїв, не список питань). Щоб знайти питання з минулих сесій, які самі по собі не
зберігаються дослівно ніде постійно, дивись git-історію `Docs/SESSION_NOTES.md`
(`git log --oneline -- Docs/SESSION_NOTES.md`, потім `git show <hash>:Docs/SESSION_NOTES.md`)
— там короткий підсумок кожної сесії, включно з C#-механіками, на яких застрягав студент.

**Коли додається новий файл:** тільки коли студент сам явно попросить нотатку саме на цю
тему — не автоматично щойно механіка згадана в чаті (сама механіка все одно пояснюється в
чаті одразу, без очікування кінця уроку — файл лише не заводиться без прямого прохання).

## Теми

- [`Interfaces.md`](Interfaces.md) — навіщо інтерфейси, на прикладі
  `IExitableState`/`IState`/`IPayloadedState<TPayload>`.
- [`Generics.md`](Generics.md) — узагальнені типи/методи, кілька типових параметрів,
  `where`-обмеження (і поширена помилка з комою), на прикладі
  `GameStateMachine.Enter<TState>()`.
- [`Downcasting_and_as.md`](Downcasting_and_as.md) — upcast/downcast, оператор `as` проти
  прямого касту, на прикладі `_states[typeof(TState)] as TState`.
- [`Delegates_Events_and_Subscriptions.md`](Delegates_Events_and_Subscriptions.md) —
  делегати (`Action`/`Action<T>`/`Func`), method group conversion, `event`,
  підписка/відписка (`+=`/`-=`), подія з параметром, кілька подій на одному об'єкті
  (`started`/`performed`/`canceled`), інші форми подій (`EventHandler`, `UnityEvent`);
  на прикладах `onLoaded` (урок 01) і `IInputService` (урок 03).
- [`Debug_Logging_and_Reflection.md`](Debug_Logging_and_Reflection.md) — `Debug.Log`,
  string interpolation, `GetType().Name`, читання стек-трейсів у Console.
- [`Coroutines_and_Scene_Loading.md`](Coroutines_and_Scene_Loading.md) — `IEnumerator`,
  `yield return null`, `SceneManager.LoadSceneAsync`/`AsyncOperation`.
- [`DontDestroyOnLoad.md`](DontDestroyOnLoad.md) — межі сцен, пастка з дочірньою
  ієрархією (реальний баг із `Curtain` в уроці 01).
- [`Resources_Load_and_Instantiate.md`](Resources_Load_and_Instantiate.md) —
  `Resources.Load<T>` + `Object.Instantiate`, на прикладі `AssetProvider` в уроці 02.
- [`Field_and_Variable_Shadowing.md`](Field_and_Variable_Shadowing.md) — локальна змінна з
  тим самим ім'ям, що й поле класу, ховає поле; реальний баг `BootstrapState.RegisterServices()`
  в уроці 02.
- [`Input_System.md`](Input_System.md) — новий Input System: Action Map/Action/Binding/
  Composite, `Button` vs `Value`, `Generate C# Class`, `.Enable()`, poll (`ReadValue<T>()`)
  vs event (`.performed`/`CallbackContext`), на прикладі `Input Actions` asset'у уроку 03.
- [`Reflection_Basics.md`](Reflection_Basics.md) — довідник за задачами (не
  інструкція до конкретного ДЗ): для кожної задачі — "дізнатись конструктори
  типу", "створити об'єкт через конструктор", "викликати метод на готовому
  об'єкті", "закрити generic-метод/тип конкретним типом", "перевірити
  `where`-обмеження" — окремий розділ із мінімальним ізольованим прикладом і
  **реальним, запущеним виводом консолі**. Виникло з опційного ДЗ уроку 03
  "constructor-only auto-resolver", але саме рішення ДЗ у файлі не описане.
- [`Input_System_Types_Reference.md`](Input_System_Types_Reference.md) — довідник за **типами
  даних** нового Input System: ланцюг `InputActions` → `PlayerActions` → `InputAction`,
  `ReadValue<T>()`/`IsPressed()`/`WasPressedThisFrame()`, події `started`/`performed`/
  `canceled` і коли кожна спрацьовує, поля `CallbackContext`, інтерфейс `IPlayerActions`,
  що лежить у `Vector2` від `Move` (W = `(0, 1)`), що віддає `IInputService`; наприкінці —
  порожня таблиця для самостійного співставлення з `Movement_Approaches.md`.
- [`Movement_Approaches.md`](Movement_Approaches.md) — довідник за задачами: рух через
  `transform` / `CharacterController` / `Rigidbody`, `ForceMode`, кінематичне тіло, колізії й
  тригери в кожному підході, порівняльна таблиця, правило вибору і типові пастки (дубль
  `CapsuleCollider`, телепорт при увімкненому контролері, `Time.deltaTime`, `isGrounded`).
  Виникло з питання "що таке `CharacterController`" в уроці 04; готового рішення уроку
  чи ДЗ в файлі немає.
- [`InputAction_Events_and_CallbackContext.md`](InputAction_Events_and_CallbackContext.md) —
  поглиблено: `InputAction` без обгортки, `.started`/`.performed`/`.canceled` на різних
  реальних прикладах (проста кнопка, аналогове значення, кілька біндингів, `Hold`
  interaction), вкладений тип `CallbackContext`, коли він обов'язковий, коли підписку
  на `InputAction`-подію треба відписувати, а коли ні.
