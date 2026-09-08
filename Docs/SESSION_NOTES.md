# Нотатка для наступної сесії

Не історія (для цього `git log -3`) і не деталі (для цього `ROADMAP.md` §6 + `ls
Docs/Lessons/`) — тільки те, що з них не видно: на чому саме зупинились.

## Урок 02 — ЗАКРИТО (2026-09-08)

Повне рев'ю пройдене, Play Mode підтверджено (Console: реєстрація `IAssetProvider` →
побудова всіх станів → `Enter BootstrapState → Enter LoadLevelState → Enter
GameLoopState` → `[AssetProvider] Loaded TestObject` → `[AssetProvider] Spawning
TestObject ...`, без винятків). Усі формальності закриті цієї сесії:
- `ROADMAP.md` §6 — чекбокс уроку 02 позначено `[x]`.
- `Docs/PATTERNS.md` — розділ уроку 02 написано (Service Locator, Provider, YAGNI-нотатка
  про прибирання невикористаних параметрів конструктора).
- `Docs/HISTORY.md` — розділ уроку 02 додано у форматі занурення/повернення, з
  реального фінального коду.
- `Docs/CV_LOG.md` — новий розділ "Урок 02" зі статусами `[перевірено]`.
- `Docs/tools/verify_lesson.sh` — додано кейс `02` (8 перевірок, усі проходять).

**Під час рев'ю знайдено й виправлено (студентом):**
- Невикористані параметри конструктора: `SceneLoader sceneLoader` у `BootstrapState`,
  `GameStateMachine stateMachine` у `GameLoopState` — обидва прибрані (скопійовані по
  аналогії з сусідніх станів, реально не використовувались; `GameLoopState` поверне собі
  `GameStateMachine` в уроці 13, коли почне сам ініціювати переходи у
  `VictoryState`/`GameOverState`).
- `BootstrapState._stateMachine`/`_services` — додано `readonly`.
- Лог-теги розведені по джерелу: `[FSM]` тільки в станах і `GameStateMachine`,
  `[Services]` в `AllServices`, `[AssetProvider]` в `AssetProvider` (спочатку все було
  позначено спільним `[FSM]`, що робило фільтр по тегу марним).

**Ще не закомічено в git** — working tree на момент цієї нотатки містить усі зміни
уроку 02 незакомічені. Запропоновано студенту два окремі коміти (код студента окремо
від правки `verify_lesson.sh`), описи англійською — чекаємо, поки студент сам зробить
`git add`/`git commit`, не виконувати за нього.

**Наступний крок:** урок 03 (`IInputService`, другий сервіс у контейнері,
`ROADMAP.md` §4 — Input навмисно йде другим, щоб закріпити "етику роботи із сервісами"
на конкретному прикладі) — ще не написаний, workflow A.
