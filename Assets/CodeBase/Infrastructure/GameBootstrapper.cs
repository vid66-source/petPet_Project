using CodeBase.Infrastructure.Logic;
using CodeBase.Infrastructure.States;
using UnityEngine;

namespace CodeBase.Infrastructure
{
    public class GameBootstrapper : MonoBehaviour, ICoroutineRunner
    {
        [SerializeField] private LoadingCurtain _curtain;
        private Game _game;

        private void Awake()
        {
            _game = new Game(this,  _curtain);
            DontDestroyOnLoad(this);
            _game.StateMachine.Enter<BootstrapState>();
        }
    }
}