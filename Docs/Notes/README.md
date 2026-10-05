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

## Структура тек

Кожна тека — глобальна тема. Новий конспект кладеться в теку своєї теми; нова тека
заводиться лише для справді нової глобальної теми (наприклад, `Math/`, `Animation/`, `UI/`,
коли до них дійде курс), не під один файл "про всяк випадок".

| Тека | Що туди йде |
|---|---|
| `CSharp/` | мова C# і .NET без Unity: типи, generics, делегати/події, reflection, касти |
| `Unity_Basics/` | базове API рушія: життєвий цикл, сцени, корутини, ресурси, логування |
| `Input/` | ввід: Input System, дії, події вводу |
| `Physics_and_Movement/` | рух, фізика, кінематика, колізії |
| `Tools/` | інструменти поза кодом гри: git, IDE, профайлер |

## Теми

### `CSharp/`

- [`Interfaces.md`](CSharp/Interfaces.md) — навіщо інтерфейси, на прикладі
  `IExitableState`/`IState`/`IPayloadedState<TPayload>`.
- [`Generics.md`](CSharp/Generics.md) — узагальнені типи/методи, кілька типових параметрів,
  `where`-обмеження (і поширена помилка з комою), на прикладі
  `GameStateMachine.Enter<TState>()`.
- [`Downcasting_and_as.md`](CSharp/Downcasting_and_as.md) — upcast/downcast, оператор `as`
  проти прямого касту, на прикладі `_states[typeof(TState)] as TState`.
- [`Delegates_Events_and_Subscriptions.md`](CSharp/Delegates_Events_and_Subscriptions.md) —
  делегати (`Action`/`Action<T>`/`Func`), method group conversion, `event`,
  підписка/відписка (`+=`/`-=`), подія з параметром, кілька подій на одному об'єкті
  (`started`/`performed`/`canceled`), інші форми подій (`EventHandler`, `UnityEvent`);
  на прикладах `onLoaded` (урок 01) і `IInputService` (урок 03).
- [`Field_and_Variable_Shadowing.md`](CSharp/Field_and_Variable_Shadowing.md) — локальна
  змінна з тим самим ім'ям, що й поле класу, ховає поле; реальний баг
  `BootstrapState.RegisterServices()` в уроці 02.
- [`Reflection_Basics.md`](CSharp/Reflection_Basics.md) — довідник за задачами: розділ 0
  про всі форми `[]` (тип масиву, `new T[n]`, ініціалізатори, `new[]`, індекс, `params`,
  що не компілюється в C# 9, ``Box`1[[…]]`` у виводі); далі "дізнатись
  конструктори типу", "створити об'єкт через конструктор", "викликати метод",
  "закрити generic-метод/тип конкретним типом", "перевірити `where`-обмеження",
  "обрати з кількох конструкторів найжадібніший розв'язний" (+ його ціна й пастки) — з
  **реальним, запущеним виводом консолі**. Виникло з опційного ДЗ уроку 03, саме рішення
  ДЗ у файлі не описане.

### `Unity_Basics/`

- [`Debug_Logging_and_Reflection.md`](Unity_Basics/Debug_Logging_and_Reflection.md) —
  `Debug.Log`, string interpolation, `GetType().Name`, читання стек-трейсів у Console.
- [`Coroutines_and_Scene_Loading.md`](Unity_Basics/Coroutines_and_Scene_Loading.md) —
  `IEnumerator`, `yield return null`, `SceneManager.LoadSceneAsync`/`AsyncOperation`.
- [`DontDestroyOnLoad.md`](Unity_Basics/DontDestroyOnLoad.md) — межі сцен, пастка з
  дочірньою ієрархією (реальний баг із `Curtain` в уроці 01).
- [`Resources_Load_and_Instantiate.md`](Unity_Basics/Resources_Load_and_Instantiate.md) —
  `Resources.Load<T>` + `Object.Instantiate`, на прикладі `AssetProvider` в уроці 02.

### `Input/`

- [`Input_System.md`](Input/Input_System.md) — увесь новий Input System в одному довіднику
  за задачами (об'єднано з колишніх `Input_System.md`, `Input_System_Types_Reference.md`,
  `InputAction_Events_and_CallbackContext.md`): asset (Action Map/Action/Binding/Composite,
  `Button` vs `Value`, дії проєкту і що лежить у `Vector2` від `Move`), `Generate C# Class`
  і ланцюг типів, `Enable`/`Disable`, poll (`ReadValue<T>()`, `IsPressed()`...) vs event
  (`started`/`performed`/`canceled`, `Interaction` `hold`, хто викликає `Invoke`, коли в
  кадрі приходять колбеки), розбір підписки лямбдою, `CallbackContext` і коли він
  потрібен, відписка, `IPlayerActions`, `InputAction` без asset'у, що віддає
  `IInputService`; наприкінці — порожня таблиця для самостійного співставлення з
  `Movement_Approaches.md`.

### `Physics_and_Movement/`

- [`Movement_Approaches.md`](Physics_and_Movement/Movement_Approaches.md) — довідник за
  задачами: рух через `transform` / `CharacterController` / `Rigidbody`, `ForceMode`,
  кінематичне тіло, колізії й тригери в кожному підході, порівняльна таблиця, правило
  вибору і типові пастки (дубль `CapsuleCollider`, телепорт при увімкненому контролері,
  `Time.deltaTime`, `isGrounded`). Готового рішення уроку чи ДЗ в файлі немає.
- [`Kinematics_For_Jump.md`](Physics_and_Movement/Kinematics_For_Jump.md) — шкільна
  кінематика для стрибка: як читати формули, `v = v0 − g·t`, `y = v0·t − g·t²/2`, висота
  стрибка, `v0 = √(2·g·h)`, одиниці, кадри й `dt`, таблиця «формула → рядок коду», чому
  покадровий код трохи не збігається з формулою, типові помилки; складніші варіації 3.9–3.17:
  швидкість на висоті, платформа/зіскок, довжина стрибка, стрибок із висоти й довжини, різна
  гравітація вгору/вниз, відпускання кнопки, кут, точний крок за кадр, опір повітря.

### `Tools/`

- [`Git_Commands.md`](Tools/Git_Commands.md) — довідник git за задачами: як читати команду
  (`-`/`--`/окреме `--`/`:`/`HEAD~1`/`@{N}`/`<...>`), три місця змін (диск / індекс /
  історія), `status`/`log`/`reflog`/`diff`/`show`/`switch`/`restore`/`add`/`commit`/`push`/
  `cherry-pick`/`commit --amend` + `push --force-with-lease` з реальним виводом цього
  репозиторію; наприкінці — **журнал команд, реально застосованих у проєкті**
  (поповнюється з кожною новою командою).
