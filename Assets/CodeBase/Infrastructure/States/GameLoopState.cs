using CodeBase.Infrastructure.AssetManagement;
using CodeBase.Infrastructure.Input;
using UnityEngine;

namespace CodeBase.Infrastructure.States
{
    public class GameLoopState : IState
    {
        private readonly IAssetProvider _assetProvider;
        private readonly IInputService _inputService;
        private readonly string _assetPath = "TestObject";

        public GameLoopState(IAssetProvider assetProvider, IInputService inputService)
        {
            _assetProvider = assetProvider;
            _inputService = inputService;
        }

        public void Enter()
        {
            Debug.Log($"[FSM] Enter {GetType().Name}");
            var obj = _assetProvider.LoadAsset(_assetPath);
            _assetProvider.SpawnAsset(obj, Vector3.one, Quaternion.identity);
            _inputService.OnJumpPressed += TestJump;
        }

        private void TestJump()
        {
            Debug.Log("Jump Action!");
        }

        public void Exit()
        {
            _inputService.OnJumpPressed -= TestJump;
        }
    }
}