using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "Sim Config", fileName = "SimConfig")]
    public sealed class SimConfig : ScriptableObject
    {
        [Header("Drone — mass & thrust")]
        public float Mass = 1.5f;
        public float MaxThrustPerMotorN = 13f;
        public float MotorTau = 0.08f;

        [Header("Drone — aerodynamics")]
        public float QuadraticDragCoef = 0.030f;
        public float AngularDamping = 0.05f;

        [Header("Drone — altitude ceiling")]
        public float MaxAltitudeMeters = 200f;

        [Header("World — wrap-around")]
        public float MapHalfSizeMeters = 500f;

        [Header("Acro rate control (combat FPV)")]
        public float MaxPitchRollRateDeg = 200f;
        public float MaxYawRateDeg = 120f;
        public float StickSmoothTau = 0.15f;
        public float RateResponse = 20f;

        [Header("FPV camera")]
        public float CameraTiltDeg = 30f;
        public float FovDeg = 110f;

        [Header("Input — throttle integration")]
        public float ThrottleRatePerSec = 1.0f;

        [Header("Gameplay — FSM timings")]
        public float ArmingDurationSec = 0.3f;
        public float RespawnFreezeSec = 0.5f;

        [Header("Gameplay — crash detection")]
        public float CrashSpeedThreshold = 8f;

        [Header("Gameplay — warhead")]
        public float BlastRadiusMeters = 3f;

        private void OnValidate()
        {
            if (Mass <= 0f) Mass = 0.001f;
        }
    }
}
