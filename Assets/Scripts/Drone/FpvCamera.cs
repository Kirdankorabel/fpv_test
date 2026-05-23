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
            transform.localRotation = Quaternion.Euler(-_cfg.CameraTiltDeg, 0f, 0f);
            _cam.fieldOfView = _cfg.FovDeg;
        }
    }
}
