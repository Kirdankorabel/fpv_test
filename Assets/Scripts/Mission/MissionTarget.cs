using UnityEngine;
using Zenject;

namespace Mission
{
    public sealed class MissionTarget : MonoBehaviour
    {
        [Inject] private IMissionTargetRegistry _registry;

        private void Awake()
        {
            _registry.Register(transform);
        }
    }
}
