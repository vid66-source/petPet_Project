namespace CodeBase.Infrastructure.States
{
    public interface IPayloadedState<TPlayload> : IExitableState
    {
        void Enter(TPlayload payload);
    }
}