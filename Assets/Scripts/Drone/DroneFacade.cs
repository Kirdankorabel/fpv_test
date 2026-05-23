using System.Collections.Generic;
using UnityEngine;
using Zenject;
using Core;
using Input;
using StateMachine;

namespace Drone
{
    public sealed class DroneFacade : MonoBehaviour
    {
        [SerializeField] private Rigidbody _rb;

        [Inject] private SimConfig _cfg;
        [Inject] private SignalBus _signals;
        [Inject] private IPilotInputProvider _input;

        private DroneStateMachine _fsm;
        private DirectFlightDynamics _dyn;
        private DroneController _controller;

        public Rigidbody Rb => _rb;
        public IFlightDynamics Dynamics => _dyn;

        private void Awake()
        {
            _dyn = new DirectFlightDynamics();
            _dyn.Configure(_rb, transform, _cfg);

            var states = new List<IDroneState>
            {
                new DisarmedState(),
                new ArmingState(_cfg),
                new FlyingState(),
                new CrashedState(),
                new RespawningState(_cfg, _input),
            };
            _fsm = new DroneStateMachine(states);

            var ctx = new DroneContext
            {
                Rb = _rb,
                Root = transform,
                Dynamics = _dyn,
                Fsm = _fsm,
                Signals = _signals,
                SpawnPos = transform.position,
                SpawnRot = transform.rotation,
            };
            _fsm.BindContext(ctx);
            _fsm.SubscribeToCrashSignal(_signals);
            _fsm.ChangeState(DroneStateId.Disarmed);

            _controller = new DroneController(_fsm, _input, _cfg, _signals);
        }

        private void FixedUpdate()
        {
            _controller.Tick(Time.fixedDeltaTime);
        }

        private void OnCollisionEnter(Collision c)
        {
            _controller.HandleCollision(c);
        }

        public void RequestRespawn()
        {
            _controller.RequestRespawn();
        }
    }
}
