using CodeBase.Infrastructure.AssetManagement;
using CodeBase.Infrastructure.Services;
using UnityEngine;

namespace CodeBase.Infrastructure.States
{
    public class BootstrapState : IState
    {
        private const string SceneName  =  "Level_Arena";

        private readonly GameStateMachine _stateMachine;
        private readonly AllServices _services;

        public BootstrapState(GameStateMachine gameStateMachine, AllServices services)
        {
            _stateMachine = gameStateMachine;
            _services = services;
            RegisterServices();
        }

        public void Enter()
        {
            Debug.Log($"[FSM] Enter {GetType().Name}");
            Debug.Log($"[FSM] {GetType().Name} initiated enter LoadLevelState");
            _stateMachine.Enter<LoadLevelState, string>(SceneName);
        }

        private void RegisterServices()
        {
            AssetProvider assetProvider = new AssetProvider();
            Debug.Log($"[FSM] {GetType().Name} initiated registration of a new service {assetProvider.GetType().Name}");
            _services.RegisterService<IAssetProvider>(assetProvider);
        }

        public void Exit()
        {
            // TODO
        }
    }
}