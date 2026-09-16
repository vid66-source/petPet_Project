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

`Type.EmptyTypes` — готовий, спільний для всього .NET, статичний `Type[]`
довжини 0 — скорочення замість `new Type[0]`.

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

## Пов'язане

- [`Debug_Logging_and_Reflection.md`](Debug_Logging_and_Reflection.md) — найпростіша
  форма рефлексії, вже використана в проєкті (`GetType().Name` для логів FSM).
- [`Generics.md`](Generics.md) — generics на рівні мови (без рефлексії):
  `where`-обмеження, кілька типових параметрів, generic-колекції.
