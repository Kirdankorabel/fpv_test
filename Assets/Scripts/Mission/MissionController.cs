using System;
using Zenject;
using Core;

namespace Mission
{
    public sealed class MissionController : IMissionTelemetry, IInitializable, IDisposable
    {
        private readonly SignalBus _signals;
        private readonly IMissionTargetRegistry _targetRegistry;
        private readonly SimConfig _cfg;
        private int _hitCount;

        public MissionController(SignalBus signals, IMissionTargetRegistry targetRegistry, SimConfig cfg)
        {
            _signals = signals;
            _targetRegistry = targetRegistry;
            _cfg = cfg;
        }

        public void Initialize()
        {
            _signals.Subscribe<DroneCrashedSignal>(OnDroneCrashed);
            _signals.Subscribe<DroneRespawnedSignal>(OnDroneRespawned);
        }

        public void Dispose()
        {
            _signals.Unsubscribe<DroneCrashedSignal>(OnDroneCrashed);
            _signals.Unsubscribe<DroneRespawnedSignal>(OnDroneRespawned);
        }

        public int TargetsHit => _hitCount;

        private void OnDroneCrashed(DroneCrashedSignal s)
        {
            var blastRadiusSqr = _cfg.BlastRadiusMeters * _cfg.BlastRadiusMeters;
            var targets = _targetRegistry.Targets;
            for (var i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (!t.gameObject.activeSelf) continue;
                if ((t.position - s.ImpactPoint).sqrMagnitude > blastRadiusSqr) continue;
                t.gameObject.SetActive(false);
                _hitCount++;
            }
        }

        private void OnDroneRespawned(DroneRespawnedSignal _)
        {
            _hitCount = 0;
        }
    }
}
