using UnityEngine;
using Input;
using Core;

namespace StateMachine
{
    public sealed class DisarmedState : IDroneState
    {
        public DroneStateId Id => DroneStateId.Disarmed;

        public void Enter(DroneContext c)
        {
            c.Rb.linearVelocity = Vector3.zero;
            c.Rb.angularVelocity = Vector3.zero;
            c.Rb.isKinematic = true;
            c.Dynamics.Reset();
            c.Signals.Fire(new DroneDisarmedSignal());
        }

        public void Tick(DroneContext c, PilotCommand cmd, float dt)
        {
            if (cmd.ArmRequested) c.Fsm.ChangeState(DroneStateId.Arming);
        }

        public void Exit(DroneContext c)
        {
            c.Rb.isKinematic = false;
        }
    }
}
