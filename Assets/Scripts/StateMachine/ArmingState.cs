using Input;
using Core;

namespace StateMachine
{
    public sealed class ArmingState : IDroneState
    {
        private readonly SimConfig _cfg;

        public ArmingState(SimConfig cfg)
        {
            _cfg = cfg;
        }

        public DroneStateId Id => DroneStateId.Arming;

        public void Enter(DroneContext c) { }

        public void Tick(DroneContext c, PilotCommand cmd, float dt)
        {
            if (c.TimeInState >= _cfg.ArmingDurationSec) c.Fsm.ChangeState(DroneStateId.Flying);
        }

        public void Exit(DroneContext c)
        {
            c.Signals.Fire(new DroneArmedSignal());
        }
    }
}
