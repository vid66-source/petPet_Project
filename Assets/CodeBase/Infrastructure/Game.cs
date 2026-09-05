using CodeBase.Infrastructure.Logic;
using CodeBase.Infrastructure.States;

namespace CodeBase.Infrastructure
{
    public class Game
    {
        public GameStateMachine StateMachine {get;}

        public Game(ICoroutineRunner coroutineRunner, LoadingCurtain curtain)
        {
            SceneLoader sceneLoader = new SceneLoader(coroutineRunner);
            StateMachine = new GameStateMachine(sceneLoader, curtain);
        }
    }
}