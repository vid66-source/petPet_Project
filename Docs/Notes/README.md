# Конспекти

Довідник тих C#/Unity понять, з якими студент був мало знайомий, коли вони вперше
трапились у роботі над проєктом — не архітектурні патерни (для цього `Docs/PATTERNS.md`) і
не як побудований сам проєкт (для цього `Docs/HISTORY.md`), а базові мовні/платформні
речі: generics, делегати, конкретні API Unity тощо. Кожна тема — окремий файл, написаний
розгорнуто, із прикладами з реального коду цього проєкту там, де це можливо.

**Джерело тем:** реальні питання, які студент ставив у чаті (цієї чи попередніх сесій) —
**не** `Docs/CV_LOG.md` (це ширший лог навичок для резюме, включно з речами з інших
репозиторіїв, не список питань). Щоб знайти питання з минулих сесій, які самі по собі не
зберігаються дослівно ніде постійно, дивись git-історію `Docs/SESSION_NOTES.md`
(`git log --oneline -- Docs/SESSION_NOTES.md`, потім `git show <hash>:Docs/SESSION_NOTES.md`)
— там короткий підсумок кожної сесії, включно з C#-механіками, на яких застрягав студент.

**Коли додається новий файл:** щойно в чаті пояснюється мовна/платформна механіка, в якій
студент не був упевнений. Не чекати кінця уроку.

## Теми

- [`Interfaces.md`](Interfaces.md) — навіщо інтерфейси, на прикладі
  `IExitableState`/`IState`/`IPayloadedState<TPayload>`.
- [`Generics.md`](Generics.md) — узагальнені типи/методи, кілька типових параметрів,
  `where`-обмеження (і поширена помилка з комою), на прикладі
  `GameStateMachine.Enter<TState>()`.
- [`Downcasting_and_as.md`](Downcasting_and_as.md) — upcast/downcast, оператор `as` проти
  прямого касту, на прикладі `_states[typeof(TState)] as TState`.
- [`Delegates_and_Action.md`](Delegates_and_Action.md) — делегати, `Action`, method group
  conversion, на прикладі `onLoaded` у `SceneLoader`/`LoadLevelState`.
- [`Debug_Logging_and_Reflection.md`](Debug_Logging_and_Reflection.md) — `Debug.Log`,
  string interpolation, `GetType().Name`, читання стек-трейсів у Console.
- [`Coroutines_and_Scene_Loading.md`](Coroutines_and_Scene_Loading.md) — `IEnumerator`,
  `yield return null`, `SceneManager.LoadSceneAsync`/`AsyncOperation`.
- [`DontDestroyOnLoad.md`](DontDestroyOnLoad.md) — межі сцен, пастка з дочірньою
  ієрархією (реальний баг із `Curtain` в уроці 01).
