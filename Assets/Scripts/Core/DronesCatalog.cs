using System;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "Drones Catalog", fileName = "DronesCatalog")]
    public sealed class DronesCatalog : ScriptableObject
    {
        [SerializeField] private DroneEntry[] _drones;

        public DroneEntry Default => _drones[0];
    }

    [Serializable]
    public sealed class DroneEntry
    {
        public string Id;
        public string UiId;
        public GameObject Prefab;
    }
}
