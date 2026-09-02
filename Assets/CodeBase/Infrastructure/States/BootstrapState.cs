using System;

namespace CodeBase.Infrastructure.States
{
    public class BootstrapState : IState
    {
        public BootstrapState(GameStateMachine gameStateMachine, SceneLoader sceneLoader) { }

        public void Exit() { }

        public void Enter() { }
    }
}