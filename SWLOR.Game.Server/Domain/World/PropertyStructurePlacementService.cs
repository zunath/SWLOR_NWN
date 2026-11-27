using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// Placing a structure: the item use handler that checks the limits and permissions of a property before it creates the
    /// structure or building. Split from <c>PropertyService</c>; the members moved unchanged.
    /// </summary>
    public class PropertyStructurePlacementService : IPropertyStructurePlacementService
    {
        private readonly IDBService _db;
        private readonly IPropertyCatalogService _catalog;
        private readonly IPropertyLoadRegistryService _registry;
        private readonly IPropertyService _property;
        private readonly IConversationMenuService _conversationMenu;

        public PropertyStructurePlacementService(
            IDBService db,
            IPropertyCatalogService catalog,
            IPropertyLoadRegistryService registry,
            IPropertyService property,
            IConversationMenuService conversationMenu)
        {
            _db = db;
            _catalog = catalog;
            _registry = registry;
            _property = property;
            _conversationMenu = conversationMenu;
        }

        /// <summary>
        /// Retrieves the structure type from an item.
        /// Item's resref must start with 'structure_' and end with 4 numbers.
        /// I.E: 'structure_0004'
        /// Returns StructureType.Invalid on error.
        /// </summary>
        /// <param name="item">The item to retrieve from.</param>
        /// <returns>A structure type associated with the item.</returns>
        public StructureType GetStructureTypeFromItem(uint item)
        {
            var resref = GetResRef(item);
            if (!resref.StartsWith("structure_")) return StructureType.Invalid;

            var id = resref.Substring(resref.Length - 4, 4);

            if (!int.TryParse(id, out var structureId))
            {
                return StructureType.Invalid;
            }

            return (StructureType)structureId;
        }

        /// <summary>
        /// Before an item is used, if it is a structure item, place it at the specified location.
        /// </summary>
        [NWNEventHandler(ScriptName.OnItemUseBefore)]
        public void PlaceStructure()
        {
            var item = StringToObject(EventsPlugin.GetEventData("ITEM_OBJECT_ID"));
            if (!GetResRef(item).StartsWith("structure_"))
                return;

            EventsPlugin.SkipEvent();

            var player = OBJECT_SELF;
            var area = GetArea(player);
            var propertyId = _registry.GetPropertyId(area);
            var playerId = GetObjectUUID(player);
            var structureType = GetStructureTypeFromItem(item);
            var position = Vector3(
                (float)Convert.ToDouble(EventsPlugin.GetEventData("TARGET_POSITION_X")),
                (float)Convert.ToDouble(EventsPlugin.GetEventData("TARGET_POSITION_Y")),
                (float)Convert.ToDouble(EventsPlugin.GetEventData("TARGET_POSITION_Z")));

            // Special case: City Hall pulls up a menu with details about the land and an option to place it down, claiming the land.
            if (structureType == StructureType.CityHall)
            {
                if (!GetLocalBool(area, "IS_BUILDABLE"))
                {
                    FloatingTextStringOnCreature("Cities cannot be founded here.", player, false);
                    return;
                }

                SetLocalObject(player, "PROPERTY_CITY_HALL_ITEM", item);
                SetLocalFloat(player, "PROPERTY_CITY_HALL_X", position.X);
                SetLocalFloat(player, "PROPERTY_CITY_HALL_Y", position.Y);
                SetLocalFloat(player, "PROPERTY_CITY_HALL_Z", position.Z);
                _conversationMenu.Start(player, player, nameof(PlaceCityHallDialog));
                return;
            }

            if (structureType == StructureType.Invalid) return;

            // Must be in a player property.
            if (string.IsNullOrWhiteSpace(propertyId))
            {
                FloatingTextStringOnCreature($"Structures may only be placed within player properties.", player, false);
                return;
            }

            var query = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), propertyId, false);
            var permission = _db.Search(query).FirstOrDefault();

            // Player must have permission to edit structures.
            if (permission == null ||
                !permission.Permissions[PropertyPermissionType.EditStructures])
            {
                FloatingTextStringOnCreature($"You do not have permission to place structures within this property.", player, false);
                return;
            }

            var property = _db.Get<WorldProperty>(propertyId);
            var layout = _catalog.GetLayoutByType(property.Layout);
            int structureLimit;
            var structureDetail = _catalog.GetStructureByType(structureType);
            var structureQuery = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.ParentPropertyId), propertyId, false)
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Structure);
            var structureQueryCount = (int)_db.SearchCount(structureQuery);
            var structures = _db.Search(structureQuery.AddPaging(structureQueryCount, 0));
            int structureCount;
            string fixtureName;

            // Special Case: Cities differentiate between structures and buildings.
            // They use the BuildingLimit value for buildings and StructureLimit for everything else.
            // Buildings are determined by structures whose restrictions are solely to "City" property types.
            if (property.PropertyType == PropertyType.City &&
                structureDetail.RestrictedPropertyTypes == PropertyType.City)
            {
                structureCount = structures.Count(x => _catalog.GetStructureByType(x.StructureType).LayoutType != PropertyLayoutType.Invalid);
                structureLimit = layout.BuildingLimit;
                fixtureName = "Building";
            }
            else
            {
                if (structureDetail.Category == StructureCategoryType.Structure)
                {
                    structureCount = structures.Count(x =>
                    {
                        var structureByType = _catalog.GetStructureByType(x.StructureType);
                        return structureByType.LayoutType == PropertyLayoutType.Invalid &&
                               structureByType.Category == StructureCategoryType.Structure;
                    });
                    structureLimit = layout.StructureLimit;
                    fixtureName = "Structure";
                }
                else if (structureDetail.Category == StructureCategoryType.ResearchDevice)
                {
                    structureCount = structures.Count(x =>
                    {
                        var structureByType = _catalog.GetStructureByType(x.StructureType);
                        return structureByType.LayoutType == PropertyLayoutType.Invalid &&
                               structureByType.Category == StructureCategoryType.ResearchDevice;
                    });
                    structureLimit = layout.ResearchDeviceLimit;
                    fixtureName = "Research Device";
                }
                else
                {
                    FloatingTextStringOnCreature($"Unable to place structure in this property. Notify an admin.", player, false);
                    return;
                }
            }

            // Over the structure limit.
            if (structureCount >= structureLimit)
            {
                FloatingTextStringOnCreature($"{fixtureName} limit reached for this property.", player, false);
                return;
            }

            // Structure can't be placed within this type of property.
            if (!structureDetail.RestrictedPropertyTypes.HasFlag(property.PropertyType))
            {
                FloatingTextStringOnCreature($"This {fixtureName} cannot be placed within this type of property.", player, false);
                return;
            }

            var location = Location(area, position, 0.0f);

            // If no interior layout is defined, this is a basic structure.
            if (structureDetail.LayoutType == PropertyLayoutType.Invalid)
            {
                _property.CreateStructure(propertyId, item, structureType, location);
            }
            // Otherwise we have a layout which means an interior must be spawned.
            else
            {
                var structureLayout = _catalog.GetLayoutByType(structureDetail.LayoutType);
                _property.CreateBuilding(
                    player,
                    item,
                    propertyId,
                    structureLayout.PropertyType,
                    structureDetail.LayoutType,
                    structureType, location);
            }

            SendMessageToPC(player, $"{fixtureName} Limit: {structureCount+1} / {structureLimit}");
        }
    }
}
