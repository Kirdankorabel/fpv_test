using UnityEngine;

namespace Core
{
    public readonly struct DroneArmedSignal { }
    public readonly struct DroneDisarmedSignal { }
    public readonly struct DroneRespawnedSignal { }

    public readonly struct DroneCrashedSignal
    {
        public readonly Vector3 ImpactPoint;
        public readonly float ImpactSpeed;

        public DroneCrashedSignal(Vector3 impactPoint, float impactSpeed)
        {
            ImpactPoint = impactPoint;
            ImpactSpeed = impactSpeed;
        }
    }
}
