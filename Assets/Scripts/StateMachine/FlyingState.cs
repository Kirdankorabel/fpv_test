using Input;

namespace StateMachine
{
    public sealed class FlyingState : IDroneState
    {
        public DroneStateId Id => DroneStateId.Flying;

        public void Enter(DroneContext c) { }

        public void Tick(DroneContext c, PilotCommand cmd, float dt)
        {
            if (cmd.RespawnRequested)
            {
                c.Fsm.ChangeState(DroneStateId.Respawning);
                return;
            }
            c.Dynamics.Tick(cmd, dt);
        }

        public void Exit(DroneContext c) { }
    }
}
