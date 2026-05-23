using Zenject;
using Drone;

namespace Core
{
    public sealed class DroneFactory : IDroneFactory
    {
        private readonly DiContainer _container;
        private readonly SignalBus _signals;

        public DroneFactory(DiContainer container, SignalBus signals)
        {
            _container = container;
            _signals = signals;
        }

        public void Spawn(DroneEntry entry, DroneSpawnPoint spawn)
        {
            var facade = _container.InstantiatePrefabForComponent<DroneFacade>(
                entry.Prefab, spawn.transform.position, spawn.transform.rotation, null);
            _signals.Fire(new DroneSpawnedSignal(facade, entry.UiId));
        }
    }
}
