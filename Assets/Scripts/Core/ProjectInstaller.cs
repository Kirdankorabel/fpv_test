using UnityEngine;
using Zenject;

namespace Core
{
    [CreateAssetMenu(menuName = "Installers/ProjectInstaller", fileName = "ProjectInstaller")]
    public sealed class ProjectInstaller : ScriptableObjectInstaller<ProjectInstaller>
    {
        public override void InstallBindings()
        {
            SignalBusInstaller.Install(Container);
            DeclareSignals();
        }

        private void DeclareSignals()
        {
            Container.DeclareSignal<DroneArmedSignal>();
            Container.DeclareSignal<DroneDisarmedSignal>();
            Container.DeclareSignal<DroneCrashedSignal>();
            Container.DeclareSignal<DroneRespawnedSignal>();
            Container.DeclareSignal<DroneSpawnedSignal>();
        }
    }
}
