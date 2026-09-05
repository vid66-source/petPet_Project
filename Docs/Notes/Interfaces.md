# Інтерфейси — навіщо

Уперше трапилось: урок 01, коли застряг на написанні `GameStateMachine` — розібрано з
нуля на ізольованому прикладі, перед тим як повертатись до generic-обмежень і самого
класу.

## Ізольований приклад

```csharp
public interface ICanMakeSound
{
    void MakeSound();
}

public class Dog : ICanMakeSound
{
    public void MakeSound() => Console.WriteLine("Гав!");
}

public class Cat : ICanMakeSound
{
    public void MakeSound() => Console.WriteLine("Няв!");
}
```

Ідея: код, який хоче "змусити когось видати звук", не повинен знати, чи це `Dog`, чи
`Cat`, чи будь-хто ще — йому достатньо знати, що об'єкт **уміє** `MakeSound()`:

```csharp
void MakeItSound(ICanMakeSound animal) => animal.MakeSound();

MakeItSound(new Dog()); // "Гав!"
MakeItSound(new Cat()); // "Няв!"
```

Інтерфейс — контракт "що об'єкт уміє робити", без жодної прив'язки до того, **як** саме
він це робить усередині. Клас, що реалізує інтерфейс, зобов'язаний надати тіло для
кожного його методу — інакше не скомпілюється.

## Реальний приклад із проєкту

```csharp
public interface IExitableState { void Exit(); }
public interface IState : IExitableState { void Enter(); }
public interface IPayloadedState<TPayload> : IExitableState { void Enter(TPayload payload); }
```

`BootstrapState`, `LoadLevelState`, `GameLoopState` — три зовсім різні класи з різною
логікою всередині, але `GameStateMachine` працює з ними тільки через ці контракти
(`IExitableState`/`IState`/`IPayloadedState<TPayload>`), не знаючи наперед, який саме
клас отримає. Це той самий принцип, що й `ICanMakeSound` вище, тільки на реальному коді.

## Навіщо саме три вузькі, а не один товстий

Якби існував один `IState` одразу з `Enter()` **і** `Enter(TPayload)`, кожен клас, якому
не потрібен параметр (`BootstrapState`, `GameLoopState`), був би змушений реалізувати
метод "для галочки", яким ніколи не користується. Це Interface Segregation Principle —
детальніше в `Docs/PATTERNS.md`, розділ уроку 01.

## Пов'язане

- [`Generics.md`](Generics.md) — `where T : IState` використовує саме цю ідею: обмежити
  тип не через успадкування від класу, а через "уміє робити X".
