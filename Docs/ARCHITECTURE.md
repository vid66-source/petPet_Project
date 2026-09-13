# Архітектура проєкту — дерево залежностей

Живий документ поточного стану проєкту: хто кого створює, хто від чого залежить, що
куди передається, хто на що підписується. На відміну від `Docs/HISTORY.md`
(наскрізний наратив виконання, урок за уроком, з поясненням "чому") — тут **тільки
поточний стан**, стисло, з візуальною схемою. Оновлюється в кінці кожного уроку,
одразу після `Docs/HISTORY.md`/`Docs/PATTERNS.md`/`Docs/CV_LOG.md` (той самий гейт:
код пройшов рев'ю **і** протестований у Play Mode).

**Легенда стрілок:**
- `──new──▶` — створює екземпляр (`new X(...)`), найчастіше одразу передаючи його
  далі в конструктор.
- `──register──▶` — кладе в `AllServices` (`RegisterService<TService>()`).
- `──resolve──▶` — дістає з `AllServices` (`GetService<TService>()`).
- `──subscribe──▶` — підписується на подію (`+=`), з відповідною відпискою (`-=`) в
  `Exit()`, якщо клас — довготривалий підписник.
- `──Enter<T>()──▶` — стан сам ініціює перехід у наступний стан (викликає
  `_stateMachine.Enter<TState>()`/`Enter<TState, TPayload>()` зсередини свого
  `Enter()`/колбека) — рішення "що далі" належить конкретному стану, не
  `GameStateMachine`.

---

## 1. Дерево створення (Composition Root)

Хто кого фізично будує, в якому порядку, починаючи з єдиної точки входу
(`GameBootstrapper.Awake()`):

--------------------------- СХЕМА ---------------------------
<pre>
GameBootstrapper (MonoBehaviour, Composition Root, реалізує ICoroutineRunner)
 │
 ├──new──▶ Game
 │           │
 │           ├──new──▶ SceneLoader(coroutineRunner)
 │           │           (coroutineRunner = сам GameBootstrapper, як ICoroutineRunner)
 │           │
 │           └──new──▶ GameStateMachine(sceneLoader, curtain, AllServices.Instance)
 │                       │
 │                       ├──new──▶ BootstrapState(this, services)
 │                       │           │
 │                       │           └── RegisterServices() — див. §2 нижче
 │                       │
 │                       ├──new──▶ LoadLevelState(this, sceneLoader, curtain)
 │                       │           │
 │                       │           └── curtain = LoadingCurtain (MonoBehaviour,
 │                       │               перетягнутий в інспекторі GameBootstrapper)
 │                       │
 │                       └──new──▶ GameLoopState(assetProvider, inputService)
 │                                   (обидва параметри — resolve з AllServices,
 │                                    див. §2 нижче, не new)
 │
 └── DontDestroyOnLoad(this) — зберігає GameBootstrapper і всю дочірню ієрархію
     (включно з LoadingCurtain) між сценами
</pre>
------------------------------------------------------------

## 2. Ланцюжок переходів станів (FSM runtime)

Хто з ким "передає естафету". `GameStateMachine.Enter<TState>()` лише механічно
дістає стан за типом і викликає його `Enter()`/`Exit()` — рішення, **який саме**
стан наступний, приймає щоразу конкретний стан-виконавець, не сама машина
(доказ OCP: додати новий перехід — це правка одного стану, не `GameStateMachine`):

--------------------------- СХЕМА ---------------------------
<pre>
BootstrapState.Enter()
 │
 └──Enter&lt;LoadLevelState, string&gt;(SceneName)──▶ LoadLevelState.Enter(sceneName)
                                                    │
                                                    └── показує завісу, стартує
                                                        _sceneLoader.Load(...)
                                                        │
                                                        └── (коли сцена довантажена)
                                                            onLoaded()
                                                             │
                                                             └──Enter&lt;GameLoopState&gt;()──▶ GameLoopState.Enter()
                                                                                            (кінець ланцюжка,
                                                                                            поки немає переходу далі)
</pre>
------------------------------------------------------------

## 3. Реєстр сервісів (`AllServices`)

`AllServices` — не дерево, а центральний хаб: хтось кладе сервіс, хтось інший його
дістає, вони не знають одне про одного напряму.

--------------------------- СХЕМА ---------------------------
<pre>
                    AllServices (eager static singleton)
                             ▲
                             │
   BootstrapState.RegisterServices() ──register──▶ IAssetProvider (= AssetProvider)
                                      ──register──▶ IInputService  (= InputService)

   GameStateMachine (у власному конструкторі)
                                      ──resolve──▶ IAssetProvider ──▶ передає в GameLoopState
                                      ──resolve──▶ IInputService  ──▶ передає в GameLoopState
</pre>
------------------------------------------------------------

## 4. Runtime-зв'язки (події)

Єдина подія в проєкті поки — `IInputService.OnJumpPressed`. Дві окремі половини:
хто її **зсередини** ретранслює з Unity, і хто на неї **зовні** підписаний.

--------------------------- СХЕМА ---------------------------
<pre>
Unity Input System (рушій, NativeInputRuntime — не наш код)
 │
 └──invokes──▶ InputService: _inputActions.Player.Jump.performed (лямбда в конструкторі)
                 │
                 └── OnJumpPressed?.Invoke()
                       │
                       └──subscribe──▶ GameLoopState.TestJump()
                             (підписка в Enter(), відписка в Exit() — симетрична пара)
</pre>
------------------------------------------------------------

## 5. Класи — тезисно: залежності, що створює/реєструє, ключові методи/події

- **`GameBootstrapper`** (`Infrastructure/GameBootstrapper.cs`, `MonoBehaviour`) —
  залежності: `[SerializeField] LoadingCurtain _curtain` (з інспектора). Створює:
  `Game`. Реалізує `ICoroutineRunner` (дає `Game`/`SceneLoader` доступ до
  `StartCoroutine`, не будучи самі `MonoBehaviour`).

- **`Game`** (`Infrastructure/Game.cs`) — залежності конструктора:
  `ICoroutineRunner`, `LoadingCurtain`. Створює: `SceneLoader`, `GameStateMachine`.
  Публічно віддає: `StateMachine` (властивість, читає `GameBootstrapper`).

- **`SceneLoader`** (`Infrastructure/SceneLoader.cs`) — залежність: `ICoroutineRunner`.
  Нічого не створює. Ключовий метод: `Load(sceneName, onLoaded)` — приймає колбек
  (`Action`), викликає його по завершенню `SceneManager.LoadSceneAsync`.

- **`GameStateMachine`** (`Infrastructure/States/GameStateMachine.cs`) — залежності:
  `SceneLoader`, `LoadingCurtain`, `AllServices`. Створює: `BootstrapState`,
  `LoadLevelState`, `GameLoopState` (усі одразу в конструкторі, кладе в
  `Dictionary<Type, IExitableState>`). Резолвить з `AllServices`: `IAssetProvider`,
  `IInputService` — обидва передає в `GameLoopState`. Ключові методи:
  `Enter<TState>()`, `Enter<TState, TPayload>()`.

- **`BootstrapState`** (`Infrastructure/States/BootstrapState.cs`) — залежності:
  `GameStateMachine`, `AllServices`. Створює й реєструє: `AssetProvider` (як
  `IAssetProvider`), `InputService` (як `IInputService`) — обидва через
  `RegisterServices()`, викликається з конструктора. `Enter()` ініціює перехід у
  `LoadLevelState`.

- **`LoadLevelState`** (`Infrastructure/States/LoadLevelState.cs`) — залежності:
  `GameStateMachine`, `SceneLoader`, `LoadingCurtain`. Нічого не створює/реєструє.
  `Enter(sceneName)` показує завісу й викликає `_sceneLoader.Load(...)` з приватним
  колбеком `onLoaded`, який переходить у `GameLoopState`. `Exit()` ховає завісу.

- **`LoadingCurtain`** (`Infrastructure/Logic/LoadingCurtain.cs`, `MonoBehaviour`) —
  без залежностей. Методи: `Show()`/`Hide()` (`gameObject.SetActive`).

- **`GameLoopState`** (`Infrastructure/States/GameLoopState.cs`) — залежності:
  `IAssetProvider`, `IInputService` (обидва отримані готовими, не резолвить сам).
  `Enter()`: вантажить і спавнить `TestObject` через `_assetProvider`, підписується
  на `_inputService.OnJumpPressed`. `Exit()`: симетрично відписується.

- **`AllServices`** (`Infrastructure/Services/AllServices.cs`) — без залежностей
  (eager static singleton, `Instance`). Методи: `RegisterService<TService>()`,
  `GetService<TService>()`, обидва з `where TService : class, IService`.

- **`IService`** (`Infrastructure/Services/IService.cs`) — маркерний інтерфейс, без
  методів, без залежностей. Generic-обмеження для `AllServices`.

- **`IAssetProvider`/`AssetProvider`** (`Infrastructure/AssetManagement/`) —
  залежності: тільки `UnityEngine` (`Resources`, `Object`). Методи:
  `LoadAsset(path)`, `SpawnAsset(asset, position?, rotation?)`.

- **`IInputService`/`InputService`** (`Infrastructure/Input/`) — залежності:
  `UnityEngine.InputSystem` (єдине місце в проєкті, де він імпортований),
  згенерований `InputActions`. Методи/подія: `GetDirection()` (poll,
  `ReadValue<Vector2>()`), `event Action OnJumpPressed` (ретранслює
  `_inputActions.Player.Jump.performed`).

---

Детальніше про кожен клас, крок за кроком з "чому" — `Docs/HISTORY.md`. Про названі
патерни й SOLID-приклади — `Docs/PATTERNS.md`.
