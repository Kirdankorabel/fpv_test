namespace Input
{
    public readonly struct PilotCommand
    {
        public readonly float Throttle;
        public readonly float Roll;
        public readonly float Pitch;
        public readonly float Yaw;
        public readonly bool ArmRequested;
        public readonly bool RespawnRequested;

        public PilotCommand(float throttle, float roll, float pitch, float yaw, bool armRequested, bool respawnRequested)
        {
            Throttle = throttle;
            Roll = roll;
            Pitch = pitch;
            Yaw = yaw;
            ArmRequested = armRequested;
            RespawnRequested = respawnRequested;
        }
    }
}
