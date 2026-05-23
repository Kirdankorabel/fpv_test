using System;
using UnityEngine;
using Zenject;
using Drone;

namespace Core
{
    public sealed class DroneTelemetryController : IDroneTelemetry, IInitializable, IDisposable
    {
        private static readonly Color ColorDisarmed = new Color(0.7f, 0.7f, 0.7f);
        private static readonly Color ColorArmed = new Color(0.35f, 1f, 0.45f);
        private static readonly Color ColorCrashed = new Color(1f, 0.25f, 0.25f);

        private readonly SignalBus _signals;
        private DroneFacade _drone;
        private string _modeText = "DISARMED";
        private Color _modeColor = ColorDisarmed;
        private bool _isArmed;
        private float _armTime;

        public DroneTelemetryController(SignalBus signals)
        {
            _signals = signals;
        }

        public void Initialize()
        {
            _signals.Subscribe<DroneSpawnedSignal>(OnDroneSpawned);
            _signals.Subscribe<DroneArmedSignal>(OnArmed);
            _signals.Subscribe<DroneDisarmedSignal>(OnDisarmed);
            _signals.Subscribe<DroneCrashedSignal>(OnCrashed);
            _signals.Subscribe<DroneRespawnedSignal>(OnRespawned);
        }

        public void Dispose()
        {
            _signals.Unsubscribe<DroneSpawnedSignal>(OnDroneSpawned);
            _signals.Unsubscribe<DroneArmedSignal>(OnArmed);
            _signals.Unsubscribe<DroneDisarmedSignal>(OnDisarmed);
            _signals.Unsubscribe<DroneCrashedSignal>(OnCrashed);
            _signals.Unsubscribe<DroneRespawnedSignal>(OnRespawned);
        }

        public string ModeText => _modeText;
        public Color ModeColor => _modeColor;
        public float Throttle01 => _drone.Dynamics.CurrentThrottle01;
        public float AltitudeMeters => _drone.Dynamics.CurrentAltitudeAboveGround;
        public float SpeedMetersPerSec => _drone.Rb.linearVelocity.magnitude;
        public float TimeArmedSeconds => _isArmed ? Time.time - _armTime : 0f;

        public void RequestRespawn()
        {
            _drone.RequestRespawn();
        }

        private void OnDroneSpawned(DroneSpawnedSignal s)
        {
            _drone = s.Drone;
        }

        private void OnArmed(DroneArmedSignal _)
        {
            _modeText = "ARMED ACRO";
            _modeColor = ColorArmed;
            _isArmed = true;
            _armTime = Time.time;
        }

        private void OnDisarmed(DroneDisarmedSignal _)
        {
            _modeText = "DISARMED";
            _modeColor = ColorDisarmed;
            _isArmed = false;
        }

        private void OnCrashed(DroneCrashedSignal _)
        {
            _modeText = "DRONE DESTROYED";
            _modeColor = ColorCrashed;
            _isArmed = false;
        }

        private void OnRespawned(DroneRespawnedSignal _)
        {
            _modeText = "DISARMED";
            _modeColor = ColorDisarmed;
            _isArmed = false;
        }
    }
}
