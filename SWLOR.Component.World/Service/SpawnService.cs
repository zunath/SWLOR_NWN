using SWLOR.Component.World.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Domain.World.Contracts;

namespace SWLOR.Component.World.Service
{
    public class SpawnService : ISpawnService
    {
        private readonly ILogger _logger;
        private readonly ISpawnCacheService _spawnCache;
        private readonly ISpawnManagementService _spawnManagement;
        private readonly ISpawnProcessingService _spawnProcessing;
        private readonly ISpawnCreationService _spawnCreation;

        public SpawnService(
            ILogger logger,
            ISpawnCacheService spawnCache,
            ISpawnManagementService spawnManagement,
            ISpawnProcessingService spawnProcessing,
            ISpawnCreationService spawnCreation)
        {
            _logger = logger;
            _spawnCache = spawnCache;
            _spawnManagement = spawnManagement;
            _spawnProcessing = spawnProcessing;
            _spawnCreation = spawnCreation;
        }

        public int DespawnMinutes => _spawnProcessing.DespawnMinutes;
        public int DefaultRespawnMinutes => 5;

        public void CacheData()
        {
            _spawnCache.LoadSpawnTables();
            _spawnManagement.StoreSpawns();
        }

        public void SpawnArea()
        {
            _spawnManagement.SpawnArea();
        }

        public void QueueDespawnArea()
        {
            _spawnManagement.QueueDespawnArea();
        }

        public void QueueRespawn()
        {
            _spawnManagement.QueueRespawn();
        }

        public void ProcessSpawnSystem()
        {
            _spawnProcessing.ProcessSpawnSystem();
        }

        public void ProcessQueuedSpawns()
        {
            _spawnProcessing.ProcessQueuedSpawns();
        }

        public void DMSpawnCreature()
        {
            _spawnCreation.DMSpawnCreature();
        }
    }
}
