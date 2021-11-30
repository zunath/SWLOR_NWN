using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PropertyService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class Property
    {
        private static readonly Dictionary<StructureType, StructureAttribute> _activeStructures = new();
        private static readonly Dictionary<PropertyType, PropertyTypeAttribute> _propertyTypes = new();
        private static readonly Dictionary<PropertyLayoutType, PropertyLayoutTypeAttribute> _activeLayouts = new();
        private static readonly Dictionary<PropertyType, List<PropertyLayoutType>> _layoutsByPropertyType = new();
        private static readonly Dictionary<PropertyLayoutType, Vector4> _entrancesByLayout = new();
        private static readonly Dictionary<PropertyPermissionType, PropertyPermissionAttribute> _activePermissions = new();

        private static readonly Dictionary<string, uint> _instanceTemplates = new();
        private static readonly Dictionary<string, PropertyInstance> _propertyInstances = new();
        private static readonly Dictionary<PropertyType, List<PropertyPermissionType>> _permissionsByPropertyType = new();

        private static readonly Dictionary<string, uint> _structurePropertyIdToPlaceable = new();

        /// <summary>
        /// When the module loads, cache all relevant data into memory.
        /// </summary>
        [NWNEventHandler("mod_cache")]
        public static void CacheData()
        {
            CachePropertyTypes();
            CachePropertyLayoutTypes();
            CachePermissions();
            CacheStructures();
            CacheInstanceTemplates();
        }

        /// <summary>
        /// When the module loads, clean up any deleted data, refreshes permissions and then load properties.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void OnModuleLoad()
        {
            CleanUpData();
            RefreshPermissions();
            LoadProperties();
        }

        private static void CachePropertyTypes()
        {
            var propertyTypes = Enum.GetValues(typeof(PropertyType)).Cast<PropertyType>();
            foreach (var type in propertyTypes)
            {
                var detail = type.GetAttribute<PropertyType, PropertyTypeAttribute>();
                _propertyTypes[type] = detail;
            }
        }

        private static void CachePropertyLayoutTypes()
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

            // Assign the list of permissions associated with each property type.
            _permissionsByPropertyType[PropertyType.Apartment] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.ExtendLease,
                PropertyPermissionType.CancelLease,
                PropertyPermissionType.EnterProperty,
                PropertyPermissionType.ChangeDescription,
                PropertyPermissionType.EditCategories
            };

            _permissionsByPropertyType[PropertyType.CityHall] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.ChangeDescription
            };

            _permissionsByPropertyType[PropertyType.Starship] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.EnterProperty,
                PropertyPermissionType.ChangeDescription,
                PropertyPermissionType.EditCategories,
                PropertyPermissionType.PilotShip,
                PropertyPermissionType.RefitShip
            };

            _permissionsByPropertyType[PropertyType.City] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.ExtendLease,
                PropertyPermissionType.CancelLease,
                PropertyPermissionType.EnterProperty,
                PropertyPermissionType.ChangeDescription
            };

            _permissionsByPropertyType[PropertyType.Category] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.AccessStorage
            };

            _permissionsByPropertyType[PropertyType.Bank] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.ChangeDescription,
                PropertyPermissionType.EditCategories,
            };

            _permissionsByPropertyType[PropertyType.MedicalCenter] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.ChangeDescription,
            };

            _permissionsByPropertyType[PropertyType.Starport] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.ChangeDescription,
            };

            _permissionsByPropertyType[PropertyType.Cantina] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.ChangeDescription,
            };

            _permissionsByPropertyType[PropertyType.House] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.ChangeDescription,
                PropertyPermissionType.EnterProperty,
                PropertyPermissionType.EditCategories,
            };
        }

        /// <summary>
        /// When the module loads, read all structure types and store them into the cache.
        /// </summary>
        private static void CacheStructures()
        {
            var structureTypes = Enum.GetValues(typeof(StructureType)).Cast<StructureType>();
            foreach (var structure in structureTypes)
            {
                var detail = structure.GetAttribute<StructureType, StructureAttribute>();

                if (detail.IsActive)
                {
                    _activeStructures[structure] = detail;
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
        private static Vector4 GetEntrancePosition(string areaResref)
        {
            var area = Cache.GetAreaByResref(areaResref);
            
            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                if (GetTag(obj) != "PROPERTY_ENTRANCE") continue;

                var position = GetPosition(obj);
                return new Vector4(position, GetFacing(obj));
            }
            
            return new Vector4();
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
            _propertyInstances[propertyId] = new PropertyInstance(instance);
        }

        /// <summary>
        /// Retrieves the instanced area associated with a specific property Id.
        /// </summary>
        /// <param name="propertyId">The property Id</param>
        /// <returns>An area associated with the property Id.</returns>
        public static PropertyInstance GetRegisteredInstance(string propertyId)
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
                    DB.Set(property);
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

            // Starship properties should have their current location wiped on every boot.
            // This ensures the player's ship doesn't get lost in space when they're thrown out of an instance.
            query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Starship);
            properties = DB.Search(query);

            foreach (var property in properties)
            {
                if (property.Positions.ContainsKey(PropertyLocationType.CurrentPosition))
                {
                    property.Positions.Remove(PropertyLocationType.CurrentPosition);
                    DB.Set(property);
                }
            }
        }

        private static void DeleteProperty(WorldProperty property)
        {
            // Recursively clear any children properties tied to this property.
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

            // Clear permissions for the property.
            var permissionsQuery = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), property.Id, false);
            var permissions = DB.Search(permissionsQuery);

            foreach (var permission in permissions)
            {
                DB.Delete<WorldPropertyPermission>(permission.Id);
                Log.Write(LogGroup.Property, $"Deleted property permission for property '{permission.PropertyId}' and player '{permission.PlayerId}'.");
            }

            // Clear item categories and their permissions
            var categoriesQuery = new DBQuery<WorldPropertyCategory>()
                .AddFieldSearch(nameof(WorldPropertyCategory.ParentPropertyId), property.Id, false);
            var categories = DB.Search(categoriesQuery).ToList();
            var categoryPropertyIds = categories.Select(s => s.Id).ToList();

            // Clear any permissions tied to categories.
            if (categoryPropertyIds.Count > 0)
            {
                permissionsQuery = new DBQuery<WorldPropertyPermission>()
                    .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), categoryPropertyIds);
                permissions = DB.Search(permissionsQuery).ToList();

                foreach (var permission in permissions)
                {
                    DB.Delete<WorldPropertyPermission>(permission.Id);
                    Log.Write(LogGroup.Property, $"Deleted property permission for category '{permission.PropertyId}'.");
                }
            }

            // Clear the actual categories (and any associated items)
            foreach (var category in categories)
            {
                DB.Delete<WorldPropertyCategory>(category.Id);
                Log.Write(LogGroup.Property, $"Deleted property category '{category.Name}', id: '{category.Id}' from property '{category.ParentPropertyId}'");
            }

            // Finally delete the entire property.
            DB.Delete<WorldProperty>(property.Id);
            Log.Write(LogGroup.Property, $"Property '{property.CustomName}' deleted.");
        }

        /// <summary>
        /// When the module loads, update the permissions list on all properties to reflect any changes.
        /// </summary>
        private static void RefreshPermissions()
        {
            foreach (var (type, permissions) in _permissionsByPropertyType)
            {
                var propertyQuery = new DBQuery<WorldProperty>()
                    .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)type);
                var dbProperties = DB.Search(propertyQuery);

                foreach (var property in dbProperties)
                {
                    var permissionQuery = new DBQuery<WorldPropertyPermission>()
                        .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), property.Id, false);
                    var dbPropertyPermissions = DB.Search(permissionQuery).ToList();
                    
                    foreach (var propertyPermission in dbPropertyPermissions)
                    {
                        // Perform a refresh of permissions (adding/removing as needed)
                        // If changes occurred, save them.
                        if (RefreshPermissions(property.OwnerPlayerId, propertyPermission, permissions))
                        {
                            DB.Set(propertyPermission);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Modifies the permissions on an individual world property permission object.
        /// </summary>
        /// <param name="propertyOwnerId">The Id of the property owner.</param>
        /// <param name="dbPermission">The permission to modify.</param>
        /// <param name="masterList">The master list of permissions.</param>
        private static bool RefreshPermissions(string propertyOwnerId, WorldPropertyPermission dbPermission, List<PropertyPermissionType> masterList)
        {
            var hasChanges = false;
            // Remove any permissions that have been removed from the master list.
            for (var index = dbPermission.Permissions.Count - 1; index >= 0; index--)
            {
                // Permission no longer exists in master list. Remove it from this instance.
                var permission = dbPermission.Permissions.ElementAt(index).Key;
                if (!masterList.Contains(permission))
                {
                    dbPermission.Permissions.Remove(permission);
                    Log.Write(LogGroup.Property, $"Removing permission {permission} from property {dbPermission.PropertyId} for player Id {dbPermission.PlayerId}.");
                    hasChanges = true;
                }
            }
            for (var index = dbPermission.GrantPermissions.Count - 1; index >= 0; index--)
            {
                var grantPermission = dbPermission.GrantPermissions.ElementAt(index).Key;
                if (!masterList.Contains(grantPermission))
                {
                    dbPermission.Permissions.Remove(grantPermission);
                    Log.Write(LogGroup.Property, $"Removing grant permission {grantPermission} from property {dbPermission.PropertyId} for player Id {dbPermission.PlayerId}.");
                    hasChanges = true;
                }
            }

            // Now add any new permissions
            var hasAccess = propertyOwnerId == dbPermission.PlayerId;
            foreach (var masterPermission in masterList)
            {
                if (!dbPermission.Permissions.ContainsKey(masterPermission))
                {
                    dbPermission.Permissions[masterPermission] = hasAccess;
                    Log.Write(LogGroup.Property, $"Adding permission {dbPermission.Permissions[masterPermission]} to property {dbPermission.PropertyId} for player Id {dbPermission.PlayerId}.");
                    hasChanges = true;
                }

                if (!dbPermission.GrantPermissions.ContainsKey(masterPermission))
                {
                    dbPermission.GrantPermissions[masterPermission] = hasAccess;
                    Log.Write(LogGroup.Property, $"Adding permission {dbPermission.Permissions[masterPermission]} to property {dbPermission.PropertyId} for player Id {dbPermission.PlayerId}.");
                    hasChanges = true;
                }
            }

            return hasChanges;
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
                SpawnIntoWorld(property, OBJECT_INVALID);
            }

            var cities = DB.Search(new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.City));
            foreach (var city in cities)
            {
                var area = Cache.GetAreaByResref(city.ParentPropertyId);
                SpawnIntoWorld(city, area);
            }
        }

        /// <summary>
        /// Creates a list of default item categories for use in freshly made properties.
        /// </summary>
        /// <returns>A list of categories</returns>
        private static List<WorldPropertyCategory> CreateDefaultCategories(string parentPropertyId)
        {
            return new List<WorldPropertyCategory>
            {
                new WorldPropertyCategory
                {
                    ParentPropertyId = parentPropertyId,
                    Name = "Weapons"
                },
                new WorldPropertyCategory
                {
                    ParentPropertyId = parentPropertyId,
                    Name = "Armor"
                },
                new WorldPropertyCategory
                {
                    ParentPropertyId = parentPropertyId,
                    Name = "Crafting"
                },
                new WorldPropertyCategory
                {
                    ParentPropertyId = parentPropertyId,
                    Name = "Miscellaneous"
                },
            };
        }

        private static WorldProperty CreateProperty(
            uint player, 
            PropertyType type, 
            PropertyLayoutType layout, 
            uint targetArea = OBJECT_INVALID,
            Action<WorldProperty> constructionAction = null)
        {
            var ownerId = GetObjectUUID(player);
            var layoutDetail = GetLayoutByType(layout);
            var propertyDetail = _propertyTypes[type];

            var property = new WorldProperty
            {
                CustomName = $"{GetName(player)}'s {propertyDetail.Name}",
                PropertyType = type,
                OwnerPlayerId = ownerId,
                IsPubliclyAccessible = false,
                Layout = layout,
                ItemStorageCount = layoutDetail.ItemStorageLimit
            };

            constructionAction?.Invoke(property);

            var permissions = new WorldPropertyPermission
            {
                PropertyId = property.Id,
                PlayerId = ownerId
            };

            foreach (var permission in _permissionsByPropertyType[type])
            {
                permissions.Permissions[permission] = true;
                permissions.GrantPermissions[permission] = true;
            }

            DB.Set(property);
            DB.Set(permissions);

            if (propertyDetail.HasStorage)
            {
                // Create the default item storage categories and give permission to the owner for all categories.
                foreach (var category in CreateDefaultCategories(property.Id))
                {
                    DB.Set(category);

                    var categoryPermission = new WorldPropertyPermission
                    {
                        PropertyId = category.Id,
                        PlayerId = ownerId
                    };

                    foreach (var permission in _permissionsByPropertyType[PropertyType.Category])
                    {
                        categoryPermission.Permissions[permission] = true;
                        categoryPermission.GrantPermissions[permission] = true;
                    }

                    DB.Set(categoryPermission);
                }
            }

            if (propertyDetail.ExistsInGameWorld)
            {
                SpawnIntoWorld(property, targetArea);
            }

            return property;
        }

        /// <summary>
        /// Creates a new apartment in the database for a given player.
        /// </summary>
        /// <param name="player">The player to associate the apartment with.</param>
        /// <param name="layout">The layout to use.</param>
        /// <returns>The new world property.</returns>
        public static WorldProperty CreateApartment(uint player, PropertyLayoutType layout)
        {
            return CreateProperty(player, PropertyType.Apartment, layout, OBJECT_INVALID, property =>
            {
                property.Timers[PropertyTimerType.Lease] = DateTime.UtcNow.AddDays(7);
            });
        }

        /// <summary>
        /// Creates a new starship in the database for a given player and returns the world property.
        /// </summary>
        /// <param name="player">The player to associate the starship with.</param>
        /// <param name="layout">The layout to use.</param>
        /// <param name="spaceLocation">Location of the space transfer point (when a player is converted to a ship)</param>
        /// <param name="landingLocation">Location of the ground transfer point (when a player is converted back to normal)</param>
        /// <returns>The new world property.</returns>
        public static WorldProperty CreateStarship(uint player, PropertyLayoutType layout, Location spaceLocation, Location landingLocation)
        {
            var spacePosition = GetPositionFromLocation(spaceLocation);
            var spaceOrientation = GetFacingFromLocation(spaceLocation);
            var spaceArea = GetAreaFromLocation(spaceLocation);
            var spaceAreaResref = GetResRef(spaceArea);

            var landingPosition = GetPositionFromLocation(landingLocation);
            var landingOrientation = GetFacingFromLocation(landingLocation);
            var landingArea = GetAreaFromLocation(landingLocation);
            var landingAreaResref = GetResRef(landingArea);

            return CreateProperty(player, PropertyType.Starship, layout, OBJECT_INVALID, property =>
            {
                property.Positions[PropertyLocationType.DockPosition] = new PropertyLocation
                {
                    X = landingPosition.X,
                    Y = landingPosition.Y,
                    Z = landingPosition.Z,
                    Orientation = landingOrientation,
                    AreaResref = landingAreaResref
                };

                property.Positions[PropertyLocationType.SpacePosition] = new PropertyLocation
                {
                    X = spacePosition.X,
                    Y = spacePosition.Y,
                    Z = spacePosition.Z,
                    Orientation = spaceOrientation,
                    AreaResref = spaceAreaResref
                };
            });
        }

        public static void CreateCity(uint player, uint area, uint item, Location location)
        {
            var city = CreateProperty(player, PropertyType.City, PropertyLayoutType.City, area, property =>
            {
                property.ParentPropertyId = GetResRef(area);
                AssignPropertyId(area, property.Id);
            });

            CreateBuilding(
                player,
                item,
                city.Id,
                PropertyType.CityHall,
                PropertyLayoutType.CityHall,
                StructureType.CityHall,
                location);
        }

        /// <summary>
        /// Creates a new structure and interior property associated with the building.
        /// </summary>
        /// <param name="player">The player to associate the building with.</param>
        /// <param name="parentPropertyId">The parent property Id.</param>
        /// <param name="propertyType">The type of property to create</param>
        /// <param name="layout">The layout to use.</param>
        /// <param name="item">The item used to create the building.</param>
        /// <param name="structureType">The type of structure to create.</param>
        /// <param name="location">The location to spawn the structure.</param>
        /// <returns>The new world property.</returns>
        public static void CreateBuilding(
            uint player, 
            uint item, 
            string parentPropertyId, 
            PropertyType propertyType, 
            PropertyLayoutType layout,
            StructureType structureType,
            Location location)
        {
            CreateStructure(parentPropertyId, item, structureType, location);
            CreateProperty(player, propertyType, layout);
        }

        /// <summary>
        /// Creates a structure inside a specific property.
        /// </summary>
        /// <param name="parentPropertyId">The parent property to associate this structure with.</param>
        /// <param name="item">The item used to spawn the structure.</param>
        /// <param name="type">The type of structure to spawn.</param>
        /// <param name="location">The location to spawn the structure at.</param>
        public static void CreateStructure(
            string parentPropertyId, 
            uint item,
            StructureType type, 
            Location location)
        {
            var structureDetail = GetStructureByType(type);
            var area = GetAreaFromLocation(location);
            var areaResref = GetResRef(area);
            var position = GetPositionFromLocation(location);
            var parentProperty = DB.Get<WorldProperty>(parentPropertyId);
            var structureItemStorage = structureDetail.ItemStorage;

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) != ItemPropertyType.StructureBonus)
                    continue;

                structureItemStorage += GetItemPropertyCostTableValue(ip);
            }

            var structure = new WorldProperty
            {
                CustomName = structureDetail.Name,
                PropertyType = PropertyType.Structure,
                SerializedItem = ObjectPlugin.Serialize(item),
                OwnerPlayerId = string.Empty,
                ParentPropertyId = parentPropertyId,
                StructureType = type,
                ItemStorageCount = structureItemStorage
            };

            structure.Positions[PropertyLocationType.StaticPosition] = new PropertyLocation
            {
                AreaResref = areaResref,
                Orientation = 0.0f,
                X = position.X,
                Y = position.Y,
                Z = position.Z
            };

            parentProperty.ChildPropertyIds.Add(structure.Id);
            parentProperty.ItemStorageCount += structureItemStorage; 

            DB.Set(structure);
            DB.Set(parentProperty);

            // Now spawn it within the game world.
            var placeable = CreateObject(ObjectType.Placeable, structureDetail.Resref, location);
            AssignPropertyId(placeable, structure.Id);

            _structurePropertyIdToPlaceable[structure.Id] = placeable;

            DestroyObject(item);
        }

        /// <summary>
        /// Retrieves a list of permissions associated with the item storage of a property for a given player.
        /// </summary>
        /// <param name="playerId">The player Id to search for</param>
        /// <param name="propertyId">The property Id to search for</param>
        /// <returns>A list of permissions</returns>
        public static List<WorldPropertyPermission> GetCategoryPermissions(string playerId, string propertyId)
        {
            var categoriesQuery = new DBQuery<WorldPropertyCategory>()
                .AddFieldSearch(nameof(WorldPropertyCategory.ParentPropertyId), propertyId, false);
            var categories = DB.Search(categoriesQuery).ToList();
            var categoryIds = categories.Select(s => s.Id).ToList();

            var permissionQuery = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), categoryIds)
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false);
            var permissions = DB.Search(permissionQuery);

            return permissions.ToList();
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
        /// Retrieves a structure detail by its type.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static StructureAttribute GetStructureByType(StructureType structure)
        {
            return _activeStructures[structure];
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
        public static Vector4 GetEntrancePosition(PropertyLayoutType type)
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
        /// Retrieves the list of available permissions for a given property type.
        /// </summary>
        /// <param name="type">The type of property</param>
        /// <returns>A list of available permissions</returns>
        public static List<PropertyPermissionType> GetPermissionsByPropertyType(PropertyType type)
        {
            return _permissionsByPropertyType[type];
        }

        /// <summary>
        /// Retrieves a placeable associated with a property Id.
        /// </summary>
        /// <param name="propertyId">The property Id to search for</param>
        /// <returns>A placeable or OBJECT_INVALID if not found.</returns>
        public static uint GetPlaceableByPropertyId(string propertyId)
        {
            return !_structurePropertyIdToPlaceable.ContainsKey(propertyId) 
                ? OBJECT_INVALID 
                : _structurePropertyIdToPlaceable[propertyId];
        }

        /// <summary>
        /// When an apartment terminal is used, open the Apartment NUI
        /// </summary>
        [NWNEventHandler("apartment_term")]
        public static void StartApartmentConversation()
        {
            var player = GetLastUsedBy();
            var terminal = OBJECT_SELF;
            
            Gui.TogglePlayerWindow(player, GuiWindowType.ManageApartment, null, terminal);
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
            var query = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), propertyId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false);
            var permissions = DB.Search(query).FirstOrDefault();

            // Player doesn't exist in the permissions list. No permission.
            if (permissions == null)
                return false;

            // Player exists, check their permission.
            return permissions.Permissions[permission];
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
            var query = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), propertyId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false);
            var permissions = DB.Search(query).FirstOrDefault();

            // Player doesn't exist in the permissions list. No permission.
            if (permissions == null)
                return false;

            // Player exists, check their permission.
            return permissions.GrantPermissions[permission];
        }

        /// <summary>
        /// Sends a player to the template area of a particular layout.
        /// </summary>
        /// <param name="player">The player to send.</param>
        /// <param name="layout">The layout type to send them to.</param>
        public static void PreviewProperty(uint player, PropertyLayoutType layout)
        {
            var entrance = GetEntrancePosition(layout);
            var area = GetInstanceTemplate(layout);
            var position = new Vector3(entrance.X, entrance.Y, entrance.Z);
            var location = Location(area, position, entrance.W);

            StoreOriginalLocation(player);
            AssignCommand(player, () =>
            {
                JumpToLocation(location);
            });
        }

        /// <summary>
        /// When a player enters a property instance, add them to the list of players.
        /// </summary>
        [NWNEventHandler("area_enter")]
        public static void EnterPropertyInstance()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var propertyId = GetPropertyId(OBJECT_SELF);

            if (!_propertyInstances.ContainsKey(propertyId))
                return;

            if (!_propertyInstances[propertyId].Players.Contains(player))
                _propertyInstances[propertyId].Players.Add(player);
        }

        /// <summary>
        /// When a player exits a property instance, remove them from the list of players.
        /// </summary>
        [NWNEventHandler("area_exit")]
        public static void ExitPropertyInstance()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var propertyId = GetPropertyId(OBJECT_SELF);

            if (!_propertyInstances.ContainsKey(propertyId))
                return;

            if (_propertyInstances[propertyId].Players.Contains(player))
                _propertyInstances[propertyId].Players.Remove(player);
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
            var entrance = _entrancesByLayout[property.Layout];
            var instance = GetRegisteredInstance(property.Id);
            var position = new Vector3(entrance.X, entrance.Y, entrance.Z);
            var location = Location(instance.Area, position, entrance.W);

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

        /// <summary>
        /// When the property menu feat is used, open the GUI window.
        /// </summary>
        [NWNEventHandler("feat_use_bef")]
        public static void PropertyMenu()
        {
            var feat = (FeatType)Convert.ToInt32(EventsPlugin.GetEventData("FEAT_ID"));

            if (feat != FeatType.PropertyMenu) return;

            var player = OBJECT_SELF;

            if (Gui.IsWindowOpen(player, GuiWindowType.ManageStructures))
            {
                Gui.TogglePlayerWindow(player, GuiWindowType.ManageStructures);
                return;
            }

            var area = GetArea(player);
            var propertyId = GetPropertyId(area);

            if (string.IsNullOrWhiteSpace(propertyId))
            {
                FloatingTextStringOnCreature($"This menu can only be accessed within player properties.", player, false);
                return;
            }

            var playerId = GetObjectUUID(player);
            var permissionQuery = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), propertyId, false);
            var permission = DB.Search(permissionQuery).FirstOrDefault();

            var categoryQuery = new DBQuery<WorldPropertyCategory>()
                .AddFieldSearch(nameof(WorldPropertyCategory.ParentPropertyId), propertyId, false);
            var categoryIds = DB.Search(categoryQuery).Select(s => s.Id).ToList();
            long categoryPermissionCount = 0;

            if (categoryIds.Count > 0)
            {
                permissionQuery = new DBQuery<WorldPropertyPermission>()
                    .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                    .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), categoryIds);
                categoryPermissionCount = DB.SearchCount(permissionQuery);
            }

            // Player can access this menu only if they have permission to manipulate structures,
            // retrieve structures, or access the property's item storage.
            if (permission == null ||
                !permission.Permissions[PropertyPermissionType.RetrieveStructures] &&
                !permission.Permissions[PropertyPermissionType.EditStructures] &&
                categoryPermissionCount <= 0)
            {
                FloatingTextStringOnCreature($"You do not have permission to access this property.", player, false);
                return;
            }
            
            Gui.TogglePlayerWindow(player, GuiWindowType.ManageStructures);
        }

        /// <summary>
        /// Retrieves the structure type from an item.
        /// Item's resref must start with 'structure_' and end with 4 numbers.
        /// I.E: 'structure_0004'
        /// Returns StructureType.Invalid on error.
        /// </summary>
        /// <param name="item">The item to retrieve from.</param>
        /// <returns>A structure type associated with the item.</returns>
        public static StructureType GetStructureTypeFromItem(uint item)
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
        [NWNEventHandler("item_use_bef")]
        public static void PlaceStructure()
        {
            var item = StringToObject(EventsPlugin.GetEventData("ITEM_OBJECT_ID"));
            if (!GetResRef(item).StartsWith("structure_"))
                return;

            EventsPlugin.SkipEvent();

            var player = OBJECT_SELF;
            var area = GetArea(player);
            var propertyId = GetPropertyId(area);
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
                Dialog.StartConversation(player, player, nameof(PlaceCityHallDialog));
                return;
            }

            // Must be in a player property.
            if (string.IsNullOrWhiteSpace(propertyId))
            {
                FloatingTextStringOnCreature($"Structures may only be placed within player properties.", player, false);
                return;
            }

            var query = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), propertyId, false);
            var permission = DB.Search(query).FirstOrDefault();

            // Player must have permission to edit structures.
            if (permission == null ||
                !permission.Permissions[PropertyPermissionType.EditStructures])
            {
                FloatingTextStringOnCreature($"You do not have permission to place structures within this property.", player, false);
                return;
            }
            
            var propertyQuery = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.Id), propertyId, false);
            var property = DB.Search(propertyQuery).Single();
            var structureLimit = property.ChildPropertyIds.Count;
            var layout = GetLayoutByType(property.Layout);

            // Over the structure limit.
            if (structureLimit >= layout.StructureLimit)
            {
                FloatingTextStringOnCreature($"No more structures may be placed here.", player, false);
                return;
            }

            // Structure can't be placed within this type of property.
            var structureDetail = GetStructureByType(structureType);

            if (!structureDetail.RestrictedPropertyTypes.HasFlag(property.PropertyType))
            {
                FloatingTextStringOnCreature($"This type of structure cannot be placed within this type of property.", player, false);
                return;
            }

            var location = Location(area, position, 0.0f);

            CreateStructure(propertyId, item, structureType, location);

            SendMessageToPC(player, $"Furniture Limit: {property.ChildPropertyIds.Count+1} / {layout.StructureLimit}");
        }

        /// <summary>
        /// Spawns the property into the game world.
        /// For structures, this means spawning a placeable at the location.
        /// For cities, starships, apartments, and buildings this means spawning area instances.
        /// </summary>
        /// <param name="property">The property to spawn into the world.</param>
        /// <param name="area">The area to spawn the property into. Leave OBJECT_INVALID if spawning an instance.</param>
        private static void SpawnIntoWorld(WorldProperty property, uint area)
        {
            // Structures represent placeables within the game world such as furniture and buildings
            if (property.PropertyType == PropertyType.Structure)
            {
                var furniture = GetStructureByType(property.StructureType);

                var staticPosition = property.Positions[PropertyLocationType.StaticPosition];
                var position = Vector3(staticPosition.X, staticPosition.Y, staticPosition.Z);
                var location = Location(area, position, staticPosition.Orientation);

                var placeable = CreateObject(ObjectType.Placeable, furniture.Resref, location);
                AssignPropertyId(placeable, property.Id);

                _structurePropertyIdToPlaceable[property.Id] = placeable;
            }
            // All other property types are area instances or regular areas (in the case of cities)
            else
            {
                uint targetArea;

                // If no interior layout is defined, the provided area will be used.
                if (property.Layout == PropertyLayoutType.Invalid ||
                    property.Layout == PropertyLayoutType.City)
                {
                    targetArea = area;
                    AssignPropertyId(targetArea, property.Id);
                }
                // If there is an interior, create an instance and use that as our target.
                else
                {
                    var layout = GetLayoutByType(property.Layout);
                    targetArea = CreateArea(layout.AreaInstanceResref);
                    RegisterInstance(property.Id, targetArea);

                    SetName(targetArea, property.CustomName);
                }

                if (property.ChildPropertyIds.Count > 0)
                {
                    var query = new DBQuery<WorldProperty>()
                        .AddFieldSearch(nameof(WorldProperty.Id), property.ChildPropertyIds);
                    var children = DB.Search(query);

                    foreach (var child in children)
                    {
                        SpawnIntoWorld(child, targetArea);
                    }
                }
            }
        }
    }
}
