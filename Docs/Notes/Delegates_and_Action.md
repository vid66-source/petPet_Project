# Делегати та `Action`

Уперше трапилось: урок 01, `SceneLoader.Load(...)` / `LoadLevelState.onLoaded`.

## Що таке делегат

Делегат — тип, значенням якого є **посилання на метод**, а не на дані. Усі "звичайні"
типи (`int`, `string`, твій власний клас) описують дані; делегат описує "щось, що можна
викликати" — конкретний метод з певною сигнатурою (параметри + тип результату).

`Action` — вбудований у .NET (namespace `System`) делегат для найпростішого випадку:
метод без параметрів і без результату (`void MethodName()`). Є родичі для інших сигнатур:
- `Action<T>` — метод з одним параметром типу `T`, без результату.
- `Func<TResult>` — метод без параметрів, повертає `TResult`.
- `Func<T, TResult>` — один параметр, повертає результат.

(У проєкті поки використано тільки `Action` без параметрів — решта знадобиться пізніше,
наприклад коли треба буде передати колбек із результатом чи з аргументом.)

## Наскрізний приклад — `onLoaded` в уроці 01

**Крок 1**, `LoadLevelState.cs`:
```csharp
public void Enter(string sceneName)
{
    _loadingCurtain.Show();
    _sceneLoader.Load(sceneName, onLoaded);
}

private void onLoaded() => _stateMachine.Enter<GameLoopState>();
```
`onLoaded` — звичайний приватний метод. `_sceneLoader.Load(sceneName, onLoaded)` передає
його **без дужок** — тобто не викликає, а передає посилання. Оскільки цільовий параметр
має тип `Action`, компілятор сам загортає пару "цей метод + цей екземпляр
`LoadLevelState`" у делегат-об'єкт. Це називається **method group conversion**.

**Крок 2**, `SceneLoader.cs`:
```csharp
public void Load(string sceneName, Action onLoaded = null)
{
    if (SceneManager.GetActiveScene().name == sceneName)
        onLoaded?.Invoke();
    else
        _coroutineRunner.StartCoroutine(LoadScene(sceneName, onLoaded));
}

private IEnumerator LoadScene(string sceneName, Action onLoaded)
{
    AsyncOperation sceneLoadOperation = SceneManager.LoadSceneAsync(sceneName);
    while (!sceneLoadOperation.isDone) { yield return null; }
    onLoaded?.Invoke();
}
```
`Action onLoaded` тут — звичайний параметр методу, той самий механізм, що й
`string sceneName` поруч, тільки тип інший. Ніде не потрібна окрема змінна
(`Action x = ...;`) — сам параметр і є "місцем, де це зберігається" на час виконання
методу. Той самий об'єкт-делегат передається далі в `LoadScene` без жодних змін.

**Крок 3 — виклик:** `onLoaded?.Invoke()` — `.Invoke()` виконує метод, загорнутий у
делегат; `?.` захищає від `NullReferenceException`, якщо викликати `Load` без колбека
(звідси й `= null` за замовчуванням у сигнатурі — необов'язковий параметр).

**Підсумок:** фізично виклик `.Invoke()` стається всередині `SceneLoader` (класу, який
нічого не знає про `GameStateMachine`), але виконується код, визначений у
`LoadLevelState` — делегат це і дозволяє: передати "що робити далі" як звичайні дані,
не називаючи наперед конкретний тип-отримувач.

## Чому саме так, а не прямий виклик

Якби `SceneLoader` сам викликав щось на кшталт `_stateMachine.Enter<GameLoopState>()`
напряму, він був би змушений знати про `GameStateMachine` — а це вже не його
відповідальність (SRP) і зайва залежність. Через `Action`-колбек `SceneLoader` каже
"я завантажу сцену і повідомлю, викликавши те, що мені дали" — і йому байдуже, що саме
викликається.

## Пов'язане

- [`Generics.md`](Generics.md) — інший приклад параметризації поведінки без знання
  конкретного типу наперед, тільки через типи, а не через методи.
