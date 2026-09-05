# CV Log — навички й технології для резюме

Лог того, з чим реально працювали чи що реально розібрали: C#/Unity механіки, класи,
бібліотеки, патерни. На відміну від `Docs/PATTERNS.md` (глибокий розбір патерна з кодом,
тільки після тестування), сюди запис додається одразу, коли тема зʼявилась у роботі чи
розмові — не чекаючи кінця уроку.

**Статуси**, щоб на співбесіді не видати недороблене за готове:
- `[вивчено]` — розібрано концептуально (у чаті чи в уроці), у коді ще нема або код не
  написаний.
- `[у коді]` — написано, ще не пройшло рев'ю або не перевірено в Play Mode.
- `[перевірено]` — пройшло рев'ю **і** запущено/протестовано в Play Mode. Тільки це
  без застережень годиться в резюме як "маю досвід".

---

## Урок 01 — Composition Root і FSM

### C# — мова
- Generics: узагальнені класи (`class Box<T>`) і методи (`T Echo<T>(T value)`), кілька
  типових параметрів одночасно (`Pair<TFirst, TSecond>`) `[вивчено]`
- Generic constraints (`where T : ...`): `class`-constraint як "reference-тип" (не
  успадкування), constraint на інтерфейс/клас як доступ до його методів, окремий `where`
  на кожен типовий параметр `[перевірено]`
- `System.Type` і `typeof(...)` як унікальний ідентифікатор класу, використання як ключа
  `Dictionary<Type, TValue>` `[перевірено]`
- `Dictionary<TKey, TValue>` як реєстр об'єктів за ключем-типом (уникнення
  окремих полів на кожен варіант) `[перевірено]`
- Інтерфейси як контракт ("не важливо, який клас, важливо — що він уміє"), Interface
  Segregation (кілька вузьких інтерфейсів замість одного товстого) `[перевірено]`
- Делегати/`Action` і method group conversion — передача методу як значення
  (`sceneLoader.Load(sceneName, onLoaded)`) замість виклику `[перевірено]`
- `object.GetType()` і `Type.Name` — рефлексія на рівні "ім'я реального типу під час
  виконання", використано для узагальненого логування (`GetType().Name`) `[перевірено]`
- Expression-bodied members (`=>`) для однорядкових методів (`Exit() => ...`) `[перевірено]`

### Патерни й архітектура
- State (GoF) — явна FSM (`IState`/`IExitableState`/`IPayloadedState<T>` +
  `GameStateMachine`) замість розкиданих bool-прапорців `[перевірено]`
- Composition Root (`GameBootstrapper`) — єдина точка ручної збірки графу залежностей
  через конструктори, без DI-контейнера `[перевірено]`
- Dependency Inversion через `ICoroutineRunner` — чистий C#-клас (`SceneLoader`) залежить
  від абстракції запуску корутин, а не від конкретного `MonoBehaviour` `[перевірено]`

### Unity
- `SceneManager.LoadSceneAsync` + `AsyncOperation.isDone` для асинхронного завантаження
  сцен, корутина, що чекає завершення без блокування гри `[перевірено]`
- `MonoBehaviour.Awake()` vs `Start()` — порядок ініціалізації, чому Composition Root
  саме в `Awake()` `[вивчено]`
- `DontDestroyOnLoad` для збереження об'єкта між сценами, включно з практичною пасткою:
  зберігає лише позначений об'єкт і його дітей в ієрархії — сестринський GameObject
  (знайдено й виправлено на прикладі `Curtain`, який спершу лежав окремим коренем сцени,
  а не дитиною `Bootstraper`) не переживе перехід `[перевірено]`
- `IEnumerator`/корутини і `yield return null` як "почекати кадр, не блокуючи потік"
  `[перевірено]`
- Читання стек-трейсу Console (порядок викликів знизу вгору: `Awake()` →
  `GameStateMachine.Enter<T>()` → `BootstrapState.Enter()`) для діагностики FSM
  `[перевірено]`

---

## Інші проєкти (аналіз GitHub vid66-source)

Проаналізовано 03.09.2026 — переглянуто структуру, `Packages/manifest.json` і кілька
репрезентативних скриптів у кожному репо (не весь код). Статус `[перевірено]` тут
означає "реально прочитано в коді", а не "усе в проєкті ідеальне" — це не рев'ю якості,
а інвентаризація того, з чим була справа.

### PET-Project — найзмістовніший з набору, TPS-заготовка
Свій пет-проєкт (не туторіал) — попередня спроба шутера від третьої особи, до цього курсу.
- Generic Object Pool: `GenericPoolMono<T> where T : MonoBehaviour` — пул з autoexpand,
  видача вільного елемента через `GetFreeElement()` `[перевірено]`
- Інтерфейс `IDamageAble` (`TakeDamage(float)`) для системи пошкоджень `[перевірено]`
- Конфіги через клас-ієрархію (`EnemyBaseConfig`/`EnemyPistol`,
  `WeaponBaseConfig`/`PistolConfig`) — практика конфігурованих даних окремо від логіки
  `[перевірено]`
- `PlayerController` — фізичний рух через `Rigidbody`, `[RequireComponent(typeof(Rigidbody))]`,
  обробка вводу і стрибка вручну (без FSM) `[перевірено]`
- Пакети: новий Input System (`com.unity.inputsystem`), ProBuilder (для швидкого
  прототипування рівня) `[перевірено]`

### Video_Courses — конспект-код під час проходження відеокурсів
Не окремий проєкт, а збірка вправ по темах курсу.
- **State pattern, той самий підхід, що зараз у петPet**: `IPlayerBehavior` (`Enter`/`Exit`/
  `Update`) + `Player` з `Dictionary<Type, IPlayerBehavior>` і generic `GetBehavior<T>()` —
  тобто цей патерн студент уже практикував раніше самостійно, до поточного курсу
  `[перевірено]`
- Observer pattern власної руки: `IObservable`/`IObserver`, `ObservableVariable`,
  `ObservableLogger` `[перевірено]`
- Generic object pool (ще одна практика пулу, окремо від PET-Project): `PoolMono<T>`
  `[перевірено]`
- C# `event`/`Action`-based повідомлення: `Bank`, кілька `Tester*`-класів для практики
  підписки на події `[перевірено]`
- Ієрархія ворогів через успадкування: `EnemyBase` → `EnemyAgile`/`EnemyMage`/`EnemyTank`
  `[перевірено]`

### 2048_tutorial — проходив туторіал з клону 2048
- `GameManager`/`TileBoard`/`TileGrid`/`TileCell`/`TileRow`/`Tile` — стандартна структура
  тайлової сітки для 2048 `[перевірено]`
- `TileState` як `ScriptableObject` — конфігурація кольору/значення тайла через асет, а не
  хардкод `[перевірено]`

### UI_Tutorial — проходив туторіал по uGUI
- Drag&Drop (`Draggable`/`Droppable`), `ScrollviewController`, `SliderviewController`,
  динамічна зміна тексту/картинки (`ChangeText`/`ChangeImage`) `[перевірено]`

### MyFirst2DGame — проходив туторіал клону Flappy Bird
- `BirdScript`, `PipeSpawnScript`/`PipeMoveScript`, `CloudsSpawner`, `AudioManager`,
  `LogicScript` (game over/рахунок) — класична структура 2D-раннера з перешкодами
  `[перевірено, назви й розподіл відповідальностей орієнтовно — детально не читав кожен файл]`

### Basic-Math-for-Game-Development-with-Unity-3D-Second-Edition — приклади з книги
Офіційний репозиторій-супровід книги Kelvin Sung & Gregory Smith, Apress 2023 (не власний
код, а навчальні приклади з видавництва) — практика математики для геймдеву:
- Розділи: Intervals+AABB, Distances+BoundingSpheres, Vectors, DotProducts, CrossProducts,
  VectorComponents, Quaternions `[вивчено — приклади з книги, не написано з нуля]`

### Unity-Basics-Practice — велика збірка навчальних вправ по темах
Юніті-версія 2022.3.62f2, набір ізольованих папок-вправ, не єдиний проєкт:
- Таймери й корутини: `Clock`, `CoroutineTimer`, `TimerRepeater` `[перевірено]`
- Гаманець/економіка: `WalletLogic` — інкапсуляція (`private set`), `event Action<int>`,
  валідація через `throw new ArgumentException` `[перевірено]`
- Слідування/спостереження за об'єктом: `Follower`/`AdaptiveFollower`/`LinearFollower`
  (успадкування, `protected virtual`/`override`) `[перевірено]`
- Звук по типу поверхні: `Surface`/`SurfaceType`/`SurfaceStepsSound` — розпізнавання
  матеріалу під ногами для різних кроків `[перевірено]`
- Просте будування а-ля Minecraft: `Block`/`Builder`/`BuildPreview`/`Grenade` `[перевірено]`
- Збереження даних у файл: `FileSaver` (у парі з Wallet) `[вивчено, не перечитував деталі
  реалізації]`
- Пакети: HDRP, URP, ShaderGraph, VFX Graph, Cinemachine, Timeline, Terrain Tools,
  Visual Scripting (стандартний "усе включено" шаблон Unity, не обов'язково свідомий вибір
  студента) `[перевірено]`

### Playground — сендбокс, мінімальний вміст
2D/3D-практика без вираженої архітектури — `DroneController`, `SoldierController`
`[перевірено, зміст файлів не читав]`

### Practice (CSLight) — чиста C#-практика поза Unity
Консольні вправи без Unity: масиви (у т.ч. динамічний масив), `Dictionary`, черга (`queue`),
функції, цикли, ООП (кілька `OOPPractice*`, `DataBaseApp`, `StoreApplication`, `MapApp`)
`[перевірено — структура папок, окремі файли не читав]`

### my_project — порожній/початковий проєкт
Unity 2019.4.21f1, є тільки `Scenes`/`Textures` (спрайти персонажів, оточення, tilemaps) —
**жодного `.cs`-файлу в репозиторії немає**. Найімовірніше, найперша спроба щось зробити в
Unity, ще до написання коду. Нема що вносити як навичку, крім факту "перший контакт з
Unity Editor" `[вивчено]`.

