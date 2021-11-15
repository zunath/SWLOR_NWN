using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
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
        private static readonly Dictionary<PropertyPermissionType, PropertyPermissionAttribute> _activePermissions = new();

        private static readonly Dictionary<string, uint> _instanceTemplates = new();
        private static readonly Dictionary<string, uint> _propertyInstances = new();

        /// <summary>
        /// When the module loads, cache all relevant data into memory.
        /// </summary>
        [NWNEventHandler("mod_cache")]
        public static void CacheData()
        {
            CachePropertyTypes();
            CachePermissions();
            CacheFurniture();
            CacheInstanceTemplates();
        }

        /// <summary>
        /// When the module loads, clean up any deleted data and then load properties.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void OnModuleLoad()
        {
            CleanUpData();
            LoadProperties();
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

        private static void CachePermissions()
        {
            var permissionTypes = Enum.GetValues(typeof(PropertyPermissionType)).Cast<PropertyPermissionType>();
            foreach (var type in permissionTypes)
            {
                var permission = type.GetAttribute<PropertyPermissionType, PropertyPermissionAttribute>();

                if (permission.IsActive)
                {
                    _activePermissions[type] = permission;
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
        /// When the module loads, iterate over all areas and cache any that are instance templates.
        /// </summary>
        private static void CacheInstanceTemplates()
        {
            var templateResrefs = _activeLayouts
                .Select(x => x.Value.AreaInstanceResref)
                .ToList();

            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                var resref = GetResRef(area);
                if (templateResrefs.Contains(resref))
                    _instanceTemplates[resref] = area;
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
        /// Registers an area instance to a given property Id.
        /// </summary>
        /// <param name="propertyId">The property Id</param>
        /// <param name="instance">The area instance to register</param>
        public static void RegisterInstance(string propertyId, uint instance)
        {
            AssignPropertyId(instance, propertyId);
            _propertyInstances[propertyId] = instance;
        }

        /// <summary>
        /// Unregisters an area instance for a given property Id.
        /// </summary>
        /// <param name="propertyId">The property Id</param>
        public static void UnregisterInstance(string propertyId)
        {
            _propertyInstances.Remove(propertyId);
        }

        /// <summary>
        /// Retrieves the instanced area associated with a specific property Id.
        /// </summary>
        /// <param name="propertyId">The property Id</param>
        /// <returns>An area associated with the property Id.</returns>
        public static uint GetRegisteredInstance(string propertyId)
        {
            return _propertyInstances[propertyId];
        }
        
        /// <summary>
        /// When the module loads, remove all data marked for deletion and any properties with expired leases.
        /// </summary>
        private static void CleanUpData()
        {
            var now = DateTime.UtcNow;

            // Mark any properties with expired leases as queued for deletion. They will be picked up on the last
            // step of this method.
            var propertyTypesWithLeases = new[]
            {
                (int)PropertyType.Apartment,
                (int)PropertyType.City
            };
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), propertyTypesWithLeases);
            var properties = DB.Search(query);

            foreach (var property in properties)
            {
                var lease = property.Timers[PropertyTimerType.Lease];
                if (lease <= now)
                {
                    Log.Write(LogGroup.Property, $"Property '{property.CustomName}' has an expired lease. Expired on: {lease.ToString("G")}");

                    property.IsQueuedForDeletion = true;
                    DB.Set(property.Id.ToString(), property);
                }
            }

            // Remove any properties queued for deletion.
            query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), true);
            properties = DB.Search(query);

            foreach (var property in properties)
            {
                Log.Write(LogGroup.Property, $"Property '{property.CustomName}' scheduled for deletion. Peforming delete now.");
                DeleteProperty(property);
            }
        }

        private static void DeleteProperty(WorldProperty property)
        {
            if (property.ChildPropertyIds.Count > 0)
            {
                var query = new DBQuery<WorldProperty>()
                    .AddFieldSearch(nameof(WorldProperty.Id), property.ChildPropertyIds);
                var children = DB.Search(query);

                foreach (var child in children)
                {
                    DeleteProperty(child);
                }
            }

            DB.Delete<WorldProperty>(property.Id.ToString());
            Log.Write(LogGroup.Property, $"Property '{property.CustomName}' deleted.");
        }

        /// <summary>
        /// When the module loads, load all properties.
        /// </summary>
        private static void LoadProperties()
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
        /// <returns>The new world property.</returns>
        public static WorldProperty CreateApartment(uint player, PropertyLayoutType layout)
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
                        { PropertyPermissionType.EnterProperty , true},
                        { PropertyPermissionType.RenameStructures , true}
                    }
                },
                Timers =
                {
                    { PropertyTimerType.Lease, DateTime.UtcNow.AddDays(7) }
                },
                CustomName = $"{GetName(player)}'s Apartment",
                PropertyType = PropertyType.Apartment,
                OwnerPlayerId = playerId,
                IsPubliclyAccessible = false,
                InteriorLayout = layout
            };

            DB.Set(property.Id.ToString(), property);
            property.SpawnIntoWorld(OBJECT_INVALID);

            return property;
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

        /// <summary>
        /// Retrieves the template area by a given layout type.
        /// </summary>
        /// <param name="type">The type to search for.</param>
        /// <returns>The instance template area associated with this type.</returns>
        public static uint GetInstanceTemplate(PropertyLayoutType type)
        {
            var layout = _activeLayouts[type];
            return _instanceTemplates[layout.AreaInstanceResref];
        }

        /// <summary>
        /// Retrieves the detail for a given permission type.
        /// </summary>
        /// <param name="permission">The type of permission to retrieve.</param>
        /// <returns>A permission detail</returns>
        public static PropertyPermissionAttribute GetPermissionByType(PropertyPermissionType permission)
        {
            return _activePermissions[permission];
        }

        /// <summary>
        /// When an apartment terminal is used, open the Apartment NUI
        /// </summary>
        [NWNEventHandler("apartment_term")]
        public static void StartApartmentConversation()
        {
            var player = GetLastUsedBy();
            var terminal = OBJECT_SELF;

            var playerId = GetObjectUUID(player);
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.OwnerPlayerId), playerId, false)
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Apartment)
                .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), false);
            var existingApartment = DB.Search(query).FirstOrDefault();

            Gui.TogglePlayerWindow(player,
                existingApartment == null 
                    ? GuiWindowType.RentApartment 
                    : GuiWindowType.ManageApartment, null, terminal);
        }

        /// <summary>
        /// Determines whether a player has a specific permission.
        /// This will always return true for DMs.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <param name="propertyId">The property Id to check.</param>
        /// <param name="permission">The type of permission to check.</param>
        /// <returns>true if player has permission, false otherwise</returns>
        public static bool HasPropertyPermission(uint player, string propertyId, PropertyPermissionType permission)
        {
            // DMs always have permission.
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return true;
            
            if (!GetIsPC(player))
                return false;

            var playerId = GetObjectUUID(player);
            var property = DB.Get<WorldProperty>(propertyId);

            // Player doesn't exist in the permissions list. No permission.
            if (!property.Permissions.ContainsKey(playerId))
                return false;

            // Player exists, check their permission.
            return property.Permissions[playerId][permission];
        }

        /// <summary>
        /// Determines whether a player can GRANT a specific permission to another player.
        /// This will always return true for DMs.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <param name="propertyId">The property Id to check.</param>
        /// <param name="permission">The type of permission to check.</param>
        /// <returns>true if player can grant the permission, false otherwise</returns>
        public static bool CanGrantPermission(uint player, string propertyId, PropertyPermissionType permission)
        {
            // DMs always have permission.
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return true;

            if (!GetIsPC(player))
                return false;

            var playerId = GetObjectUUID(player);
            var property = DB.Get<WorldProperty>(propertyId);

            // Player doesn't exist in the permissions list. No permission.
            if (!property.GrantPermissions.ContainsKey(playerId))
                return false;

            // Player exists, check their permission.
            return property.GrantPermissions[playerId][permission];
        }

        /// <summary>
        /// Sends a player to the template area of a particular layout.
        /// </summary>
        /// <param name="player">The player to send.</param>
        /// <param name="layout">The layout type to send them to.</param>
        public static void PreviewProperty(uint player, PropertyLayoutType layout)
        {
            var position = GetEntrancePosition(layout);
            var area = GetInstanceTemplate(layout);
            var location = Location(area, position, 0.0f);

            StoreOriginalLocation(player);
            AssignCommand(player, () =>
            {
                JumpToLocation(location);
            });
        }

        /// <summary>
        /// Sends a player to a specific property's instance.
        /// </summary>
        /// <param name="player">The player to send.</param>
        /// <param name="propertyId">The property Id</param>
        public static void EnterProperty(uint player, string propertyId)
        {
            if (!HasPropertyPermission(player, propertyId, PropertyPermissionType.EnterProperty))
            {
                FloatingTextStringOnCreature("You do not have permission to access that property.", player, false);
                return;
            }

            var property = DB.Get<WorldProperty>(propertyId);
            var layout = _activeLayouts[property.InteriorLayout];
            var position = GetEntrancePosition(layout.AreaInstanceResref);
            var instance = GetRegisteredInstance(property.Id.ToString());
            var location = Location(instance, position, 0.0f);

            StoreOriginalLocation(player);
            AssignCommand(player, () =>
            {
                JumpToLocation(location);
            });
        }

        /// <summary>
        /// Stores the original location of a player, before being ported into a property instance.
        /// </summary>
        /// <param name="player">The player whose location will be stored.</param>
        public static void StoreOriginalLocation(uint player)
        {
            var position = GetPosition(player);
            var facing = GetFacing(player);
            var area = GetArea(player);

            SetLocalFloat(player, "PROPERTY_STORED_LOCATION_X", position.X);
            SetLocalFloat(player, "PROPERTY_STORED_LOCATION_Y", position.Y);
            SetLocalFloat(player, "PROPERTY_STORED_LOCATION_Z", position.Z);
            SetLocalFloat(player, "PROPERTY_STORED_LOCATION_FACING", facing);
            SetLocalObject(player, "PROPERTY_STORED_LOCATION_AREA", area);
        }

        /// <summary>
        /// Jumps player to their original location, which is where they were before entering a property instance.
        /// This will also clear the temporary data related to the original location.
        /// </summary>
        /// <param name="player">The player who will jump.</param>
        public static void JumpToOriginalLocation(uint player)
        {
            var position = Vector3(
                GetLocalFloat(player, "PROPERTY_STORED_LOCATION_X"),
                GetLocalFloat(player, "PROPERTY_STORED_LOCATION_Y"),
                GetLocalFloat(player, "PROPERTY_STORED_LOCATION_Z"));
            var facing = GetLocalFloat(player, "PROPERTY_STORED_LOCATION_FACING");
            var area = GetLocalObject(player, "PROPERTY_STORED_LOCATION_AREA");
            var location = Location(area, position, facing);

            AssignCommand(player, () => ActionJumpToLocation(location));

            DeleteLocalFloat(player, "PROPERTY_STORED_LOCATION_X");
            DeleteLocalFloat(player, "PROPERTY_STORED_LOCATION_Y");
            DeleteLocalFloat(player, "PROPERTY_STORED_LOCATION_Z");
            DeleteLocalFloat(player, "PROPERTY_STORED_LOCATION_FACING");
            DeleteLocalObject(player, "PROPERTY_STORED_LOCATION_AREA");
        }

    }
}
