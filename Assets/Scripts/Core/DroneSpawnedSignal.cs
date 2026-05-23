using Drone;

namespace Core
{
    public readonly struct DroneSpawnedSignal
    {
        public readonly DroneFacade Drone;
        public readonly string UiId;

        public DroneSpawnedSignal(DroneFacade drone, string uiId)
        {
            Drone = drone;
            UiId = uiId;
        }
    }
}
