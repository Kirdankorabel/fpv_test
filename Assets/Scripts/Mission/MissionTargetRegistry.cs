using System.Collections.Generic;
using UnityEngine;

namespace Mission
{
    public interface IMissionTargetRegistry
    {
        IReadOnlyList<Transform> Targets { get; }
        void Register(Transform target);
    }

    public sealed class MissionTargetRegistry : IMissionTargetRegistry
    {
        private readonly List<Transform> _targets = new List<Transform>();

        public IReadOnlyList<Transform> Targets => _targets;

        public void Register(Transform target)
        {
            _targets.Add(target);
        }
    }
}
