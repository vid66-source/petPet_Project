using CodeBase.Infrastructure.Logic;
using UnityEngine;

namespace CodeBase.Infrastructure.States
{
    public class LoadLevelState : IPayloadedState<string>
    {
        private readonly GameStateMachine _stateMachine;
        private readonly SceneLoader _sceneLoader;
        private readonly LoadingCurtain _loadingCurtain;


        public LoadLevelState(GameStateMachine gameStateMachine, SceneLoader sceneLoader, LoadingCurtain loadingCurtain)
        {
            _stateMachine = gameStateMachine;
            _sceneLoader = sceneLoader;
            _loadingCurtain = loadingCurtain;
        }

        public void Enter(string sceneName)
        {
            Debug.Log($"[FSM] Enter {GetType().Name}");
            _loadingCurtain.Show();
            _sceneLoader.Load(sceneName, onLoaded);
        }

        private void onLoaded() => _stateMachine.Enter<GameLoopState>();

        public void Exit() => _loadingCurtain.Hide();
    }
}