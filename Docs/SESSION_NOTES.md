# Нотатка для наступної сесії

Не історія (для цього `git log -3`) і не деталі (для цього `ROADMAP.md` §6 + `ls
Docs/Lessons/`) — тільки те, що з них не видно: на чому саме зупинились.

## Docs тепер читаються через VS Code, не Calibre (2026-09-16)

Студент більше не читає `Docs/` через Calibre e-book viewer — перейшов на VS Code.
Зроблено: окремий `petPet-Notes.code-workspace` у корені репо (відкриває тільки
`Docs/` окремо від важкого Unity-дерева, рекомендує Markdown All in One +
markdownlint, `workbench.startupEditor: readme` — авто-відкриває `Docs/README.md`
у Preview при старті, хоча ця настройка іноді працює тільки з User Settings, а не
workspace — якщо не спрацює саме, `Ctrl+Shift+V` вручну). Додано `Docs/README.md` —
індекс-точка входу з посиланнями на всі top-level файли й `Notes/README.md`.

Усі 123 фрагменти коду/діаграм/консольного виводу в `Docs/**/*.md` (16+ файлів)
конвертовано з формату `<pre>` + дефіс-роздільники ("КОД"/"СХЕМА"/"ВИВІД") +
HTML-екранування назад на звичайні markdown ``` -огорожі (```csharp для коду, ```
без тега для діаграм/виводу) — той формат, що був **до** 2026-09-12, коли ввели
Calibre-обхід. Правило в скілі `petpet-mentor` ("Формат коду в документах")
оновлено відповідно — новий код у `Docs/` тепер одразу пишеться в ``` -огорожах.

## Опційне ДЗ уроку 03 — код готовий і закомічений, урок 04 виданий (2026-09-16)

`ServicesResolver` (constructor-only авто-резолвер) доведений до робочого стану й
закомічений трьома окремими коммітами (reflection-резолвер, фікс Input System UI
Module на сцені `Bootstrap`, конспекти). Ключове рішення сесії, відмінне від плану
нижче: резолвер **не лишився ізольованим смоук-тестом** — студент сам вирішив
інтегрувати його в робочий шлях, `AllServices.RegisterService<TIService,
TService>()` тепер завжди йде через `ServicesResolver.ResolveServiceWithTypes`,
`BootstrapState` реєструє `IAssetProvider`/`IInputService` парами
(інтерфейс+клас). Розв'язана проблема "інтерфейс не має конструктора" —
`TService` (конкретний клас) резолвиться рефлексією, `TIService` (інтерфейс) —
лише ключ словника й тип каста; `where TService : class, TIService` ловить
невідповідну пару на етапі компіляції, а не рантайм-`InvalidCastException`.
Побічно виправлено: `EventSystem` на сцені `Bootstrap` мав застарілий Standalone
Input Module, несумісний з `activeInputHandler: 1` — замінено на Input System UI
Input Module.

**Не зроблено ще (чекає на студента):** підтвердження, що це реально
запускалось/перевірялось у Play Mode. Без цього підтвердження — не оновлювати
`Docs/PATTERNS.md`/`Docs/HISTORY.md`/`Docs/CV_LOG.md`/`Docs/ARCHITECTURE.md` під
цю роботу (workflow B, крок 5 скіла `petpet-mentor`). Запитано в чаті цієї сесії,
відповідь ще не отримана.

**Виданий урок 04** — `Docs/Lessons/04_player_controller.md`
(`PlayerController`: рух+стрибок, вибір `CharacterController`/`Rigidbody`,
перше реальне застосування Construct-патерна, `GameLoopState` спавнить гравця
через уже наявний `IAssetProvider` замість смоук-тесту `TestObject`). Не
починати рев'ю уроку 04, поки студент не покаже код і не підтвердить
компіляцію+Play Mode.

**Рішення 2026-09-18:** студент обрав для уроку 04 `CharacterController`
(шутер → кінематичний рух — стандарт жанру). Про `Rigidbody` йому цікаво, тому
в кінець `Docs/Lessons/04_player_controller.md` додано опційне ДЗ ⭐ — та сама
поведінка на `Rigidbody` в окремій git-гілці, після рев'ю уроку 04. Не блокує
урок 05. Питання розуміння вже зашиті в ДЗ (три проблеми `Rigidbody`, що було
"безкоштовно" в `CharacterController`, чи змінився контракт `IInputService`).
Студент так і не відповів на мої два перевірочні питання про вибір; його
мотивація — "ця технологія поширеніша для шутера" — по суті правильна.
На рев'ю уроку 04 обов'язково запитати, як влаштований стрибок (ручна
вертикальна швидкість + гравітація), — це умова, яку я поставив студенту.

**Рішення 2026-09-19:** студент підтвердив додавання уроку `05b_animation.md`
(після камери, не раніше; файл уроку писати лише після закриття 05). Запис уже
в `ROADMAP.md` §6. Студент вважав, що референса по анімаціях у нас немає —
насправді є, але лише C#-бік: у `ref_for_claude/CodeBaseByLesson/NN/CodeBase/`
`Hero/HeroAnimator.cs`, `Enemy/EnemyAnimator.cs`, `Enemy/AnimateAlongAgent.cs`,
`Logic/IAnimationStateReader.cs`, `Logic/AnimatorState.cs`,
`Logic/AnimatorStateReporter.cs` (`StateMachineBehaviour`). Те, чого там немає:
самі `Animator Controller`/`Blend Tree`/моделі/кліпи (референс — тільки код), і
референс — мілі-RPG (один float `Walking`, тригери Attack/Hit/Die), тобто
strafe-blend-tree і aim/shoot-шари для TPS доведеться брати з офіційної
документації Unity/Starter Assets. Не показувати студенту код референса
напряму, лише форму (правило скіла). Слабкі місця референса, які можна
використати як "що не копіювати": публічне `_animator`, if/else-мапінг хешів у
`StateFor`, `GetComponent` усередині `StateMachineBehaviour`.

**Аудит покриття референсу, 2026-09-19:** студент зауважив, що карта уроків
не охоплює весь референс. Порівняно 15 знімків `CodeBaseByLesson` (нові файли
на кожному уроці) — виявились прогалини: дані рівня й Editor-інструменти,
повне збереження світу, лут, HP-бар у світі, магазин, кілька рівнів, Ads/IAP.
Студент вирішив: **Ads/IAP і рівні — "як у референсу"** (тобто реальні SDK і
два рівні). `ROADMAP.md` оновлено: §1 (GDD), §2 (Ads/IAP тепер у стеку), §3
(патерни 14–17), §6 (нова нумерація 04–19 + stretch 20). Скіл `petpet-mentor`
скоригований. Уроки 05+ файлами ще не існують, тож перенумерація нічого не
ламає (існують лише `00–04`).
Нюанси: (а) у дзеркалі референсу є код Ads, але **нема коду IAP** (README
дзеркала стверджує протилежне — там лише пакет `com.unity.purchasing` у
manifest), тож урок 18 писати з офіційної документації Unity IAP; (б) для
Ads/IAP потрібен Unity-акаунт/Project ID — попередити студента перед уроком
17; (в) конспект курсу за шляхом `Desktop\New folder (2)` на цій машині не
знайдено, вміст референсних уроків 07/13/17 (без знімків коду) невідомий;
(г) ⭐ до уроку 03 — другий `IInputService` (для скриптованого/тестового
вводу) лише запропоновано, студент не підтвердив.

**Повне дерево курсу, урок 13 (студент вказав шлях, 2026-09-19):**
`E:\syndicate\Architecture\k-syndicate.school\13\knowledge-is-power-master\
knowledge-is-power-master\src\KnowledgeIsPower\Assets` — розпаковано, з
prefab/сценами/анімаціями (у портативному дзеркалі цього нема). Раніше в чаті я
помилково сказав, що асетного боку анімацій у референсі немає — він є тут:
- `Resources/Hero/Heavy Knight PBR/AnimControllers/knight_Controller.controller`
  — 13 кліпів (idle/walk/walkBack/walkLeft/walkRight/run/jump/attack×2/hit/die/
  defend/taunt), параметри: float `Walking` + тригери `AttackNormal`, `Hit`,
  `Die` та ін.; на станах висить `AnimatorStateReporter`.
- `Resources/Enemies/EnemyAnimatorController.controller` (= `Lich.controller`) —
  float `Speed`, bool `IsMoving`, тригери `Attack_1/2`, `Hit`, `Win`, `Die`,
  `Blend Tree` по `Speed`; `Golem/OverrideController.overrideController` —
  `AnimatorOverrideController`, підміняє кліпи Голема в тому самому контролері
  (один Animator Controller на кілька моделей — тема для 05b/08).
- Також: `Resources/Infrastructure/{GameBootstrapper,GameRunner}.prefab`,
  `Resources/Hud/{Hud,Curtain,Loot}.prefab`, `Resources/UI/{Shop,ShopItem,
  Window}.prefab`, `Resources/Loot/*` (`PickupPopup.controller` — UI-анімація
  підбору), `Resources/Enemies/{SpawnMarker,SpawnPoint}.prefab`,
  `Resources/Settings/{Standalone,Touch}InputSettings.asset`, `Resources/Static
  Data/{Monsters,Levels}`, `Scenes/{Initial,Main}.unity` + `Scenes/Main/
  NavMesh.asset`, `SceneAssets/Fx/{Ambient,Beams}` (сценічні VFX), модульне
  оточення `Resources/Levels/Graveyard` (54 prefab із FBX-кіт).
Важливе: герой референсу — мілі-лицар (не стрілець), ассети моделей купувати/
брати не варто — беремо власні (Mixamo тощо); з референсу переносимо
**структуру** (контролер+параметри+reporter+override), не ассети. Скрипт
`GameRunner` як prefab у `Resources/Infrastructure` варто мати на увазі для
уроку 12 (старт із будь-якої сцени).

**Повний огляд курсу, 2026-09-19 (студент спитав "ти пройшовся по кожній теці?" —
ні, лише дзеркало й `13/Assets`; потім пройдено все):** `E:\syndicate\Architecture\
k-syndicate.school\01..17` містить відео (назви — реальний силабус), `.txt`-конспекти
(`Info.txt`, `6.txt`…`17.txt` — короткі, корисне: у `Info.txt` посилання на асети
курсу, у `6.txt` про хак у `ActorUI` для ворогів без фабрики, у `15.txt` про
вебінар по Addressables, у `17.txt` про Unity Cloud Build — платний, trial 30
днів) і розпаковані проєкти. Що змінилось у `ROADMAP.md` за цим переглядом:
`Animation events` + `OverlapSphere`-хітбокс + корутинний кулдаун + "Component
Model" як тема → в урок 08; окремий урок 14 про UI-верстку (Canvas/Anchors/
Scroll/DeviceSimulator/оптимізація — курс має на це цілий урок "UI part 1");
Addressables підвищено зі stretch до звичайного уроку 19 (IAP від нього
залежить, в курсі порядок Ads→Addressables→IAP); CI — окремий урок 22 (курсовий
фінал). Нова нумерація: 14 UI-верстка, 15 UI-вікна, 16 рівні, 17 win/lose, 18
Ads, 19 Addressables, 20 IAP, 21 поліш/білд, 22 CI. Код IAP відсутній і в
повному репозиторії курсу (лише 15 відео) — писати з відео/документації.
Асети курсу (кладовище, лицар, SimpleInput, Cartoon FX) — посилання в `01/Info.txt`
і `06/6.txt`; ми беремо власні, а не копіюємо.

**Стиль викладання з курсів студента, 2026-09-19:** студент пояснив, що дає різні
курси, щоб я перейняв навчальний стиль, і що каталог тем "не корисний". Зроблено:
прийоми з Syndicate/курсу патернів (Нестерук, C#)/Mario записані в `ROADMAP.md`
§5 "Стиль уроку" і в `SKILL.md`; пам'ять `feedback_extract_teaching_style_from_courses`.
Застосовано вже в `Docs/Lessons/04_player_controller.md`: антипатерн перед
`Construct`, крок 6 "другий споживач", мікро-перевірки по кроках, точки коміту,
"Ціна патерну". Пропозиція, що ЧЕКАЄ згоди студента: unit-тести для чистого C#
(курс патернів дає задачу з NUnit-тестами до кожного розділу). У проєкті зараз
**нема** `com.unity.test-framework` у `Packages/manifest.json` і жодного
`.asmdef` — тож це окрема робота (пакет + asmdef для CodeBase + Tests). Природна
перша ціль: `ServicesResolver`/`AllServices` (вже написані; тест закрив би й
питання "чи це реально працює", яке досі висить без відповіді) — студент пише тест
сам за списком поведінок Given/When/Then, а не з готової сигнатури.
Примітка: Mario-курс у `F:\Development\C#\5GamesUnityCourses` студент
реорганізував (розпаковані теки зникли, лишились zip і `Code/`); цей огляд зроблено
до реорганізації, Mario-курс коду не містить.

**5-games/Mario курс — усі секції, 2026-09-19** (`F:\Development\C#\5GamesUnityCourses\Code`:
194 файли, тільки `.srt`/`.docx`/`.html`, без відео і без коду — п'ять ігор: DDR Mario Mix,
Super Mario Bros 2D, Jetpack Joyride+Sunshine, Baseball, Mario VR). Витягнуто ще 5 прийомів
(ROADMAP §5, п.9–13): результат навчання на старті ("Після уроку ти вмієш пояснити"),
case study перед кодом (DDR-секція відкривається розбором оригіналу), мілстоун-білди
(4 із 5 секцій закінчуються білдом і "Playing on…"; є лекція "Considerations when building
for X"), вперед-посилання (Section Intro: "AI-базу підкласуємо далі"), і НЕПІДТВЕРДЖЕНА
пропозиція витягти `Infrastructure` у шаблон + другий мініпроєкт (курс використовує один
"reusable framework" у п'яти іграх). Застосовано в уроці 04 (питання "Після уроку…",
вперед-посилання) і в ROADMAP (мілстоун-білди після 08/13/17). Незакрито: тести
(пакет+asmdef) і шаблон-фреймворк — обидва чекають згоди студента.

**Відбір best practices, 2026-09-19:** студент попросив лише best practice — порівняти курси
й вибрати, що додати чи довершити. `ROADMAP.md` §5 "Стиль уроку" переписано: таблиця
порівняння Syndicate/патерни/Mario (сильне і слабке), 11 прийнятих прийомів з курсів, 2 додані
з науки про навчання (перепис з нуля з пам'яті; поступове зняття підказок + запитання на
пригадування), і список відкинутого (диктування коду, глобальна статика, повтор контенту).
Прийом "шаблон + другий мініпроєкт" замінено ідеєю студента "переписати інфраструктуру з
нуля" — новий урок `12b_rebuild_infrastructure.md` у §6 (після 12, без відкритого старого
коду; лише власні PATTERNS/ARCHITECTURE; потім порівняння). `SKILL.md` і пам'ять оновлені.
Ще чекає згоди студента: тести (пакет + asmdef).

**Курс з unit-тестів студента, 2026-09-19:** `E:\syndicate\K-syndicate (Knowledge Syndicate) -
Advanced Unit Testing in Unity\` — 13 відео (`01_01`…`10`, без субтитрів, тож зміст не видно) і
`repo_unit-tests_vanilla\unit-tests_vanilla_02` — це **стартовий** репозиторій ("vanilla"): Open Project
#1 (Unity "Chop Chop") + Factory-приклад з DOTS; **жодного** `[Test]`/NUnit/`asmdef` для тестів
(перевірено пошуком), `com.unity.test-framework` у manifest відсутній — тести з'являються у відео.
Тобто стиль курсу не витягти без субтитрів/готового репозиторію. Питання до студента: чи є
субтитри або "завершений" репозиторій? Тим часом тести в нашому проєкті — окремий крок, який
має спиратися саме на цей курс. Спостереження для майбутнього пояснення тестів: у нас
`AllServices` — статичний singleton (`Instance`, приватний конструктор, `Dictionary.Add`), тож
два тести, що реєструють той самий інтерфейс, впадуть з `ArgumentException` — це живий приклад
"проблеми з тестуванням" Singleton із курсу патернів.

**Рішення про тести, 2026-09-19:** студент проходитиме курс з unit-тестів сам, по відео.
Питання "чекає згоди" знято: тести не вбудовуємо в уроки, пакет `com.unity.test-framework` і
`asmdef` не додаємо, поки студент сам не захоче (ROADMAP §5 п.11 і SKILL.md оновлено).

Портативний мирор курсу склонований локально: `I:\UnityProjects\ref_for_claude`
(раніше в скілі був лише посилання на GitHub без факту клонування на цій
машині).

---

## Урок 03 — ЗАКРИТО, опційне ДЗ із зірочкою (2026-09-15) [попередній запис]

Основний чекліст уроку 03 закритий раніше (див. попередній запис нижче в git-історії
цього файлу). Зараз студент працює над **опційним ДЗ п.2** з
`Docs/Lessons/03_service_locator_and_input.md` — "constructor-only auto-resolver":
клас через рефлексію дивиться на параметри конструктора й сам підставляє відповідні
сервіси з `AllServices`, без атрибутів, не чіпаючи робочий код.

**Уже вирішено за цю сесію (не перепитувати, не переобговорювати):**

- Резолвер — **окремий static-клас** `ServicesResolver` у
  `Assets/CodeBase/Experimental/` (папка вже створена, `.meta`-файли застейджені).
  Не інтегрується в робочий `GameStateMachine`/`BootstrapState` — доводить себе
  власним смоук-тестом, ізольовано.
- Обране рішення "кілька конструкторів" — **варіант 1**: вимагати рівно один
  публічний конструктор (`GetConstructors().Single()`), а не "жадібний" підбір
  найкращого з кількох (варіант 3). Мотивація: жоден сервіс у проєкті сьогодні
  (`AssetProvider`, `InputService`) не має кількох конструкторів — варіант 3 був би
  YAGNI. **Студент попросив показати варіант 3 пізніше, окремо, просто для
  ознайомлення** — ще не дано, він чекає на це після завершення варіанту 1.
  Не пропонувати самому, чекати, поки попросить.
- Резолвер призначений резолвити **сервіси** (типи, зареєстровані в `AllServices`,
  тобто `IService`), НЕ стани `GameStateMachine`. Смоук-тест варто робити на класі
  типу `GameLoopState` (обидва параметри конструктора — реальні сервіси), а не на
  `BootstrapState`/`LoadLevelState` (їхні параметри — `GameStateMachine`,
  `SceneLoader`, `LoadingCurtain` — жоден не `IService`, резолвер на них
  принципово не спрацює: `MakeGenericMethod` кине `ArgumentException` на
  `where TService : class, IService`).

**Поточний стан коду (незакомічено, `git status`):**

`Assets/CodeBase/Experimental/ServicesResolver.cs` — чернетка студента, **ще не
компілюється**:
```
public static class ServicesResolver
{
    public TService Resolve<TService>(TService service) where TService : class, IService
    {
        Type type = typeof(TService);
        ConstructorInfo constructor = type.GetConstructors().Single();
        ParameterInfo[] parameters = constructor.GetParameters();
        object serviceInstant = constructor.Invoke(null);
    }
}
```
Відомі студенту (уже пояснено в чаті цієї сесії, не пояснювати повторно з нуля):
метод без `static` у static-класі не скомпілюється; сигнатура з `TService service`
як параметром концептуально дивна (мета — саме СТВОРИТИ інстанс, а не отримати
готовий); `Invoke(null)` ігнорує реальні параметри конструктора замість зібрати їх
через `AllServices.GetService<T>()` (для кожного `parameters[i]`, через
`MakeGenericMethod`, бо `T` відомий лише в рантаймі); методу бракує `return`.
**Не виправляти за студента** — student writes it himself, обговорено явно
("щоб я так нічому не навчився, якщо на готове").

Також є дрібна незакомічена правка в `AllServices.cs` (`var` → явний `TService` у
`GetService<TService>()`) — не суттєво, ймовірно студентське.

**`Docs/Notes/Reflection_Basics.md` сьогодні повністю переписаний з нуля** (двічі:
спершу як "кроки побудови резолвера", потім, після різкого фідбеку студента,
перероблено як **довідник за задачами** — таблиця "хочу зробити X → який API" +
9 розділів, кожен ізольований приклад, мінімум `Console.WriteLine`-шуму, і що
найважливіше — **саме рішення ДЗ у файлі більше не описане**. Це фінальна версія,
подобається студенту, взірець формату нотаток надалі.

**Наступний крок:** дочекатись, поки студент сам допише/поправить
`ServicesResolver.cs`, тоді рев'ю. Далі — урок 04 (`PlayerController`), як і
раніше, не починати без явного підтвердження готовності студента.

---

## Урок 03 — ЗАКРИТО (2026-09-12) [попередній запис]

Увесь чекліст уроку виконано, рев'ю пройдено, Play Mode підтверджено логами (реєстрація
`IAssetProvider`+`IInputService`, повний прохід FSM, реальне натискання Jump доходить до
`GameLoopState.TestJump()` через `InputService.OnJumpPressed`). `ROADMAP.md` §6 позначено
`[x]`. `Docs/PATTERNS.md`, `Docs/HISTORY.md`, `Docs/CV_LOG.md` оновлені під фінальний код.

За сесію також, за явним проханням студента, консолідовано нотатку
`Docs/Notes/InputAction_Events_and_CallbackContext.md` (поглиблений розбір
`.started`/`.performed`/`.canceled`, `CallbackContext`, кілька реальних варіантів
використання, хто насправді викликає `Invoke()` для вбудованих Unity-подій) і злито
`Delegates_and_Action.md` + `Events_and_Subscriptions.md` в один файл
`Delegates_Events_and_Subscriptions.md`.

Окремо: усі код-блоки в `Docs/**/*.md` переведено з ```` ``` ````-огорож на сирий HTML
`<pre>` з дефіс-роздільниками — студент читає ці файли через Calibre e-book viewer, який
не показує ```` ``` ````-блоки коректно (рядки зливаються в один абзац). Формат
задокументований у скілі `petpet-mentor` (розділ "Формат коду в документах") —
дотримуватись і надалі для будь-якого нового коду в `Docs/`.

Окрема поведінкова примітка на майбутнє (від студента, 2026-09-12): навіть для чисто
механічних кроків (реєстрація/прокидання вже відомого патерна) — не вказувати, в якому
саме файлі/методі/порядку це має статись; описувати лише "що має стати правдою" і
лишати вибір місця студенту. Деталі — `feedback_dont_prescribe_placement` у пам'яті.
