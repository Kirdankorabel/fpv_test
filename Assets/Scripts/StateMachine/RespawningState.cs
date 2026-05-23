using UnityEngine;
using Input;
using Core;

namespace StateMachine
{
    public sealed class RespawningState : IDroneState
    {
        private readonly SimConfig _cfg;
        private readonly IPilotInputProvider _input;

        public RespawningState(SimConfig cfg, IPilotInputProvider input)
        {
            _cfg = cfg;
            _input = input;
        }

        public DroneStateId Id => DroneStateId.Respawning;

        public void Enter(DroneContext c)
        {
            c.Rb.isKinematic = true;
            c.Rb.linearVelocity = Vector3.zero;
            c.Rb.angularVelocity = Vector3.zero;
            c.Rb.position = c.SpawnPos;
            c.Rb.rotation = c.SpawnRot;
            c.Root.SetPositionAndRotation(c.SpawnPos, c.SpawnRot);
            c.Dynamics.Reset();
            _input.Reset();
            c.Signals.Fire(new DroneRespawnedSignal());
        }

        public void Tick(DroneContext c, PilotCommand cmd, float dt)
        {
            if (c.TimeInState >= _cfg.RespawnFreezeSec) c.Fsm.ChangeState(DroneStateId.Disarmed);
        }

        public void Exit(DroneContext c)
        {
            c.Rb.isKinematic = false;
        }
    }
}
