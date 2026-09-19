using CodeBase.Infrastructure.AssetManagement;
using CodeBase.Infrastructure.Input;
using CodeBase.Player;
using UnityEngine;

namespace CodeBase.Infrastructure.States
{
    public class GameLoopState : IState
    {
        private readonly IAssetProvider _assetProvider;
        private readonly IInputService _inputService;
        private readonly string _assetPath = "Player";
        private readonly Vector3 _startPlayerPos = new Vector3(0.0f, 6.0f, 0.0f);

        public GameLoopState(IAssetProvider assetProvider, IInputService inputService)
        {
            _assetProvider = assetProvider;
            _inputService = inputService;
        }

        public void Enter()
        {
            Debug.Log($"[FSM] Enter {GetType().Name}");
            GameObject playerPrefab = _assetProvider.LoadAsset(_assetPath);
            GameObject playerOnScene = _assetProvider.SpawnAsset(playerPrefab, _startPlayerPos, Quaternion.identity);
            PlayerController playerController = playerOnScene.GetComponent<PlayerController>();
            playerController.Construct(_inputService);
        }

        public void Exit()
        {

        }
    }
}