namespace SWLOR.Component.World.Contracts
{
    public interface ISpawnService
    {
        void CacheData();
        void SpawnArea();
        void QueueDespawnArea();
        void QueueRespawn();
        void ProcessSpawnSystem();
        void ProcessQueuedSpawns();
        void DMSpawnCreature();
    }
}
