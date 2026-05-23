using UnityEngine;
using Zenject;

namespace Core
{
    public sealed class SceneStartup : MonoBehaviour
    {
        [SerializeField] private DronesCatalog _catalog;

        [Inject] private IDroneFactory _factory;
        [Inject] private DroneSpawnPoint[] _spawnPoints;

        private void Start()
        {
            _factory.Spawn(_catalog.Default, _spawnPoints[0]);
        }
    }
}
