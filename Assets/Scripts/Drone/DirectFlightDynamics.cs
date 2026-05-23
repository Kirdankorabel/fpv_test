using Core;
using Input;
using UnityEngine;

namespace Drone
{
    public sealed class DirectFlightDynamics : IFlightDynamics
    {
        private Rigidbody _rb;
        private Transform _root;
        private SimConfig _cfg;
        private float _smoothedThrottle;
        private float _smoothedPitch;
        private float _smoothedRoll;
        private float _smoothedYaw;

        public void Configure(Rigidbody rb, Transform root, SimConfig cfg)
        {
            _rb = rb;
            _root = root;
            _cfg = cfg;
            rb.mass = cfg.Mass;
            rb.useGravity = true;
            rb.linearDamping = 0f;
            rb.angularDamping = cfg.AngularDamping;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            _smoothedThrottle = 0f;
            _smoothedPitch = 0f;
            _smoothedRoll = 0f;
            _smoothedYaw = 0f;
        }

        public void Tick(PilotCommand cmd, float dt)
        {
            float throttleAlpha = 1f - Mathf.Exp(-dt / Mathf.Max(_cfg.MotorTau, 1e-4f));
            _smoothedThrottle = Mathf.Lerp(_smoothedThrottle, Mathf.Clamp01(cmd.Throttle), throttleAlpha);

            float stickAlpha = 1f - Mathf.Exp(-dt / Mathf.Max(_cfg.StickSmoothTau, 1e-4f));
            _smoothedPitch = Mathf.Lerp(_smoothedPitch, cmd.Pitch, stickAlpha);
            _smoothedRoll = Mathf.Lerp(_smoothedRoll, cmd.Roll, stickAlpha);
            _smoothedYaw = Mathf.Lerp(_smoothedYaw, cmd.Yaw, stickAlpha);

            float pitchOmegaTarget = _smoothedPitch * _cfg.MaxPitchRollRateDeg * Mathf.Deg2Rad;
            float rollOmegaTarget = -_smoothedRoll * _cfg.MaxPitchRollRateDeg * Mathf.Deg2Rad;
            float yawOmegaTarget = _smoothedYaw * _cfg.MaxYawRateDeg * Mathf.Deg2Rad;

            Vector3 targetBodyAngVel = new Vector3(pitchOmegaTarget, yawOmegaTarget, rollOmegaTarget);
            Vector3 targetWorldAngVel = _root.TransformDirection(targetBodyAngVel);

            float rateAlpha = Mathf.Clamp01(_cfg.RateResponse * dt);
            _rb.angularVelocity = Vector3.Lerp(_rb.angularVelocity, targetWorldAngVel, rateAlpha);

            float thrustForce = _smoothedThrottle * _cfg.MaxThrustPerMotorN * 4f;
            if (_cfg.MaxAltitudeMeters > 0f && _rb.position.y > _cfg.MaxAltitudeMeters) thrustForce = 0f;
            _rb.AddForce(_root.up * thrustForce, ForceMode.Force);

            Vector3 v = _rb.linearVelocity;
            _rb.AddForce(-_cfg.QuadraticDragCoef * v * v.magnitude, ForceMode.Force);
        }

        public void Reset()
        {
            _smoothedThrottle = 0f;
            _smoothedPitch = 0f;
            _smoothedRoll = 0f;
            _smoothedYaw = 0f;
        }

        public float CurrentAltitudeAboveGround
        {
            get
            {
                if (Physics.Raycast(_rb.position, Vector3.down, out RaycastHit hit, 1000f)) return _rb.position.y - hit.point.y;
                return _rb.position.y;
            }
        }

        public float CurrentThrottle01 => _smoothedThrottle;
    }
}
