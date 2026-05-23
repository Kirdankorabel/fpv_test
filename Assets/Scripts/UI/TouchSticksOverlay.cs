using UnityEngine;
using UnityEngine.Serialization;

namespace UI
{
    public sealed class TouchSticksOverlay : MonoBehaviour
    {
        [FormerlySerializedAs("leftStickRoot")]
        [SerializeField] private GameObject _leftStickRoot;

        [FormerlySerializedAs("rightStickRoot")]
        [SerializeField] private GameObject _rightStickRoot;

        private void Awake()
        {
            bool show = Application.isMobilePlatform || Application.platform == RuntimePlatform.WindowsEditor;
            _leftStickRoot.SetActive(show);
            _rightStickRoot.SetActive(show);
        }
    }
}
