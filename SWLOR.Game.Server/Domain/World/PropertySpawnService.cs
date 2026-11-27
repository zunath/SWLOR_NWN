using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// Spawning a property into the game world: a structure becomes a placeable, an instanced property becomes an area instance,
    /// an area property marks its pre-built area. Split from <c>PropertyLoaderService</c>; the members moved unchanged.
    /// </summary>
    public class PropertySpawnService : IPropertySpawnService
    {
        private readonly IAreaService _area;
        private readonly IPropertyCatalogService _catalog;
        private readonly IPropertyLoadRegistryService _registry;

        public PropertySpawnService(IAreaService area, IPropertyCatalogService catalog, IPropertyLoadRegistryService registry)
        {
            _area = area;
            _catalog = catalog;
            _registry = registry;
        }

        /// <summary>
        /// Shows a structure's player-given name on its placeable. A structure that still has its catalog name keeps its template's.
        /// </summary>
        public void ApplyStructureWorldName(uint placeable, WorldProperty structure)
        {
            var catalogName = _catalog.GetStructureByType(structure.StructureType).Name;
            SetName(placeable, StructureWorldName.Resolve(structure.CustomName, catalogName));
        }

        /// <summary>
        /// Spawns the property into the game world.
        /// For structures, this means spawning a placeable at the location.
        /// For cities, starships, apartments, and buildings this means spawning area instances.
        /// </summary>
        /// <param name="property">The property to spawn into the world.</param>
        /// <param name="area">The area to spawn the property into. Leave OBJECT_INVALID if spawning an instance.</param>
        public void SpawnIntoWorld(WorldProperty property, uint area)
        {
            var propertyDetail = _catalog.GetPropertyDetail(property.PropertyType);

            // World spawns represent placeables within the game world such as furniture and buildings
            if (propertyDetail.SpawnType == PropertySpawnType.World)
            {
                if (_registry.TryGetStructurePlaceable(property.Id, out var existingPlaceable) &&
                    GetIsObjectValid(existingPlaceable))
                {
                    _catalog.RunStructureChangedEvent(property.StructureType, StructureChangeType.PositionChanged, property, existingPlaceable);
                    return;
                }

                var furniture = _catalog.GetStructureByType(property.StructureType);

                var staticPosition = property.Positions[PropertyLocationType.StaticPosition];
                var position = Vector3(staticPosition.X, staticPosition.Y, staticPosition.Z);
                var location = Location(area, position, staticPosition.Orientation);

                var placeable = CreateObject(ObjectType.Placeable, furniture.Resref, location);
                SetPlotFlag(placeable, true);
                _registry.AssignPropertyId(placeable, property.Id);
                ApplyStructureWorldName(placeable, property);

                _registry.RegisterStructurePlaceable(property.Id, placeable);

                // Some structures have custom spawn-in actions which also need to be run
                // when brought into the world.
                _catalog.RunStructureChangedEvent(property.StructureType, StructureChangeType.PositionChanged, property, placeable);
            }
            // Instance spawns are instanced areas that are spawned dynamically into the game world.
            else if(propertyDetail.SpawnType == PropertySpawnType.Instance)
            {
                var layout = _catalog.GetLayoutByType(property.Layout);
                if (_registry.TryGetRegisteredInstance(property.Id, out var existingInstance) &&
                    GetIsObjectValid(existingInstance.Area))
                {
                    if (!_registry.IsSpawnActionCompleted(property.Id))
                    {
                        layout.OnSpawnAction?.Invoke(existingInstance.Area);
                        _registry.MarkSpawnActionCompleted(property.Id);
                    }

                    _registry.SetPropertyLoadState(property.Id, PropertyLoadState.Loaded);
                    return;
                }

                // If no interior layout is defined, the provided area will be used.
                var targetArea = _area.CreateInstance(layout.AreaInstanceResref);
                if (!GetIsObjectValid(targetArea))
                    throw new InvalidOperationException($"Unable to create property area from resref '{layout.AreaInstanceResref}'.");

                _registry.RegisterInstance(property.Id, targetArea, property.Layout);

                SetName(targetArea, "{PC} " + property.CustomName);

                if (layout.OnSpawnAction != null)
                {
                    layout.OnSpawnAction(targetArea);
                }

                _registry.MarkSpawnActionCompleted(property.Id);
                _registry.SetPropertyLoadState(property.Id, PropertyLoadState.Loaded);
            }
            // Area spawns exist in a pre-built area.
            else if(propertyDetail.SpawnType == PropertySpawnType.Area)
            {
                _registry.AssignPropertyId(area, property.Id);
                _registry.SetPropertyLoadState(property.Id, PropertyLoadState.Loaded);
            }
        }

    }
}
