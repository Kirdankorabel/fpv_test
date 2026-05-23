using UnityEngine;
using Zenject;
using Input;
using Mission;

namespace Core
{
    public sealed class SceneInstaller : MonoInstaller
    {
        [SerializeField] private SimConfig _simConfig;
        [SerializeField] private DroneSpawnPoint[] _spawnPoints;

        public override void InstallBindings()
        {
            Container.Bind<SimConfig>().FromInstance(_simConfig).AsSingle();
            Container.Bind<DroneSpawnPoint[]>().FromInstance(_spawnPoints).AsSingle();
            Container.Bind<IPilotInputProvider>().To<UnityInputSystemPilotProvider>().AsSingle();
            Container.Bind<IDroneFactory>().To<DroneFactory>().AsSingle();
            Container.Bind<IMissionTargetRegistry>().To<MissionTargetRegistry>().AsSingle();
            Container.BindInterfacesAndSelfTo<DroneTelemetryController>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<MissionController>().AsSingle().NonLazy();
        }
    }
}
