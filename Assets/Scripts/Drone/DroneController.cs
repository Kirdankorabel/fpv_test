using UnityEngine;
using Zenject;
using Input;
using Core;
using StateMachine;

namespace Drone
{
    public sealed class DroneController
    {
        private readonly IDroneStateMachine _fsm;
        private readonly IPilotInputProvider _input;
        private readonly SimConfig _cfg;
        private readonly SignalBus _signals;

        public DroneController(
            IDroneStateMachine fsm,
            IPilotInputProvider input,
            SimConfig cfg,
            SignalBus signals)
        {
            _fsm = fsm;
            _input = input;
            _cfg = cfg;
            _signals = signals;
        }

        public void Tick(float dt)
        {
            _fsm.Tick(_input.Read(), dt);
        }

        public void HandleCollision(Collision c)
        {
            var speed = c.relativeVelocity.magnitude;
            if (speed < _cfg.CrashSpeedThreshold) return;
            _signals.Fire(new DroneCrashedSignal(c.GetContact(0).point, speed));
        }

        public void RequestRespawn()
        {
            _fsm.ChangeState(DroneStateId.Respawning);
        }
    }
}
