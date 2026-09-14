# Рефлексія — покроково, з реальним виводом консолі

Уперше з'явилось: опційне ДЗ уроку 03 (constructor-only auto-resolver). **Кожен блок
"ВИВІД" нижче — реально запущений код** (окремий консольний .NET-проєкт, поза цим
репозиторієм), вивід скопійований з реальної консолі, не вигаданий і не
"приблизно так має бути". Якщо хочеш перевірити сам — весь код нижче можна вставити
в один консольний проєкт і запустити (`dotnet run`).

## Класи, з якими працюємо (ізольовано, поза проєктом)

--------------------------- КОД ---------------------------
<pre>
public class Robot
{
    public Robot(string name, int power)
    {
        Console.WriteLine($"[всередині конструктора Robot] Robot {name} created with power {power}");
    }
}

public class Box
{
    public T GetValue&lt;T&gt;()
    {
        Console.WriteLine($"[всередині GetValue] called for type {typeof(T).Name}");
        return default(T);
    }
}
</pre>
------------------------------------------------------------

## Крок 1 — `typeof(Robot)`

--------------------------- КОД ---------------------------
<pre>
Type type = typeof(Robot);
Console.WriteLine($"type              -&gt; {type}");
Console.WriteLine($"type.Name         -&gt; {type.Name}");
Console.WriteLine($"type.FullName     -&gt; {type.FullName}");
</pre>
------------------------------------------------------------

--------------------------- ВИВІД ---------------------------
<pre>
type              -&gt; Robot
type.Name         -&gt; Robot
type.FullName     -&gt; Robot
</pre>
------------------------------------------------------------

Усі три однакові, бо `Robot` тут — top-level клас без `namespace`. Якби він лежав у
`namespace MyGame`, `type.FullName` показав би `MyGame.Robot`, а `type.Name` і далі
був би просто `Robot` (`Name` — коротке ім'я, `FullName` — з неймспейсом).

## Крок 2 — `type.GetConstructors()`

--------------------------- КОД ---------------------------
<pre>
ConstructorInfo[] constructors = type.GetConstructors();
Console.WriteLine($"constructors.Length -&gt; {constructors.Length}");
Console.WriteLine($"constructors[0]      -&gt; {constructors[0]}");
ConstructorInfo constructor = constructors[0];
Console.WriteLine($"constructor.Name     -&gt; {constructor.Name}");
</pre>
------------------------------------------------------------

--------------------------- ВИВІД ---------------------------
<pre>
constructors.Length -&gt; 1
constructors[0]      -&gt; Void .ctor(System.String, Int32)
constructor.Name     -&gt; .ctor
</pre>
------------------------------------------------------------

`constructors[0]` при друку показує **рядкове представлення сигнатури**: `Void` —
конструктор нічого "не повертає" (рефлексія все одно показує тип результату, і для
конструктора це завжди `Void`), `.ctor` — службова внутрішня назва, якою .NET називає
**геть усі** конструктори (сам клас `Robot` тут ніде не згаданий за іменем — тільки
типи параметрів у дужках). `constructor.Name` окремо підтверджує те саме: реальне
ім'я цього "методу" в системі типів — `.ctor`, не `Robot`.

## Крок 3 — `constructor.GetParameters()`

--------------------------- КОД ---------------------------
<pre>
ParameterInfo[] parameters = constructor.GetParameters();
Console.WriteLine($"parameters.Length -&gt; {parameters.Length}");
for (int i = 0; i &lt; parameters.Length; i++)
{
    ParameterInfo p = parameters[i];
    Console.WriteLine($"parameters[{i}].Name              -&gt; {p.Name}");
    Console.WriteLine($"parameters[{i}].ParameterType      -&gt; {p.ParameterType}");
    Console.WriteLine($"parameters[{i}].ParameterType.Name -&gt; {p.ParameterType.Name}");
}
</pre>
------------------------------------------------------------

--------------------------- ВИВІД ---------------------------
<pre>
parameters.Length -&gt; 2
parameters[0].Name              -&gt; name
parameters[0].ParameterType      -&gt; System.String
parameters[0].ParameterType.Name -&gt; String
parameters[1].Name              -&gt; power
parameters[1].ParameterType      -&gt; System.Int32
parameters[1].ParameterType.Name -&gt; Int32
</pre>
------------------------------------------------------------

Ось воно — конкретні дані про кожен параметр, здобуті **без жодного рядка коду, що
згадує "Robot" за назвою**. `ParameterType` (повне ім'я `System.String`/`System.Int32`)
і `ParameterType.Name` (коротке `String`/`Int32`) — це той самий `Type`-об'єкт, що й
на кроці 1, тільки для типу параметра, а не для самого класу.

## Крок 4 — `constructor.Invoke(args)` — реальне створення об'єкта

--------------------------- КОД ---------------------------
<pre>
object[] args = { "R2D2", 100 };
Console.WriteLine("викликаємо constructor.Invoke(args)...");
object robotInstance = constructor.Invoke(args);
Console.WriteLine($"robotInstance          -&gt; {robotInstance}");
Console.WriteLine($"robotInstance.GetType() -&gt; {robotInstance.GetType()}");
Robot robot = (Robot)robotInstance;
</pre>
------------------------------------------------------------

--------------------------- ВИВІД ---------------------------
<pre>
викликаємо constructor.Invoke(args)...
[всередині конструктора Robot] Robot R2D2 created with power 100
robotInstance          -&gt; Robot
robotInstance.GetType() -&gt; Robot
</pre>
------------------------------------------------------------

Рядок `[всередині конструктора Robot] ...` — це `Console.WriteLine`, який фізично
стоїть **усередині** тіла конструктора `Robot`. Він спрацював — доказ, що
`Invoke(args)` реально виконав справжній конструктор класу `Robot` з аргументами
`"R2D2"` і `100`, а не якусь підробку. Ніде в цьому коді немає `new Robot("R2D2", 100)`.

`robotInstance -> Robot` — це видно, бо `Robot` не перевизначив `ToString()`, і
типова поведінка `object.ToString()` — просто надрукувати ім'я типу. `.GetType()`
підтверджує те саме напряму: об'єкт, отриманий через рефлексію, — реально `Robot`.

## Чому саме `object[]`, а не конкретні типи — і що буде, якщо переплутати

Тут накладаються одразу кілька окремих речей, розберемо кожну.

**1. Масив у C# завжди однотипний.** `object[] args = { "R2D2", 100 };` містить
`string` і `int` — два різні типи. Масив не може бути одночасно `string[]` і
`int[]` — потрібен **один спільний** тип елемента. Єдиний тип, спільний геть для
всього в C# — `object`: **будь-який** клас чи структура успадковується від нього
(той самий факт, завдяки якому `GetType()` доступний на будь-чому).

**2. Boxing — що фізично стається зі значеннєвим типом (`int`), коли він стає `object`.**
`int` — значеннєвий тип (value type), живе "на місці" (у стеку чи прямо всередині
об'єкта-власника), а не окремим об'єктом у купі. Щоб `100` взагалі можна було
покласти в змінну/масив типу `object`, компілятор загортає його в прихований
об'єкт-обгортку в купі — це і називається **boxing**. Реальний доказ:

--------------------------- КОД ---------------------------
<pre>
int number = 100;
object boxed = number;
Console.WriteLine($"number           -&gt; {number}");
Console.WriteLine($"boxed            -&gt; {boxed}");
Console.WriteLine($"boxed.GetType()  -&gt; {boxed.GetType()}");
</pre>
------------------------------------------------------------

--------------------------- ВИВІД ---------------------------
<pre>
number           -&gt; 100
boxed            -&gt; 100
boxed.GetType()  -&gt; System.Int32
</pre>
------------------------------------------------------------

Значення лишається тим самим (`100`), і `boxed.GetType()` і далі каже, що це
`Int32` — обгортка пам'ятає свій реальний тип усередині, хоч зовні змінна
оголошена як `object`. Для `string` (він і так reference-тип, живе в купі за
замовчуванням) boxing не потрібен — просто звичайний upcast до `object`.

**3. Чому саме сигнатура `Invoke(object[] parameters)` — не твій вибір.** Це
оголошення самого методу в бібліотеці `System.Reflection` (`ConstructorInfo`/
`MethodBase`). Причина: `Invoke` мусить працювати з **будь-яким** конструктором
**будь-якого** класу — типи параметрів яких заздалегідь невідомі авторам .NET (вони
дізнаються про них лише в рантаймі, через `GetParameters()`, як у Кроці 3). Єдиний
тип, який гарантовано підходить під що завгодно — `object`.

**4. Ціна цієї гнучкості — нуль перевірки типів під час компіляції.** Компілятор не
може перевірити наперед, чи `object[]` відповідає реальним параметрам конструктора
— все з'ясовується лише в момент виклику `Invoke`. Ось що реально стається, якщо
переплутати порядок чи кількість аргументів:

--------------------------- КОД ---------------------------
<pre>
try
{
    object[] wrongArgs = { 100, "R2D2" };   // переплутаний порядок
    constructor.Invoke(wrongArgs);
}
catch (Exception ex)
{
    Console.WriteLine($"Виняток: {ex.GetType().FullName}");
    Console.WriteLine($"Message: {ex.Message}");
}
</pre>
------------------------------------------------------------

--------------------------- ВИВІД ---------------------------
<pre>
Виняток: System.ArgumentException
Message: Object of type 'System.Int32' cannot be converted to type 'System.String'.
</pre>
------------------------------------------------------------

А якщо аргументів забагато чи замало:

--------------------------- КОД ---------------------------
<pre>
try
{
    object[] tooFewArgs = { "R2D2" };   // бракує другого аргумента
    constructor.Invoke(tooFewArgs);
}
catch (Exception ex)
{
    Console.WriteLine($"Виняток: {ex.GetType().FullName}");
    Console.WriteLine($"Message: {ex.Message}");
}
</pre>
------------------------------------------------------------

--------------------------- ВИВІД ---------------------------
<pre>
Виняток: System.Reflection.TargetParameterCountException
Message: Parameter count mismatch.
</pre>
------------------------------------------------------------

Порівняй зі звичайним `new Robot(100, "R2D2")` — таке взагалі не скомпілювалось би,
компілятор одразу підкреслив би помилку типів. З рефлексією цей захист зникає:
`object[]` пропустить що завгодно на етапі компіляції, а розплата настає лише в
рантаймі, у вигляді винятку. Це і є той компроміс, заради якого рефлексію
використовують лише тоді, коли вона реально потрібна (як у constructor-only
резолвері) — не як заміну звичайним, типобезпечним викликам.

## Крок 5 — generic-метод, коли тип відомий лише в рантаймі (`MakeGenericMethod`)

### Спочатку детально про сам тип `Box` — це НЕ `Box<T>` з `Generics.md`

Легко переплутати з `Box<T>` із [`Generics.md`](Generics.md) (`Put`/`Take`), але це
**зовсім інша форма**, і різниця тут ключова для всього кроку 5:

--------------------------- КОД ---------------------------
<pre>
public class Box               // клас БЕЗ жодного &lt;T&gt; — звичайний, не generic
{
    public T GetValue&lt;T&gt;()    // а ось ЦЕЙ МЕТОД — generic, &lt;T&gt; належить йому, не класу
    {
        Console.WriteLine($"[всередині GetValue] called for type {typeof(T).Name}");
        return default(T);
    }
}
</pre>
------------------------------------------------------------

- **`Box<T>` (Generics.md)** — generic **клас**. `T` фіксується один раз, коли
  створюєш об'єкт (`Box<int> box = new Box<int>();`) — і назавжди лишається `int`
  для цього конкретного `box`, для всіх його методів.
- **`Box` тут** — звичайний клас, зовсім без типового параметра на собі. Натомість
  типовий параметр `<T>` належить **окремому методу** `GetValue<T>()` — і кожен
  **виклик** цього методу може мати **свій власний** `T`, незалежно від попередніх
  викликів, на тому самому об'єкті:

--------------------------- КОД ---------------------------
<pre>
Box box = new Box();
Console.WriteLine($"box.GetType() -&gt; {box.GetType()}");

int intResult = box.GetValue&lt;int&gt;();
string stringResult = box.GetValue&lt;string&gt;();
Console.WriteLine($"intResult    -&gt; {intResult}");
Console.WriteLine($"stringResult -&gt; {stringResult ?? "null"}");

MethodInfo method = typeof(Box).GetMethod("GetValue");
Console.WriteLine($"method.IsGenericMethodDefinition -&gt; {method.IsGenericMethodDefinition}");
Console.WriteLine($"typeof(Box).IsGenericType         -&gt; {typeof(Box).IsGenericType}");
</pre>
------------------------------------------------------------

--------------------------- ВИВІД ---------------------------
<pre>
box.GetType() -&gt; Box

[всередині GetValue] called for type Int32
[всередині GetValue] called for type String
intResult    -&gt; 0
stringResult -&gt; null

method.IsGenericMethodDefinition -&gt; True
typeof(Box).IsGenericType         -&gt; False
</pre>
------------------------------------------------------------

Один і той самий `box` (`box.GetType() -> Box`, без жодного `<T>` у назві типу)
викликав `GetValue` двічі — раз з `int`, раз з `string` — і обидва рази спрацювало.
`typeof(Box).IsGenericType -> False` підтверджує: сам клас нічого не знає про `T`.
`method.IsGenericMethodDefinition -> True` підтверджує: саме **метод** несе на собі
незакритий типовий параметр — це і є те, що `MakeGenericMethod` "закриває" далі.

**Чому цей приклад побудований саме так, а не через `Box<T>`:** тому що реальний
`AllServices` у проєкті влаштований точно так само, як цей `Box` — не `Box<T>`.
`AllServices` (клас) не має жодного `<T>` на собі — існує рівно один
`AllServices.Instance` на всю гру, і він тримає **всі** типи сервісів одразу. А ось
його метод `GetService<TService>()` — генеричний, кожен виклик обирає свій
`TService`. Саме тому в резолвері не можна просто написати `AllServices.GetService`
без `MakeGenericMethod` — потрібно "закрити" типовий параметр **методу**, а не
класу, точнісінько як щойно проробили з `Box.GetValue<T>()`.

--------------------------- КОД ---------------------------
<pre>
Box box = new Box();
Type wantedType = typeof(int);

MethodInfo unboundMethod = typeof(Box).GetMethod("GetValue");
Console.WriteLine($"unboundMethod -&gt; {unboundMethod}");

MethodInfo boundMethod = unboundMethod.MakeGenericMethod(wantedType);
Console.WriteLine($"boundMethod   -&gt; {boundMethod}");

Console.WriteLine("викликаємо boundMethod.Invoke(box, null)...");
object result = boundMethod.Invoke(box, null);
Console.WriteLine($"result           -&gt; {result}");
Console.WriteLine($"result.GetType() -&gt; {result.GetType()}");
</pre>
------------------------------------------------------------

--------------------------- ВИВІД ---------------------------
<pre>
unboundMethod -&gt; T GetValue[T]()
boundMethod   -&gt; Int32 GetValue[Int32]()
викликаємо boundMethod.Invoke(box, null)...
[всередині GetValue] called for type Int32
result           -&gt; 0
result.GetType() -&gt; System.Int32
</pre>
------------------------------------------------------------

`unboundMethod` — це "шаблон" generic-методу `GetValue<T>()`, ще без конкретного
`T` (звідси `T GetValue[T]()` — `T` буквально як текст-заповнювач). Після
`MakeGenericMethod(typeof(int))` той самий метод "прив'язаний" до конкретного типу —
рядкове представлення вже показує `Int32 GetValue[Int32]()`. Виклик
`boundMethod.Invoke(box, null)` фактично виконав `box.GetValue<int>()` — видно з
рядка `[всередині GetValue] called for type Int32`, надрукованого зсередини самого
методу. `result -> 0` — це `default(int)`, саме те, що повертає тіло `GetValue<T>()`.
Другий аргумент `null` в `Invoke(box, null)` — бо у `GetValue<T>()` немає власних
параметрів (тільки типовий параметр `T`), тому масив аргументів методу порожній.

### А якщо це справді `Box<T>` — `MakeGenericType`, не `MakeGenericMethod`

Дзеркальний випадок до щойно розібраного: у `Box` "невідомість" сидить у **методі**
(`GetValue<T>`), тому закриваємо метод. У `Box<T>` (Put/Take з `Generics.md`)
"невідомість" сидить у **класі** — тому закривати треба тип, і робить це інший
метод, `Type.MakeGenericType`, не `MethodInfo.MakeGenericMethod`.

Спочатку звичне використання, для контрасту:

--------------------------- КОД ---------------------------
<pre>
public class Box&lt;T&gt;
{
    private T _value;
    public void Put(T value) =&gt; _value = value;
    public T Take() =&gt; _value;
}

Box&lt;int&gt; box = new Box&lt;int&gt;();
box.Put(42);
Console.WriteLine($"box.GetType()          -&gt; {box.GetType()}");
Console.WriteLine($"box.Take()              -&gt; {box.Take()}");
Console.WriteLine($"typeof(Box&lt;int&gt;).IsGenericType -&gt; {typeof(Box&lt;int&gt;).IsGenericType}");

MethodInfo takeMethod = typeof(Box&lt;int&gt;).GetMethod("Take");
Console.WriteLine($"takeMethod.IsGenericMethodDefinition -&gt; {takeMethod.IsGenericMethodDefinition}");
</pre>
------------------------------------------------------------

--------------------------- ВИВІД ---------------------------
<pre>
box.GetType()          -&gt; Box`1[System.Int32]
box.Take()              -&gt; 42
typeof(Box&lt;int&gt;).IsGenericType -&gt; True
takeMethod.IsGenericMethodDefinition -&gt; False
</pre>
------------------------------------------------------------

Усе рівно навпаки порівняно з `Box`: `typeof(Box<int>).IsGenericType -> True` — сам
тип знає про свій `T` (він уже "закритий" в `int` назавжди для цього `box`). А
`takeMethod.IsGenericMethodDefinition -> False` — метод `Take()` сам по собі *не*
генеричний, у нього немає власного `<T>` в кутових дужках, він просто користується
`T`, який уже зафіксований на рівні класу. `box.GetType()` друкує `Box`1[System.Int32]`
— CLR-нотація "закритого" (closed) generic-типу: `` `1 `` означає "1 типовий
параметр", `[System.Int32]` — чим він закритий.

Тепер сам сценарій — `T` невідомий до рантайму:

--------------------------- КОД ---------------------------
<pre>
Type openType = typeof(Box&lt;&gt;);
Console.WriteLine($"openType                      -&gt; {openType}");
Console.WriteLine($"openType.IsGenericTypeDefinition -&gt; {openType.IsGenericTypeDefinition}");

Type wantedType = typeof(int);
Type closedType = openType.MakeGenericType(wantedType);
Console.WriteLine($"closedType                    -&gt; {closedType}");
Console.WriteLine($"closedType.IsGenericType       -&gt; {closedType.IsGenericType}");

object instance = Activator.CreateInstance(closedType);
Console.WriteLine($"instance.GetType()            -&gt; {instance.GetType()}");

MethodInfo putOnClosed = closedType.GetMethod("Put");
MethodInfo takeOnClosed = closedType.GetMethod("Take");
Console.WriteLine($"putOnClosed                   -&gt; {putOnClosed}");
Console.WriteLine($"putOnClosed.IsGenericMethodDefinition -&gt; {putOnClosed.IsGenericMethodDefinition}");

putOnClosed.Invoke(instance, new object[] { 99 });
object result = takeOnClosed.Invoke(instance, null);
Console.WriteLine($"result           -&gt; {result}");
Console.WriteLine($"result.GetType() -&gt; {result.GetType()}");
</pre>
------------------------------------------------------------

--------------------------- ВИВІД ---------------------------
<pre>
openType                      -&gt; Box`1[T]
openType.IsGenericTypeDefinition -&gt; True
closedType                    -&gt; Box`1[System.Int32]
closedType.IsGenericType       -&gt; True
instance.GetType()            -&gt; Box`1[System.Int32]
putOnClosed                   -&gt; Void Put(Int32)
putOnClosed.IsGenericMethodDefinition -&gt; False
result           -&gt; 99
result.GetType() -&gt; System.Int32
</pre>
------------------------------------------------------------

Розбір по рядках:

- `typeof(Box<>)` — це "шаблон" самого **типу**, ще без підставленого `T`
  (`Box`1[T]`, `T` буквально як текст-заповнювач). `IsGenericTypeDefinition -> True`
  — паралель до `unboundMethod.IsGenericMethodDefinition -> True` з прикладу з
  `Box`, тільки тут "незакритим" є клас, а не метод.
- `openType.MakeGenericType(wantedType)` — метод на `Type`, не на `MethodInfo`. Він
  "закриває" типовий параметр класу — точний аналог
  `unboundMethod.MakeGenericMethod(wantedType)`, застосований на рівень вище.
- Раз тип уже закритий (`closedType` = `Box<int>`), для створення об'єкта потрібен
  `Activator.CreateInstance(closedType)` — конструктора з відомим `T` напряму
  викликати вже не можна (`new Box<int>()` не напишеш, бо `int` тут — змінна,
  невідома під час компіляції).
- `putOnClosed.IsGenericMethodDefinition -> False` — ключове: `Put`/`Take` на
  `closedType` вже **не** генеричні методи, бо `T` на них зафіксований
  типом-власником. Тому їх викликають звичайним `Invoke`, без жодного
  `MakeGenericMethod` — на цьому кроці він просто не потрібен.

**Підсумок контрасту:** у `Box` "невідомість" сидить у методі → закриваєш метод
(`MakeGenericMethod`). У `Box<T>` "невідомість" сидить у класі → закриваєш тип
(`MakeGenericType`), а вже потім звичайний `Invoke` на звичному (не-generic) методі
закритого типу.

## Підсумок: типи `System.Reflection`, які тут з'явились

Усе, чим у цьому файлі реально користувались вище — жоден метод/властивість тут не
згаданий "про запас", лише те, що застосовано в прикладах.

### `Type` (namespace `System`)

Опис одного типу (класу, структури, інтерфейсу) як об'єкта — метадані про сам тип,
без прив'язки до конкретного екземпляра.

- **Як отримати:** `typeof(Robot)` (тип відомий у коді на етапі компіляції, Крок 1)
  або `obj.GetType()` (реальний тип конкретного об'єкта в рантаймі — див. окремий
  розбір різниці `typeof` vs `GetType()` вище в чаті цього уроку).
- **Властивості:**
  - `Name` — коротке ім'я типу (`"Robot"`), без неймспейсу.
  - `FullName` — повне ім'я з неймспейсом (`"MyGame.Robot"`, якщо є `namespace`).
  - `IsGenericType` — чи тип generic (для `Box<int>` → `True`, для звичайного `Box`
    → `False`).
  - `IsGenericTypeDefinition` — чи це "незакритий шаблон" типу (`typeof(Box<>)` →
    `True`; закритий `Box<int>` → `False`).
- **Методи:**
  - `GetConstructors()` → `ConstructorInfo[]` — усі публічні конструктори типу.
  - `GetMethod(string name)` → `MethodInfo` — конкретний метод типу за іменем.
  - `MakeGenericType(Type[] typeArguments)` → `Type` — закриває generic **клас**
    (`Box<>` → `Box<int>`), коли конкретний тип відомий лише в рантаймі.

### `ConstructorInfo` (namespace `System.Reflection`)

Опис одного конкретного конструктора — які в нього параметри, як його викликати.

- **Властивості:** `Name` — завжди `.ctor` (службова назва, однакова для всіх
  конструкторів усіх класів, ім'я самого класу тут не зберігається).
- **Методи:**
  - `GetParameters()` → `ParameterInfo[]` — параметри саме цього конструктора.
  - `Invoke(object[] parameters)` → `object` — реально створює новий об'єкт,
    еквівалент `new Robot(...)`, але з параметрами, невідомими на етапі компіляції.

### `ParameterInfo` (namespace `System.Reflection`)

Опис одного параметра методу/конструктора.

- **Властивості:** `Name` (ім'я параметра, `"name"`/`"power"`), `ParameterType`
  (`Type` цього параметра — `System.String`, `System.Int32`).

### `MethodInfo` (namespace `System.Reflection`)

Опис одного конкретного методу — окремо від `ConstructorInfo`, бо метод (на відміну
від конструктора) може сам бути generic.

- **Властивості:** `IsGenericMethodDefinition` — чи в цього методу є власний,
  ще не закритий типовий параметр (`GetValue<T>()` → `True`; звичайний `Take()` на
  закритому `Box<int>` → `False`, бо `T` там уже зафіксований класом, не методом).
- **Методи:**
  - `MakeGenericMethod(Type[] typeArguments)` → `MethodInfo` — закриває generic
    **метод** (`GetValue<T>` → `GetValue<int>`), коли `T` відомий лише в рантаймі.
  - `Invoke(object obj, object[] parameters)` → `object` — викликає метод на
    конкретному об'єкті (`obj`) із цими аргументами; для методу без власних
    параметрів (лише типовий `T`) — `parameters` це `null`.

### `Activator` (namespace `System`)

Статичний клас-фабрика, не потребує власного екземпляра.

- **Методи:** `CreateInstance(Type type)` → `object` — створює новий об'єкт
  закритого типу (`Box<int>`), коли сам конструктор напряму викликати не можна
  (`new Box<int>()` не напишеш, якщо `int` — змінна, невідома на етапі компіляції).

## Пов'язане

- [`Debug_Logging_and_Reflection.md`](Debug_Logging_and_Reflection.md) — найпростіша
  форма рефлексії, вже використана в проєкті (`GetType().Name` для логів FSM).
