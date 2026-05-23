using Input;

namespace StateMachine
{
    public interface IDroneState
    {
        DroneStateId Id { get; }
        void Enter(DroneContext ctx);
        void Tick(DroneContext ctx, PilotCommand cmd, float dt);
        void Exit(DroneContext ctx);
    }
}
