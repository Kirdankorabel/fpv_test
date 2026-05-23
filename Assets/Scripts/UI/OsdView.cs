using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Core;

namespace UI
{
    public sealed class OsdView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _modeText;
        [SerializeField] private TMP_Text _throttleText;
        [SerializeField] private TMP_Text _altText;
        [SerializeField] private TMP_Text _spdText;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private TMP_Text _fpsText;
        [SerializeField] private Image _throttleBar;
        [SerializeField] private Button _respawnButton;

        [Inject] private IDroneTelemetry _telemetry;

        private float _fpsTimer;
        private int _fpsFrames;
        private int _currentFps;

        private void Awake()
        {
            _respawnButton.onClick.AddListener(OnRespawnClicked);
        }

        private void OnDestroy()
        {
            _respawnButton.onClick.RemoveListener(OnRespawnClicked);
        }

        private void Update()
        {
            _fpsTimer += Time.unscaledDeltaTime;
            _fpsFrames++;
            if (_fpsTimer >= 0.5f)
            {
                _currentFps = Mathf.RoundToInt(_fpsFrames / _fpsTimer);
                _fpsTimer = 0f;
                _fpsFrames = 0;
            }

            _modeText.text = _telemetry.ModeText;
            _modeText.color = _telemetry.ModeColor;
            _throttleBar.fillAmount = _telemetry.Throttle01;
            _throttleText.text = $"{Mathf.RoundToInt(_telemetry.Throttle01 * 100)}%";
            _altText.text = $"ALT {_telemetry.AltitudeMeters:F1} m";
            _spdText.text = $"SPD {_telemetry.SpeedMetersPerSec:F1} m/s";
            _fpsText.text = $"FPS {_currentFps}";

            var t = _telemetry.TimeArmedSeconds;
            var mm = Mathf.FloorToInt(t / 60f);
            var ss = Mathf.FloorToInt(t % 60f);
            _timeText.text = $"T {mm:00}:{ss:00}";
        }

        private void OnRespawnClicked()
        {
            _telemetry.RequestRespawn();
        }
    }
}
