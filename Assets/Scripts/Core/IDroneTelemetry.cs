using UnityEngine;

namespace Core
{
    public interface IDroneTelemetry
    {
        string ModeText { get; }
        Color ModeColor { get; }
        float Throttle01 { get; }
        float AltitudeMeters { get; }
        float SpeedMetersPerSec { get; }
        float TimeArmedSeconds { get; }
        void RequestRespawn();
    }
}
