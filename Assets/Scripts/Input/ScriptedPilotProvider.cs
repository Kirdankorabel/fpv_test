using System;
using UnityEngine;

namespace Input
{
    public sealed class ScriptedPilotProvider : IPilotInputProvider
    {
        private readonly Func<float, PilotCommand> _sequence;
        private float _t;

        public ScriptedPilotProvider(Func<float, PilotCommand> sequence)
        {
            _sequence = sequence;
        }

        public PilotCommand Read()
        {
            PilotCommand c = _sequence(_t);
            _t += Time.fixedDeltaTime;
            return c;
        }

        public void Reset()
        {
            _t = 0f;
        }
    }
}
