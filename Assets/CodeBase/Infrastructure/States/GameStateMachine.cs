using System;
using System.Collections.Generic;
using CodeBase.Infrastructure.AssetManagement;
using CodeBase.Infrastructure.Input;
using CodeBase.Infrastructure.Logic;
using CodeBase.Infrastructure.Services;
using UnityEngine;

namespace CodeBase.Infrastructure.States
{
    public class GameStateMachine
    {
        private readonly Dictionary<Type, IExitableState> _states;
        private IExitableState _currentState;

        public GameStateMachine(SceneLoader sceneLoader, LoadingCurtain loadingCurtain, AllServices services)
        {
            _states = new Dictionary<Type, IExitableState>();
            _states.Add(typeof(BootstrapState), new BootstrapState(this, services));
            _states.Add(typeof(LoadLevelState), new LoadLevelState(this, sceneLoader, loadingCurtain));
            IAssetProvider assetProvider = services.GetService<IAssetProvider>();
            IInputService inputService = services.GetService<IInputService>();
            _states.Add(typeof(GameLoopState), new GameLoopState(assetProvider, inputService));

            Debug.Log($"[FSM] {GetType().Name} created {_states[typeof(BootstrapState)].GetType().Name} " +
                      $"{_states[typeof(LoadLevelState)].GetType().Name} " +
                      $"{_states[typeof(GameLoopState)].GetType().Name} states");
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