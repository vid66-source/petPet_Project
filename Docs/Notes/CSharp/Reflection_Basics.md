# Рефлексія — довідник за задачами

Це не інструкція до конкретного ДЗ і не готовий розв'язок — довідник по API
`System.Reflection`, організований за принципом **"хочу зробити X → який API,
що він повертає, коли застосовувати"**. Кожен розділ самодостатній. Приклади —
навмисно ізольовані, непов'язані з жодним конкретним класом проєкту. Кожен блок
"ВИВІД" — реально запущений код (окремий консольний .NET-проєкт), вивід
скопійований з консолі, не вигаданий.

## Мапа задач

| Хочу дізнатись/зробити | API | Повертає | Розділ |
|---|---|---|---|
| Тип об'єкта, відомий у коді | `typeof(X)` | `Type` | 1 |
| Тип об'єкта, відомий лише в рантаймі | `obj.GetType()` | `Type` | 1 |
| Усі публічні конструктори типу | `type.GetConstructors()` | `ConstructorInfo[]` | 2 |
| Конструктор із заздалегідь відомою сигнатурою | `type.GetConstructor(Type[])` | `ConstructorInfo?` | 2 |
| Параметри конструктора/методу | `ctor.GetParameters()` | `ParameterInfo[]` | 3 |
| Тип конкретного параметра | `parameterInfo.ParameterType` | `Type` | 3 |
| Створити об'єкт через конструктор | `ctor.Invoke(object[])` | `object` | 4 |
| Метод типу за іменем | `type.GetMethod(name)` | `MethodInfo` | 5 |
| Викликати метод на існуючому об'єкті | `method.Invoke(instance, object[])` | `object` | 5 |
| "Закрити" generic-**метод** конкретним типом | `methodInfo.MakeGenericMethod(Type[])` | `MethodInfo` | 6 |
| "Закрити" generic-**клас** конкретним типом | `typeInfo.MakeGenericType(Type[])` | `Type` | 7 |
| Створити об'єкт закритого generic-типу | `Activator.CreateInstance(Type)` | `object` | 7 |
| Чи тип — сам шаблон / закритий / частково закритий | `IsGenericTypeDefinition`, `ContainsGenericParameters` | `bool` | 8 |
| Обмеження (`where`) generic-параметра | `typeParam.GetGenericParameterConstraints()`, `.GenericParameterAttributes` | `Type[]`, flags | 9 |
| Обрати з кількох конструкторів "найжадібніший", який можна виконати | `GetConstructors()` + `Array.Sort` + перевірка параметрів | `ConstructorInfo` | 10 |
| Прочитати будь-яку форму `[]` у цьому файлі | — | — | 0 |

---

## 0. Масиви й `[]` — як читати всі форми в цьому файлі

### Чому в рефлексії стільки масивів

| Причина | Що з цього випливає |
|---|---|
| Кількість параметрів/конструкторів відома лише в рантаймі, але після отримання вже не змінюється | Масив — фіксованої довжини, з доступом за індексом; рости йому не треба |
| Значення різних типів (`string` і `int`) треба передати в одному аргументі | Масив елементів спільного типу `object` → `object[]` (розділ 4) |
| `System.Reflection` існує з .NET 1.0 — до появи generics і `List<T>` | Старі API приймають і повертають масиви, а не колекції |

### Квадратні дужки в різних місцях означають різне

| Де стоять `[]` | Приклад з цього файлу | Що означає |
|---|---|---|
| Після **типу** | `ConstructorInfo[] all`, `Type[] types`, `object[] args` | Тип "масив елементів цього типу". Довжини тут нема — довжина належить об'єкту, не змінній. |
| Після **`new Тип`**, з числом | `new object[parameters.Length]` | Створити масив заданої довжини, заповнений значеннями за замовчуванням. |
| Після `new Тип`, без числа, з `{ }` | `new object[] { 5 }` | Створити масив із перелічених елементів; довжину рахує компілятор. |
| Після `new`, без типу | `new[] { typeof(string), typeof(int) }` | Те саме, але тип елементів компілятор виводить сам зі значень. |
| Лише `{ }`, без `new` | `object[] args = { "R2D2", 100 };` | Скорочення попереднього — **тільки** в рядку оголошення змінної. |
| Після **змінної** | `args[i]`, `parameters[i].ParameterType`, `GetConstructors()[0]` | Доступ до елемента за індексом (з 0). |
| У виводі консолі | ``Box`1[System.Int32]``, `[[System.String, …]]` | Не синтаксис C#, а текстове ім'я типу від .NET (див. нижче). |

### Створення масиву — усі форми

```csharp
object[] sized = new object[3];
int[] sizedInts = new int[3];
object[] full = new object[] { "R2D2", 100 };
Type[] inferred = new[] { typeof(string), typeof(int) };
object[] shortForm = { "R2D2", 100 };
Type[] empty = Type.EmptyTypes;
```

Що реально лежить у кожній змінній після цих рядків (перевірено запуском: для
кожної змінної надруковано `.GetType()`, `.Length` і кожен елемент):

| Змінна | `.GetType()` | `.Length` | Елементи |
|---|---|---|---|
| `sized` | `System.Object[]` | 3 | `null`, `null`, `null` |
| `sizedInts` | `System.Int32[]` | 3 | `0`, `0`, `0` |
| `full` | `System.Object[]` | 2 | `"R2D2"`, `100` |
| `inferred` | `System.Type[]` | 2 | `System.String`, `System.Int32` |
| `shortForm` | `System.Object[]` | 2 | `"R2D2"`, `100` |
| `empty` | `System.Type[]` | 0 | — |

Що з цього видно:
- `new T[n]` створює `n` клітинок, але нічого в них не кладе. Там лежить значення за
  замовчуванням: `null` для посилальних типів (`object`, `string`, будь-який клас) і `0`
  для `int`.
- `full` і `shortForm` однакові: `{ … }` без `new` — лише коротший запис того самого.
- `inferred` отримав тип `Type[]`, хоча після `new` тип не написаний — див. підрозділ нижче.

| Форма | Коли брати |
|---|---|
| `new T[n]` | Довжина відома, значення будуть пізніше: `new object[parameters.Length]`, потім цикл `args[i] = …` (розділ 4). Порожні клітинки — `null` для класів, `0` для `int`. |
| `new T[] { … }` | Значення відомі одразу, і масив передається прямо в аргумент методу, без окремої змінної: `method.Invoke(target, new object[] { 5 })` (розділ 5). |
| `new[] { … }` | Те саме, коли тип очевидний зі значень і писати його вдруге зайве. |
| `T[] x = { … }` | Значення відомі одразу, і є рядок оголошення змінної. |
| `Type.EmptyTypes` | API вимагає `Type[]`, а передати нічого (розділ 2). |

Запис і читання за індексом:

```csharp
sized[1] = "filled";
object first = sized[0];
object second = sized[1];
```

Після цього `first` — `null` (клітинку 0 ніхто не заповнював), `second` — `"filled"`,
довжина `sized` так і лишилась 3.

### `new[]` без типу — звідки компілятор знає тип масиву

Тип масиву можна не писати: `new[] { 1, 2 }` замість `new int[] { 1, 2 }`. Тоді
компілятор дивиться на тип значень у дужках і робить масив цього типу — **виведення
типу** (type inference, та сама ідея, що в `var`). Тип при цьому такий самий суворий,
його лише вирахував компілятор:

```csharp
int[] numbers = new[] { 1, 2 };
string[] words = new[] { "a", "b" };
Type[] types = new[] { typeof(string), typeof(int) };
```

```
System.Int32[]
System.String[]
System.Type[]
```

Пастка в третьому рядку: `typeof(string)` — **не рядок**, а об'єкт-опис типу `string`
(його ім'я, конструктори, методи), і цей об'єкт має тип `Type` (розділ 1). Обидва
елементи — `Type`, тому й масив `Type[]`:

| Запис | Що в масиві | Тип масиву |
|---|---|---|
| `new[] { "a", "b" }` | два **рядки** | `string[]` |
| `new[] { typeof(string), typeof(int) }` | два **описи типів** | `Type[]` |

Навіщо масив описів типів: `GetConstructor(Type[])` (розділ 2) шукає конструктор за
списком **типів** параметрів — "знайди конструктор, що приймає (`string`, `int`)", — а
не за значеннями.

Якщо надрукувати `typeof(string).GetType()`, вийде `System.RuntimeType`, а не
`System.Type`: це внутрішній клас .NET, що успадковує `Type`. Думати про нього можна
просто як про `Type` — тому `Type t = typeof(string);` і компілюється.

Якщо значення в дужках різних типів (`"R2D2"` і `100`), вивести тип не вийде — див.
помилку (2) нижче.


### Що не компілюється (перевірено з `LangVersion 9.0`, як у Unity-проєкті)

```csharp
object[] args;
args = { "R2D2", 100 };                              // (1)
object[] mixed = new[] { "R2D2", 100 };              // (2)
object[] wrongSize = new object[3] { "R2D2", 100 };  // (3)
int[] modern = [1, 2, 3];                            // (4)
```

```
(1) error CS1525: Invalid expression term '{'
(2) error CS0826: No best type found for implicitly-typed array
(3) error CS0847: An array initializer of length '3' is expected
(4) error CS8773: Feature 'collection expressions' is not available in C# 9.0. Please use language version 12.0 or greater.
```

| № | Чому |
|---|---|
| 1 | Голі `{ }` дозволені лише при оголошенні; для присвоєння пізніше потрібен `new object[] { … }`. |
| 2 | `new[]` виводить тип зі значень, а в `string` і `int` спільного типу, крім `object`, компілятор сам не обирає — треба явно `new object[] { … }`. |
| 3 | Якщо вказано і довжину, і елементи, вони мусять збігатися. |
| 4 | `[1, 2, 3]` — collection expressions з C# 12. У свіжій документації .NET їх видно часто, але в Unity (C# 9) вони не компілюються. |

### `params` — масив, якого не видно у виклику

Частина API рефлексії оголошена з `params Type[]`. Тому в розділах 6–7
`MakeGenericMethod(typeof(int))` викликається з **одним** `Type`, хоча в мапі
задач вгорі написано `MakeGenericMethod(Type[])`: масив збирає компілятор.

```csharp
static int Sum(params int[] numbers)
{
    Console.WriteLine($"  Sum got {numbers.GetType()} Length={numbers.Length}");
    int total = 0;
    foreach (int n in numbers)
        total += n;
    return total;
}

Console.WriteLine(Sum(1, 2, 3));
Console.WriteLine(Sum(new int[] { 1, 2, 3 }));
Console.WriteLine(Sum());
```

```
  Sum got System.Int32[] Length=3
6
  Sum got System.Int32[] Length=3
6
  Sum got System.Int32[] Length=0
0
```

Усередині методу завжди звичайний масив. Окремі аргументи компілятор пакує в
масив сам, готовий масив передається як є, а виклик без аргументів дає масив
довжини 0.

`ConstructorInfo.Invoke(object[])` оголошено **без** `params`, тому там масив
завжди створюється вручну.

### `[]` у виводі консолі — ім'я типу від .NET, не C#

**Де ти це побачиш.** Коли друкуєш об'єкт `Type` через `Console.WriteLine`/`Debug.Log`,
а також у стек-трейсах помилок, наприклад ``ListBuilder`1[[System.__Canon, …]]`` у
`Stack overflow` з розділу 10. Квадратні дужки там — не масиви й не синтаксис C#.

**Клас для прикладу.** `Box<T>` — найпростіший generic-клас, оголошений тут лише для
демонстрації: "коробка", що зберігає одне значення типу `T`. `Box<int>` — коробка для
`int`, `Box<string>` — для `string`. (Generics — у [`Generics.md`](Generics.md).)

```csharp
public class Box<T>
{
    public T Value;
}
```

`typeof(X)` повертає об'єкт `Type`, що описує тип `X` (докладніше — розділ 1).
Друкуємо його трьома способами й порівнюємо зі звичними типами:

```csharp
Type boxOfInt = typeof(Box<int>);
Console.WriteLine(boxOfInt.Name);
Console.WriteLine(boxOfInt);
Console.WriteLine(boxOfInt.FullName);

Type intArray = typeof(int[]);
Console.WriteLine(intArray);

Type listOfString = typeof(List<string>);
Console.WriteLine(listOfString);

Type dictionary = typeof(Dictionary<string, int>);
Console.WriteLine(dictionary);
```

```
Box`1
Box`1[System.Int32]
Box`1[[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
System.Int32[]
System.Collections.Generic.List`1[System.String]
System.Collections.Generic.Dictionary`2[System.String,System.Int32]
```

| Рядок виводу | Звідки | Як читати |
|---|---|---|
| ``Box`1`` | `.Name` — коротке ім'я | `` `1 `` = "generic-тип з **одним** типовим параметром". Запис `Box<T>` існує лише в C#; у самому .NET тип називається ``Box`1``. Чим закрито `T`, `.Name` не каже. |
| ``Box`1[System.Int32]`` | `Console.WriteLine(type)` — те, що бачиш найчастіше | `[System.Int32]` — чим закрито `T`: це `Box<int>`. `System.Int32` — справжнє ім'я `int` у .NET. |
| ``Box`1[[System.Int32, System.Private.CoreLib, …]]`` | `.FullName` — повне ім'я | Зовнішні `[ ]` — список типових аргументів; внутрішні `[ ]` — один аргумент разом зі збіркою (`.dll`), з якої він узятий. Довгий хвіст `Version=…, PublicKeyToken=…` — паспорт цієї збірки, для читання його можна пропускати. |
| `System.Int32[]` | `typeof(int[])` | Єдиний випадок, де `[]` означає те саме, що в C#: масив `int`. |
| ``List`1[System.String]`` | `typeof(List<string>)` | Те саме правило для вбудованого класу: `List<string>`. |
| ``Dictionary`2[System.String,System.Int32]`` | `typeof(Dictionary<string, int>)` | `` `2 `` — два типові параметри; в дужках обидва через кому: `Dictionary<string, int>`. |

Правило перекладу назад у C#: ``Ім'я`N[A,B]`` → `Ім'я<A, B>`.

---

## 1. Тип об'єкта — `Type`

Два способи дістати `Type`, залежно від того, коли тип відомий:

- `typeof(X)` — тип відомий **у коді**, на етапі компіляції.
- `obj.GetType()` — тип відомий лише в рантаймі, з конкретного вже наявного об'єкта.

```csharp
public class Robot
{
    public Robot(string name, int power)
    {
        Console.WriteLine($"Robot {name} created with power {power}");
    }
}

Type type = typeof(Robot);
Console.WriteLine(type.Name);      // коротке ім'я
Console.WriteLine(type.FullName);  // з неймспейсом, якщо є
```

```
Robot
Robot
```

Обидва однакові, бо `Robot` тут — top-level клас без `namespace`. З `namespace
MyGame` — `FullName` показав би `MyGame.Robot`, `Name` лишився б просто `Robot`.

## 2. Конструктори типу — `ConstructorInfo`

### Усі конструктори одразу

`GetConstructors()` (множина) — повертає **всі** публічні конструктори типу, без
фільтрів:

```csharp
ConstructorInfo[] all = typeof(Robot).GetConstructors();
Console.WriteLine(all.Length); // скільки їх
Console.WriteLine(all[0]);     // сигнатура першого
```

```
1
Void .ctor(System.String, Int32)
```

`Void .ctor(System.String, Int32)` — текстове представлення сигнатури: `Void`
(конструктор "нічого не повертає"), `.ctor` (службова назва — **однакова для
всіх** конструкторів усіх класів, самого імені `Robot` тут нема взагалі). Це лише
`ToString()` для читання людиною — не шлях доступу до властивостей на кшталт
`constructor.System.String` (такого не існує; типи параметрів дістаються окремо,
розділ 3).

### Конструктор із заздалегідь відомою сигнатурою

`GetConstructor(Type[] types)` (однина) — шукає **точно** ту сигнатуру, яку йому
назвали. Повертає один `ConstructorInfo` або `null`, якщо такого нема:

```csharp
ConstructorInfo? noArgs = typeof(Robot).GetConstructor(Type.EmptyTypes);
Console.WriteLine(noArgs == null); // у Robot нема конструктора без параметрів

ConstructorInfo? matched = typeof(Robot).GetConstructor(new[] { typeof(string), typeof(int) });
Console.WriteLine(matched);
```

```
True
Void .ctor(System.String, Int32)
```

`Type.EmptyTypes` — готове, спільне для всього .NET, статичне поле типу `Type[]`
довжини 0 — скорочення замість `new Type[0]`, коли API вимагає `Type[]`, а
сказати нема чого (конструктор/метод без параметрів). Перевірено запуском:

```csharp
Console.WriteLine(Type.EmptyTypes.GetType());
Console.WriteLine(Type.EmptyTypes.Length);
Console.WriteLine(ReferenceEquals(Type.EmptyTypes, Type.EmptyTypes));

Type[] manual = new Type[0];
Console.WriteLine(ReferenceEquals(Type.EmptyTypes, manual));
Console.WriteLine(ReferenceEquals(manual, new Type[0]));
```

```
System.Type[]
0
True
False
False
```

`ReferenceEquals(Type.EmptyTypes, Type.EmptyTypes) -> True` доводить: це не
"конструктор масиву", а вже готовий, **один і той самий** об'єкт щоразу, коли
до нього звертаються. `new Type[0]`, навпаки, щоразу створює новий об'єкт-масив
нульової довжини — два окремі `new Type[0]` між собою теж не той самий об'єкт
(`ReferenceEquals -> False`). Функціонально для `GetConstructor` різниці немає
(обидва варіанти знаходять той самий безпараметровий конструктор), але
`Type.EmptyTypes` не виділяє нову пам'ять щоразу, `new Type[0]` — виділяє.

**Коли застосовний цей метод, а коли ні:** тільки коли типи параметрів **уже
відомі** в коді, що пише виклик. Якщо мета — навпаки, дізнатись, чого хоче
конструктор, не знаючи наперед — цей метод не підходить, потрібен
`GetConstructors()` (множина) + розділ 3.

### Вибір одного з масиву — `[0]` проти `.Single()`

Коли `GetConstructors()` повернув масив, а працювати треба з одним елементом:

```csharp
public class TwoCtors
{
    public TwoCtors() { }
    public TwoCtors(string a) { }
}

ConstructorInfo a = typeof(TwoCtors).GetConstructors()[0];       // мовчки бере перший
ConstructorInfo b = typeof(TwoCtors).GetConstructors().Single(); // вимагає РІВНО 1
```

```
a -> Void .ctor()
b -> System.InvalidOperationException: Sequence contains more than one element
```

`[0]` бере перший елемент масиву, яким би він не був — мовчки, без перевірки, чи
він єдиний. `.Single()` (LINQ, `System.Linq`) вимагає, щоб елемент був **рівно
один** — і кидає виняток, якщо їх більше (чи менше). Різниця має значення, коли
код **розрахований** на "рівно один конструктор": `.Single()` цю умову документує
й гучно ловить порушення; `[0]` — ні.

Заміряно окремо (мільйон ітерацій, після прогріву JIT): `.Single()` на масиві
**не виділяє додаткової пам'яті в купі** — LINQ має швидкий шлях для джерел, що
реалізують `IList<T>` (масив реалізує), і не створює enumerator. Єдина реальна
витрата — сам `GetConstructors()` (масив), однакова незалежно від того, `[0]` чи
`.Single()` йде після нього.

## 3. Параметри конструктора/методу — `ParameterInfo`

```csharp
ConstructorInfo constructor = typeof(Robot).GetConstructors()[0];
ParameterInfo[] parameters = constructor.GetParameters();

foreach (ParameterInfo p in parameters)
    Console.WriteLine($"{p.Name}: {p.ParameterType}");
```

```
name: System.String
power: System.Int32
```

`GetParameters()` повертає масив **описів** параметрів — не значень. `.Name` —
ім'я параметра як текст. `.ParameterType` — той самий `Type`-об'єкт, що й у
розділі 1, тільки для типу параметра. Це дані **про форму** конструктора; самі
значення для виклику — окрема річ, розділ 4.

## 4. Створення об'єкта через конструктор — `Invoke`

`ConstructorInfo.Invoke(object[] args)` — реально виконує конструктор і повертає
новий об'єкт:

```csharp
object[] args = { "R2D2", 100 };
object robot = constructor.Invoke(args);
Console.WriteLine(robot.GetType());
```

```
Robot R2D2 created with power 100
Robot
```

Перший рядок виводу — `Console.WriteLine` **зсередини самого конструктора**
`Robot` — доказ, що виконався реальний конструктор, не підробка.

**Найчастіша помилка:** плутати `args` (`object[]`, реальні значення) із
`parameters` (`ParameterInfo[]` з розділу 3, лише опис). `Invoke` очікує
значення для підстановки, не опис того, чого очікує конструктор.

### Чому саме `object[]`, а не конкретні типи

Масив у C# завжди однотипний, а `args` тут містить `string` і `int` одночасно —
єдиний спільний тип для будь-чого в C# це `object`. Для значеннєвих типів
(`int`) це означає **boxing** — прихована обгортка в купі, щоб `100` взагалі
можна було покласти в змінну типу `object`:

```csharp
object boxed = 100;
Console.WriteLine(boxed.GetType()); // обгортка пам'ятає реальний тип
```

```
System.Int32
```

Сама сигнатура `Invoke(object[] parameters)` — рішення авторів `System.Reflection`,
не вибір розробника: `Invoke` мусить працювати з **будь-яким** конструктором
**будь-якого** класу, типи параметрів яких заздалегідь невідомі.

**Ціна цієї гнучкості — нуль перевірки типів компілятором.** Помилки
з'ясовуються лише в рантаймі, як виняток:

```
переплутаний порядок аргументів   -> System.ArgumentException: Object of type 'System.Int32' cannot be converted to type 'System.String'.
неправильна кількість аргументів -> System.Reflection.TargetParameterCountException: Parameter count mismatch.
```

Звичайний `new Robot(100, "R2D2")` такого типу помилку компілятор впіймав би
одразу; з рефлексією цей захист зникає.

### Побудова масиву аргументів, коли кількість параметрів заздалегідь невідома

Якщо код працює з довільним конструктором (кількість параметрів невідома
наперед), масив аргументів варто будувати за розміром `parameters`, а не
перевіряти окремо "а раптом їх нуль":

```csharp
object[] args = new object[parameters.Length]; // 0 параметрів -> порожній масив сам собою
// ... заповнити args[i] для кожного parameters[i] ...
object instance = constructor.Invoke(args);
```

Перевірено: і `null`, і порожній `new object[0]` спрацьовують на `Invoke` так
само, як і заповнений масив — окремого `if` для "0 параметрів" не треба.
Конкретний приклад із самим конструктором без параметрів:

```csharp
public class Drone
{
    public Drone()
    {
        Console.WriteLine("Drone created, no parameters needed");
    }
}

ConstructorInfo constructor = typeof(Drone).GetConstructors()[0];
ParameterInfo[] parameters = constructor.GetParameters();
Console.WriteLine($"parameters.Length = {parameters.Length}");

object[] args = new object[parameters.Length];
Console.WriteLine($"args.Length = {args.Length}");

object instance = constructor.Invoke(args);
Console.WriteLine(instance.GetType());
```

```
parameters.Length = 0
args.Length = 0
Drone created, no parameters needed
Drone
```

`GetParameters()` для конструктора без параметрів повертає порожній
`ParameterInfo[]` (`Length = 0`, не `null`) → `new object[0]` — так само легальний
`object[]`, просто порожній → `Invoke` з порожнім масивом виконує конструктор без
жодної підстановки значень. Спецвипадку "0 параметрів" немає в самому API:
`parameters.Length == 0` природно дає `args.Length == 0`, і `Invoke` однаково
обробляє масив будь-якого розміру.

### Рекурсивне resolve параметрів, коли їхній тип теж потребує побудови

Якщо конструктор класу сам приймає параметр, який теж треба створити через
власний конструктор (а не просто підставити готове значення) — той самий підхід
з розділу 4 застосовується рекурсивно, за `ParameterType` кожного параметра:

```csharp
public class Engine
{
    public Engine() { Console.WriteLine("Engine created"); }
}

public class Car
{
    public Car(Engine engine)
    {
        Console.WriteLine($"Car created with {engine.GetType().Name}");
    }
}

static object ResolveRecursively(Type type)
{
    ConstructorInfo constructor = type.GetConstructors()[0];
    ParameterInfo[] parameters = constructor.GetParameters();
    object[] args = new object[parameters.Length];
    for (int i = 0; i < parameters.Length; i++)
        args[i] = ResolveRecursively(parameters[i].ParameterType); // рекурсія за ParameterType
    return constructor.Invoke(args);
}

object car = ResolveRecursively(typeof(Car));
```

```
Engine created
Car created with Engine
```

`parameters[i].ParameterType` — це вираз (property access на елементі масиву), не
іменована змінна; аргументом методу може бути будь-який вираз потрібного типу,
компілятору байдуже, названо проміжне значення чи ні (`ResolveRecursively(x)` і
`ResolveRecursively(parameters[i].ParameterType)` — однаково легальні).

Рекурсія зупиняється сама, без явної умови виходу: на типі, у якого
`parameters.Length == 0` (тут — `Engine`), цикл `for` не робить жодної ітерації,
вкладеного виклику не буде, і `Invoke` одразу повертає готовий об'єкт назад у
той призупинений виклик, що на нього чекав.

**Межа підходу:** рекурсія за `ParameterType` працює, лише поки кожен параметр —
конкретний клас із власним конструктором, який рефлексія може викликати
безпосередньо. Щойно параметр — інтерфейс, той самий метод ламається на тому ж
місці, що й розділ 2 (`[0]` на порожньому масиві):

```csharp
public interface IEngine { }
public class SmartCar
{
    public SmartCar(IEngine engine)
    {
        Console.WriteLine($"SmartCar created with {engine.GetType().Name}");
    }
}

object smartCar = ResolveRecursively(typeof(SmartCar));
```

```
System.IndexOutOfRangeException: Index was outside the bounds of the array.
```

Причина та сама, що й для `typeof(IEngine).GetConstructors()` у розділі 2:
інтерфейс не має жодного конструктора, масив порожній, `[0]` на порожньому
масиві падає (тут навіть менш промовисто, ніж `.Single()` — саме повідомлення
нічого не каже про "чому"). `ParameterType` дає лише сам `Type` інтерфейсу — не
дає (і не може дати) інформацію про те, який клас цей інтерфейс реалізує; такої
відповідності в метаданих самого інтерфейсу не існує.

## 5. Виклик методу на вже створеному об'єкті — `GetMethod` + `Invoke`

Інша задача, ніж розділ 4: не створити об'єкт, а викликати на **вже готовому**
об'єкті метод, ім'я якого відоме лише як рядок.

```csharp
public class Calculator
{
    public Calculator(int start) { Value = start; }
    public int Value { get; private set; }
    public int Add(int amount) { Value += amount; return Value; }
}

object calc = typeof(Calculator).GetConstructors()[0].Invoke(new object[] { 10 });

MethodInfo addMethod = typeof(Calculator).GetMethod("Add");
object result = addMethod.Invoke(calc, new object[] { 5 }); // на calc, з аргументом 5
Console.WriteLine(result);
```

```
15
```

Ключова відмінність сигнатур: `ConstructorInfo.Invoke(object[] args)` — **один**
аргумент (конструктор завжди створює новий об'єкт). `MethodInfo.Invoke(object
obj, object[] args)` — **два**: перший каже, **на якому** об'єкті викликати,
другий — аргументи самого методу.

## 6. Generic-метод, коли тип-параметр невідомий заздалегідь — `MakeGenericMethod`

Стосується класу **без** `<T>` на собі, у якого generic лише один конкретний
метод (типовий параметр належить методу, не класу):

```csharp
public class Box               // звичайний клас, БЕЗ <T>
{
    public T GetValue<T>() => default(T); // а метод -- generic
}

MethodInfo unbound = typeof(Box).GetMethod("GetValue");
Console.WriteLine(unbound.IsGenericMethodDefinition); // ще не закритий

MethodInfo bound = unbound.MakeGenericMethod(typeof(int)); // закрили T = int
object result = bound.Invoke(new Box(), null); // без параметрів методу -> null
Console.WriteLine(result);
```

```
True
0
```

`unbound` — "шаблон" методу, ще без конкретного `T`. `MakeGenericMethod(typeof(int))`
підставляє `T = int` і повертає новий, уже "закритий" `MethodInfo`, готовий до
`Invoke`. Це застосовується, коли клас сам звичайний, але **окремий його метод**
generic, і потрібний тип для нього відомий лише в рантаймі (наприклад, узятий з
`ParameterType` якогось параметра, розділ 3) — типовий приклад: узагальнений
метод-геттер зі сховища об'єктів за типом (у проєкті так само влаштований
`AllServices.GetService<TService>()`).

## 7. Generic-тип, коли сам тип невідомий заздалегідь — `MakeGenericType`

Дзеркальний випадок до розділу 6: тепер "невідомість" сидить у **класі**
(`Box<T>`), не в окремому методі:

```csharp
public class Box<T>
{
    private T _value;
    public void Put(T value) => _value = value;
    public T Take() => _value;
}

Type openType = typeof(Box<>);                    // шаблон, T ще не підставлений
Type closedType = openType.MakeGenericType(typeof(int)); // Box<int>

object instance = Activator.CreateInstance(closedType); // new Box<int>() не напишеш -- int невідомий у коді
closedType.GetMethod("Put").Invoke(instance, new object[] { 99 });
object result = closedType.GetMethod("Take").Invoke(instance, null);
Console.WriteLine(result);
```

```
99
```

`MakeGenericType` — метод на `Type`, аналог `MakeGenericMethod` рівнем вище: не
метод закриває свій `T`, а весь тип. Раз тип уже закритий (`Box<int>`), звичайний
конструктор `new Box<int>()` написати не можна (`int` — змінна, не літерал у
коді) — тому потрібен `Activator.CreateInstance`. Методи (`Put`/`Take`) на вже
закритому типі — **не** generic самі по собі (`IsGenericMethodDefinition ->
False`), викликаються звичайним `Invoke`, без `MakeGenericMethod`.

**Підсумок контрасту розділів 6-7:** невідомість у методі → закриваєш метод
(`MakeGenericMethod`). Невідомість у класі → закриваєш тип (`MakeGenericType`),
а методи на закритому типі виконуються вже звичайним шляхом.

## 8. Стан generic-типу: шаблон / закритий / частково закритий

`IsGenericTypeDefinition` — популярна помилка: думати, що вона відповідає на
"закритий цей тип чи ні". Насправді станів **три**:

| Стан | Приклад | `IsGenericTypeDefinition` | `ContainsGenericParameters` |
|---|---|---|---|
| **Шаблон** (generic type definition) | `typeof(Box<>)` | **True** | True |
| **Закритий** (closed constructed) | `typeof(Box<int>)` | False | **False** |
| **Відкритий сконструйований** (open constructed) — тип підставлений частково | `List<T>`, де `T` ще не підставлений (наприклад, узятий із поля `typeof(Outer<>)`) | False | **True** |

```
typeof(Box<>)     -> IsGenericTypeDefinition True,  ContainsGenericParameters True
typeof(Box<int>)  -> IsGenericTypeDefinition False, ContainsGenericParameters False
List<T> (T не підставлений) -> IsGenericTypeDefinition False, ContainsGenericParameters True
```

Пастка видно в рядках 2 і 3: **обидва** дають `IsGenericTypeDefinition -> False`,
хоча в третьому `T` явно не підставлений. `IsGenericTypeDefinition` перевіряє
лише "це сам шаблон, чи вже якась його підстановка" — не розрізняє повну й
часткову підстановку. Питання "чи можна взагалі створити екземпляр цього типу"
відповідає **`ContainsGenericParameters`**.

## 9. Обмеження (`where`) generic-параметра в рефлексії

`where T : class, ISomething` розпадається у рефлексії на дві різні речі, і CLR
сам перевіряє їх під час `MakeGenericMethod` — **до** `Invoke`:

```csharp
public interface IService { }
public class GoodService : IService { }
public class NotAService { } // не реалізує IService

public class Container
{
    public T GetService<T>() where T : class, IService => null;
}

MethodInfo unbound = typeof(Container).GetMethod("GetService");
Type tParam = unbound.GetGenericArguments()[0];
Console.WriteLine(tParam.GenericParameterAttributes);       // "class"
Console.WriteLine(tParam.GetGenericParameterConstraints()[0]); // IService

unbound.MakeGenericMethod(typeof(GoodService));  // ОК
unbound.MakeGenericMethod(typeof(NotAService));  // ?
```

```
ReferenceTypeConstraint
IService
System.ArgumentException: GenericArguments[0], 'NotAService', on 'T GetService[T]()' violates the constraint of type 'T'.
```

`class` (обмеження "reference-тип") стає прапорцем `GenericParameterAttributes`
(`ReferenceTypeConstraint`); `IService` (обмеження "реалізує цей інтерфейс") —
окремим записом у `GetGenericParameterConstraints()`. Обидва читаються ще **до**
підстановки. Сама перевірка стається на `MakeGenericMethod`, не на `Invoke`: тип,
що не задовольняє `where`, ламає виклик одразу, ще до виконання методу — навіть
коли компілятор цю помилку перевірити не міг (тип підставляється рефлексією, не
в коді).

## 10. Вибір одного конструктора з кількох — "жадібна" стратегія

Розділ 2 дає два способи взяти один конструктор: `[0]` (мовчки довільний) і
`.Single()` (вимагає рівно один). Третій спосіб — як у справжніх DI-контейнерах
(Microsoft.Extensions.DependencyInjection, Autofac): **відсортувати конструктори
від найбільшої кількості параметрів до найменшої і взяти перший, для якого кожен
параметр вдається отримати**.

"Отримати параметр" тут означає одне з двох:
- тип уже є в словнику готових об'єктів (зареєстрований);
- тип — конкретний клас, і хоча б один його конструктор теж можна виконати (рекурсивно).

### Код

```csharp
static Dictionary<Type, object> registered = new Dictionary<Type, object>();

static object ResolveRecursively(Type type)
{
    if (registered.TryGetValue(type, out object existing))
        return existing;

    ConstructorInfo constructor = PickGreediestConstructor(type);
    ParameterInfo[] parameters = constructor.GetParameters();
    object[] args = new object[parameters.Length];
    for (int i = 0; i < parameters.Length; i++)
        args[i] = ResolveRecursively(parameters[i].ParameterType);
    return constructor.Invoke(args);
}

static int MoreParametersFirst(ConstructorInfo a, ConstructorInfo b)
{
    int aCount = a.GetParameters().Length;
    int bCount = b.GetParameters().Length;
    return bCount.CompareTo(aCount);
}

static ConstructorInfo PickGreediestConstructor(Type type)
{
    ConstructorInfo[] constructors = type.GetConstructors();
    Array.Sort(constructors, MoreParametersFirst);

    foreach (ConstructorInfo constructor in constructors)
    {
        if (AllParametersResolvable(constructor))
            return constructor;
    }

    throw new InvalidOperationException($"{type.Name}: no constructor whose parameters can all be resolved");
}

static bool AllParametersResolvable(ConstructorInfo constructor)
{
    foreach (ParameterInfo parameter in constructor.GetParameters())
    {
        if (!CanResolve(parameter.ParameterType))
            return false;
    }
    return true;
}

static bool CanResolve(Type type)
{
    if (registered.ContainsKey(type))
        return true;

    if (type.IsInterface || type.IsAbstract)
        return false;

    foreach (ConstructorInfo constructor in type.GetConstructors())
    {
        if (AllParametersResolvable(constructor))
            return true;
    }
    return false;
}
```

### `ResolveRecursively` — будує об'єкт

| Рядок | Що робить |
|---|---|
| `registered.TryGetValue(type, out object existing)` | Один пошук у словнику, який і перевіряє наявність (`bool`), і віддає значення через `out`. |
| `return existing;` | Тип уже є готовим — повертаємо його. Саме так параметр-**інтерфейс** отримує реалізацію (те, на чому падав розділ 4, "Межа підходу"). |
| `PickGreediestConstructor(type)` | Єдине місце, що відрізняється від розділу 4: замість `[0]` — свідомий вибір. |
| `GetParameters()` … `Invoke(args)` | Те саме, що в розділі 4: рекурсивно отримати кожен аргумент і викликати конструктор. |

### `MoreParametersFirst` — компаратор для сортування

| Рядок | Що робить |
|---|---|
| `int aCount = a.GetParameters().Length;` | Кількість параметрів першого конструктора. |
| `int bCount = b.GetParameters().Length;` | Кількість параметрів другого. |
| `return bCount.CompareTo(aCount);` | Контракт компаратора: `< 0` — `a` іде раніше, `> 0` — `b` іде раніше, `0` — рівні. `b` стоїть **ліворуч**, тому порядок **спадний** (3, 2, 1, 0 параметрів). Поміняти `a`/`b` місцями — вийде "скромний" вибір (спочатку найменше параметрів). |

### `PickGreediestConstructor` — вибір

| Рядок | Що робить |
|---|---|
| `type.GetConstructors()` | Усі публічні конструктори. Порядок у масиві документацією **не гарантований** — тому й потрібне сортування. |
| `Array.Sort(constructors, MoreParametersFirst);` | Сортує масив **на місці** (нового масиву не створює). `MoreParametersFirst` передано як method group — компілятор сам перетворює його на делегат `Comparison<ConstructorInfo>` (див. [`Delegates_Events_and_Subscriptions.md`](Delegates_Events_and_Subscriptions.md)). |
| `foreach` + `AllParametersResolvable` | Від найжаднішого до найскромнішого; перший, що проходить перевірку, — переможець. |
| `throw new InvalidOperationException(...)` | Жоден не підійшов — падіння з повідомленням, що пояснює **чому**, замість `IndexOutOfRangeException` глибоко в рекурсії. |

### `AllParametersResolvable` і `CanResolve` — перевірка без створення

| Рядок | Що робить |
|---|---|
| `foreach (ParameterInfo parameter …)` + `return false` | Досить одного неотримуваного параметра — конструктор відпадає, решта параметрів не перевіряється. |
| `registered.ContainsKey(type)` → `true` | Готовий об'єкт є. |
| `type.IsInterface \|\| type.IsAbstract` → `false` | Незареєстрований інтерфейс/абстрактний клас створити нічим: метадані інтерфейсу не знають, хто його реалізує. |
| останній `foreach` | Конкретний клас можна створити, якщо **хоч один** його конструктор розв'язний — рекурсія вниз по залежностях. |

`CanResolve` нічого не створює — лише відповідає "чи можна". Створює потім
`ResolveRecursively`.

### Вивід

Класи прикладу:

```csharp
public interface IEngine { }
public interface IWheels { }
public interface ITrailer { }
public class Engine : IEngine { }
public class Wheels : IWheels { }

public class Car
{
    public Car() { Console.WriteLine("Car()"); }
    public Car(IEngine engine) { Console.WriteLine("Car(IEngine)"); }
    public Car(IEngine engine, IWheels wheels) { Console.WriteLine("Car(IEngine, IWheels)"); }
    public Car(IEngine engine, IWheels wheels, ITrailer trailer) { Console.WriteLine("Car(IEngine, IWheels, ITrailer)"); }
}
```

Зареєстровані `IEngine` і `IWheels`, `ITrailer` — ні (рядки `-> True/False` друкує
відлагоджувальний рядок усередині `foreach` вибору):

```
Void .ctor(IEngine, IWheels, ITrailer) -> False
Void .ctor(IEngine, IWheels) -> True
Car(IEngine, IWheels)
CanResolve calls: 5
```

Після `registered.Remove(typeof(IWheels))` — той самий `Car`, інший конструктор, жодної
зміни в коді `Car`:

```
Void .ctor(IEngine, IWheels, ITrailer) -> False
Void .ctor(IEngine, IWheels) -> False
Void .ctor(IEngine) -> True
Car(IEngine)
CanResolve calls: 5
```

### Ціна: чому "дорогий"

Ланцюжок із 10 класів, кожен приймає **два** екземпляри наступного:
`L1(L2 a, L2 b)`, `L2(L3 a, L3 b)`, …, `L10()`.

```
L1..L10, 2 params each -> CanResolve calls: 8194
```

| Причина | Наслідок |
|---|---|
| Подвійний обхід: `CanResolve` перевіряє піддерево, потім `ResolveRecursively` іде вниз і на кожному рівні **знову** перевіряє своє піддерево | Та сама перевірка повторюється на кожному рівні |
| Дві однакові залежності перевіряються двічі — результат ніде не запам'ятовується | Кількість росте як ~2^глибина |
| `GetConstructors()`/`GetParameters()` щоразу виділяють нові масиви, сортування викликає `GetParameters()` ще раз | Зайва алокація на кожному кроці |

Як це вирішують справжні контейнери: рішення "який конструктор і які аргументи для
типу X" обчислюється **один раз** і кешується у словнику `Type → план створення`.

### Що ламається

| Ситуація | Що станеться |
|---|---|
| Цикл: `Chicken(Egg)`, `Egg(Chicken)` | Нескінченна рекурсія в `CanResolve` → `Stack overflow.` — процес завершується, `try/catch` такий виняток **не ловить**. Контейнери тримають стек "зараз будую" й кидають зрозумілий виняток про цикл. |
| Два конструктори з **однаковою** кількістю параметрів, обидва розв'язні | `Array.Sort` — нестабільне сортування, переможець не визначений. Microsoft DI у цьому випадку свідомо кидає виняток про неоднозначні конструктори. |
| Реєстрацію додали/прибрали | Клас **мовчки** переходить на інший конструктор (див. другий вивід вище) — поведінка змінилась без жодної зміни в його коді. |

Реальний вивід для циклу:

```
Stack overflow.
   at System.RuntimeType+ListBuilder`1[[System.__Canon, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]].ToArray()
   at System.Type.GetConstructors()
   at Program.CanResolve(System.Type)
```

### Коли що обирати

| Стратегія | Коли |
|---|---|
| `.Single()` (розділ 2) | Правило "у сервісу рівно один публічний конструктор". Явно, дешево, порушення ловиться одразу. |
| Жадібна (цей розділ) | Класи, які ти не контролюєш і які мають кілька конструкторів; загальний контейнер для чужого коду. |
| `[0]` | Ніколи для вибору конструктора — порядок не гарантований. |

## Пов'язане

- [`Debug_Logging_and_Reflection.md`](../Unity_Basics/Debug_Logging_and_Reflection.md) — найпростіша
  форма рефлексії, вже використана в проєкті (`GetType().Name` для логів FSM).
- [`Generics.md`](Generics.md) — generics на рівні мови (без рефлексії):
  `where`-обмеження, кілька типових параметрів, generic-колекції.
