using System.Collections.Generic;
using Zenject;
using Input;
using Core;

namespace StateMachine
{
    public sealed class DroneStateMachine : IDroneStateMachine
    {
        private readonly Dictionary<DroneStateId, IDroneState> _states = new Dictionary<DroneStateId, IDroneState>();
        private DroneContext _ctx;
        private IDroneState _current;

        public DroneStateId CurrentId => _current.Id;

        public DroneStateMachine(List<IDroneState> all)
        {
            foreach (var s in all) _states[s.Id] = s;
        }

        public void BindContext(DroneContext newCtx)
        {
            _ctx = newCtx;
        }

        public void ChangeState(DroneStateId id)
        {
            var next = _states[id];
            _current?.Exit(_ctx);
            _ctx.TimeInState = 0f;
            _current = next;
            _current.Enter(_ctx);
        }

        public void Tick(PilotCommand cmd, float dt)
        {
            _ctx.TimeInState += dt;
            _current.Tick(_ctx, cmd, dt);
        }

        public void SubscribeToCrashSignal(SignalBus bus)
        {
            bus.Subscribe<DroneCrashedSignal>(OnCrash);
        }

        private void OnCrash(DroneCrashedSignal _)
        {
            if (_current.Id == DroneStateId.Flying) ChangeState(DroneStateId.Crashed);
        }
    }
}
