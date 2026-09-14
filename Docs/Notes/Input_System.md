# Input System (`UnityEngine.InputSystem`)

Уперше трапилось: урок 03, `Input Actions` asset + `IInputService`/`InputService` — новий
(package-based) Input System замість старого `Input.GetAxis`/`Input.GetKey`.

## Навіщо новий Input System, а не старий `Input.GetKey`

Старий (`UnityEngine.Input`) читає конкретні клавіші напряму в коді: `Input.GetKey(KeyCode.W)`.
Це означає, що сама прив'язка "яка клавіша відповідає за рух вперед" зашита в C#-код —
щоб додати геймпад чи перебіндити клавішу, треба міняти код.

Новий Input System розділяє це на два шари:
1. **Дані** (`.inputactions` asset) — які дії існують ("Move", "Jump") і якими фізичними
   кнопками/осями вони викликаються. Редагується в Input Actions Editor, без коду.
2. **Код** — питає лише "яке зараз значення дії Move", не знаючи й не переймаючись, яка
   конкретно клавіша/стік це викликала.

Це та сама причина, чому в проєкті лише один клас (`InputService`) має право імпортувати
`UnityEngine.InputSystem` напряму — решта гри залежить від `IInputService`, не від деталі
"як саме читається ввід" (SRP + DIP, той самий принцип, що й `AssetProvider`/`Resources.Load`
в [`Resources_Load_and_Instantiate.md`](Resources_Load_and_Instantiate.md)).

## Пакет + `.inputactions` asset

Input System — окремий package (`com.unity.inputsystem`), не частина ядра Unity. Дані
живуть у файлі-асеті (в проєкті — `Assets/Input/Input Actions.inputactions`), який
редагується у своєму вікні (подвійний клік по файлу).

### Ієрархія понять усередині asset'у

--------------------------- КОД ---------------------------
<pre>
InputActionAsset ("Input Actions")
└── Action Map ("Player")           — іменована група дій одного "режиму"
    ├── Action ("Move")             — дія, незалежна від конкретної клавіші
    │   ├── Composite Binding ("Keyboard", тип 2D Vector)
    │   │   ├── Up    → W [Keyboard]
    │   │   ├── Down  → S [Keyboard]
    │   │   ├── Left  → A [Keyboard]
    │   │   └── Right → D [Keyboard]
    │   └── Binding → Left Stick [Gamepad]   (окремо, без композиту)
    ├── Action ("Jump")
    │   └── Binding → Space [Keyboard]
    └── Action ("Fire")
        └── Binding → Left Button [Mouse]
</pre>
------------------------------------------------------------

- **Action Map** — групування дій за "режимом гравця" (наприклад, `Player` під час гри,
  окремий `UI` під час меню — у проєкті поки лише `Player`).
- **Action** — сама дія гравця (`Move`, `Jump`), відв'язана від конкретної кнопки.
- **Binding** — одна конкретна фізична кнопка/вісь, прив'язана до дії.
- **Composite Binding** — кілька окремих Binding, об'єднаних Unity в одне значення
  (WASD → один `Vector2`). Дії не потрібен композит, якщо джерело вже саме по собі
  багатовимірне — стік геймпада вже видає `Vector2`, тому `Left Stick` іде окремим
  простим Binding поруч із композитом, не всередину нього.

### `Action Type`: `Button` vs `Value`

- **`Button`** — дискретна подія: натиснуто / не натиснуто (внутрішньо `float 0`/`1`).
  Годиться для `Jump`, `Fire` — разових дій.
- **`Value`** — безперервне значення, яке має сенс опитувати будь-якої миті: `float`,
  `Vector2`, `Vector3`... Годиться для `Move` — там потрібен поточний напрямок, а не факт
  "щось натиснуто".

Разом з `Action Type = Value` вибирається ще й **`Control Type`** — конкретний тип
значення (`Vector2` для `Move`).

## `Generate C# Class`

Чекбокс в Inspector'і самого asset'у. Без нього довелось би шукати дії по рядкових іменах
у коді (`asset.FindActionMap("Player").FindAction("Move")` — крихко, легко зробити
одруківку в рядку, помилка виявиться лише в рантаймі).

З увімкненим чекбоксом Unity генерує клас-обгортку (ім'я/простір імен/шлях — поля тут же,
в Inspector'і) із типізованими властивостями на кожен Action Map і кожну дію всередині.
Загальна форма використання (назви умовні — залежать від того, як генератор назве
Action Map/дії з твого asset'у):

--------------------------- КОД ---------------------------
<pre>
var actions = new &lt;ЗгенерованийКлас&gt;();   // конструктор без параметрів
var player = actions.Player;              // властивість на Action Map "Player"

player.Enable();                          // без цього нічого не читається!

Vector2 move = player.Move.ReadValue&lt;Vector2&gt;();   // опитування (poll)

player.Jump.performed += context =&gt; { /* ... */ }; // підписка (event)
</pre>
------------------------------------------------------------

## `.Enable()` / `.Disable()` — найпоширеніша перша пастка

Дія/Action Map **не читає ввід**, поки на ній явно не викликано `.Enable()`. Без цього
`ReadValue<T>()` мовчки повертає нульове значення (`Vector2.zero`, `false`...), і події
`.performed` просто ніколи не спрацюють — без винятку, без помилки в консолі, тому
дебажиться неприємно довго, якщо не знати про цю вимогу заздалегідь.

`.Disable()` — симетрична пара, вимикає читання (знадобиться, наприклад, для паузи).

**Важливо для майбутньої паузи:** Action Map-и повністю незалежні одна від одної.
`_inputActions.Player.Disable()` вимикає лише `Player` — якщо поруч є ще одна мапа
(наприклад, `Menu`, для навігації по паузі клавіатурою/геймпадом), її треба вмикати
окремим викликом (`_inputActions.Menu.Enable()`) — вимкнена `Player` не робить цього
за тебе. Якщо ж пауза керується лише мишкою через звичайні UI-кнопки (`Button.onClick`),
окрема `Menu`-мапа взагалі не потрібна — клік мишкою по кнопці обробляється через
Unity `EventSystem`, який працює незалежно від стану будь-якої Action Map.

## Два способи прочитати дію: Poll vs Event

- **Poll (опитування)** — `action.ReadValue<T>()`: питаєш "яке значення зараз", коли самому
  потрібно, найчастіше в `Update()`. Підходить для `Move` — рух перевіряється щокадру.
- **Event (подія/callback)** — підписка на `action.started` / `action.performed` /
  `action.canceled`, викликається Unity сама, коли щось відбувається. Підходить для
  дискретних дій (`Jump`, `Fire`) — не треба щокадру перевіряти "чи натиснута кнопка",
  досить відреагувати один раз у момент натискання. Той самий принцип C#-подій, що вже
  використаний у проєкті — `Action onLoaded` в `SceneLoader`
  ([`Delegates_Events_and_Subscriptions.md`](Delegates_Events_and_Subscriptions.md)) —
  тут просто Unity сама генерує подію замість того, щоб проєкт її сам викликав.

  - `started` — момент початку взаємодії (кнопку почали тиснути).
  - `performed` — дія фактично відбулась (найчастіше саме цей інтересний).
  - `canceled` — взаємодію перервано (кнопку відпустили).

  Обробник отримує `InputAction.CallbackContext` — з нього можна дістати значення саме
  цього спрацювання: `context.ReadValue<T>()`. Детальний розбір `CallbackContext`,
  кількох реальних варіантів використання (`Value`-дії, кілька біндингів, `Hold`
  interaction) і того, коли підписку на `InputAction`-подію треба відписувати, а
  коли ні — див. [`InputAction_Events_and_CallbackContext.md`](InputAction_Events_and_CallbackContext.md).

## Підсумок: типи `UnityEngine.InputSystem`, які тут з'явились

Поглиблений розбір самих типів (`InputAction`, `CallbackContext`,
`InputActionType`) — у
[`InputAction_Events_and_CallbackContext.md`](InputAction_Events_and_CallbackContext.md);
тут лише те, що додає рівень Unity-редактора/згенерованого класу:

### Згенерований клас (`Generate C# Class`, ім'я умовне, тут — `<ЗгенерованийКлас>`)

- Конструктор без параметрів: `new <ЗгенерованийКлас>()`.
- Властивість на кожен Action Map (тут — `.Player`) → обгортка над Action Map з
  властивістю на кожну дію (`.Move`, `.Jump`) → `InputAction`.

### Дія (`InputAction`, той самий тип, що в `InputAction_Events_and_CallbackContext.md`)

- `.Enable()` / `.Disable()` → `void`.
- `.ReadValue<T>()` → `T` (poll) — приклад:
  `Vector2 move = player.Move.ReadValue<Vector2>();` (`Vector2` — два `float`, x/y).
- `.performed` (event) — приклад: `player.Jump.performed += context => { ... };`.

## Пов'язане

- [`InputAction_Events_and_CallbackContext.md`](InputAction_Events_and_CallbackContext.md) —
  поглиблений розбір `.started`/`.performed`/`.canceled`, `CallbackContext`,
  кілька реальних варіантів використання.
- [`Delegates_Events_and_Subscriptions.md`](Delegates_Events_and_Subscriptions.md) —
  механіка `event`/`Action`, яку `.performed`/`.started`/`.canceled` використовують
  під капотом.
- [`Resources_Load_and_Instantiate.md`](Resources_Load_and_Instantiate.md) — інший приклад
  Unity API, поділеного на "дістати посилання" + "використати" двома окремими викликами.
