# Урок 00 — Налаштування проєкту

Мета цього уроку: підготувати Unity-проєкт так, щоб у наступному уроці можна було одразу
писати код Composition Root'а, не відволікаючись на інфраструктуру. Коду тут ще не буде —
тільки налаштування редактора, пакети, сцени та скелет папок.

Це "завдання", не "лекція" — виконай кроки сам у Unity Editor / Rider, а не проси мене
зробити це за тебе. Якщо на якомусь кроці незрозуміло *чому* так — питай, перш ніж рухатись далі.

---

## Крок 1. Встановити пакети

Відкрий **Window → Package Manager**, вкладка **Unity Registry**, і встанови по назві (кнопка
"Add package by name..." або пошук):

1. `com.unity.inputsystem` — новий Input System.
2. `com.unity.cinemachine` — камера.

**Чому новий Input System, а не старий `Input.GetAxis`?**
Старий Input Manager — це рядкові назви осей (`"Horizontal"`, `"Fire1"`) розкидані по коду,
жодної абстракції. Новий Input System дозволяє описати дії (`Move`, `Look`, `Fire`,
`Jump`) окремим ассетом і підписуватись на них через C# events — це природно лягає на
патерн **Strategy**, який ми будемо реалізовувати як `IInputService` (аналог
`IInputService`/`StandaloneInputService`/`MobileInputService` з референс-проєкту, тільки
замість Standalone/Mobile в нас буде, наприклад, Keyboard/Gamepad).

**Чому Cinemachine, а не ручний скрипт камери?**
TPS-камера (обертання навколо гравця по плечу, згладжене слідування, обробка колізій з
геометрією рівня) вручну — це багато математики заради речей, які Cinemachine вже вирішує.
Наше завдання — вчити архітектуру гри, а не писати камеру з нуля.

---

## Крок 2. Увімкнути новий Input System

Після встановлення `com.unity.inputsystem` Unity запропонує перезапустити редактор і
змінити Active Input Handling. Якщо запиту не було — зроби вручну:

**Edit → Project Settings → Player → Other Settings → Active Input Handling** → постав
**"Input System Package (New)"** (НЕ "Both").

**Чому не "Both"?** "Both" залишає стару систему поруч — спокуса випадково викликати
`Input.GetKey` замість того, щоб пройти через `IInputService`, і архітектура почне текти.
Обираючи тільки новий Input System, ти фізично не зможеш використати старий API — Unity
просто не дасть скомпілюватись.

Після зміни Unity попросить перезапустити редактор — погоджуйся.

---

## Крок 3. Скелет папок у `Assets/CodeBase`

Створи (поки що порожню) структуру папок — вона повторює референс-проєкт, адаптовану під
шутер. Порожні папки Unity не тримає в git без файлів усередині — це нормально, вони
наповняться в наступних уроках. Створювати можна прямо в Unity (Project window → right
click → Create → Folder) або в файловому провіднику/Rider.

```csharp
Assets/CodeBase/
  Infrastructure/
    States/
    Factory/
    AssetManagement/
  Services/
    PersistentProgress/
    SaveLoad/
    StaticData/
    Input/
  StaticData/
  Data/
  Player/
  Weapons/
  Enemy/
  UI/
    Windows/
    Elements/
    Services/
  CameraLogic/
```

Не створюй жодного `.cs`-файлу зараз — це буде зміст уроку 01. Мета цього кроку —
щоб структура проєкту вже існувала, коли ми почнемо класти в неї класи.

---

## Крок 4. Сцени

У проєкті зараз одна сцена — `Assets/Scenes/SampleScene.unity`. Нам потрібні дві:

1. **Bootstrap** — порожня сцена, єдина мета якої — містити `GameBootstrapper` (з'явиться в
   уроці 01) і одразу передати керування далі. Вона ніколи не показується гравцю як
   геймплей.
2. **Level_Arena** — сцена, де відбувається сам геймплей (арена з ворогами).

Дій так:
- Перейменуй `SampleScene` на `Level_Arena` (Project window → right click → Rename), або
  створи нову сцену з такою назвою і видали `SampleScene` — обирай сам, головне щоб у
  проєкті не залишилось порожньої `SampleScene`, яка нічого не означає.
- Створи нову сцену **Bootstrap** (File → New Scene → Basic (Built-in) → Save As
  `Assets/Scenes/Bootstrap.unity`).

Додай обидві в **File → Build Settings → Scenes In Build**, у порядку:
1. `Bootstrap` (індекс 0 — це та, що завантажиться першою при запуску білду)
2. `Level_Arena`

**Чому порядок важливий?** Build Settings визначає, яка сцена вантажиться при старті білду
(індекс 0). У нас це завжди має бути Bootstrap — composition root, який потім сам вирішує,
яку геймплейну сцену завантажити далі (через `SceneLoader`, як у референсі).

---

## Крок 5. Перевірка

Переконайся, що:

- [ ] У `Packages/manifest.json` з'явились `com.unity.inputsystem` і `com.unity.cinemachine`.
- [ ] **Active Input Handling** = "Input System Package (New)".
- [ ] Існує `Assets/CodeBase/` з підпапками зі списку вище.
- [ ] Є дві сцени: `Assets/Scenes/Bootstrap.unity` і `Assets/Scenes/Level_Arena.unity`,
      немає порожньої `SampleScene`.
- [ ] Обидві сцени додані в Build Settings, `Bootstrap` — під індексом 0.
- [ ] Проєкт відкривається і компілюється без помилок у Console (жовтих/червоних
      повідомлень бути не повинно, крім, можливо, попередження від самого Cinemachine/Input
      System про пакет — це нормально).

Коли всі пункти виконані — напиши мені, і перейдемо до `01_composition_root.md`
(Composition Root, `GameBootstrapper`, перша `FSM`).
