# Input System (`UnityEngine.InputSystem`) — довідник за задачами

Об'єднує три колишні конспекти: `Input_System.md` (концепції), `Input_System_Types_Reference.md`
(що приймає й віддає кожен виклик) і `InputAction_Events_and_CallbackContext.md` (події й
`CallbackContext` детально).

Уперше трапилось: урок 03, `Input Actions` asset + `IInputService`/`InputService` — новий
(package-based) Input System замість старого `Input.GetAxis`/`Input.GetKey`.

Пакет `com.unity.inputsystem`, версія проєкту — **1.14.2**. Факти про дії проєкту (`Move`,
`Jump`, `Fire`) взято з реального `Assets/Input/InputActions.inputactions` і згенерованого
`InputActions.cs`. Виводу консолі немає: це API рушія, поза Unity воно не запускається;
поведінка описана за документацією пакета.

## Мапа задач

| Хочу | API | Повертає / приймає | Розділ |
|---|---|---|---|
| Зрозуміти, з чого складається asset | Action Map → Action → Binding / Composite | — | 2 |
| Отримати типізований доступ до дій з коду | `Generate C# Class` → `new InputActions()` | `InputActions` → `PlayerActions` → `InputAction` | 3 |
| Увімкнути читання дій мапи | `Player.Enable()` / `action.Enable()` | `void` | 4 |
| Поточний напрямок руху | `action.ReadValue<Vector2>()` | `Vector2` | 5 |
| Чи затиснута кнопка зараз | `action.IsPressed()` | `bool` | 5 |
| Чи натиснули / відпустили саме в цьому кадрі | `WasPressedThisFrame()` / `WasReleasedThisFrame()` | `bool` | 5 |
| Числове значення кнопки (0..1) | `action.ReadValue<float>()` | `float` | 5 |
| Відреагувати одразу, коли щось сталось | `action.performed += handler` | `Action<InputAction.CallbackContext>` | 6, 7 |
| Розрізнити "почав тиснути" / "утримав" / "відпустив зарано" | `Interaction` (`hold`) + `started`/`performed`/`canceled` | — | 6 |
| Значення в момент події | `context.ReadValue<T>()` | `T` | 8 |
| Яка саме кнопка/вісь спрацювала | `context.control` | `InputControl` | 8 |
| Відписатися правильно | `-=` тим самим іменованим методом | — | 9 |
| Отримувати всі події через інтерфейс | `Player.AddCallbacks(IPlayerActions)` | `OnMove/OnJump/OnFire(CallbackContext)` | 10 |
| Створити дію без asset'у (для тесту) | `new InputAction(...)`, `AddBinding(...)` | `InputAction` | 11 |
| У якій фазі дія | `action.phase` / `context.phase` | `InputActionPhase` | 12 |
| Що бачить решта гри | `IInputService` | `Vector2`, `event Action` | 13 |

---

## 1. Навіщо новий Input System, а не старий `Input.GetKey`

Старий (`UnityEngine.Input`) читає конкретні клавіші напряму в коді: `Input.GetKey(KeyCode.W)`.
Прив'язка "яка клавіша відповідає за рух вперед" зашита в C#-код — щоб додати геймпад чи
перебіндити клавішу, треба міняти код.

Новий Input System розділяє це на два шари:
1. **Дані** (`.inputactions` asset) — які дії існують ("Move", "Jump") і якими фізичними
   кнопками/осями вони викликаються. Редагується в Input Actions Editor, без коду.
2. **Код** — питає лише "яке зараз значення дії Move", не знаючи, яка конкретно
   клавіша/стік це викликала.

Тому в проєкті лише один клас (`InputService`) імпортує `UnityEngine.InputSystem`
напряму — решта гри залежить від `IInputService` (SRP + DIP, той самий принцип, що й
`AssetProvider`/`Resources.Load` в
[`Resources_Load_and_Instantiate.md`](../Unity_Basics/Resources_Load_and_Instantiate.md)).

## 2. Asset: Action Map, Action, Binding

Input System — окремий package, не частина ядра Unity. Дані живуть у файлі-асеті
(`Assets/Input/InputActions.inputactions`), який редагується у своєму вікні (подвійний
клік по файлу).

```
InputActionAsset ("InputActions")
└── Action Map ("Player")           — іменована група дій одного "режиму"
    ├── Action ("Move")             — дія, незалежна від конкретної клавіші
    │   ├── Composite Binding (тип 2D Vector)
    │   │   ├── Up    → W [Keyboard]
    │   │   ├── Down  → S [Keyboard]
    │   │   ├── Left  → A [Keyboard]
    │   │   └── Right → D [Keyboard]
    │   └── Binding → Left Stick [Gamepad]   (окремо, без композиту)
    ├── Action ("Jump")
    │   └── Binding → Space [Keyboard]
    └── Action ("Fire")
        └── Binding → Left Button [Mouse]
```

- **Action Map** — групування дій за "режимом гравця" (`Player` під час гри, окремий `UI`
  під час меню — у проєкті поки лише `Player`).
- **Action** — сама дія гравця (`Move`, `Jump`), відв'язана від конкретної кнопки.
- **Binding** — одна конкретна фізична кнопка/вісь, прив'язана до дії.
- **Composite Binding** — кілька Binding, об'єднаних в одне значення (WASD → один
  `Vector2`). Стік геймпада вже сам видає `Vector2`, тому `Left Stick` іде окремим простим
  Binding поруч із композитом, не всередину нього.
- **Interaction** (необов'язково) — правило "коли саме вважати дію виконаною": за
  замовчуванням немає ("натиснуто" = "відбулось"), або, наприклад, `Hold` (утримати N
  секунд). Див. розділ 6.

### `Action Type`: `Button` vs `Value`

- **`Button`** — дискретна подія: натиснуто / не натиснуто (внутрішньо `float 0`/`1`).
  Для `Jump`, `Fire` — разових дій.
- **`Value`** — безперервне значення, яке має сенс опитувати будь-якої миті: `float`,
  `Vector2`, `Vector3`... Для `Move` — потрібен поточний напрямок, а не факт "щось натиснуто".
  Разом із ним обирається **`Control Type`** — конкретний тип значення (`Vector2` для `Move`).

### Дії нашого проєкту

| Дія | `Action Type` | Біндинги | Тип значення в коді |
|---|---|---|---|
| `Move` | `Value` (`Vector2`) | композит `2DVector`: W/A/S/D + `<Gamepad>/leftStick` | `Vector2` |
| `Jump` | `Button` | `<Keyboard>/space` | `float` 0/1 (або `bool` через `IsPressed()`) |
| `Fire` | `Button` | `<Mouse>/leftButton` | `float` 0/1 |

**Що лежить у `Vector2` від `Move`:**

- Вісь **x**: `D` → +1, `A` → −1. Вісь **y**: `W` → +1, `S` → −1. Тобто `W` = `(0, 1)`,
  `D` = `(1, 0)`.
- Нічого не натиснуто → `(0, 0)`.
- Діагональ (`W`+`D`) за замовчуванням нормалізується: довжина 1, кожна компонента
  ≈ `0.707`, а не `(1, 1)`.
- Стік геймпада: компоненти від −1 до 1, довжина не більша за 1, з мертвою зоною біля центру.

## 3. `Generate C# Class` — ланцюг типів від файла до значення

Чекбокс в Inspector'і самого asset'у. Без нього довелось би шукати дії за рядковими
іменами (`asset.FindActionMap("Player").FindAction("Move")`) — одруківка в рядку
виявиться лише в рантаймі. З ним Unity генерує клас-обгортку (ім'я/простір імен/шлях —
поля там же, в Inspector'і) із типізованою властивістю на кожну мапу й дію.

```
Input Actions asset (Assets/Input/InputActions.inputactions)
        │  "Generate C# Class"
        ▼
class InputActions : IInputActionCollection2, IDisposable     ← new InputActions()
        │  властивість на мапу
        ▼
struct PlayerActions                                          ← inputActions.Player
        │  властивість на дію
        ▼
InputAction                                                   ← .Move  .Jump  .Fire
        │  ReadValue<T>() / події started, performed, canceled
        ▼
T (наприклад Vector2)  /  InputAction.CallbackContext
```

- `InputActions` — згенерований клас. Конструктор без параметрів.
- `PlayerActions` — `struct` із властивістю на кожну дію плюс `Enable()`/`Disable()` для
  всієї мапи і `AddCallbacks(...)`.
- `InputAction` — сама дія; саме в неї є `ReadValue<T>()` і події. `_inputActions.Player.Jump`
  — це просто вже готовий, попередньо сконфігурований `InputAction` (той самий тип можна
  створити й вручну — розділ 11).

Той самий ланцюг у коді, кожен крок окремою змінною з явним типом:

```csharp
InputActions actions = new InputActions();            // 1. об'єкт згенерованого класу
InputActions.PlayerActions player = actions.Player;   // 2. мапа "Player"
InputAction move = player.Move;                       // 3. дія "Move"
InputAction jump = player.Jump;                       // 3. дія "Jump"

player.Enable();                                      // 4. увімкнути всю мапу — без цього нічого не читається

Vector2 direction = move.ReadValue<Vector2>();        // 5. опитати значення (poll), розділ 5
jump.performed += OnJumpPerformed;                    // 6. підписатися на подію (event), розділи 6–7
```

| Рядок | Що тут відбувається |
|---|---|
| 1 | `InputActions` — клас, який Unity згенерувала з asset'у (`Assets/Input/InputActions.cs`). `new` створює його об'єкт: усередині він завантажує всі мапи й дії |
| 2 | `.Player` — властивість, що повертає мапу `Player`. Її тип — `PlayerActions`, але він оголошений **всередині** класу `InputActions` (вкладений тип), тому ззовні пишеться повним ім'ям `InputActions.PlayerActions` — так само як `InputAction.CallbackContext` (розділ 8) |
| 3 | `.Move` / `.Jump` — властивості мапи, кожна повертає звичайний `InputAction` — одну дію |
| 4 | `Enable()` на мапі вмикає всі її дії разом (розділ 4) |
| 5 | у дії питаємо поточне значення. `Move` налаштована як `Vector2`, тому й читаємо `Vector2` |
| 6 | на дію підписуємо метод `void OnJumpPerformed(InputAction.CallbackContext context)` |

У проєкті (`InputService`) проміжних змінних немає — ланцюг пишеться одним виразом, але
це ті самі кроки:

```csharp
_inputActions = new InputActions();                                       // крок 1
_inputActions.Player.Enable();                                            // кроки 2 + 4
_inputActions.Player.Jump.performed += ctx => OnJumpPressed?.Invoke();    // кроки 2 + 3 + 6
Vector2 moveDirection = _inputActions.Player.Move.ReadValue<Vector2>();   // кроки 2 + 3 + 5
```

## 4. `Enable()` / `Disable()` — найпоширеніша перша пастка

| Виклик | Що робить | Повертає |
|---|---|---|
| `Player.Enable()` | вмикає **всі** дії мапи `Player` | `void` |
| `Player.Disable()` | вимикає всі дії мапи | `void` |
| `action.Enable()` / `action.Disable()` | одна дія | `void` |
| `action.enabled` | чи увімкнена | `bool` |

Поки не викликано `Enable()`, `ReadValue<T>()` мовчки повертає нуль (`Vector2.zero`,
`0f`), а події не спрацьовують — **без винятку й без помилки в консолі**.

**Для майбутньої паузи:** мапи незалежні одна від одної. `_inputActions.Player.Disable()`
вимикає лише `Player`; якщо є ще мапа (наприклад, `Menu` для навігації по паузі
клавіатурою/геймпадом), її вмикають окремо (`_inputActions.Menu.Enable()`). Якщо пауза
керується лише мишкою через UI-кнопки (`Button.onClick`), окрема мапа не потрібна — клік
обробляє `EventSystem`, незалежно від стану будь-якої Action Map.

## 5. Poll — опитати значення, коли тобі треба

Питаєш "яке значення зараз", коли самому потрібно (найчастіше в `Update`). Підходить для
`Move` — рух перевіряється щокадру.

| Задача | API | Тип | Примітка |
|---|---|---|---|
| Поточне значення | `ReadValue<TValue>()` | `TValue` | `TValue` має збігатись з типом дії: `Move` — `Vector2`, кнопка — `float` |
| Кнопка зараз затиснута | `IsPressed()` | `bool` | |
| Натиснули в цьому кадрі | `WasPressedThisFrame()` | `bool` | `true` рівно один кадр |
| Відпустили в цьому кадрі | `WasReleasedThisFrame()` | `bool` | |
| Дія "виконалась" у цьому кадрі | `WasPerformedThisFrame()` | `bool` | |
| Ім'я дії | `name` | `string` | `"Move"`, `"Jump"` |
| Тип дії | `type` | `InputActionType` | розділ 12 |
| Поточна фаза | `phase` | `InputActionPhase` | розділ 12 |

**Правило типів:** `ReadValue<T>()` з не тим `T` кидає `InvalidOperationException` у
рантаймі. Одноосьовий контрол / кнопка → `float`; стік / композит з 4 кнопок → `Vector2`.
`ReadValue<bool>()` для кнопки не підходить — бери `IsPressed()`.

## 6. Events — `started` / `performed` / `canceled`

Усі три — `event Action<InputAction.CallbackContext>`. Unity сама викликає їх, коли щось
відбувається; тобі не треба щокадру перевіряти "чи натиснута кнопка". Підходить для
дискретних дій (`Jump`, `Fire`). Той самий механізм C#-подій, що й `Action onLoaded` у
`SceneLoader` ([`Delegates_Events_and_Subscriptions.md`](../CSharp/Delegates_Events_and_Subscriptions.md)),
тільки подію генерує Unity, а не проєкт.

- `started` — момент початку взаємодії (кнопку почали тиснути).
- `performed` — дія фактично відбулась (найчастіше потрібна саме ця).
- `canceled` — взаємодію перервано (кнопку відпустили).

**Коли спрацьовує кожна (без `Interaction`):**

| Дія | `started` | `performed` | `canceled` |
|---|---|---|---|
| `Button` (`Jump`, `Fire`) | у момент натискання | у момент натискання (одразу після `started`) | у момент відпускання |
| `Value` (`Move`) | коли значення вперше відхилилось від нуля | **щоразу**, коли значення змінюється | коли значення повернулось до нуля |

Наслідок: `Jump.performed` — один виклик на натискання. `Move.performed` — багато
викликів, тому рух зручніше опитувати `ReadValue<Vector2>()` (розділ 5), а не слухати.

### З `Interaction` фази стають справді різними моментами

```csharp
_reloadAction = new InputAction(
    "Reload",
    type: InputActionType.Button,
    binding: "<Keyboard>/r",
    interactions: "hold(duration=1)");   // тримай R секунду

_reloadAction.started += context => Debug.Log("Reload: почав тиснути R");
_reloadAction.performed += context => Debug.Log("Reload: утримав секунду — перезарядка!");
_reloadAction.canceled += context => Debug.Log("Reload: відпустив R ЗАРАНО — скасовано");
_reloadAction.Enable();
```

- Натиснув `R` і відпустив раніше ніж за секунду → "почав", потім "скасовано".
  **`performed` не спрацює.**
- Тримав ≥ 1 с → "почав" одразу, через секунду "перезарядка!". `canceled` не спрацює.

У `Jump` (без `Interaction`) `started` і `performed` спрацьовують в одну мить натискання.
Саме `Interaction` визначає, коли дія вважається "виконаною".

### Хто викликає `Invoke()` для цих подій

У нашому коді немає рядка `performed.Invoke(...)` — і це не пропуск. У будь-якого `event`
рівно один "власник", який має право його викликати (принцип "видавця газети" з
[`Delegates_Events_and_Subscriptions.md`](../CSharp/Delegates_Events_and_Subscriptions.md)).
Для `OnJumpPressed` власник — наш `InputService`, тому `?.Invoke()` пишемо ми. Для
`performed`/`started`/`canceled` власник — клас `InputAction` усередині пакета (код Unity).
Наш код завжди лише з боку `+=`. Спрощена ілюстрація принципу (не реальний код Unity):

```csharp
public class InputAction
{
    public event Action<CallbackContext> performed;

    // Unity сама викликає це щокадру, читаючи стан заліза:
    private void ProcessCurrentValue()
    {
        if (/* поріг Button пройдено, або Interaction каже "виконано" */)
            performed?.Invoke(new CallbackContext(...));
    }
}
```

### Коли в кадрі приходять колбеки

За замовчуванням (Input System → Update Mode: **Process Events In Dynamic Update**) події
вводу обробляються раз на кадр, **перед `Update`**, у головному потоці. Тобто колбеки
приходять у ритмі кадрів, а не в ритмі фізики (`FixedUpdate`). `ReadValue<T>()` у
будь-якому методі повертає останнє оброблене значення.

## 7. Підписка — сигнатура обробника і лямбди

Обробник — метод (чи лямбда), що **приймає один `InputAction.CallbackContext` і повертає
`void`**:

```csharp
void Handler(InputAction.CallbackContext context) { }

_inputActions.Player.Jump.performed += Handler;       // підписка
_inputActions.Player.Jump.performed -= Handler;       // відписка
```

### Дві різні події в `InputService`: чужа `performed` і своя `OnJumpPressed`

Увесь клас (реальний код, `Assets/CodeBase/Infrastructure/Input/InputService.cs`):

```csharp
public class InputService : IInputService
{
    private readonly InputActions _inputActions;

    public event Action OnJumpPressed;                                        // (А) НАША подія

    public InputService()
    {
        _inputActions = new InputActions();
        _inputActions.Player.Enable();
        _inputActions.Player.Jump.performed += ctx => OnJumpPressed?.Invoke(); // (Б) міст
    }

    public Vector2 GetDirection()
    {
        Vector2 moveDirection = _inputActions.Player.Move.ReadValue<Vector2>();
        return moveDirection;
    }
}
```

| Подія | Чия | Тип | Хто викликає | Хто слухає |
|---|---|---|---|---|
| `_inputActions.Player.Jump.performed` | Unity (пакет Input System) | `event Action<InputAction.CallbackContext>` | рушій, коли натиснули Space | лише `InputService` |
| `OnJumpPressed` | **наша**, оголошена в рядку (А) і в `IInputService` | `event Action` (без параметрів) | `InputService`, рядок (Б) | `PlayerController` і будь-хто, хто отримав `IInputService` |

Рядок (Б) — **міст** між ними: "коли Unity скаже `performed` — я скажу `OnJumpPressed`".
Ланцюг при натисканні Space:

```
Space → Unity: Jump.performed → лямбда з (Б) → OnJumpPressed?.Invoke() → PlayerController.Jump()
```

Навіщо дві події, а не одразу `performed` у `PlayerController`: тоді `PlayerController`
мусив би знати про `InputActions` і `InputAction.CallbackContext`, тобто залежати від
Unity Input System напряму. А так він знає лише `IInputService` з простою подією
`Action` — і не зміниться, якщо ввід прийде з геймпада, AI чи тесту (DIP).

Рядок (Б) по частинах:

- `_inputActions` — поле типу `InputActions` (згенерований клас).
- `.Player.Jump` — конкретний `InputAction`.
- `.performed` — подія Unity типу `Action<InputAction.CallbackContext>`.
- `ctx => OnJumpPressed?.Invoke()` — лямбда (анонімний метод) з одним параметром `ctx`
  типу `InputAction.CallbackContext`; сам `ctx` не використовується, тіло просто викликає
  нашу подію (А). `?.` — "виклич, лише якщо хтось підписаний (не `null`)".

**Ті самі рядки (Б), записані по-різному** — усі чотири роблять одне й те саме, і в усіх
`OnJumpPressed` — це наша подія (А):

```csharp
// 1 — як у проєкті: параметр з іменем ctx, не використовується
_inputActions.Player.Jump.performed += ctx => OnJumpPressed?.Invoke();

// 2 — параметр "_" (discard): явний сигнал "параметр не потрібен"
_inputActions.Player.Jump.performed += _ => OnJumpPressed?.Invoke();

// 3 — з явним типом параметра (зайве, тип і так виводиться)
_inputActions.Player.Jump.performed += (InputAction.CallbackContext ctx) => OnJumpPressed?.Invoke();

// 4 — іменований метод замість лямбди (метод — член того ж класу InputService)
_inputActions.Player.Jump.performed += OnJumpPerformed;

private void OnJumpPerformed(InputAction.CallbackContext ctx)
{
    OnJumpPressed?.Invoke();
}
```

### `Action<InputAction.CallbackContext>` по частинах

```
Action    <    InputAction.CallbackContext    >
  │            │
  │            └─ типовий аргумент: один конкретний тип,
  │               яким параметризовано Action<T>
  │
  └─ generic-делегат з .NET (System.Action<T>):
     "посилання на метод, що приймає ОДИН параметр
     типу T і нічого не повертає (void)"

InputAction   .   CallbackContext
    │              │
    │              └─ вкладений тип (struct), оголошений усередині InputAction
    │
    └─ зовнішній клас, що представляє одну гравецьку дію
```

Метод `OnJumpPerformed(InputAction.CallbackContext ctx)` з форми 4 збігається з цим
один-в-один (один параметр того самого типу, `void`), тому компілюється при `+=`.

**`OnJumpPressed?.Invoke()`** — не новий метод, а виклик готового: кожен делегат має
вбудований `Invoke()`, який генерує компілятор
([`Delegates_Events_and_Subscriptions.md`](../CSharp/Delegates_Events_and_Subscriptions.md) §4).
Оголошений тут лише сам обробник (лямбда чи `OnJumpPerformed`).

**Що не скомпілюється:**

```csharp
// ПОМИЛКА КОМПІЛЯЦІЇ — подія вимагає рівно один параметр, а тут нуль:
// "Delegate 'Action<InputAction.CallbackContext>' does not take 0 arguments"
_inputActions.Player.Jump.performed += () => OnJumpPressed?.Invoke();
```

`_` — не ключове слово, а ідентифікатор, який компілятор (з C# 9) розпізнає як
"discard": IDE не підсвічує його як невикористаний параметр, на відміну від `ctx`.

## 8. `InputAction.CallbackContext` — що в ньому є і коли він потрібен

`CallbackContext` — `struct`, оголошений **всередині** класу `InputAction` (вкладений тип:
існує лише в контексті дії). Той самий прийом в ізольованому вигляді:

```csharp
public class Order
{
    public struct Item   // вкладений тип
    {
        public string Name;
        public int Price;
    }
}

Order.Item item = new Order.Item { Name = "Меч", Price = 100 };
```

Це "знімок" саме цього одного спрацювання:

| Що дістати | Член | Тип |
|---|---|---|
| Значення в момент події | `ReadValue<TValue>()` | `TValue` (правило типів — розділ 5) |
| Значення кнопки як булеве | `ReadValueAsButton()` | `bool` |
| Значення без відомого типу | `ReadValueAsObject()` | `object` |
| Яка дія | `action` | `InputAction` |
| Яка фаза | `phase` | `InputActionPhase` |
| Який контрол спрацював | `control` | `InputControl` (`.name`, `.path`, `.displayName` — рядки) |
| Час події (секунди від старту) | `time` | `double` |
| Час початку взаємодії | `startTime` | `double` |
| Тривалість від початку до події | `duration` | `double` |
| Фаза як булеві прапорці | `started`, `performed`, `canceled` | `bool` |

### Коли `context` не потрібен, а коли обов'язковий

**Проста кнопка — можна ігнорувати** (як `Jump` у `InputService`): сам факт "клацнули" —
вся інформація.

```csharp
_fireAction = new InputAction("Fire", binding: "<Mouse>/leftButton");
_fireAction.performed += _ => Shoot();
_fireAction.Enable();
```

**Аналогове значення — обов'язковий:** `performed` приходить щоразу з іншим числом, і без
`context.ReadValue<float>()` обробник його не має звідки взяти.

```csharp
_zoomAction = new InputAction("Zoom", type: InputActionType.Value, binding: "<Mouse>/scroll/y");
_zoomAction.performed += context =>
{
    float scrollDelta = context.ReadValue<float>();
    Debug.Log($"Прокрутка: {scrollDelta}");
};
_zoomAction.Enable();
```

**Кілька біндингів на одну дію — обов'язковий,** якщо треба знати, яка саме кнопка:

```csharp
_fireAction = new InputAction("Fire");
_fireAction.AddBinding("<Keyboard>/space");
_fireAction.AddBinding("<Mouse>/leftButton");
_fireAction.performed += context => Debug.Log($"Fire triggered by: {context.control.displayName}");
_fireAction.Enable();
```

| Дія | Тип | Interaction | Чи потрібен `context` | Навіщо |
|---|---|---|---|---|
| `Fire`/`Jump` | Button | немає | ні (`_`) | сам факт натискання — вся інформація |
| `Zoom` | Value | немає | так, `ReadValue<float>()` | подія без значення безглузда |
| `Fire` (кілька біндингів) | Button | немає | так, `context.control` | треба знати, яка саме кнопка |
| `Reload` | Button | `hold` | не обов'язково, але фази реально різні | без Interaction всі три фази злиті в одну |

## 9. Відписка від подій `InputAction`

Загальний принцип — у
[`Delegates_Events_and_Subscriptions.md`](../CSharp/Delegates_Events_and_Subscriptions.md) §6.
Застосування саме до `InputAction`:

- **`InputService` (реальний проєкт):** підписка в конструкторі, який виконується раз за
  все життя гри. Підписник (`InputService`) і джерело (`_inputActions`, його ж поле)
  живуть і помирають разом — відписуватись нема коли. Тому інлайн-лямбда там нормальна.
- **`MonoBehaviour`, що слухає дію** (тестові класи нижче, або будь-який компонент на
  об'єкті, який можуть знищити раніше за кінець гри): потрібна явна відписка `-=` в
  `OnDestroy()`.
- **Чому тоді іменований метод, а не лямбда:** для `-=` потрібне посилання на **той самий**
  делегат, яким підписувались. Інлайн-лямбда щоразу створює **новий** об'єкт-делегат —
  навіть з однаковим текстом `-=` стару підписку не знайде. Іменований метод — те саме
  посилання і при `+=`, і при `-=`.

## 10. Через інтерфейс `IPlayerActions` (згенерований)

Альтернатива підписці на кожну подію окремо:

```csharp
public interface IPlayerActions
{
    void OnMove(InputAction.CallbackContext context);
    void OnJump(InputAction.CallbackContext context);
    void OnFire(InputAction.CallbackContext context);
}
```

- `Player.AddCallbacks(instance)` (і `SetCallbacks`) підписує `OnMove` на **всі три** події
  `Move` (`started`, `performed`, `canceled`), так само `OnJump`/`OnFire`. Тому всередині
  треба перевіряти `context.phase` (або `context.performed`), інакше реакція піде на кожну фазу.

## 11. `InputAction` без asset'у — для швидкого тесту

Той самий тип можна створити вручну, без `.inputactions` і генерації:

- Конструктор: `new InputAction(string name = null, InputActionType type = ..., string binding = null, string interactions = null)` —
  усі параметри опційні; `binding` — шлях контролу (`"<Keyboard>/space"`), `interactions` —
  правило (`"hold(duration=1)"`).
- `AddBinding(string path)` — ще один фізичний контрол для тієї самої дії.

Повний приклад (усі варіанти з розділів 6 і 8 разом, з правильною відпискою з розділу 9):

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponInputTest : MonoBehaviour
{
    private InputAction _fireAction;
    private InputAction _zoomAction;
    private InputAction _reloadAction;

    private void Awake()
    {
        _fireAction = new InputAction("Fire", binding: "<Mouse>/leftButton");
        _fireAction.performed += OnFirePerformed;

        _zoomAction = new InputAction("Zoom", type: InputActionType.Value, binding: "<Mouse>/scroll/y");
        _zoomAction.performed += OnZoomPerformed;

        _reloadAction = new InputAction("Reload", binding: "<Keyboard>/r", interactions: "hold(duration=1)");
        _reloadAction.started += OnReloadStarted;
        _reloadAction.performed += OnReloadPerformed;
        _reloadAction.canceled += OnReloadCanceled;

        _fireAction.Enable();
        _zoomAction.Enable();
        _reloadAction.Enable();
    }

    private void OnFirePerformed(InputAction.CallbackContext context) => Debug.Log("BANG!");

    private void OnZoomPerformed(InputAction.CallbackContext context)
        => Debug.Log($"Zoom: {context.ReadValue<float>()}");

    private void OnReloadStarted(InputAction.CallbackContext context) => Debug.Log("Reload: почав тиснути R");
    private void OnReloadPerformed(InputAction.CallbackContext context) => Debug.Log("Reload: перезарядка!");
    private void OnReloadCanceled(InputAction.CallbackContext context) => Debug.Log("Reload: скасовано, відпустив зарано");

    private void OnDestroy()
    {
        _fireAction.performed -= OnFirePerformed;
        _zoomAction.performed -= OnZoomPerformed;
        _reloadAction.started -= OnReloadStarted;
        _reloadAction.performed -= OnReloadPerformed;
        _reloadAction.canceled -= OnReloadCanceled;
    }
}
```

## 12. Enum-и

- `InputActionType` (тип дії в `.inputactions`): `Value`, `Button`, `PassThrough`.
- `InputActionPhase` (стан дії): `Disabled`, `Waiting`, `Started`, `Performed`, `Canceled`.

## 13. Що віддає наш `IInputService`

Решта гри бачить ввід лише через інтерфейс:

```csharp
public interface IInputService : IService
{
    event Action OnJumpPressed;   // порожній Action, без CallbackContext
    Vector2 GetDirection();       // = _inputActions.Player.Move.ReadValue<Vector2>()
}
```

- `GetDirection()` → `Vector2` (розділ 2: `W` = `(0, 1)`).
- `OnJumpPressed` → `Action` **без параметрів**: `CallbackContext` лишається всередині
  `InputService`; споживачі його не бачать.

---

## 14. Заготовка для твого співставлення (заповни сам)

Ліві колонки — що дає ввід, середні — що приймають API руху з
[`Movement_Approaches.md`](../Physics_and_Movement/Movement_Approaches.md). Остання колонка —
твоя: який крок перетворення потрібен між ними (порожня навмисно).

| Що дає ввід | Тип | Що приймає API руху | Тип | Перетворення (заповни сам) |
|---|---|---|---|---|
| `GetDirection()` | `Vector2` | `CharacterController.Move(...)` | `Vector3` (зсув за кадр) | |
| `GetDirection()` | `Vector2` | `CharacterController.SimpleMove(...)` | `Vector3` (швидкість, y ігнорується) | |
| `GetDirection()` | `Vector2` | `Rigidbody.AddForce(..., ForceMode)` | `Vector3`, `ForceMode` | |
| `OnJumpPressed` | `Action` (без параметрів) | що прикласти для стрибка | залежить від підходу (Movement_Approaches, розділи 2 і 3) | |
| — | — | `CharacterController.isGrounded` | `bool` | |

Показуй заповнену таблицю — я перевірю, а не підкажу наперед.

## Пов'язане

- [`Delegates_Events_and_Subscriptions.md`](../CSharp/Delegates_Events_and_Subscriptions.md) —
  механіка `event`/`Action`/лямбд і загальний принцип "коли потрібна відписка".
- [`Movement_Approaches.md`](../Physics_and_Movement/Movement_Approaches.md) — API руху.
- [`Resources_Load_and_Instantiate.md`](../Unity_Basics/Resources_Load_and_Instantiate.md) —
  інший приклад Unity API за власною абстракцією.
