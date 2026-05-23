using System;
using UnityEngine;
using Zenject;
using Core;

namespace UI
{
    public sealed class DroneUiManager : MonoBehaviour
    {
        [SerializeField] private UiBinding[] _bindings;

        [Inject] private SignalBus _signals;

        [Serializable]
        public struct UiBinding
        {
            public string UiId;
            public GameObject Panel;
        }

        private void Awake()
        {
            for (var i = 0; i < _bindings.Length; i++)
            {
                _bindings[i].Panel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            _signals.Subscribe<DroneSpawnedSignal>(OnDroneSpawned);
        }

        private void OnDisable()
        {
            _signals.Unsubscribe<DroneSpawnedSignal>(OnDroneSpawned);
        }

        private void OnDroneSpawned(DroneSpawnedSignal s)
        {
            for (var i = 0; i < _bindings.Length; i++)
            {
                var b = _bindings[i];
                b.Panel.SetActive(b.UiId == s.UiId);
            }
        }
    }
}
