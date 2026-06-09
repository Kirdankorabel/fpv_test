using UnityEngine;
using Zenject;
using Core;

namespace Drone
{
    public sealed class FpvCamera : MonoBehaviour
    {
        [SerializeField] private Camera _cam;

        [Inject] private SimConfig _cfg;

        private void Start()
        {
            _cam.fieldOfView = _cfg.FovDeg;
        }
    }
}
