using System;
using UnityEngine;
using Core;

namespace Input
{
    public sealed class UnityInputSystemPilotProvider : IPilotInputProvider, IDisposable
    {
        private readonly SimConfig _cfg;
        private readonly PilotInputActions _actions;
        private bool _armLatched;
        private bool _respawnLatched;
        private float _throttle01;

        public UnityInputSystemPilotProvider(SimConfig cfg)
        {
            _cfg = cfg;
            _actions = new PilotInputActions();
            _actions.Pilot.Arm.performed += _ => _armLatched = true;
            _actions.Pilot.Respawn.performed += _ => _respawnLatched = true;
            _actions.Enable();
        }

        public PilotCommand Read()
        {
            float throttleAxis = _actions.Pilot.Throttle.ReadValue<float>();
            _throttle01 = Mathf.Clamp01(_throttle01 + throttleAxis * _cfg.ThrottleRatePerSec * Time.fixedDeltaTime);
            float roll = Mathf.Clamp(_actions.Pilot.Roll.ReadValue<float>(), -1f, 1f);
            float pitch = Mathf.Clamp(_actions.Pilot.Pitch.ReadValue<float>(), -1f, 1f);
            float yaw = Mathf.Clamp(_actions.Pilot.Yaw.ReadValue<float>(), -1f, 1f);
            PilotCommand cmd = new PilotCommand(_throttle01, roll, pitch, yaw, _armLatched, _respawnLatched);
            _armLatched = false;
            _respawnLatched = false;
            return cmd;
        }

        public void Reset()
        {
            _throttle01 = 0f;
            _armLatched = false;
            _respawnLatched = false;
        }

        public void Dispose()
        {
            _actions.Disable();
            _actions.Dispose();
        }
    }
}
