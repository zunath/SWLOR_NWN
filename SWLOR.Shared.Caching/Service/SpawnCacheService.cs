using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Domain.World.Contracts;
using SWLOR.Shared.Domain.World.ValueObjects;

namespace SWLOR.Shared.Caching.Service
{
    /// <summary>
    /// Service responsible for caching spawn table data.
    /// </summary>
    public class SpawnCacheService : ISpawnCacheService
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded service to break circular dependency
        private readonly Lazy<IGenericCacheService> _cacheService;

        public SpawnCacheService(ILogger logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            
            // Initialize lazy services
            _cacheService = new Lazy<IGenericCacheService>(() => _serviceProvider.GetRequiredService<IGenericCacheService>());
        }
        
        // Lazy-loaded service to break circular dependency
        private IGenericCacheService CacheService => _cacheService.Value;

        public IInterfaceCache<string, SpawnTable> SpawnTableCache { get; private set; }

        public void LoadSpawnTables()
        {
            SpawnTableCache = CacheService.BuildInterfaceCache<ISpawnListDefinition, string, SpawnTable>()
                .WithDataExtractor(instance => instance.BuildSpawnTables())
                .Build();

            // Validate spawn tables
            foreach (var (key, table) in SpawnTableCache.AllItems)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    _logger.Write<ErrorLogGroup>($"Spawn table {key} has an invalid key. Values must be greater than zero.");
                    continue;
                }
            }

            Console.WriteLine($"Loaded {SpawnTableCache.AllItems.Count} spawn tables.");
        }

        public SpawnTable GetSpawnTable(string spawnTableId)
        {
            return SpawnTableCache?.AllItems.TryGetValue(spawnTableId, out var table) == true ? table : null;
        }

        public bool HasSpawnTable(string spawnTableId)
        {
            return SpawnTableCache?.AllItems.ContainsKey(spawnTableId) == true;
        }
    }
}
