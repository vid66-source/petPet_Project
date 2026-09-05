using System;
using System.Collections.Generic;
using CodeBase.Infrastructure.Logic;

namespace CodeBase.Infrastructure.States
{
    public class GameStateMachine
    {
        private readonly Dictionary<Type, IExitableState> _states;
        private IExitableState _currentState;

        public GameStateMachine(SceneLoader sceneLoader, LoadingCurtain loadingCurtain)
        {
            _states = new Dictionary<Type, IExitableState>();
            _states.Add(typeof(BootstrapState), new BootstrapState(this, sceneLoader));
            _states.Add(typeof(LoadLevelState), new LoadLevelState(this, sceneLoader, loadingCurtain));
            _states.Add(typeof(GameLoopState), new GameLoopState(this));
        }

        public void Enter<TState>() where TState : class, IState
        {
            _currentState?.Exit();

            TState newState = _states[typeof(TState)] as TState;

            _currentState = newState;

            newState?.Enter();
        }

        public void Enter<TState, TPayload>(TPayload payload) where TState : class, IPayloadedState<TPayload>
        {
            _currentState?.Exit();

            TState newState = _states[typeof(TState)] as TState;

            _currentState = newState;

            newState?.Enter(payload);
        }
    }
}