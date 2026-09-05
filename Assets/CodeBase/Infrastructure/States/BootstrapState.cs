using UnityEngine;

namespace CodeBase.Infrastructure.States
{
    public class BootstrapState : IState
    {
        private const string SceneName  =  "Level_Arena";

        private GameStateMachine _stateMachine;

        public BootstrapState(GameStateMachine gameStateMachine, SceneLoader sceneLoader)
        {
            _stateMachine = gameStateMachine;
        }

        public void Enter()
        {
            Debug.Log($"[FSM] Enter {GetType().Name}");
            _stateMachine.Enter<LoadLevelState, string>(SceneName);
        }

        public void Exit()
        {
            // TODO
        }
    }
}