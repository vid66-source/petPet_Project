# Корутини та асинхронне завантаження сцен

Уперше трапилось: урок 01, `SceneLoader.cs` — пояснення всіх Unity-викликів, з якими
студент раніше не працював.

## `SceneManager` і `Scene`

`UnityEngine.SceneManagement.SceneManager` — статичний клас, точка входу до всього, що
стосується сцен. Не створюється (`new SceneManager()` так не роблять), методи викликаються
напряму: `SceneManager.LoadSceneAsync(...)`, `SceneManager.GetActiveScene()`.

`SceneManager.GetActiveScene().name` — `Scene` — легкий "дескриптор" завантаженої й
показаної гравцю сцени (Unity дозволяє мати кілька завантажених сцен одночасно, але
рівно одна з них — активна); `.name` — її ім'я (те саме, що в файлі `.unity`).

## `SceneManager.LoadSceneAsync` і `AsyncOperation`

`SceneManager.LoadScene(name)` (без `Async`) блокує весь потік, доки сцена не
завантажиться — гра "зависає" на кадр і більше. `LoadSceneAsync(name)` натомість
завантажує сцену **у фоні**, кадр за кадром, і одразу повертає керування далі по коду, а
не блокує.

Повертає об'єкт `AsyncOperation` — "квиток" на асинхронну операцію Unity (той самий тип
використовується не тільки для сцен, а й, наприклад, для Addressables чи
`UnityWebRequest`). У нього є властивість `isDone` (`bool`) — `true`, коли операція
фактично завершилась.

## Корутина, яка чекає завершення

--------------------------- КОД ---------------------------
<pre>
private IEnumerator LoadScene(string sceneName, Action onLoaded)
{
    AsyncOperation sceneLoadOperation = SceneManager.LoadSceneAsync(sceneName);
    while (!sceneLoadOperation.isDone)
    {
        yield return null;
    }
    onLoaded?.Invoke();
}
</pre>
------------------------------------------------------------

`IEnumerator` — стандартний .NET-інтерфейс для ітераторів, але Unity використовує його
по-особливому для корутин через `MonoBehaviour.StartCoroutine` (звідси й
`ICoroutineRunner` у проєкті — див. `Docs/PATTERNS.md`, DIP-приклад уроку 01).

`yield return null` — не "поверни null" у звичайному сенсі, а команда Unity: **"призупини
виконання цього методу тут, віддай керування далі, і продовж з цього самого місця на
наступному кадрі"**. Тому `while (!sceneLoadOperation.isDone) yield return null;`
перевіряє `isDone` рівно раз на кадр, не блокуючи гру, доки сцена не довантажиться.

## Чому потрібен `ICoroutineRunner`

`SceneLoader` — чистий C#-клас (не `MonoBehaviour`), а корутини Unity вміє запускати
тільки `MonoBehaviour`. Тому `SceneLoader` просить про це через абстракцію
(`ICoroutineRunner`), а не стає сам `MonoBehaviour` і не шукає когось напряму. Детальніше
— [`Delegates_Events_and_Subscriptions.md`](Delegates_Events_and_Subscriptions.md) (як
передається колбек `onLoaded`, викликаний саме тут, у кінці корутини) і
`Docs/PATTERNS.md` (DIP).

## Пов'язане

- [`Delegates_Events_and_Subscriptions.md`](Delegates_Events_and_Subscriptions.md) —
  `onLoaded`, який викликається тут після завершення завантаження.
- [`DontDestroyOnLoad.md`](DontDestroyOnLoad.md) — що стається з об'єктами сцени під час
  цього завантаження.
