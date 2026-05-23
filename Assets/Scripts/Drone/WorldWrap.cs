using UnityEngine;
using Zenject;
using Core;

namespace Drone
{
    public sealed class WorldWrap : MonoBehaviour
    {
        [SerializeField] private Rigidbody _rb;

        [Inject] private SimConfig _cfg;

        private void FixedUpdate()
        {
            var half = _cfg.MapHalfSizeMeters;
            if (half <= 0f) return;

            var pos = _rb.position;
            var full = 2f * half;
            var changed = false;

            if (pos.x > half) { pos.x -= full; changed = true; }
            else if (pos.x < -half) { pos.x += full; changed = true; }

            if (pos.z > half) { pos.z -= full; changed = true; }
            else if (pos.z < -half) { pos.z += full; changed = true; }

            if (!changed) return;
            _rb.position = pos;
            _rb.transform.position = pos;
        }
    }
}
