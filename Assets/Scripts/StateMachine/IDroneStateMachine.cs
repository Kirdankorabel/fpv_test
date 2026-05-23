using Input;

namespace StateMachine
{
    public interface IDroneStateMachine
    {
        DroneStateId CurrentId { get; }
        void ChangeState(DroneStateId id);
        void Tick(PilotCommand cmd, float dt);
        void BindContext(DroneContext ctx);
    }
}
