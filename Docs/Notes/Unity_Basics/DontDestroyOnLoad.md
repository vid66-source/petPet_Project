# `DontDestroyOnLoad` і межі сцен

Уперше трапилось: урок 01, `GameBootstrapper.Awake()` — і практична пастка, знайдена й
виправлена в сцені `Bootstrap` цього ж уроку.

## Навіщо

За замовчуванням `SceneManager.LoadSceneAsync(name)` (без додаткового режиму) вивантажує
**всі** об'єкти поточної сцени, коли завантажує нову (`LoadSceneMode.Single`). Але дещо
має пережити цей перехід — сам `GameBootstrapper` і побудований ним граф об'єктів
(`Game`, `GameStateMachine`, стани), інакше вся FSM обнулиться разом зі зміною сцени.

```csharp
private void Awake()
{
    _game = new Game(this, _curtain);
    DontDestroyOnLoad(this);
    _game.StateMachine.Enter<BootstrapState>();
}
```

`DontDestroyOnLoad(this)` (де `this` — сам `GameBootstrapper`, тобто його GameObject)
позначає той конкретний GameObject незнищуваним при зміні сцени.

## Пастка: позначається GameObject і його ДІТИ, не сестринські об'єкти

`DontDestroyOnLoad` зберігає позначений GameObject **і всю його дочірню ієрархію**
(children, і children дітей, і так далі) — але **не** сусідні об'єкти на тому самому
рівні сцени.

**Що сталось у проєкті:** `Curtain` (Canvas із чорним `Image` для `LoadingCurtain`) був
окремим коренем сцени `Bootstrap`, а не дитиною `Bootstraper` (GameObject з
`GameBootstrapper`). У `.unity`-файлі це видно по `m_Father: {fileID: 0}` в обох — обидва
кореневі, на одному рівні.

Наслідок: `DontDestroyOnLoad(this)` зберігав тільки `Bootstraper`, а `Curtain` знищувався
разом із рештою сцени `Bootstrap` під час переходу на `Level_Arena`. Коли пізніше
`LoadLevelState.Exit()` викликав `_loadingCurtain.Hide()` (уже після завантаження нової
сцени) — об'єкт до цього моменту вже не існував → `MissingReferenceException`. І оскільки
виняток стається всередині `Exit()`, який `GameStateMachine.Enter<TState>()` викликає **до**
переключення на новий стан, виконання зупинилось би там же — `GameLoopState.Enter()`
взагалі не встиг би викликатись.

**Виправлення:** перетягнути `Curtain` в Hierarchy так, щоб він став дочірнім об'єктом
`Bootstraper`. Тоді `DontDestroyOnLoad(this)` зберігає всю гілку разом.

## Правило на майбутнє

Усе, що має пережити зміну сцени і при цьому не створюється через код (а лежить готовим
GameObject'ом у сцені, як `Curtain`), має бути **дитиною** того GameObject, на якому
викликається `DontDestroyOnLoad`, — а не окремим сусіднім об'єктом, хай навіть на вигляд
пов'язаним.

## Підсумок: бібліотечні типи тут

### `MonoBehaviour.DontDestroyOnLoad(Object target)` (namespace `UnityEngine`)

- Аргумент: `target` типу `UnityEngine.Object` (тут — `this`, сам компонент
  `GameBootstrapper`; Unity під капотом захищає **весь GameObject**, на якому цей
  компонент висить, і всю його дочірню ієрархію).
- Повертає: `void`. Ефект — побічний: помічає GameObject незнищуваним при
  `SceneManager.LoadSceneAsync`.

## Пов'язане

- [`Coroutines_and_Scene_Loading.md`](Coroutines_and_Scene_Loading.md) — сам процес
  завантаження нової сцени, під час якого стара вивантажується.
