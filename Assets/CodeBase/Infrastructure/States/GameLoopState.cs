using CodeBase.Infrastructure.AssetManagement;
using UnityEngine;

namespace CodeBase.Infrastructure.States
{
    public class GameLoopState : IState
    {
        private readonly IAssetProvider _assetProvider;
        private readonly string _assetPath = "TestObject";

        public GameLoopState(IAssetProvider assetProvider)
        {
            _assetProvider = assetProvider;
        }

        public void Enter()
        {
            Debug.Log($"[FSM] Enter {GetType().Name}");
            var obj = _assetProvider.LoadAsset(_assetPath);
            _assetProvider.SpawnAsset(obj, Vector3.one, Quaternion.identity);
        }

        public void Exit() { }
    }
}