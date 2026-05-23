using UnityEngine;
using Core;
using Input;

namespace Drone
{
    public interface IFlightDynamics
    {
        void Configure(Rigidbody rb, Transform root, SimConfig cfg);
        void Tick(PilotCommand cmd, float dt);
        void Reset();
        float CurrentAltitudeAboveGround { get; }
        float CurrentThrottle01 { get; }
    }
}
