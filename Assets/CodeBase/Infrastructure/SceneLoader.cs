using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CodeBase.Infrastructure
{
    public class SceneLoader
    {
        private readonly ICoroutineRunner _coroutineRunner;
        public SceneLoader(ICoroutineRunner coroutineRunner) => _coroutineRunner = coroutineRunner;

        public void Load(string sceneName, Action onLoaded = null)
        {
            if (SceneManager.GetActiveScene().name == sceneName)
                onLoaded?.Invoke();
            else
                _coroutineRunner.StartCoroutine(LoadScene(sceneName, onLoaded));
        }

        private IEnumerator LoadScene(string sceneName, Action onLoaded)
        {
            AsyncOperation sceneLoadOperation = SceneManager.LoadSceneAsync(sceneName);
            while (!sceneLoadOperation.isDone)
            {
                yield return null;
            }
            onLoaded?.Invoke();
        }
    }
}