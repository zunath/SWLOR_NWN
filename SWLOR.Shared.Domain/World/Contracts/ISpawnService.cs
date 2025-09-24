namespace SWLOR.Component.World.Contracts
{
    public interface ISpawnService
    {
        int DespawnMinutes { get; }
        int DefaultRespawnMinutes { get; }
        void CacheData();
        void SpawnArea();
        void QueueDespawnArea();
        void QueueRespawn();
        void ProcessSpawnSystem();
        void ProcessQueuedSpawns();
        void DMSpawnCreature();
    }
}
