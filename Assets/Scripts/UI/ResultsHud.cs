using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Mission;
using Core;

namespace UI
{
    public sealed class ResultsHud : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private TMP_Text _hitsText;
        [SerializeField] private Button _respawnButton;

        [Inject] private SignalBus _signals;
        [Inject] private IDroneTelemetry _telemetry;
        [Inject] private IMissionTelemetry _mission;

        private void Awake()
        {
            _respawnButton.onClick.AddListener(OnRespawnClicked);
            Hide();
        }

        private void OnDestroy()
        {
            _respawnButton.onClick.RemoveListener(OnRespawnClicked);
        }

        private void OnEnable()
        {
            _signals.Subscribe<DroneCrashedSignal>(OnCrashed);
            _signals.Subscribe<DroneRespawnedSignal>(OnRespawned);
        }

        private void OnDisable()
        {
            _signals.Unsubscribe<DroneCrashedSignal>(OnCrashed);
            _signals.Unsubscribe<DroneRespawnedSignal>(OnRespawned);
        }

        private void OnCrashed(DroneCrashedSignal _)
        {
            _hitsText.text = $"TARGETS HIT: {_mission.TargetsHit}";
            Show();
        }

        private void OnRespawned(DroneRespawnedSignal _)
        {
            Hide();
        }

        private void OnRespawnClicked()
        {
            _telemetry.RequestRespawn();
        }

        private void Show()
        {
            _image.gameObject.SetActive(true);
        }

        private void Hide()
        {
            _image.gameObject.SetActive(false);
        }
    }
}
