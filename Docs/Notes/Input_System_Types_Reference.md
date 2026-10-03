# Input System: що приймає й віддає кожен метод і колбек — довідник за типами

Це довідник **за типами даних** до нового Input System (пакет `com.unity.inputsystem`,
версія проєкту — **1.14.2**). Концепції (Action Map, Binding, `Button` vs `Value`,
`.Enable()`) — у [`Input_System.md`](Input_System.md), глибокий розбір подій —
у [`InputAction_Events_and_CallbackContext.md`](InputAction_Events_and_CallbackContext.md).
Тут — лише: **який виклик → який тип повертає / приймає**. Мета — щоб можна було покласти
цей файл поруч із [`Movement_Approaches.md`](Movement_Approaches.md) і самостійно
співставити, що з чого куди йде.

Факти про дії проєкту (`Move`, `Jump`, `Fire`) взято з реального файла
`Assets/Input/InputActions.inputactions` і з згенерованого `InputActions.cs`. Прикладів
виводу консолі немає: це API рушія, поза Unity воно не запускається; поведінка описана за
документацією пакета.

## Мапа задач

| Хочу | API | Повертає / приймає | Розділ |
|---|---|---|---|
| Увімкнути читання дій мапи | `Player.Enable()` / `action.Enable()` | `void` | 2 |
| Поточний напрямок руху | `action.ReadValue<Vector2>()` | `Vector2` | 4 |
| Чи затиснута кнопка зараз | `action.IsPressed()` | `bool` | 4 |
| Чи натиснули саме в цьому кадрі | `action.WasPressedThisFrame()` | `bool` | 4 |
| Чи відпустили саме в цьому кадрі | `action.WasReleasedThisFrame()` | `bool` | 4 |
| Числове значення кнопки (0..1) | `action.ReadValue<float>()` | `float` | 4 |
| Відреагувати одразу, коли щось сталось | `action.performed += handler` | `Action<InputAction.CallbackContext>` | 5 |
| Значення в момент події | `context.ReadValue<T>()` | `T` | 6 |
| Кнопка в момент події як `bool` | `context.ReadValueAsButton()` | `bool` | 6 |
| У якій фазі дія | `action.phase` / `context.phase` | `InputActionPhase` | 7 |
| Яка саме кнопка/вісь спрацювала | `context.control` | `InputControl` | 6 |
| Час події / тривалість | `context.time` / `context.duration` | `double` (секунди) | 6 |
| Отримувати всі події через інтерфейс | `Player.AddCallbacks(IPlayerActions)` | інтерфейс із `OnMove/OnJump/OnFire(CallbackContext)` | 8 |

---

## 1. Ланцюг типів: від файла до значення

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

- `InputActions` — згенерований клас-обгортка. Конструктор без параметрів.
- `PlayerActions` — `struct` із властивістю на кожну дію (`Move`, `Jump`, `Fire`) плюс
  `Enable()`/`Disable()` для всієї мапи, `AddCallbacks(...)`.
- `InputAction` — сама дія; саме в неї є `ReadValue<T>()` і події.

## 2. Увімкнення (`Enable`/`Disable`)

| Виклик | Що робить | Повертає |
|---|---|---|
| `Player.Enable()` | вмикає **всі** дії мапи `Player` | `void` |
| `Player.Disable()` | вимикає всі дії мапи | `void` |
| `action.Enable()` / `action.Disable()` | одна дія | `void` |
| `action.enabled` | чи увімкнена | `bool` |

Без `Enable()` `ReadValue<T>()` мовчки повертає нуль/`false`, а події не спрацьовують.

## 3. Дії нашого проєкту (з `.inputactions`)

| Дія | `Action Type` | Тип значення | Біндинги | Тип значення в коді |
|---|---|---|---|---|
| `Move` | `Value` | `Vector2` | композит `2DVector`: W/A/S/D + `<Gamepad>/leftStick` | `Vector2` |
| `Jump` | `Button` | кнопка | `<Keyboard>/space` | `float` 0/1 (або `bool` через `IsPressed()`) |
| `Fire` | `Button` | кнопка | `<Mouse>/leftButton` | `float` 0/1 |

**Що саме лежить у `Vector2` від `Move`:**

- Вісь **x**: `D` → +1, `A` → −1. Вісь **y**: `W` → +1, `S` → −1.
  Тобто `W` дає `(0, 1)`, `D` дає `(1, 0)`.
- Нічого не натиснуто → `(0, 0)`.
- Діагональ (наприклад `W`+`D`) за замовчуванням нормалізується: довжина вектора 1,
  кожна компонента приблизно `0.707`, а не `(1, 1)`.
- Стік геймпада: компоненти в межах від −1 до 1, довжина не більша за 1, з мертвою
  зоною біля центру.

## 4. `InputAction` — властивості та методи

| Задача | API | Тип | Примітка |
|---|---|---|---|
| Прочитати поточне значення | `ReadValue<TValue>()` | `TValue` | `TValue` має збігатись з типом дії: для `Move` — `Vector2`, для кнопки — `float` |
| Кнопка зараз затиснута | `IsPressed()` | `bool` | |
| Натиснули в цьому кадрі | `WasPressedThisFrame()` | `bool` | `true` рівно один кадр |
| Відпустили в цьому кадрі | `WasReleasedThisFrame()` | `bool` | |
| Дія "виконалась" у цьому кадрі | `WasPerformedThisFrame()` | `bool` | |
| Ім'я дії | `name` | `string` | `"Move"`, `"Jump"` |
| Тип дії | `type` | `InputActionType` | див. розділ 7 |
| Поточна фаза | `phase` | `InputActionPhase` | див. розділ 7 |
| Увімкнена | `enabled` | `bool` | |
| Подія початку | `started` | `event Action<InputAction.CallbackContext>` | розділ 5 |
| Подія виконання | `performed` | `event Action<InputAction.CallbackContext>` | розділ 5 |
| Подія скасування | `canceled` | `event Action<InputAction.CallbackContext>` | розділ 5 |

Правило типів: `ReadValue<T>()` з не тим `T`, що в дії, кидає `InvalidOperationException`.
`Move` читай як `Vector2`; кнопку — як `float` або через `IsPressed()`; `ReadValue<bool>()`
для кнопки не підходить.

## 5. Колбеки `started` / `performed` / `canceled`

Усі три — `event Action<InputAction.CallbackContext>`. Обробник — метод (чи лямбда), що
**приймає один параметр `InputAction.CallbackContext` і нічого не повертає (`void`)**:

```csharp
void Handler(InputAction.CallbackContext context) { }

_inputActions.Player.Jump.performed += Handler;       // підписка
_inputActions.Player.Jump.performed -= Handler;       // відписка
_inputActions.Player.Jump.performed += ctx => { };    // лямбда: ctx має тип CallbackContext
```

**Коли спрацьовує кожна подія (за замовчуванням, без додаткових interaction):**

| Дія | `started` | `performed` | `canceled` |
|---|---|---|---|
| `Button` (`Jump`, `Fire`) | у момент натискання | у момент натискання (одразу після `started`) | у момент відпускання |
| `Value` (`Move`) | коли значення вперше відхилилось від нуля | **щоразу**, коли значення змінюється, поки дія активна | коли значення повернулось до нуля |

Наслідок: `Jump.performed` — це один виклик на натискання (відпускання його не
викликає). `Move.performed` — це багато викликів; тому рух зручніше не слухати подіями,
а опитувати `ReadValue<Vector2>()` у потрібному місці.

## 6. `InputAction.CallbackContext` — що в ньому є

`CallbackContext` — вкладена `struct` (тип значення), яка описує саме цей виклик колбека.

| Що дістати | Член | Тип |
|---|---|---|
| Значення в момент події | `ReadValue<TValue>()` | `TValue` |
| Значення кнопки як булеве | `ReadValueAsButton()` | `bool` |
| Значення без відомого типу | `ReadValueAsObject()` | `object` |
| Яка дія | `action` | `InputAction` |
| Яка фаза | `phase` | `InputActionPhase` |
| Який контрол спрацював (яка кнопка/вісь) | `control` | `InputControl` (`.name`, `.path` — рядки) |
| Час події (секунди від старту) | `time` | `double` |
| Час початку взаємодії | `startTime` | `double` |
| Тривалість від початку до події | `duration` | `double` |
| Фаза як булеві прапорці | `started`, `performed`, `canceled` | `bool` |

Не всі колбеки потребують `context`: у `InputService` лямбда `ctx => OnJumpPressed?.Invoke()`
отримує `CallbackContext`, але його не використовує — для простої кнопки достатньо самого
факту, що подія відбулась.

## 7. Enum-и

`InputActionPhase` (стан дії): `Disabled`, `Waiting`, `Started`, `Performed`, `Canceled`.

`InputActionType` (тип дії в `.inputactions`): `Value`, `Button`, `PassThrough`.

## 8. Через інтерфейс `IPlayerActions` (згенерований)

Згенерований клас має інтерфейс, який можна реалізувати замість підписки на кожну подію:

```csharp
public interface IPlayerActions
{
    void OnMove(InputAction.CallbackContext context);
    void OnJump(InputAction.CallbackContext context);
    void OnFire(InputAction.CallbackContext context);
}
```

- `Player.AddCallbacks(instance)` (і `SetCallbacks`) підписує `OnMove` на **всі три**
  події `Move` (`started`, `performed`, `canceled`), так само `OnJump` і `OnFire`.
  Тому в такому методі треба перевіряти `context.phase` (або `context.performed`), інакше
  реакція піде на кожну фазу.
- Кожен метод приймає `InputAction.CallbackContext` і повертає `void`.

## 9. Коли викликаються колбеки

За замовчуванням (налаштування Input System — Update Mode: **Process Events In Dynamic
Update**) події вводу обробляються раз на кадр, перед `Update`. Тому колбеки
`performed` та інші виконуються в головному потоці в цьому місці кадру, а не довільно.
`ReadValue<T>()` у будь-якому методі повертає останнє оброблене значення.

## 10. Що віддає наш `IInputService`

Решта гри бачить ввід лише через інтерфейс:

```csharp
public interface IInputService : IService
{
    Vector2 GetDirection();       // = _inputActions.Player.Move.ReadValue<Vector2>()
    event Action OnJumpPressed;   // порожній Action, без CallbackContext
}
```

- `GetDirection()` → `Vector2` (розділ 3: `W` = `(0, 1)`).
- `OnJumpPressed` → `Action` **без параметрів**: `CallbackContext` лишається всередині
  `InputService` (там лямбда `ctx => OnJumpPressed?.Invoke()`); споживачі його не бачать.

---

## 11. Заготовка для твого співставлення (заповни сам)

Ліві колонки — що дає ввід, середні — що приймають API руху з
[`Movement_Approaches.md`](Movement_Approaches.md). Остання колонка — твоя: який крок
перетворення потрібен між ними (порожня навмисно).

| Що дає ввід | Тип | Що приймає API руху | Тип | Перетворення (заповни сам) |
|---|---|---|---|---|
| `GetDirection()` | `Vector2` | `CharacterController.Move(...)` | `Vector3` (зсув за кадр) | |
| `GetDirection()` | `Vector2` | `CharacterController.SimpleMove(...)` | `Vector3` (швидкість, y ігнорується) | |
| `GetDirection()` | `Vector2` | `Rigidbody.AddForce(..., ForceMode)` | `Vector3`, `ForceMode` | |
| `OnJumpPressed` | `Action` (без параметрів) | що прикласти для стрибка | залежить від підходу (див. Movement_Approaches, розділи 2 і 3) | |
| — | — | `CharacterController.isGrounded` | `bool` | |

Показуй заповнену таблицю — я перевірю, а не підкажу наперед.

## Пов'язане

- [`Input_System.md`](Input_System.md), [`InputAction_Events_and_CallbackContext.md`](InputAction_Events_and_CallbackContext.md) — концепції і подія-детально.
- [`Movement_Approaches.md`](Movement_Approaches.md) — API руху.
- [`Delegates_Events_and_Subscriptions.md`](Delegates_Events_and_Subscriptions.md) — `Action`, `event`, підписка.
