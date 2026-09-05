using UnityEngine;

namespace CodeBase.Infrastructure.States
{
    public class GameLoopState : IState
    {
        public GameLoopState(GameStateMachine parent) { }

        public void Enter() => Debug.Log($"[FSM] Enter {GetType().Name}");

        public void Exit() { }
    }
}