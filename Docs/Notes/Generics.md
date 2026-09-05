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

```csharp
public class Box<T>
{
    private T _value;
    public void Put(T value) => _value = value;
    public T Take() => _value;
}
```
`Box<int>` зберігає `int`, `Box<string>` — `string`, той самий код класу обслуговує обидва
випадки. Компілятор на етапі компіляції знає точний тип `T` для кожного використання —
жодних кастів не треба.

## Реальний приклад із проєкту — `GameStateMachine.Enter<TState>()`

```csharp
public void Enter<TState>() where TState : class, IState
{
    _currentState?.Exit();
    TState newState = _states[typeof(TState)] as TState;
    _currentState = newState;
    newState?.Enter();
}
```

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

```csharp
public void Enter<TState, TPayload>(TPayload payload) where TState : class, IPayloadedState<TPayload>
{
    ...
    newState?.Enter(payload);
}
```

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

```csharp
// НЕПРАВИЛЬНО:
public class Pair<TFirst, TSecond> where TFirst : class, TSecond : IState
```

Це не компілюється так, як здається — кома всередині одного `where` додає **ще одну
вимогу до того самого параметра** (`TFirst`), а не перемикає на `TSecond`. Правильно —
окремий `where`-рядок для кожного типового параметра:

```csharp
public class Pair<TFirst, TSecond>
    where TFirst : class
    where TSecond : IState
{
    ...
}
```

## `where T : class` проти `where T : КонкретнийКлас`

Це дві різні речі, які легко сплутати:
- `where T : class` — обмеження "**reference-тип**" (не структура/`int`/`enum` тощо), саме
  так і використано в проєкті (`where TState : class, IState`). Це не успадкування від
  якогось класу `class` — слово тут ключове, не назва типу.
- `where T : КонкретнийКлас` (наприклад, `where T : MonoBehaviour`) — реальне обмеження на
  успадкування: `T` має бути цим класом або його нащадком, і тоді на `T` доступні методи
  саме цього класу.

## Пов'язане

- [`Interfaces.md`](Interfaces.md) — навіщо взагалі інтерфейси, база перед
  generic-обмеженнями на кшталт `where TState : IState`.
- [`Downcasting_and_as.md`](Downcasting_and_as.md) — детальніше про `as TState` у прикладі
  вище.
- [`Delegates_and_Action.md`](Delegates_and_Action.md) — інший спосіб параметризувати
  поведінку (через переданий метод, а не через тип).
