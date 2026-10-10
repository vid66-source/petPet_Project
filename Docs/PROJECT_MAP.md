# Мапа проєкту (гілка `main`)

Для повернення в проєкт після перерви: що де лежить, хто кого створює, що відбувається від
натискання Play до першого стрибка, і **чому** код складений саме так. Номери рядків — за
станом `main` на 2026-10-10 (коміт `4936166`).

Чим відрізняється від сусідніх файлів:

| Файл | Що в ньому | Коли відкривати |
|---|---|---|
| `PROJECT_MAP.md` (цей) | наскрізний прохід по всьому проєкту з посиланнями на рядки | відновити картину цілком |
| `ARCHITECTURE.md` | короткі схеми «хто кого будує» | **застарів**: досі `TestJump` у `GameLoopState`, старий `RegisterService<TService>(service)`, немає `PlayerController`. Оновиться після закриття уроку 04 |
| `PATTERNS.md` | патерн → код, з поясненням | коли питання «як називається цей прийом» |
| `HISTORY.md` | як код дійшов до такого вигляду, урок за уроком | коли питання «чому раніше було інакше» |

Різниця гілок: `phisicsPractice` = `main` мінус рух у `PlayerController.cs` (там лише
`Construct`), мінус поля `Player.prefab`, плюс один рядок у `GameLoopState.cs`. Уся
документація однакова.

---

## 1. Що є в проєкті

### Код — `Assets/CodeBase/`

| Файл | Роль одним реченням | Тип |
|---|---|---|
| `Infrastructure/GameBootstrapper.cs` | точка входу: Unity викликає `Awake()`, звідси збирається вся гра | `MonoBehaviour` |
| `Infrastructure/Game.cs` | збирає `SceneLoader` і `GameStateMachine` | звичайний клас |
| `Infrastructure/ICoroutineRunner.cs` | «дай мені запустити корутину», без вимоги бути `MonoBehaviour` | інтерфейс |
| `Infrastructure/SceneLoader.cs` | асинхронно вантажить сцену й кличе колбек | звичайний клас |
| `Infrastructure/Logic/LoadingCurtain.cs` | показати/сховати чорний екран завантаження | `MonoBehaviour` |
| `Infrastructure/States/IExitableState.cs` | спільне для всіх станів: `Exit()` | інтерфейс |
| `Infrastructure/States/IState.cs` | стан, у який входять без даних: `Enter()` | інтерфейс |
| `Infrastructure/States/IPayloadedState.cs` | стан, у який входять з даними: `Enter(TPayload)` | generic-інтерфейс |
| `Infrastructure/States/GameStateMachine.cs` | тримає всі стани, перемикає поточний | звичайний клас |
| `Infrastructure/States/BootstrapState.cs` | реєструє сервіси, переходить на завантаження рівня | стан |
| `Infrastructure/States/LoadLevelState.cs` | завіса + завантаження сцени, потім — у гру | стан з payload `string` |
| `Infrastructure/States/GameLoopState.cs` | спавнить гравця й віддає йому інпут | стан |
| `Infrastructure/Services/IService.cs` | маркер «це сервіс» (порожній) | інтерфейс |
| `Infrastructure/Services/AllServices.cs` | реєстр сервісів: тип → екземпляр | синглтон |
| `Experimental/ServicesResolver.cs` | створює сервіс через reflection, сам підбираючи аргументи конструктора | статичний клас |
| `Infrastructure/AssetManagement/IAssetProvider.cs` + `AssetProvider.cs` | завантажити префаб за шляхом і заспавнити | сервіс |
| `Infrastructure/Input/IInputService.cs` + `InputService.cs` | напрямок руху + подія стрибка | сервіс |
| `Player/PlayerController.cs` | рух, гравітація, стрибок через `CharacterController` | `MonoBehaviour` |

### Асети, з якими зв'язаний код

| Асет | Що всередині | Хто з кодом його зв'язує |
|---|---|---|
| `Assets/Scenes/Bootstrap.unity` | об'єкт `Bootstraper` (компонент `GameBootstrapper`) → дочірній `Curtain` (компонент `LoadingCurtain`, Canvas + `Image`); камера, світло, `EventSystem` | Build Settings, індекс 0 — з неї стартує гра |
| `Assets/Scenes/Level_Arena.unity` | `Plane`, світло, камера | рядок `"Level_Arena"` у `BootstrapState.cs:10` |
| `Assets/Resources/Player.prefab` | `CharacterController` (висота 2, радіус 0.5) + `PlayerController` (`_moveSpeed = 6`, `_jumpHeight = 1.5`) | рядок `"Player"` у `GameLoopState.cs:12` |
| `Assets/Resources/TestObject.prefab` | тестовий префаб з уроку 02 | зараз ніхто не вантажить |
| `Assets/Input/InputActions.inputactions` | карта `Player`: `Move` (WASD + лівий стік), `Jump` (Space), `Fire` (ЛКМ) | генерує `InputActions.cs` |
| `Assets/Input/InputActions.cs` | згенерований C#-клас (не редагувати руками) | `InputService.cs:14` |

---

## 2. Хто кого знає — одна схема

Стрілка `A ──▶ B` означає «A тримає посилання на B або викликає його».

```
                         ┌──────────────────── Unity ────────────────────┐
                         │  Bootstrap.unity: Bootstraper ─child─ Curtain │
                         └────────┬──────────────────────────┬───────────┘
                         Awake()  │            [SerializeField] _curtain
                                  ▼                          │
                          GameBootstrapper ◀─ is ─ ICoroutineRunner
                                  │ new                      │
                                  ▼                          │
                                Game ────── AllServices.Instance (єдиний доступ до static)
                                  │ new            │
                 ┌────────────────┴───────┐        │
                 ▼                        ▼        ▼
            SceneLoader ◀──────── GameStateMachine(sceneLoader, curtain, services)
     (кличе ICoroutineRunner)             │ new (у конструкторі, по черзі)
                 ▲          ┌─────────────┼───────────────────────┐
                 │          ▼             ▼                       ▼
                 │   BootstrapState   LoadLevelState         GameLoopState
                 │   (stateMachine,   (stateMachine,         (IAssetProvider,
                 │    services)        sceneLoader, curtain)   IInputService)
                 │          │             │                       │
                 └──────────┼─────────────┘                       │ GetComponent + Construct
                            │ RegisterService<I, T>()             ▼
                            ▼                              PlayerController
                       AllServices ──▶ ServicesResolver          │ += OnJumpPressed
                        ├ IAssetProvider = AssetProvider          │ GetDirection() щокадру
                        └ IInputService  = InputService ◀─────────┘
                                             │
                                             ▼
                                   InputActions (Unity Input System)
```

Три різні види зв'язку, які тут змішані:

| Вид | Де в проєкті | Як читати |
|---|---|---|
| **Через конструктор** | майже все: `Game`, `SceneLoader`, `GameStateMachine`, стани | залежність видно в сигнатурі — відкрив конструктор, побачив усе, чого класу треба |
| **Через реєстр** `AllServices` | `IAssetProvider`, `IInputService` | кладе один (`BootstrapState`), дістає інший (`GameStateMachine`), вони одне про одного не знають |
| **Через метод `Construct`** | `PlayerController` | `MonoBehaviour` не можна створити через `new` з параметрами — Unity створює його сам при `Instantiate`, тому залежність передають окремим методом одразу після |

---

## 3. Від Play до стрибка — по кроках

Гру треба запускати зі сцени `Bootstrap`. Якщо натиснути Play в `Level_Arena`, там немає
`GameBootstrapper`, і нічого не стартує.

### Фаза А: збірка (усе в межах одного `Awake()`)

| # | Де | Що відбувається |
|---|---|---|
| 1 | `GameBootstrapper.cs:12` | Unity викликає `Awake()` на `Bootstraper` у сцені `Bootstrap` |
| 2 | `GameBootstrapper.cs:14` | `new Game(this, _curtain)` — `this` іде як `ICoroutineRunner`, `_curtain` — з інспектора |
| 3 | `Game.cs:13` | `new SceneLoader(coroutineRunner)` |
| 4 | `Game.cs:14` | `new GameStateMachine(sceneLoader, curtain, AllServices.Instance)` — перше звернення до `AllServices` створює його (`AllServices.cs:12`) |
| 5 | `GameStateMachine.cs:19` | `new BootstrapState(this, services)` — **конструктор стану одразу реєструє сервіси** (`BootstrapState.cs:19`) |
| 6 | `BootstrapState.cs:31–32` | `RegisterService<IAssetProvider, AssetProvider>()`, `RegisterService<IInputService, InputService>()` |
| 7 | `AllServices.cs:21` → `ServicesResolver.cs:11–24` | resolver бере перший конструктор `AssetProvider`/`InputService` (обидва без параметрів) і викликає його. Тут же створюється `InputActions` і вмикається карта `Player` (`InputService.cs:14–16`) |
| 8 | `AllServices.cs:22` | екземпляр кладеться в словник під ключем **інтерфейсу**, не класу |
| 9 | `GameStateMachine.cs:20` | `new LoadLevelState(this, sceneLoader, loadingCurtain)` |
| 10 | `GameStateMachine.cs:21–23` | `GetService<IAssetProvider>()`, `GetService<IInputService>()` → `new GameLoopState(...)` |
| 11 | `GameBootstrapper.cs:15` | `DontDestroyOnLoad(this)` — `Bootstraper` разом з дочірнім `Curtain` переживе зміну сцени |

**Прихований порядок у кроках 5–10.** `GameStateMachine` дістає сервіси (рядки 21–22) лише
тому, що рядком вище конструктор `BootstrapState` вже їх зареєстрував. Це ніде не написано
явно — тримається тільки на порядку рядків. Якби рядки стояли так:

```csharp
// так зламається
IAssetProvider assetProvider = services.GetService<IAssetProvider>();   // словник ще порожній
IInputService inputService = services.GetService<IInputService>();
_states.Add(typeof(GameLoopState), new GameLoopState(assetProvider, inputService));
_states.Add(typeof(BootstrapState), new BootstrapState(this, services)); // реєстрація — запізно
```

`_services[typeof(TService)]` у `AllServices.cs:28` кинув би `KeyNotFoundException` ще до
першого кадру. Тримай це в голові, коли додаватимеш нові стани чи сервіси.

### Фаза Б: ланцюжок станів

| # | Де | Що відбувається |
|---|---|---|
| 12 | `GameBootstrapper.cs:16` | `Enter<BootstrapState>()` |
| 13 | `GameStateMachine.cs:32–38` | `_currentState` ще `null` → `Exit()` не викликається; дістає стан зі словника за типом, викликає `Enter()` |
| 14 | `BootstrapState.cs:26` | `Enter<LoadLevelState, string>("Level_Arena")` — **стан сам вирішує, куди далі** |
| 15 | `GameStateMachine.cs:43` | `BootstrapState.Exit()` (порожній, `TODO`) |
| 16 | `LoadLevelState.cs:23–24` | `Show()` завіси, `_sceneLoader.Load("Level_Arena", onLoaded)` |
| 17 | `SceneLoader.cs:15–18` | активна сцена — `Bootstrap`, не збігається → корутину запускає `GameBootstrapper` (через `ICoroutineRunner`) |
| 18 | `SceneLoader.cs:23–28` | `LoadSceneAsync`; щокадру `yield return null`, поки `isDone == false`. Unity вивантажує `Bootstrap` (крім `DontDestroyOnLoad`-об'єктів), вантажить `Level_Arena` |
| 19 | `SceneLoader.cs:28` → `LoadLevelState.cs:27` | колбек `onLoaded` → `Enter<GameLoopState>()` |
| 20 | `GameStateMachine.cs:32` → `LoadLevelState.cs:29` | `Exit()` попереднього стану ховає завісу |
| 21 | `GameLoopState.cs:24–25` | `LoadAsset("Player")` → `Resources.Load`; `SpawnAsset(prefab, (0, 1, 0), identity)` → `Instantiate` |
| 22 | `GameLoopState.cs:26–27` | `GetComponent<PlayerController>()` → `Construct(_inputService)` |
| 23 | `PlayerController.cs:19–20` | зберігає сервіс, підписує `Jump` на `OnJumpPressed` |

**Ще один прихований порядок — у кроках 21–23.** `Instantiate` одразу викликає `Awake()` і
`OnEnable()` нового об'єкта, а `Update()` — лише з наступного кадру. Тому `Construct` встигає
до першого `Update()`, і `_inputService` там уже не `null`. Якщо колись перенести читання
сервісу в `Awake()` гравця, він буде `null`: `Awake()` спрацює всередині `Instantiate`, ще
до рядка 27.

### Фаза В: кожен кадр (`PlayerController.Update`, `PlayerController.cs:23–36`)

| Рядок | Що | Навіщо |
|---|---|---|
| 25–26 | на землі й падає → `_verticalSpeed = -2` | притиснути до підлоги, щоб `isGrounded` не мерехтів; не скидати накопичену швидкість падіння в нуль |
| 28–29 | на землі → прочитати `GetDirection()`, перевести `Vector2 (x, y)` у `Vector3 (x, 0, z)` | у повітрі напрямок заморожений — керувати польотом не можна |
| 31, 50–55 | `_verticalSpeed += g·dt`, зсув `= _verticalSpeed·dt` | гравітація: швидкість росте щокадру, зсув — від швидкості |
| 33, 44–48 | зсув `= напрямок · _moveSpeed · dt` | рівномірний рух по землі |
| 35 | `_characterController.Move(сума)` | один виклик на кадр — CC сам розв'язує зіткнення |

**Стрибок — подією, не опитуванням:**

```
Space → Input System → InputService.cs:16 (Jump.performed) → OnJumpPressed?.Invoke()
      → PlayerController.Jump() (PlayerController.cs:57–63)
      → якщо isGrounded: _verticalSpeed = √(2·g·h) = √(2·9.81·1.5) ≈ 5.42 м/с
```

Чому формула така — `Docs/Notes/Physics_and_Movement/Kinematics_For_Jump.md`, розділ
«Мінімум» (крок 2) і 3.5. Звідти ж «Приклад C» у 3.2 (`v0 ≈ 5.425`) — це саме цей стрибок.

Відписка — `PlayerController.cs:65–69`, в `OnDestroy()`. `InputService` живе всю гру, а
гравець може бути знищений. Без `-=` подія тримала б посилання на мертвий компонент, і
наступний Space викликав би `Jump()` на знищеному об'єкті.

---

## 4. Кожен клас — чому він такий

### `GameBootstrapper` — Composition Root

Єдине місце, де Unity-світ передає керування нашому коду. Усе, що далі, створюється
**тут і вниз по ланцюжку**, а не шукається через `FindObjectOfType`. Завдяки цьому весь граф
залежностей видно, якщо пройти від `Awake()` по конструкторах (фаза А вище).

Реалізує `ICoroutineRunner` «безкоштовно»: у `MonoBehaviour` вже є метод
`StartCoroutine(IEnumerator)` з тією самою сигнатурою, тож інтерфейс просто «називає» те,
що вже є.

### `Game`

Перший звичайний C#-клас. Існує, щоб `GameBootstrapper` лишався тонким (лише Unity-речі:
`Awake`, `DontDestroyOnLoad`, `[SerializeField]`), а збірка графу жила в класі без Unity.
Єдине місце, де написано `AllServices.Instance`, — далі реєстр передається параметром.

### `ICoroutineRunner` + `SceneLoader` — DIP

`SceneLoader` потребує корутину, але не хоче бути `MonoBehaviour`. Він залежить від
**вузької абстракції** (один метод), а конкретну річ (`GameBootstrapper`) підставляє
Composition Root. Варіант без інтерфейсу:

```csharp
// гірше: SceneLoader прив'язаний до конкретного класу і до того, що він існує як синглтон
public class SceneLoader
{
    public void Load(string sceneName, Action onLoaded = null)
    {
        GameBootstrapper.Instance.StartCoroutine(LoadScene(sceneName, onLoaded));
    }
}
```

Тут `SceneLoader` знає про `GameBootstrapper`, а `GameBootstrapper` мусить мати статичний
`Instance`. З інтерфейсом жоден з них не знає про іншого — їх зводить `Game.cs:13`.

### `LoadingCurtain`

Найпростіший `MonoBehaviour`: `SetActive(true/false)`. Важливо не що він робить, а
**де живе**: дитина `Bootstraper`, тому `DontDestroyOnLoad` береже і його. Інакше завіса
зникла б разом зі сценою `Bootstrap` посеред завантаження, і `LoadLevelState.Exit()`
звертався б до знищеного об'єкта.

### Стани і `GameStateMachine` — State (GoF)

- **Три інтерфейси замість одного (ISP).** `BootstrapState`/`GameLoopState` не мають що
  приймати → `IState.Enter()`. `LoadLevelState` потребує назву сцени →
  `IPayloadedState<string>.Enter(string)`. Обидва — `IExitableState`, тому влазять в
  один словник `Dictionary<Type, IExitableState>` (`GameStateMachine.cs:13`).
- **Тип як ключ.** `Enter<LoadLevelState>()` → `_states[typeof(LoadLevelState)]` → `as TState`.
  `as` потрібен, бо в словнику лежить `IExitableState`, а викликати треба `Enter()`, якого
  в `IExitableState` немає.
- **Обмеження `where TState : class, IState`** не дасть скомпілювати
  `Enter<LoadLevelState>()` без payload: `LoadLevelState` не реалізує `IState`. Помилку
  ловить компілятор, а не гра.
- **Машина не знає порядку станів (OCP).** Куди йти далі, вирішує сам стан (`BootstrapState.cs:26`,
  `LoadLevelState.cs:27`). Новий стан = новий клас + рядок `_states.Add(...)`.
- **`Exit()` — симетрія до `Enter()`.** `LoadLevelState`: показав завісу в `Enter`, ховає в
  `Exit`. Перед кожним новим `Enter` машина викликає `Exit` попереднього
  (`GameStateMachine.cs:32/43`).

### `AllServices` + `IService` — Service Locator

- **Синглтон** (приватний конструктор + `static Instance { get; } = new ...`): один реєстр на
  гру, і ніхто ззовні не зробить другий. Свідомий виняток із «жодних статичних
  синглтонів», і зверненням до `Instance` обмежене одним місцем (`Game.cs:14`).
- **Ключ — інтерфейс** (`typeof(TIService)`). Хто дістає, просить `IInputService` і не знає,
  що за ним `InputService`. Замінити реалізацію = змінити один рядок реєстрації.
- **`IService` порожній** — це не поведінка, а «перепустка»: обмеження
  `where TIService : class, IService` не пустить у реєстр довільний об'єкт.
- **`where TService : class, TIService`** у `RegisterService` — компілятор перевіряє, що
  `AssetProvider` справді реалізує `IAssetProvider`. `RegisterService<IInputService, AssetProvider>()`
  не скомпілюється.

### `ServicesResolver` — reflection-експеримент (опційне ДЗ уроку 03)

Замість `new AssetProvider()` руками реєстр просить resolver: «ось тип, створи».
Resolver бере конструктор, дивиться типи параметрів, рекурсивно створює кожен і викликає
конструктор з ними. Зараз обидва сервіси без параметрів, тож рекурсія не заглиблюється.

Лежить в `Experimental/`, бо ще не прийнятий: Play Mode не підтверджено, і є дві відкриті
зауваги з 2026-10-04 (`SESSION_NOTES.md`, «Висить і чекає на студента»). Вирішувати їх —
тобі; подробиці механіки — `Notes/CSharp/Reflection_Basics.md`.

### `IAssetProvider` / `AssetProvider` — Provider

Решта коду не знає, **звідки** береться префаб: зараз `Resources.Load`, потім (урок 15)
Addressables. Заміна реалізації не зачепить `GameLoopState` — він тримає інтерфейс.
Логи `[AssetProvider] ...` — щоб у Console одразу було видно, який шлях не знайшовся.

### `IInputService` / `InputService` — адаптер над Input System

- **Два способи віддати інпут, бо інпут двох видів.** Рух — неперервна величина, її
  опитують щокадру (`GetDirection()`). Стрибок — мить, її розсилають подією
  (`OnJumpPressed`).
- **Подія-переклад.** `InputService` підписаний на Unity-подію `Jump.performed`
  (з параметром `CallbackContext`) і перевидає свою, без Unity-типів. Споживач не знає про
  Input System взагалі.
- `InputService` — єдиний файл у `CodeBase`, що працює з `InputActions`; що поза ним
  немає `UnityEngine.InputSystem`, перевіряє `Docs/tools/verify_lesson.sh`. Звідси крок 6 уроку 04: другий клас з `IInputService`
  міняється одним рядком у `BootstrapState.cs:32`.

### `GameLoopState`

Отримує сервіси готовими через конструктор (не лізе в `AllServices` сам) — тому з
сигнатури видно, що йому треба. Спавнить гравця й передає йому інпут. Сам на інпут більше
не підписаний (`TestJump` з уроку 03 прибраний) — цим тепер займається гравець.
`GameStateMachine` не отримує: поки нікуди не переходить (з'явиться в уроці 13).

### `PlayerController`

Рух через `CharacterController`, а не фізику: CC не реагує на сили, швидкістю й
гравітацією керуємо самі (`_verticalSpeed`). Порівняння підходів —
`Notes/Physics_and_Movement/Movement_Approaches.md`. Rigidbody-варіант — ДЗ на гілці
`phisicsPractice`.

---

## 5. «Шви» — рядки й поля, що зв'язують код з асетами

Компілятор їх не перевіряє: перейменуєш з одного боку — зламається лише в Play Mode.

| У коді | Пов'язане з | Що станеться при розсинхроні |
|---|---|---|
| `"Level_Arena"` (`BootstrapState.cs:10`) | ім'я сцени + її наявність у Build Settings | `LoadSceneAsync` не знайде сцену, помилка в Console, завіса так і висить |
| `"Player"` (`GameLoopState.cs:12`) | `Assets/Resources/Player.prefab` | `LoadAsset` поверне `null` (лог `Failed to load`), потім `NullReferenceException` у `SpawnAsset` на `asset.name` |
| `GetComponent<PlayerController>()` (`GameLoopState.cs:26`) | компонент на префабі | `null` → `NullReferenceException` на `Construct` |
| `[SerializeField] _curtain` (`GameBootstrapper.cs:9`) | `Curtain`, перетягнутий в інспекторі | `null` → `NullReferenceException` на `Show()` |
| `[SerializeField] _characterController` (`PlayerController.cs:8`) | CC на тому ж префабі | `NullReferenceException` у першому `Update` |
| `Player.Move`, `Player.Jump` (`InputService.cs:15–16, 21`) | карта й дії в `InputActions.inputactions` | після перегенерації `InputActions.cs` не скомпілюється — це шов, який компілятор таки ловить |
| `_moveSpeed`, `_jumpHeight` | значення на префабі (6, 1.5) | якщо 0 — гравець стоїть / не стрибає, без жодних помилок |

---

## 6. Що незакрите

| Що | Де описано |
|---|---|
| Урок 04, крок 6 — другий `IInputService` (не клавіатура) | `Lessons/04_player_controller.md`, «Крок 6» + «Крок 6 — пояснення» |
| ⭐ Rigidbody-ДЗ — рух через фізику | гілка `phisicsPractice`; умови там само, «Опційне ДЗ ⭐» |
| `ServicesResolver` — Play Mode + дві зауваги | `SESSION_NOTES.md`, «Висить і чекає на студента» |
| `BootstrapState.Exit()` — `// TODO`, `GameLoopState.Exit()` — порожній | поки нічого прибирати |
| Дія `Fire` в `InputActions` | є в карті, код її не читає |
| `TestObject.prefab` | лишився з уроку 02, ніхто не вантажить |
| `ARCHITECTURE.md`, `PATTERNS.md` під урок 04 | оновлюються після закриття уроку 04 |

---

## 7. Маршрут повторення

Відкривай файли в цьому порядку — він збігається з тим, як виконується гра:

1. `GameBootstrapper.cs` → `Game.cs` → `GameStateMachine.cs` (лише конструктор)
2. `BootstrapState.cs` → `AllServices.cs` → `ServicesResolver.cs`
3. `AssetProvider.cs`, `InputService.cs` (+ відкрий `InputActions.inputactions` у Unity)
4. `GameStateMachine.cs` (методи `Enter`) → `BootstrapState.Enter` → `LoadLevelState.cs` → `SceneLoader.cs`
5. `GameLoopState.cs` → `PlayerController.cs`
6. У Unity: сцена `Bootstrap`, ієрархія `Bootstraper/Curtain`, інспектор обох; префаб `Player`
7. Play зі сцени `Bootstrap` → Console: `[Services] Registered ...` ×2 → `[FSM] ... created ...`
   → `Enter BootstrapState` → `Enter LoadLevelState` → `Enter GameLoopState` →
   `[AssetProvider] Loaded Player` → `Spawning Player ...`

### Перевір себе (відповідай своїми словами, перш ніж дивитись у код)

1. Чому `GameStateMachine` може викликати `GetService<IInputService>()` у своєму конструкторі,
   якщо реєструє сервіси інший клас?
2. Навіщо `SceneLoader` отримує `ICoroutineRunner`, а не `GameBootstrapper`?
3. Що зламається, якщо `Curtain` витягнути з-під `Bootstraper` на верхній рівень сцени?
4. Чому `Enter<LoadLevelState>()` (без payload) не скомпілюється?
5. Чому рух читається через `GetDirection()` щокадру, а стрибок приходить подією?
6. Чому залежність у `PlayerController` приходить через `Construct`, а не через конструктор?
7. Що конкретно поміняється в коді, коли ти робитимеш крок 6 уроку 04, і що — ні?
