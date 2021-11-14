using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.HousingService;
using SWLOR.Game.Server.Service.PropertyService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class Property
    {
        private static readonly Dictionary<FurnitureType, FurnitureAttribute> _activeFurniture = new();
        private static readonly Dictionary<PropertyLayoutType, PropertyLayoutTypeAttribute> _activeLayouts = new();
        private static readonly Dictionary<PropertyType, List<PropertyLayoutType>> _layoutsByPropertyType = new();
        private static readonly Dictionary<PropertyLayoutType, Vector3> _entrancesByLayout = new();

        /// <summary>
        /// When the module loads, cache all relevant data into memory.
        /// </summary>
        [NWNEventHandler("mod_cache")]
        public static void CacheData()
        {
            CachePropertyTypes();
            CacheFurniture();
        }

        private static void CachePropertyTypes()
        {
            var layoutTypes = Enum.GetValues(typeof(PropertyLayoutType)).Cast<PropertyLayoutType>();
            foreach (var type in layoutTypes)
            {
                var layout = type.GetAttribute<PropertyLayoutType, PropertyLayoutTypeAttribute>();

                if (layout.IsActive)
                {
                    _activeLayouts[type] = layout;

                    if (!_layoutsByPropertyType.ContainsKey(layout.PropertyType))
                        _layoutsByPropertyType[layout.PropertyType] = new List<PropertyLayoutType>();

                    _layoutsByPropertyType[layout.PropertyType].Add(type);
                    _entrancesByLayout[type] = GetEntrancePosition(layout.AreaInstanceResref);
                }
            }
        }

        /// <summary>
        /// When the module loads, read all furniture types and store them into the cache.
        /// </summary>
        private static void CacheFurniture()
        {
            var furnitureTypes = Enum.GetValues(typeof(FurnitureType)).Cast<FurnitureType>();
            foreach (var furniture in furnitureTypes)
            {
                var furnitureDetail = furniture.GetAttribute<FurnitureType, FurnitureAttribute>();

                if (furnitureDetail.IsActive)
                {
                    _activeFurniture[furniture] = furnitureDetail;
                }
            }
        }

        /// <summary>
        /// Iterates over all areas to find the matching instance assigned to the specified resref.
        /// Then, the entrance waypoint is located and its coordinates are stored into cache.
        /// </summary>
        /// <param name="areaResref">The resref of the area to look for</param>
        /// <returns>X, Y, and Z coordinates of the entrance location</returns>
        private static Vector3 GetEntrancePosition(string areaResref)
        {
            var area = Cache.GetAreaByResref(areaResref);
            
            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                if (GetTag(obj) != "PROPERTY_ENTRANCE") continue;

                var position = GetPosition(obj);
                return position;
            }
            
            return new Vector3();
        }

        /// <summary>
        /// Assigns a property Id as a local variable to a specific object.
        /// </summary>
        /// <param name="obj">The object to assign</param>
        /// <param name="propertyId">The property Id to assign.</param>
        public static void AssignPropertyId(uint obj, string propertyId)
        {
            SetLocalString(obj, "PROPERTY_ID", propertyId);
        }

        /// <summary>
        /// Retrieves the assigned property Id assigned to a specific object.
        /// Returns an empty string if not found.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>The property Id or an empty string if not found.</returns>
        public static string GetPropertyId(uint obj)
        {
            return GetLocalString(obj, "PROPERTY_ID");
        }

        /// <summary>
        /// When the module loads, load all properties.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void LoadProperties()
        {
            var apartments = DB.Search(new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Apartment));
            var starships = DB.Search(new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Starship));

            var propertiesWithInstances = new List<WorldProperty>();
            propertiesWithInstances.AddRange(apartments);
            propertiesWithInstances.AddRange(starships);
            foreach (var property in propertiesWithInstances)
            {
                property.SpawnIntoWorld(OBJECT_INVALID);
            }

            var cities = DB.Search(new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.City));
            foreach (var city in cities)
            {
                var area = Cache.GetAreaByResref(city.ParentPropertyId);
                city.SpawnIntoWorld(area);
            }
        }

        /// <summary>
        /// Creates a new apartment in the database for a given player.
        /// </summary>
        /// <param name="player">The player to associate the apartment with.</param>
        /// <param name="layout">The layout to use.</param>
        public static void CreateApartment(uint player, PropertyLayoutType layout)
        {
            var playerId = GetObjectUUID(player);
            var property = new WorldProperty
            {
                Permissions =
                {
                    [playerId] = new Dictionary<PropertyPermissionType, bool>
                    {
                        { PropertyPermissionType.AdjustPermissions , true},
                        { PropertyPermissionType.EditStructures , true},
                        { PropertyPermissionType.RetrieveStructures , true},
                        { PropertyPermissionType.RenameProperty , true},
                        { PropertyPermissionType.AccessStorage , true},
                        { PropertyPermissionType.ExtendLease , true},
                        { PropertyPermissionType.CancelLease , true},
                        { PropertyPermissionType.Enter , true},
                        { PropertyPermissionType.RenameStructures , true}
                    }
                },
                CustomName = $"{GetName(player)}'s Apartment",
                PropertyType = PropertyType.Apartment,
                OwnerPlayerId = playerId,
                IsPubliclyAccessible = false,
                InteriorLayout = layout
            };

            DB.Set(property.Id.ToString(), property);
            property.SpawnIntoWorld(OBJECT_INVALID);
        }

        public static void CreateStarship(uint player, PropertyLayoutType layout)
        {

        }

        public static void CreateBuilding(uint player, PropertyLayoutType layout)
        {

        }

        /// <summary>
        /// Retrieves a property layout by its type.
        /// </summary>
        /// <param name="type">The type of layout to retrieve.</param>
        /// <returns>A property layout</returns>
        public static PropertyLayoutTypeAttribute GetLayoutByType(PropertyLayoutType type)
        {
            return _activeLayouts[type];
        }

        /// <summary>
        /// Retrieves a furniture detail by its type.
        /// </summary>
        /// <param name="furniture"></param>
        /// <returns></returns>
        public static FurnitureAttribute GetFurnitureByType(FurnitureType furniture)
        {
            return _activeFurniture[furniture];
        }

        /// <summary>
        /// Retrieves all layouts for a given property type.
        /// </summary>
        /// <param name="type">The type of property to search for</param>
        /// <returns>A list of layouts for the given property type.</returns>
        public static List<PropertyLayoutType> GetAllLayoutsByPropertyType(PropertyType type)
        {
            return _layoutsByPropertyType[type].ToList();
        }

        /// <summary>
        /// Retrieves the entrance point for a given layout.
        /// </summary>
        /// <param name="type">The layout type</param>
        /// <returns>The entrance position for the layout.</returns>
        public static Vector3 GetEntrancePosition(PropertyLayoutType type)
        {
            return _entrancesByLayout[type];
        }

    }
}
