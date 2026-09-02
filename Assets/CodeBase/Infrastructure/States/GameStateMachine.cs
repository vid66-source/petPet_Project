using System;
using System.Collections.Generic;

namespace CodeBase.Infrastructure.States
{
    public class GameStateMachine
    {
        private readonly Dictionary<Type, IExitableState> _states;

        public GameStateMachine(SceneLoader sceneLoader, LoadingCurtain loadingCurtain)
        {
            _states = new Dictionary<Type, IExitableState>();
            _states.Add(typeof(BootstrapState), new BootstrapState(this, sceneLoader));
            _states.Add(typeof(LoadLevelState), new LoadLevelState(this, sceneLoader, loadingCurtain));
            _states.Add(typeof(GameLoopState), new GameLoopState(this));
        }

        void Enter<TState>() where TState : class, IState
        { }

        void Enter<TState, TPayload>(TPayload payload) where TState : class, IPayloadedState<TPayload>
        { }
    }
}