using System;

namespace CodeBase.Infrastructure.States
{
    public class LoadLevelState : IState
    {
        public LoadLevelState(GameStateMachine gameStateMachine, SceneLoader sceneLoader,
            LoadingCurtain loadingCurtain) { }

        public void Exit() { }

        public void Enter() { }
    }
}