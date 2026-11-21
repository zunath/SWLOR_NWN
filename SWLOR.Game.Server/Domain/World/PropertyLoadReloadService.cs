using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// Hot reload of the property loader: leaves the loader's bookkeeping on the module while the old build quiesces, and in the
    /// new build registers the property instances and structure placeables that are in the world, takes the carried bookkeeping
    /// and resumes the queued loads. Split from <c>PropertyLoaderService</c>; the members moved unchanged.
    /// </summary>
    public class PropertyLoadReloadService : IPropertyLoadReloadService
    {
        private readonly IDBService _db;
        private readonly IPropertyCatalogService _catalog;
        private readonly IPropertyLoadRegistryService _registry;
        private readonly IPropertyLoaderService _loader;

        /// <summary>The key the loader's bookkeeping is carried under across a hot reload (<see cref="ReloadCarry"/>).</summary>
        private const string PropertyCarryKey = "properties";

        public PropertyLoadReloadService(IDBService db, IPropertyCatalogService catalog, IPropertyLoadRegistryService registry, IPropertyLoaderService loader)
        {
            _db = db;
            _catalog = catalog;
            _registry = registry;
            _loader = loader;
        }

        /// <inheritdoc />
        public void CarryLoadState()
        {
            var carry = _registry.CarryState();
            carry.Merge(_loader.CarryQueueState());

            ReloadCarry.Put(PropertyCarryKey, carry.ToString(Formatting.None));
        }

        /// <inheritdoc />
        public void AdoptLiveProperties()
        {
            var carried = ReloadCarry.Take(PropertyCarryKey);
            var properties = LoadSpawnedTypeProperties();
            var instances = 0;
            var structures = 0;

            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                var areaPropertyId = _registry.GetPropertyId(area);
                if (!string.IsNullOrEmpty(areaPropertyId) &&
                    properties.TryGetValue(areaPropertyId, out var areaProperty) &&
                    _catalog.GetPropertyDetail(areaProperty.PropertyType).SpawnType == PropertySpawnType.Instance)
                {
                    AdoptInstance(areaProperty, area);
                    instances++;
                }

                for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
                {
                    if (GetObjectType(obj) != ObjectType.Placeable)
                        continue;

                    // A building's door carries the building's id too; only the placeable of the structure's own resref is it.
                    var propertyId = _registry.GetPropertyId(obj);
                    if (string.IsNullOrEmpty(propertyId) ||
                        !properties.TryGetValue(propertyId, out var structure) ||
                        structure.PropertyType != PropertyType.Structure ||
                        GetResRef(obj) != _catalog.GetStructureByType(structure.StructureType).Resref)
                    {
                        continue;
                    }

                    _registry.RegisterStructurePlaceable(propertyId, obj);
                    structures++;
                }
            }

            if (carried != null)
            {
                var carry = JObject.Parse(carried);
                _registry.RestoreState(carry);
                _loader.RestoreQueueState(carry);
            }
            else
            {
                Log.Write(LogGroup.Error, "Hot reload: the build this one replaced carried no property load state; the properties in the " +
                                          "world are loaded, the other instanced properties load when they are entered.", true);
                foreach (var property in properties.Values)
                {
                    var spawnType = _catalog.GetPropertyDetail(property.PropertyType).SpawnType;
                    if (spawnType == PropertySpawnType.Instance)
                        _registry.SetPropertyLoadState(property.Id, _registry.TryGetRegisteredInstance(property.Id, out _) ? PropertyLoadState.Loaded : PropertyLoadState.Unloaded);
                    else if (spawnType == PropertySpawnType.Area)
                        _registry.SetPropertyLoadState(property.Id, PropertyLoadState.Loaded);
                }
            }

            var (queuedLoads, queuedWorldStructures) = _loader.ResumeQueuedLoads();

            Log.Write(LogGroup.Property, $"Hot reload: adopted {instances} property instances and {structures} structures from the world; " +
                                         $"{queuedLoads} instance loads and {queuedWorldStructures} world structures are still queued.", true);
        }

        /// <summary>Every property of the instance, world and area spawn types, by id: what the boot's load reads.</summary>
        private Dictionary<string, WorldProperty> LoadSpawnedTypeProperties()
        {
            var types = _catalog.PropertyTypes
                .Where(x => x.Value.SpawnType is PropertySpawnType.Instance or PropertySpawnType.World or PropertySpawnType.Area)
                .Select(s => (int)s.Key)
                .ToList();
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), types);
            var count = _db.SearchCount(query);
            var properties = new Dictionary<string, WorldProperty>();
            foreach (var property in _db.Search(query.AddPaging((int)count, 0)))
                properties[property.Id] = property;
            return properties;
        }

        /// <summary>
        /// Registers a live property instance as the boot's load left it: its area, the players in it, its spawn action done, and
        /// whatever its layout keeps outside the world (<see cref="PropertyLayout.OnReloadAdoptAction"/>, a starport's dock).
        /// </summary>
        private void AdoptInstance(WorldProperty property, uint area)
        {
            var instance = new PropertyInstance(area, property.Layout);
            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                if (GetIsPC(obj) && !GetIsDM(obj))
                    instance.Players.Add(obj);
            }

            _registry.AdoptInstance(property.Id, instance);

            var adopt = _catalog.GetLayoutByType(property.Layout).OnReloadAdoptAction;
            if (adopt == null)
                return;

            try
            {
                adopt(area);
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Hot reload: adopting property '{property.CustomName}' ({property.Id}) failed: {ex}", true);
            }
        }
    }
}
