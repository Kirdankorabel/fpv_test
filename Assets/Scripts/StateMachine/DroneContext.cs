using UnityEngine;
using Zenject;
using Drone;

namespace StateMachine
{
    public sealed class DroneContext
    {
        public Rigidbody Rb;
        public Transform Root;
        public IFlightDynamics Dynamics;
        public IDroneStateMachine Fsm;
        public SignalBus Signals;
        public Vector3 SpawnPos;
        public Quaternion SpawnRot;
        public float TimeInState;
    }
}
