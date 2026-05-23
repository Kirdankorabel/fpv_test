namespace Core
{
    public interface IDroneFactory
    {
        void Spawn(DroneEntry entry, DroneSpawnPoint spawn);
    }
}
