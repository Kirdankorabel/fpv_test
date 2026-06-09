using UnityEngine;

namespace Drone
{
    public sealed class PidController
    {
        public float Kp;
        public float Ki;
        public float Kd;
        public float IntegralLimit;

        private float _integral;
        private float _lastError;

        public PidController(float kp, float ki, float kd, float integralLimit)
        {
            Kp = kp;
            Ki = ki;
            Kd = kd;
            IntegralLimit = integralLimit;
        }

        public float Update(float error, float dt)
        {
            _integral = Mathf.Clamp(_integral + error * dt, -IntegralLimit, IntegralLimit);
            float derivative = (error - _lastError) / dt;
            _lastError = error;
            return Kp * error + Ki * _integral + Kd * derivative;
        }

        public void Reset()
        {
            _integral = 0f;
            _lastError = 0f;
        }
    }
}
