# Події `InputAction` (`.started`/`.performed`/`.canceled`) і `CallbackContext` — детально

Уперше з'явилось: урок 03, підписка `InputService` на `_inputActions.Player.Jump.performed`.
Це поглиблення поверх базового огляду в [`Input_System.md`](Input_System.md) (там —
Action Map/Binding/Composite/Generate C# Class на рівні Unity-редактора; тут — сама
механіка подій і `CallbackContext` у деталях, на кількох реалістичних варіантах
використання, не пов'язаних напряму з `Jump`).

## 1. Згенерований `_inputActions.Player.Jump` — це просто готовий `InputAction`

`InputAction` — тип, який Unity дає для представлення **однієї гравецької дії**
незалежно від конкретної фізичної кнопки. Його можна створити вручну, без жодного
asset'у чи "Generate C# Class":

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class InputActionTest : MonoBehaviour
{
    private InputAction _jumpAction;

    private void Awake()
    {
        _jumpAction = new InputAction(binding: "<Keyboard>/space");
        _jumpAction.performed += OnJumpPerformed;
        _jumpAction.Enable();
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Jump!");
    }

    private void OnDestroy()
    {
        _jumpAction.performed -= OnJumpPerformed;
    }
}
```

Постав це на будь-який `GameObject`, натисни Play і Space — у Console полетить
"Jump!". Це той самий тип `InputAction`, та сама подія `performed`, що й у
`InputService`. Різниця лише в тому, звідки взявся об'єкт: тут він створений вручну
(`new InputAction(...)`) з binding'ом, прописаним прямо в коді; в `InputService` цей
самий об'єкт вже створений і налаштований **за тебе** згенерованим класом
`InputActions`, коли в Input Actions Editor намалював Action Map `Player` з дією
`Jump` і прив'язкою на Space, а потім увімкнув "Generate C# Class". `_inputActions.Player.Jump`
— це просто вже готовий, попередньо сконфігурований `InputAction`.

### Хто насправді викликає `.Invoke()` для `.performed`/`.started`/`.canceled`

У жодному з прикладів у цьому документі немає рядка на кшталт `performed.Invoke(...)`,
написаного нами — і це не пропуск. `.performed` — це `event`, точно як `OnJumpPressed`
у `InputService`, а в будь-якого `event` є рівно один "власник", який має право
викликати `?.Invoke()` (той самий принцип "газети/видавця", що в
[`Delegates_Events_and_Subscriptions.md`](Delegates_Events_and_Subscriptions.md) —
право розсилки належить тому, хто знає, коли настав момент, а не підписнику).

Для `OnJumpPressed` власник — наш власний `InputService`, тому `?.Invoke()` пишемо
ми самі. Для `.performed`/`.started`/`.canceled` власник — сам клас `InputAction`,
написаний і скомпільований усередині пакета Input System (код Unity, не наш) — тому
виклик `Invoke()` там точно є, просто він захований у коді рушія, якого ми не пишемо
й не бачимо. Наш код завжди стоїть лише з боку `+=` (підписник); з боку `Invoke()`
для цих конкретних подій ми ніколи не стоїмо, бо власник — не ми.

Спрощено, приблизно так виглядає всередині самого `InputAction` (ілюстрація
принципу, не реальний вихідний код Unity):

```csharp
public class InputAction
{
    public event Action<CallbackContext> performed;

    // Unity сама викликає цей метод щокадру, читаючи стан заліза:
    private void ProcessCurrentValue()
    {
        if (/* поріг Button пройдено, або Interaction каже "виконано" */)
        {
            performed?.Invoke(new CallbackContext(...));
        }
    }
}
```

Цей `ProcessCurrentValue` (чи як він насправді називається в реальному коді Unity)
— частина рушія, яка щокадру опитує фізичну клавіатуру/мишу/геймпад і сама вирішує,
коли поріг пройдено. Ми ніколи не пишемо цей метод — тільки підписуємось на його
результат.

## 2. Три складові, які визначають поведінку дії

1. **Type** — `Button` ("натиснуто/ні") чи `Value` (несе число/вектор).
2. **Binding(и)** — одна чи кілька фізичних кнопок/осей, прив'язаних до дії.
3. **Interaction** (необов'язково) — правило "коли саме вважати дію виконаною":
   за замовчуванням немає (просто "натиснуто" = "відбулось"), або, наприклад, `Hold`
   (треба утримати N секунд).

Ці три речі разом визначають, коли спрацьовує кожна з трьох подій і що лежить у
`CallbackContext` — розберемо на конкретних прикладах нижче.

## 3. Підписка через лямбду — розбір рядка з `InputService`

```csharp
_inputActions.Player.Jump.performed += _ => OnJumpPressed?.Invoke();
```

- `_inputActions` — поле типу `InputActions` (згенерований клас).
- `.Player.Jump` — конкретний об'єкт `InputAction` усередині нього.
- `.performed` — подія цього об'єкта, типу `Action<InputAction.CallbackContext>`.
- `_ => OnJumpPressed?.Invoke()` — лямбда: анонімний метод з одним параметром,
  названим `_` (сигнал "цей параметр існує, бо вимагає сигнатура, але не
  використовується"), тіло якого викликає власну подію `InputService`.

**Еквівалентні форми того самого рядка:**

```csharp
// Варіант 1 — параметр "_" (як у InputService)
_inputActions.Player.Jump.performed += _ => OnJumpPressed?.Invoke();

// Варіант 2 — параметр з іменем, просто не використовується
_inputActions.Player.Jump.performed += context => OnJumpPressed?.Invoke();

// Варіант 3 — з явно вказаним типом параметра (зайве тут, тип і так виводиться)
_inputActions.Player.Jump.performed += (InputAction.CallbackContext context) => OnJumpPressed?.Invoke();

// Варіант 4 — іменований метод замість лямбди
_inputActions.Player.Jump.performed += OnJumpPerformed;

private void OnJumpPerformed(InputAction.CallbackContext context) => OnJumpPressed?.Invoke();
```

### Сигнатура `Action<InputAction.CallbackContext>` розібрана повністю

`.performed` усередині Unity оголошена саме так: `event Action<InputAction.CallbackContext> performed;`.
Розкладемо цей тип на частини:

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

Підставивши `T = InputAction.CallbackContext` у `Action<T>`, отримуємо: "посилання на
метод з одним параметром типу `InputAction.CallbackContext`, що повертає `void`".
Метод `OnJumpPerformed(InputAction.CallbackContext context)` з Варіанту 4 вище
збігається з цим один-в-один (один параметр того самого типу, `void`) — тому
компілюється при `+=`.

**`OnJumpPressed?.Invoke()` — не окремий метод, а виклик уже готового.** Кожен
делегат (тут — `OnJumpPressed`, тип `Action`) автоматично має вбудований метод
`Invoke()` (компілятор генерує його разом із самим типом делегата — детальніше в
[`Delegates_Events_and_Subscriptions.md`](Delegates_Events_and_Subscriptions.md) §4).
Методом, який реально **оголошений** тут, є сама лямбда (чи `OnJumpPerformed` у
Варіанті 4) — `Invoke()` всередині неї лише **викликає** ту готову функціональність.

**Що НЕ скомпілюється:**

```csharp
// ПОМИЛКА КОМПІЛЯЦІЇ — лямбда без параметрів не підходить під
// Action<InputAction.CallbackContext>: подія вимагає рівно один параметр,
// а тут його нуль. Компілятор виведе щось на кшталт:
// "Delegate 'Action<InputAction.CallbackContext>' does not take 0 arguments"
_inputActions.Player.Jump.performed += () => OnJumpPressed?.Invoke();
```

`_` — не зарезервоване слово в класичному сенсі (не ключове слово мови), а
спеціальний ідентифікатор, який компілятор (з C# 9) розпізнає як "discard": IDE не
підсвічуватиме його як "невикористаний параметр", на відміну від того, якби ти назвав
його, скажімо, `ctx` і ніде не використав.

## 4. `InputAction.CallbackContext` — вкладений тип (nested type)

`CallbackContext` — це `struct`, оголошений **всередині** класу `InputAction`, а не
окремо. Це називається вкладений тип: один тип оголошений усередині іншого, бо існує
тільки в його контексті. Мінімальний ізольований приклад того самого прийому:

```csharp
public class Order
{
    public struct Item   // вкладений тип
    {
        public string Name;
        public int Price;
    }
}

// використання ззовні:
Order.Item item = new Order.Item { Name = "Меч", Price = 100 };
```

`Item` існує тільки в контексті `Order` — немає сенсу робити його окремим top-level
типом. Той самий принцип із `CallbackContext`: він завжди існує тільки в контексті
конкретної `InputAction`, тому Unity й засунула його туди, а не зробила окремим
класом на верхньому рівні `UnityEngine.InputSystem`.

**Що лежить усередині `CallbackContext`** — "знімок" деталей саме цього одного
спрацювання:
- `context.ReadValue<T>()` — яке саме значення викликало подію.
- `context.control` — яка фізична кнопка/стік конкретно спрацювала (актуально, якщо
  на одну дію прив'язано кілька клавіш).
- `context.time` — коли саме це сталось.

**Принцип, за яким `ReadValue<T>()` повертає значення:** `T` — не довільний вибір, а
мусить збігатись із тим, що фактично видає ця дія (залежить від Type дії й типу
binding'у: одноосьовий контрол → `float`, стік/composite з 4 кнопок → `Vector2`).
Якщо вказати неправильний `T`, отримаєш помилку в рантаймі (`InvalidOperationException`),
бо реального значення такого типу там немає.

## 5. Коли `context` не потрібен, а коли обов'язковий — реальні варіанти

### 5.1. Проста кнопка — `context` можна ігнорувати (як `Jump`)

```csharp
_fireAction = new InputAction("Fire", binding: "<Mouse>/leftButton");
_fireAction.performed += _ => Shoot();
_fireAction.Enable();
```

Сам факт "клацнули" — вся потрібна інформація. Кнопка одна, значення не несе нічого
(`Button` = просто так/ні).

### 5.2. Аналогове значення — `context` обов'язковий

```csharp
_zoomAction = new InputAction("Zoom", type: InputActionType.Value, binding: "<Mouse>/scroll/y");
_zoomAction.performed += context =>
{
    float scrollDelta = context.ReadValue<float>();
    Debug.Log($"Прокрутка: {scrollDelta}");
};
_zoomAction.Enable();
```

Тут `.performed` спрацьовує щоразу, коли колесо миші прокручується, і щоразу з
**іншим** значенням. Сам факт "подія відбулась" нічого не каже — потрібно знати
**наскільки** прокрутили. Без `context.ReadValue<float>()` обробник не мав би
доступу до цього числа взагалі.

### 5.3. Кілька біндингів на одну дію — треба знати, ЯКА кнопка спрацювала

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class MultiBindingTest : MonoBehaviour
{
    private InputAction _fireAction;

    private void Awake()
    {
        _fireAction = new InputAction("Fire");
        _fireAction.AddBinding("<Keyboard>/space");
        _fireAction.AddBinding("<Mouse>/leftButton");
        _fireAction.performed += OnFirePerformed;
        _fireAction.Enable();
    }

    private void OnFirePerformed(InputAction.CallbackContext context)
    {
        Debug.Log($"Fire triggered by: {context.control.displayName}");
    }

    private void OnDestroy()
    {
        _fireAction.performed -= OnFirePerformed;
    }
}
```

Той самий обробник викликається і від Space, і від ЛКМ (це та сама дія `Fire`).
Єдиний спосіб дізнатись, яка фізична кнопка це зробила — прочитати `context.control`.
Без параметра цієї інформації просто нема звідки взяти.

### 5.4. `Interaction` — коли `.started`/`.performed`/`.canceled` це справді РІЗНІ моменти

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

Що реально станеться в Play Mode:
- Натиснув `R` і одразу відпустив (менше секунди) → `"почав тиснути R"`, потім одразу
  `"відпустив R ЗАРАНО — скасовано"`. **`.performed` взагалі не спрацює.**
- Натиснув `R` і тримав ≥1 секунду → `"почав тиснути R"` одразу, а через секунду —
  `"утримав секунду — перезарядка!"`. `.canceled` не спрацює.

Порівняй із `Jump` (без жодного `interactions`) — там `.started` і `.performed`
спрацьовують **в одну й ту саму мить** натискання, а `.canceled` — в мить
відпускання, без жодної затримки чи умови. Різниця виключно в тому, що `Jump` не має
налаштованого `Interaction`, а `Reload` має `hold` — саме `Interaction` визначає,
коли `performed` вважається "виконаним", а не сам факт натискання.

## 6. Повний робочий приклад — усі варіанти разом

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

Зверни увагу: тут усі обробники — **іменовані методи**, не інлайн-лямбди. Це не
випадково — див. §8 нижче про те, чому саме тут відписка потрібна і чому вона
взагалі можлива тільки завдяки іменованим методам.

## 7. Підсумкова таблиця

| Дія | Тип | Interaction | Чи потрібен `context` | Навіщо |
|---|---|---|---|---|
| `Fire`/`Jump` | Button | немає | ні (`_`) | сам факт натискання — вся інформація |
| `Zoom` | Value | немає | так, `ReadValue<float>()` | подія без значення безглузда |
| `Fire` (кілька біндингів) | Button | немає | так, `context.control` | треба знати, яка саме кнопка |
| `Reload` | Button | `hold` | не обов'язково, але фази реально різні | без Interaction всі три фази злиті в одну |

## 8. Підписка й відписка для подій `InputAction` конкретно

Загальний принцип "коли відписка потрібна" розібраний в
[`Delegates_Events_and_Subscriptions.md`](Delegates_Events_and_Subscriptions.md) §6 —
тут лише застосування саме до `InputAction`:

- **`InputService` (реальний проєкт):** підписка стоїть у конструкторі, який
  викликається рівно один раз за все життя гри (сервіс живе, поки жива гра). Підписник
  (`InputService`) і джерело (`_inputActions`, його ж власне поле) створюються й
  помирають разом — відписуватись нема від чого й нема коли. Тому там інлайн-лямбда
  (`_ => OnJumpPressed?.Invoke()`) — цілком нормально.
- **Тестові `MonoBehaviour` вище (`InputActionTest`, `MultiBindingTest`,
  `WeaponInputTest`):** `GameObject`, на якому вони висять, може бути знищений
  (зміна сцени, `Destroy(gameObject)`) раніше, ніж закінчиться гра — інша ситуація.
  Тому в `OnDestroy()` потрібна явна відписка `-=`.
- **Чому там навмисно іменовані методи, а не лямбди:** щоб відписатись (`-=`) від
  чогось, потрібне посилання на **той самий** делегат-об'єкт, яким підписувались.
  Інлайн-лямбда (`context => ...`) щоразу створює **новий** об'єкт-делегат — навіть
  якщо текст коду однаковий, `-=` з новою лямбдою не знайде і не прибере стару
  підписку. Іменований метод (`OnFirePerformed`) — те саме посилання і при `+=`, і
  при `-=`, тому відписка спрацює.

## Підсумок: типи `UnityEngine.InputSystem`, які тут з'явились

### `InputAction` (namespace `UnityEngine.InputSystem`, клас)

- Конструктор: `new InputAction(string name = null, InputActionType type = ..., string binding = null, string interactions = null)` —
  усі параметри опційні; `binding` — рядок шляху контролу (`"<Keyboard>/space"`),
  `interactions` — рядок правила (`"hold(duration=1)"`).
- Події (кожна — `event Action<InputAction.CallbackContext>`): `.started`,
  `.performed`, `.canceled` — три окремі моменти взаємодії.
- Методи: `.Enable()` / `.Disable()` → `void` — увімкнути/вимкнути читання вводу;
  `.AddBinding(string path)` → додає ще один фізичний контрол до тієї самої дії.

### `InputAction.CallbackContext` (вкладений `struct` усередині `InputAction`)

- `.ReadValue<T>()` → `T` — значення, що викликало спрацювання; `T` має збігатись
  із реальним типом даних дії (одноосьовий контрол → `float`, стік/composite →
  `Vector2`), інакше — `InvalidOperationException` у рантаймі.
- `.control` → `InputControl` — яка конкретно фізична кнопка/вісь спрацювала;
  `.control.displayName` → `string`.
- `.time` → `double` — момент часу спрацювання.

### `InputActionType` (namespace `UnityEngine.InputSystem`, enum)

- `Button` — дискретна подія (натиснуто/ні).
- `Value` — безперервне значення (`float`, `Vector2`, `Vector3`...).

## Пов'язане

- [`Input_System.md`](Input_System.md) — Action Map/Action/Binding/Composite,
  `Button` vs `Value`, `Generate C# Class`, poll vs event на рівні Unity-редактора.
- [`Delegates_Events_and_Subscriptions.md`](Delegates_Events_and_Subscriptions.md) —
  базова механіка `event`/`Action`/лямбд і загальний принцип "коли потрібна
  відписка", без прив'язки до Input System.
