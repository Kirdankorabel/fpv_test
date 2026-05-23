using UnityEngine;
using Zenject;
using Core;

namespace Drone
{
    public sealed class CrashFx : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _particles;

        [Inject] private SignalBus _signals;

        private void OnEnable()
        {
            _signals.Subscribe<DroneCrashedSignal>(OnCrashed);
        }

        private void OnDisable()
        {
            _signals.Unsubscribe<DroneCrashedSignal>(OnCrashed);
        }

        private void OnCrashed(DroneCrashedSignal _)
        {
            _particles.Play(true);
        }
    }
}
