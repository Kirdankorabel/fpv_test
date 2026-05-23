using Input;

namespace StateMachine
{
    public sealed class CrashedState : IDroneState
    {
        public DroneStateId Id => DroneStateId.Crashed;

        public void Enter(DroneContext c)
        {
            c.Dynamics.Reset();
        }

        public void Tick(DroneContext c, PilotCommand cmd, float dt)
        {
            if (cmd.RespawnRequested) c.Fsm.ChangeState(DroneStateId.Respawning);
        }

        public void Exit(DroneContext c) { }
    }
}
