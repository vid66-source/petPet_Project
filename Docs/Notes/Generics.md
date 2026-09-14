# Generics (узагальнені типи й методи)

Уперше трапилось: урок 01, спершу на ізольованих прикладах (`Box<T>`,
`Pair<TFirst, TSecond>`, `ModeSwitcher`), потім закріплено на реальному коді —
`GameStateMachine.Enter<TState>()`.

## Що це і навіщо

Generic — клас або метод, який параметризований типом: замість того, щоб писати окрему
версію для `int`, окрему для `string`, окрему для кожного власного класу, пишеш один раз
із "заповнювачем типу" (`T`, `TState`, будь-яке ім'я), а конкретний тип підставляється при
використанні.

Без generics довелось би або дублювати код під кожен тип, або працювати через `object`
(втрачаючи типізацію — компілятор більше не перевіряє, що саме там лежить, і потрібні
ручні каст-и всюди).

## Мінімальний ізольований приклад

--------------------------- КОД ---------------------------
<pre>
public class Box&lt;T&gt;
{
    private T _value;
    public void Put(T value) =&gt; _value = value;
    public T Take() =&gt; _value;
}
</pre>
------------------------------------------------------------
`Box<int>` зберігає `int`, `Box<string>` — `string`, той самий код класу обслуговує обидва
випадки. Компілятор на етапі компіляції знає точний тип `T` для кожного використання —
жодних кастів не треба.

## Реальний приклад із проєкту — `GameStateMachine.Enter<TState>()`

--------------------------- КОД ---------------------------
<pre>
public void Enter&lt;TState&gt;() where TState : class, IState
{
    _currentState?.Exit();
    TState newState = _states[typeof(TState)] as TState;
    _currentState = newState;
    newState?.Enter();
}
</pre>
------------------------------------------------------------

Виклик: `stateMachine.Enter<BootstrapState>()`. `TState` — тут `BootstrapState`, підставлено
явно в кутових дужках при виклику (compile-time, без рефлексії "на льоту").

### Generic-обмеження (`where`)

`where TState : class, IState` — це обіцянка компілятору: "яким би конкретним типом не
підставили `TState`, він точно клас (`class`) і точно реалізує `IState`". Без цього рядка
рядок `newState?.Enter()` не скомпілювався б — компілятор не знав би, що в `TState` взагалі
є метод `Enter()`.

### Downcast усередині generic-методу

`_states[typeof(TState)] as TState` — значення словника типізоване як `IExitableState`
(загальніший тип), а `as TState` звужує (downcast) до конкретнішого. Детальніше про сам
`as`-оператор і чому не прямий `(TState)`-каст — див. розбір цього рядка в чаті уроку 01
(звужений тип, `?.Invoke()`-подібна оборонна логіка через `null`).

### Другий приклад — `Enter<TState, TPayload>`

--------------------------- КОД ---------------------------
<pre>
public void Enter&lt;TState, TPayload&gt;(TPayload payload) where TState : class, IPayloadedState&lt;TPayload&gt;
{
    ...
    newState?.Enter(payload);
}
</pre>
------------------------------------------------------------

Два типові параметри одночасно (`TState`, `TPayload`), кожен зі своїм роллю в обмеженні:
`TState` має реалізовувати `IPayloadedState<TPayload>` — тобто мати `Enter(TPayload)`
саме з тим самим `TPayload`, який передається викликом. Приклад виклику:
`Enter<LoadLevelState, string>(sceneName)`.

## `Dictionary<Type, IExitableState>` — generic-колекція

`Dictionary<TKey, TValue>` — сам generic-клас з двох типових параметрів. У проєкті —
`Dictionary<Type, IExitableState>`: ключ — `System.Type` (через `typeof(BootstrapState)`
тощо), значення — спільний інтерфейс усіх станів. Один клас `Dictionary<,>` обслуговує
будь-яку комбінацію ключ/значення без окремого коду для кожної.

## Помилка, яка траплялась: один `where` на кілька типових параметрів

На ізольованому прикладі `Pair<TFirst, TSecond>` була спроба написати одну конструкцію
на обидва параметри:

--------------------------- КОД ---------------------------
<pre>
// НЕПРАВИЛЬНО:
public class Pair&lt;TFirst, TSecond&gt; where TFirst : class, TSecond : IState
</pre>
------------------------------------------------------------

Це не компілюється так, як здається — кома всередині одного `where` додає **ще одну
вимогу до того самого параметра** (`TFirst`), а не перемикає на `TSecond`. Правильно —
окремий `where`-рядок для кожного типового параметра:

--------------------------- КОД ---------------------------
<pre>
public class Pair&lt;TFirst, TSecond&gt;
    where TFirst : class
    where TSecond : IState
{
    ...
}
</pre>
------------------------------------------------------------

## `where T : class` проти `where T : КонкретнийКлас`

Це дві різні речі, які легко сплутати:
- `where T : class` — обмеження "**reference-тип**" (не структура/`int`/`enum` тощо), саме
  так і використано в проєкті (`where TState : class, IState`). Це не успадкування від
  якогось класу `class` — слово тут ключове, не назва типу.
- `where T : КонкретнийКлас` (наприклад, `where T : MonoBehaviour`) — реальне обмеження на
  успадкування: `T` має бути цим класом або його нащадком, і тоді на `T` доступні методи
  саме цього класу.

## Скільки типових параметрів можна/варто мати

Технічно — дуже багато (CLR дозволяє тисячі на метод/клас, обмеження рівня
метаданих збірки, ніколи на практиці в це не впираєшся). Але кількість, яку
**варто** використовувати — визначається не бажанням, а тим, скільки в конкретному
методі/класі є **справді незалежних** типів, які вирішує викликач.

**1 параметр** — коли всі місця, де використовується тип, повинні бути **тим самим**
типом:

--------------------------- КОД ---------------------------
<pre>
public static T Max&lt;T&gt;(T a, T b) where T : IComparable&lt;T&gt;
{
    return a.CompareTo(b) &gt; 0 ? a : b;
}
// Max(3, 7) — обидва аргументи мають бути одним і тим самим T
</pre>
------------------------------------------------------------

**2 параметри** — коли є дві незалежні "невідомі" (як `Enter<TState, TPayload>` вище:
який стан і яким типом даних його нагодувати — одне з одним ніяк не пов'язане).

**3 параметри** — коли з'являється ще одна незалежна вісь, наприклад результат
комбінування двох різних типів:

--------------------------- КОД ---------------------------
<pre>
public static TResult Combine&lt;TA, TB, TResult&gt;(TA a, TB b, Func&lt;TA, TB, TResult&gt; combiner)
{
    return combiner(a, b);
}
// Combine(3, 4, (x, y) =&gt; x + y) — TA=int, TB=int, TResult=int, усі три виводяться самі
</pre>
------------------------------------------------------------

**4+ параметри — технічно працює, але вже сигнал зупинитись:**

--------------------------- КОД ---------------------------
<pre>
public class Quad&lt;T1, T2, T3, T4&gt;
{
    public T1 A; public T2 B; public T3 C; public T4 D;
}
// виклик: SomeMethod&lt;int, string, bool, float&gt;(1, "x", true, 2.5f) — уже важко читати
</pre>
------------------------------------------------------------

На цьому етапі правильніший хід — згрупувати частину типів у звичайний
клас/`struct`/tuple (так само, як два гіпотетичних payload'и `Enter`-у стали б одним
класом-контейнером замість `TPayload1, TPayload2`), а не множити типові параметри
далі. .NET-родина `Action`/`Func` доходить до 16 — і навіть це вважається крайнім,
рідко реально використовуваним випадком (див.
[`Delegates_Events_and_Subscriptions.md`](Delegates_Events_and_Subscriptions.md) §2).

## Підсумок: бібліотечні типи, що тут з'явились

Сам файл — про мовну фічу C# (generics), не про конкретну бібліотеку, але кілька
реальних `.NET`-типів засвічені принагідно:

### `Dictionary<TKey, TValue>` (namespace `System.Collections.Generic`)

generic-колекція "ключ → значення". У проєкті — `Dictionary<Type, IExitableState>`:
- `TKey` = `Type` (ключ — сам тип стану, через `typeof(BootstrapState)` тощо).
- `TValue` = `IExitableState` (значення — спільний інтерфейс усіх станів).
- Індексатор `dictionary[key]` → `TValue` — саме так стан дістається в
  `GameStateMachine.Enter<TState>()` (`_states[typeof(TState)]`).

### `IComparable<T>` (namespace `System`)

Інтерфейс-контракт "уміє порівнювати себе з іншим об'єктом того самого типу".
- Метод: `int CompareTo(T other)` — аргумент `other` того самого типу `T`, повертає
  `int` (`< 0` — менше, `0` — рівне, `> 0` — більше). Саме цей метод дозволяє
  `Max<T>(T a, T b) where T : IComparable<T>` викликати `a.CompareTo(b)`.

### `Func<TA, TB, TResult>` (namespace `System`)

generic-делегат із двох аргументів. У прикладі `Combine<TA, TB, TResult>` —
`combiner` типу `Func<TA, TB, TResult>` приймає `TA a` й `TB b`, повертає `TResult`.
Детальніше про всю родину `Action`/`Func` —
[`Delegates_Events_and_Subscriptions.md`](Delegates_Events_and_Subscriptions.md).

## Пов'язане

- [`Interfaces.md`](Interfaces.md) — навіщо взагалі інтерфейси, база перед
  generic-обмеженнями на кшталт `where TState : IState`.
- [`Downcasting_and_as.md`](Downcasting_and_as.md) — детальніше про `as TState` у прикладі
  вище.
- [`Delegates_Events_and_Subscriptions.md`](Delegates_Events_and_Subscriptions.md) —
  інший спосіб параметризувати поведінку (через переданий метод, а не через тип).
