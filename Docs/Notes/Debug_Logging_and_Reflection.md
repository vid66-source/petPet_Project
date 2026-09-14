# `Debug.Log`, string interpolation, `GetType().Name`, стек-трейси

Уперше трапилось: урок 01, "Тимчасова діагностика" — треба було додати логи в кожен
`Enter()`, щоб перевірити FSM у Play Mode.

## `Debug.Log`

`UnityEngine.Debug.Log(object message)` — друкує повідомлення у вікно Console редактора
Unity. Приймає `object`, тому підходить будь-що з `ToString()` — найчастіше рядок.

## String interpolation (`$"..."`)

--------------------------- КОД ---------------------------
<pre>
Debug.Log($"[FSM] Enter {GetType().Name}");
</pre>
------------------------------------------------------------
`$"..."` — рядок, усередині якого в фігурних дужках `{ }` можна писати будь-який вираз, і
він підставиться як текст. Це те саме, що `"[FSM] Enter " + GetType().Name`, просто
читабельніше — особливо коли виразів кілька.

## `GetType()` і `Type.Name`

- `GetType()` — метод, який має **кожен** об'єкт у C# (успадкований від `System.Object`).
  Повертає `System.Type` — опис **реального** типу об'єкта під час виконання (runtime), а
  не оголошеного типу змінної.
- `.Name` — властивість `Type`, рядок з іменем класу.

**Чому `GetType().Name`, а не написати ім'я класу руками** (`"BootstrapState"`) — один і
той самий рядок коду можна дослівно скопіювати в `Enter()` кожного класу-стану, не
підлаштовуючи текст під конкретний клас: `GetType().Name` сам підставить правильну назву
залежно від того, у якому класі виконується код. Якщо клас перейменують — лог сам
оновиться, нічого правити вручну.

## Читання стек-трейсу в Console

Приклад із реального логу цього уроку:
--------------------------- КОД ---------------------------
<pre>
[FSM] Enter LoadLevelState
UnityEngine.Debug:Log (object)
CodeBase.Infrastructure.States.LoadLevelState:Enter (string) (at .../LoadLevelState.cs:22)
CodeBase.Infrastructure.States.GameStateMachine:Enter&lt;...LoadLevelState, string&gt; (string) (at .../GameStateMachine.cs:39)
CodeBase.Infrastructure.States.BootstrapState:Enter () (at .../BootstrapState.cs:19)
CodeBase.Infrastructure.States.GameStateMachine:Enter&lt;...BootstrapState&gt; () (at .../GameStateMachine.cs:28)
CodeBase.Infrastructure.GameBootstrapper:Awake () (at .../GameBootstrapper.cs:16)
</pre>
------------------------------------------------------------

Стек-трейс читається **знизу вгору** — знизу те, що викликало все спочатку, зверху те, що
виконувалось останнім (де стався сам `Debug.Log`):
1. Знизу: `GameBootstrapper.Awake()` — точка входу.
2. Викликав `GameStateMachine.Enter<BootstrapState>()`.
3. Той викликав `BootstrapState.Enter()`.
4. Той викликав `GameStateMachine.Enter<LoadLevelState, string>(...)`.
5. Той викликав `LoadLevelState.Enter(string)`.
6. Зверху: сам `Debug.Log`, звідки й повідомлення.

Це фактично "хто кого викликав" знизу вгору — корисно при діагностиці, звідки насправді
прийшов виклик, особливо коли той самий метод (`Enter`) визначений у кількох класах.

## Підсумок: типи тут

### `Debug` (namespace `UnityEngine`, статичний клас)

- `Log(object message)` → `void` — друкує в Console редактора; приймає `object`,
  тому підходить будь-що з перевизначеним чи стандартним `ToString()`.

### `object.GetType()` (метод на `System.Object`, тобто буквально на всьому)

- Повертає `System.Type` — реальний тип об'єкта в рантаймі. Детальний розбір усіх
  властивостей/методів `Type` — [`Reflection_Basics.md`](Reflection_Basics.md).
- `.Name` (властивість `Type`) → `string` — коротке ім'я класу.

## Пов'язане

- [`Generics.md`](Generics.md) — `GameStateMachine.Enter<TState>()`, чий стек-трейс тут
  розбирається.
