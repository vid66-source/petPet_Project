# Downcast і оператор `as`

Уперше трапилось: ізольований приклад `ModeSwitcher`/`IdleMode` (перемикання активного
"режиму" через каст), закріплено на реальному коді — рядок
`TState newState = _states[typeof(TState)] as TState;` у `GameStateMachine`.

## Термінологія

- **Upcast** (розширення) — привести змінну до **загальнішого** типу (від нащадка до
  предка). Завжди безпечно, компілятор дозволяє неявно: `IState s = new BootstrapState();`
- **Downcast** (звуження) — привести змінну до **конкретнішого** типу (від предка до
  нащадка). Потенційно небезпечно — компілятор не може на 100% гарантувати заздалегідь,
  що об'єкт справді має той конкретніший тип, тому потрібна явна вказівка й рантайм-
  перевірка.

## Ізольований приклад — `ModeSwitcher`/`IdleMode`

Форма, яку варто впізнавати:
```csharp
IMode _activeMode;

void SwitchTo<TMode>() where TMode : class, IMode
{
    _activeMode?.Exit();
    TMode newMode = _modes[typeof(TMode)] as TMode;
    _activeMode = newMode;
    newMode?.Enter();
}
```
Це той самий алгоритм, що й у `GameStateMachine.Enter<TState>()` — не копіювати один в
один із проєктного коду, а впізнавати форму: дістати з колекції загальнішим типом →
downcast до конкретного → присвоїти активним → викликати метод.

## Реальний код — `GameStateMachine.cs`

```csharp
public void Enter<TState>() where TState : class, IState
{
    _currentState?.Exit();
    TState newState = _states[typeof(TState)] as TState;
    _currentState = newState;
    newState?.Enter();
}
```

- `_states[typeof(TState)]` — індексатор `Dictionary<Type, IExitableState>` завжди
  повертає статичний тип `IExitableState` (тип значення словника), незалежно від того, що
  реально лежить усередині.
- `as TState` — downcast до `TState` (`class, IState`-обмежений — тобто похідний від
  `IExitableState`, отже конкретніший). Приведення "звужує" тип із загальнішого до
  конкретнішого.

## Чому `as`, а не `(TState)`

- `(TState)newState` — прямий каст. Якщо приведення не вдається — кидає
  `InvalidCastException` одразу.
- `newState as TState` — якщо приведення не вдається, повертає `null` замість винятку.

Саме тому в коді далі стоїть `newState?.Enter()` — оборонний виклик через `?.`: якби каст
не вдався (на практиці тут завжди вдається, бо словник наповнюється консистентно), гра
просто не викликала б `Enter()`, а не впала б винятком.

`as` працює тільки для reference-типів (класів/інтерфейсів) — звідси й вимога
`where TState : class` у сигнатурі: без неї компілятор не дозволив би `as TState`,
бо `TState` міг би виявитись структурою (`struct`), для якої `as` не існує.

## Пов'язане

- [`Generics.md`](Generics.md) — сам метод, у якому цей downcast стається.
- [`Interfaces.md`](Interfaces.md) — чому загальніший/конкретніший тип тут — саме
  інтерфейси, а не класи.
