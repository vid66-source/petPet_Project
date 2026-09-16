# `Resources.Load` і `Object.Instantiate`

Уперше трапилось: урок 02, `AssetProvider` — як створити ігровий об'єкт із префабу за
рядком-шляхом.

## Два окремі кроки, не один виклик

### Крок 1 — `Resources.Load<T>(path)`: дістати сам асет ("креслення")

```csharp
GameObject prefab = Resources.Load<GameObject>("DebugSpawnTest");
```

- Unity шукає файл усередині будь-якої папки, яка **буквально називається `Resources`**,
  де завгодно під `Assets/` — спеціальна, "магічна" для Unity назва папки: усе, що в ній
  лежить, можна діставати за рядковим шляхом, без прямого посилання в інспекторі.
- `path` — шлях **відносно цієї папки**, без розширення файлу. Приклади:
  - `Assets/Resources/DebugSpawnTest.prefab` → `"DebugSpawnTest"`
  - `Assets/Resources/Prefabs/DebugSpawnTest.prefab` → `"Prefabs/DebugSpawnTest"`
- `<GameObject>` — типовий параметр, яким типом асета очікуєш результат (`<AudioClip>`,
  `<Sprite>` тощо — той самий метод для будь-якого типу асету).
- Результат — посилання на сам **префаб-асет**, не на об'єкт у сцені. Аналогія: це
  креслення будинку, а не сам будинок.

### Крок 2 — `Object.Instantiate(...)`: створити живий об'єкт у сцені з цього креслення

```csharp
GameObject instance = Object.Instantiate(prefab);
GameObject instance = Object.Instantiate(prefab, at, Quaternion.identity);
```

- Перша форма — клонує префаб із тим положенням/поворотом, які збережені в самому
  префабі (найчастіше `(0,0,0)`, без повороту).
- Друга форма — явно задає світову позицію (`Vector3 at`) і поворот
  (`Quaternion` — `Quaternion.identity` = "без повороту", поки досить знати це значення,
  не всю математику кватерніонів).
- Обидві форми повертають саме `GameObject`, без ручного касту — Unity сам вибирає
  потрібний перевантажений варіант `Instantiate`, коли бачить, що переданий аргумент —
  `GameObject`.

## Разом

```csharp
GameObject prefab = Resources.Load<GameObject>(path);
return Object.Instantiate(prefab, at, Quaternion.identity);
```

Два виклики поспіль — перший дістає посилання на асет, другий створює з нього живий
екземпляр у сцені.

## Підсумок: типи `UnityEngine`, які тут з'явились

### `Resources` (namespace `UnityEngine`, статичний клас)

- `Load<T>(string path)` → `T` — `T` — будь-який тип асету (`GameObject`,
  `AudioClip`, `Sprite`...), `path` — шлях відносно папки `Resources/`, без
  розширення файлу.

### `Object` (namespace `UnityEngine`, базовий клас Unity-об'єктів; не плутати з `System.Object`)

- `Instantiate(Object original)` → `Object` — клонує асет із його власним
  положенням/поворотом.
- `Instantiate(Object original, Vector3 position, Quaternion rotation)` → `Object` —
  клонує з явно заданою позицією (`Vector3` — три `float`: x/y/z) і поворотом.
- Обидва перевантаження повертають статичний тип `Object`, але Unity сам підбирає
  конкретний варіант і по факту повертає той самий тип, що передали (`GameObject`
  → `GameObject`), без ручного касту.

### `Quaternion` (namespace `UnityEngine`, struct)

- `Quaternion.identity` → `Quaternion` — статична властивість, значення "без
  повороту".

## Пов'язане

- [`Coroutines_and_Scene_Loading.md`](Coroutines_and_Scene_Loading.md) — інший приклад
  Unity API, що працює з асетами/сценами (`SceneManager.LoadSceneAsync`).
