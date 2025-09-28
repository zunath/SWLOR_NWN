using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Domain.Properties.Contracts;
using SWLOR.Shared.Domain.Properties.Enums;

namespace SWLOR.Shared.Caching.Service
{

    public class AreaCacheService : IAreaCacheService
    {
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IPropertyService> _propertyService;
        private readonly Lazy<IGenericCacheService> _cache;
        private readonly Dictionary<string, uint> _areasByResref = new();
        private readonly Dictionary<uint, List<uint>> _playersByArea = new();

        public AreaCacheService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            
            // Initialize lazy services
            _propertyService = new Lazy<IPropertyService>(() => _serviceProvider.GetRequiredService<IPropertyService>());
            _cache = new Lazy<IGenericCacheService>(() => _serviceProvider.GetRequiredService<IGenericCacheService>());
        }
        
        // Lazy-loaded services to break circular dependencies
        private IPropertyService PropertyService => _propertyService.Value;
        private IGenericCacheService Cache => _cache.Value;

        /// <summary>
        /// Loads the area cache by iterating through all areas and storing their resrefs.
        /// </summary>
        public void LoadCache()
        {
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                var resref = GetResRef(area);
                _areasByResref[resref] = area;
            }

            Console.WriteLine($"Loaded {_areasByResref.Count} areas by resref.");
        }


        /// <summary>
        /// Retrieves an area by its resref. If the area does not exist, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="resref">The resref to use for the search.</param>
        /// <returns>The area ID or OBJECT_INVALID if area does not exist.</returns>
        public uint GetAreaByResref(string resref)
        {
            if (!_areasByResref.ContainsKey(resref))
                return OBJECT_INVALID;

            return _areasByResref[resref];
        }

        /// <summary>
        /// Retrieves list of all areas.
        /// </summary>
        /// <returns>AreasByResref cache.</returns>
        public Dictionary<string, uint> GetAreas()
        {
            return _areasByResref;
        }

        /// <summary>
        /// Retrieves all of the players currently in the specified area.
        /// If no players are in the area, an empty list will returned.
        /// </summary>
        /// <param name="area">The area to search by.</param>
        /// <returns>A list of player objects</returns>
        public List<uint> GetPlayersInArea(uint area)
        {
            if (!_playersByArea.ContainsKey(area))
                return new List<uint>();

            return _playersByArea[area].ToList();
        }

        /// <summary>
        /// When a player or DM enters an area, add them to the cache.
        /// </summary>
        /// <param name="player">The player entering the area</param>
        /// <param name="area">The area being entered</param>
        public void EnterArea(uint player, uint area)
        {
            if (!_playersByArea.ContainsKey(area))
                _playersByArea[area] = new List<uint>();

            if (!_playersByArea[area].Contains(player))
            {
                _playersByArea[area].Add(player);
            }
        }

        /// <summary>
        /// When a player or DM leaves an area, remove them from the cache.
        /// </summary>
        /// <param name="player">The player leaving the area</param>
        /// <param name="area">The area being left</param>
        public void ExitArea(uint player, uint area)
        {
            if (!_playersByArea.ContainsKey(area))
                _playersByArea[area] = new List<uint>();

            if (_playersByArea[area].Contains(player))
            {
                _playersByArea[area].Remove(player);
            }
        }

        /// <summary>
        /// Remove instance templates from the area cache on module load.
        /// This ensures player locations are not updated in places they shouldn't be.
        /// </summary>
        public void RemoveInstancesFromCache()
        {
            var propertyLayouts = PropertyService.GetAllLayoutsByPropertyType(PropertyType.Apartment);
            foreach (var type in propertyLayouts)
            {
                var layout = PropertyService.GetLayoutByType(type);
                if (_areasByResref.ContainsKey(layout.AreaInstanceResref))
                    _areasByResref.Remove(layout.AreaInstanceResref);
            }
        }
    }
}
