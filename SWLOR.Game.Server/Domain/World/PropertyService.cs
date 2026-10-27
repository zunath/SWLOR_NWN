using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The property system: creating and deleting properties, permissions, entering and leaving them, placing
    /// structures and the terminal menus. The definitions, instance loading and city governance are the parts
    /// <see cref="IPropertyCatalog"/>, <see cref="IPropertyLoader"/> and <see cref="IPropertyCityGovernance"/>; the logic and
    /// state that used to be static in <c>Service.Property</c> moved to these classes unchanged, and the members of the
    /// parts are forwarded from here so the facade keeps one interface. The facade still carries the event handlers.
    /// </summary>
    public class PropertyService : IPropertyService
    {
        private readonly IDBService _db;
        private readonly IAreaService _area;
        private readonly IPlanetService _planet;
        private readonly IPropertyCatalog _catalog;
        private readonly IPropertyLoader _loader;
        private readonly IPropertyCityGovernance _city;

        public PropertyService(
            IDBService db,
            IAreaService area,
            IPlanetService planet,
            IPropertyCatalog catalog,
            IPropertyLoader loader,
            IPropertyCityGovernance city)
        {
            _db = db;
            _area = area;
            _planet = planet;
            _catalog = catalog;
            _loader = loader;
            _city = city;
        }

        /// <inheritdoc />
        public void BroadcastCityUpkeepPaid(WorldProperty city)
        {
            _city.BroadcastCityUpkeepPaid(city);
        }

        /// <inheritdoc />
        public void CacheData()
        {
            _catalog.CacheData();
        }

        /// <summary>
        /// When the module loads, clean up any deleted data, refreshes permissions and then load properties.
        /// </summary>
        public void OnModuleLoad()
        {
            RefreshPermissions();
            _city.ProcessCities();
            CleanUpData();
            _loader.LoadProperties();
        }

        /// <inheritdoc />
        public void AssignPropertyId(uint obj, string propertyId)
        {
            _loader.AssignPropertyId(obj, propertyId);
        }

        /// <inheritdoc />
        public string GetPropertyId(uint obj)
        {
            return _loader.GetPropertyId(obj);
        }

        /// <inheritdoc />
        public void RegisterInstance(string propertyId, uint instance, PropertyLayoutType layoutType)
        {
            _loader.RegisterInstance(propertyId, instance, layoutType);
        }

        /// <inheritdoc />
        public PropertyLoadState GetPropertyLoadState(string propertyId)
        {
            return _loader.GetPropertyLoadState(propertyId);
        }

        /// <inheritdoc />
        public string GetPropertyLoadFailure(string propertyId)
        {
            return _loader.GetPropertyLoadFailure(propertyId);
        }

        /// <inheritdoc />
        public List<PropertyLoadDiagnostic> GetPropertyLoadDiagnostics()
        {
            return _loader.GetPropertyLoadDiagnostics();
        }

        /// <inheritdoc />
        public bool TryGetLoadedInstance(string propertyId, out PropertyInstance instance)
        {
            return _loader.TryGetLoadedInstance(propertyId, out instance);
        }

        /// <inheritdoc />
        public List<string> GetAllInstanceAreaResrefs()
        {
            return _catalog.GetAllInstanceAreaResrefs();
        }

        /// <inheritdoc />
        public void NotifyPropertyLoadWaitersForStaff(string propertyId)
        {
            _loader.NotifyPropertyLoadWaitersForStaff(propertyId);
        }

        /// <inheritdoc />
        public bool TryResolveEnterableInstance(uint player, string propertyId, out PropertyInstance instance)
        {
            return _loader.TryResolveEnterableInstance(player, propertyId, out instance);
        }

        /// <inheritdoc />
        public bool RetryPropertyLoad(string propertyId)
        {
            return _loader.RetryPropertyLoad(propertyId);
        }

        /// <inheritdoc />
        public bool AbortQueuedPropertyLoad(string propertyId)
        {
            return _loader.AbortQueuedPropertyLoad(propertyId);
        }

        /// <summary>
        /// When the module loads, remove all data marked for deletion and any properties with expired leases.
        /// </summary>
        private void CleanUpData()
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
            var queryCount = (int)_db.SearchCount(query);
            var properties = _db.Search(query.AddPaging(queryCount, 0));

            foreach (var property in properties)
            {
                var lease = property.Dates[PropertyDateType.Lease];
                if (lease <= now)
                {
                    Log.Write(LogGroup.Property, $"Property '{property.CustomName}' has an expired lease. Expired on: {lease.ToString("G")}");

                    property.IsQueuedForDeletion = true;
                    _db.Set(property);
                }
            }

            // Remove any properties queued for deletion.
            query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), true);
            queryCount = (int)_db.SearchCount(query);
            properties = _db.Search(query.AddPaging(queryCount, 0));

            foreach (var property in properties)
            {
                Log.Write(LogGroup.Property, $"Property '{property.CustomName}' scheduled for deletion. Peforming delete now.");

                // A failed delete stays queued for the next boot and must not stop the remaining properties from loading.
                try
                {
                    DeleteProperty(property);
                }
                catch (Exception ex)
                {
                    Log.Write(LogGroup.Error, $"Failed to delete property '{property.CustomName}' ({property.Id}): {ex}");
                }
            }

            // Starship properties should have their current location wiped on every boot.
            // This ensures the player's ship doesn't get lost in space when they're thrown out of an instance.
            var starshipQuery = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Starship);
            var starshipCount = (int)_db.SearchCount(starshipQuery);
            var starshipProperties = _db.Search(starshipQuery.AddPaging(starshipCount, 0));

            foreach (var property in starshipProperties)
            {
                if (property.Positions.ContainsKey(PropertyLocationType.CurrentPosition))
                {
                    property.Positions.Remove(PropertyLocationType.CurrentPosition);
                }

                ReturnStarshipToSafeDock(property);

                _db.Set(property);
            }
        }

        /// <summary>
        /// Starships left in a ship-only location, at a player starport that no longer exists (probably destroyed
        /// by the previous cleanup) or is being retrieved, or without a dock return to the last regular starport
        /// they docked at and its orbit. The caller saves the property.
        /// </summary>
        /// <param name="property">The starship property to update.</param>
        /// <param name="retrievedStarportId">A player starport interior being retrieved. Ships docked there return even though it still exists.</param>
        /// <param name="fallbackAreaResref">An area whose planet's starport receives the ship if it has no usable safe dock, such as the retrieved starport's city.</param>
        /// <returns>What happened to the starship's dock.</returns>
        public StarshipDockReturnResult ReturnStarshipToSafeDock(WorldProperty property, string retrievedStarportId = "", string fallbackAreaResref = "")
        {
            bool IsShipOnlyArea(string areaResref) =>
                StarshipDockReturn.IsShipOnlyArea(_area.GetAreaByResref(areaResref));

            if (StarshipDockReturn.RepairSafeDock(property.Positions, IsShipOnlyArea, GetPlanetStarport))
            {
                Log.Write(LogGroup.Property, $"Starship '{property.CustomName}' ({property.Id}) had a ship-only location recorded as its last NPC dock. It has been replaced with the planet's starport at '{property.Positions[PropertyLocationType.LastNPCDockPosition].AreaResref}'.");
            }

            property.Positions.TryGetValue(PropertyLocationType.DockPosition, out var previousDock);
            var result = StarshipDockReturn.ReturnToSafeDock(
                property.Positions,
                IsShipOnlyArea,
                starportId => starportId != retrievedStarportId && _db.Exists<WorldProperty>(starportId),
                GetPlanetStarport,
                fallbackAreaResref);

            var destination = property.Positions.TryGetValue(PropertyLocationType.DockPosition, out var dock)
                ? dock.AreaResref
                : string.Empty;
            switch (result)
            {
                case StarshipDockReturnResult.ReturnedFromShipOnlyLocation:
                    Log.Write(LogGroup.Property, $"Starship '{property.CustomName}' ({property.Id}) was docked at the ship-only location '{previousDock?.AreaResref}'. It has been returned to the last NPC dock position at '{destination}'.");
                    break;
                case StarshipDockReturnResult.ReturnedFromMissingStarport:
                    Log.Write(LogGroup.Property, previousDock?.InstancePropertyId == retrievedStarportId
                        ? $"Starship '{property.CustomName}' ({property.Id}) was docked at the retrieved player starport ({retrievedStarportId}). It has been relocated to the last NPC dock position at '{destination}'."
                        : $"Starship '{property.CustomName}' ({property.Id}) was docked at a non-existent player starport. It has been relocated to the last NPC dock position at '{destination}'.");
                    break;
                case StarshipDockReturnResult.RestoredMissingDock:
                    Log.Write(LogGroup.Property, $"Starship '{property.CustomName}' ({property.Id}) had no dock position. It has been placed at the last NPC dock position at '{destination}'.");
                    break;
                case StarshipDockReturnResult.ReturnedToPlanetStarport:
                    Log.Write(LogGroup.Property, $"Starship '{property.CustomName}' ({property.Id}) had to leave '{previousDock?.AreaResref}{previousDock?.InstancePropertyId}' without a last NPC dock position. It has been returned to the planet's starport at '{destination}'.");
                    break;
                case StarshipDockReturnResult.NoSafeDock:
                    Log.Write(LogGroup.Error, $"Starship '{property.CustomName}' ({property.Id}) needs to return to its last NPC dock position but none could be found. Dock position: '{previousDock?.AreaResref}{previousDock?.InstancePropertyId}'.");
                    break;
            }

            return result;
        }

        /// <summary>
        /// Retrieves the NPC starport landing and orbit of the planet an area belongs to.
        /// </summary>
        /// <param name="areaResref">The resref of the area.</param>
        /// <returns>The planet's landing and orbit locations, or null if the area has no planet or its waypoints are missing.</returns>
        private (PropertyLocation Landing, PropertyLocation Orbit)? GetPlanetStarport(string areaResref)
        {
            var planetType = _planet.GetPlanetType(_area.GetAreaByResref(areaResref));
            if (planetType == PlanetType.Invalid)
                return null;

            var planet = _planet.GetPlanetByType(planetType);
            var landingWaypoint = GetWaypointByTag(planet.LandingWaypointTag);
            var orbitWaypoint = GetWaypointByTag(planet.SpaceOrbitWaypointTag);
            if (!GetIsObjectValid(landingWaypoint) || !GetIsObjectValid(orbitWaypoint))
                return null;

            return (ToPropertyLocation(landingWaypoint), ToPropertyLocation(orbitWaypoint));
        }

        private PropertyLocation ToPropertyLocation(uint waypoint)
        {
            var position = GetPosition(waypoint);
            return new PropertyLocation
            {
                AreaResref = GetResRef(GetArea(waypoint)),
                X = position.X,
                Y = position.Y,
                Z = position.Z,
                Orientation = GetFacing(waypoint)
            };
        }

        /// <summary>
        /// Deletes a property and its owned children, clears associated data and permissions,
        /// and revokes its citizens' membership and unpaid citizenship taxes.
        /// </summary>
        /// <param name="property">The property to delete.</param>
        public void DeleteProperty(WorldProperty property)
        {
            if (property.PropertyType == PropertyType.Starship)
                UnregisterStarshipFromStarports(property);

            // Recursively clear any children properties tied to this property.
            // Children are also found by their parent Id because loading spawns structures that way,
            // so a stale child list can't leave orphaned buildings or interiors behind.
            var childIds = new HashSet<string>();
            foreach (var (childType, propertyIds) in property.ChildPropertyIds)
            {
                if (childType == PropertyChildType.RegisteredStarport ||
                    childType == PropertyChildType.Starship)
                    continue;

                childIds.UnionWith(propertyIds);
            }

            var childQuery = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.ParentPropertyId), property.Id, false);
            var childCount = (int)_db.SearchCount(childQuery);
            childIds.UnionWith(_db.Search(childQuery.AddPaging(childCount, 0)).Select(s => s.Id));
            childIds.Remove(property.Id);

            foreach (var childId in childIds)
            {
                var child = _db.Get<WorldProperty>(childId);
                if (child != null)
                    DeleteProperty(child);
            }

            // Clear permissions for the property.
            var permissionsQuery = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), property.Id, false);
            var permissionsCount = (int)_db.SearchCount(permissionsQuery);
            var permissions = _db.Search(permissionsQuery.AddPaging(permissionsCount, 0));

            foreach (var permission in permissions)
            {
                _db.Delete<WorldPropertyPermission>(permission.Id);
                Log.Write(LogGroup.Property, $"Deleted property permission for property '{permission.PropertyId}' and player '{permission.PlayerId}'.");
            }

            // Clear item categories and their permissions
            var categoriesQuery = new DBQuery<WorldPropertyCategory>()
                .AddFieldSearch(nameof(WorldPropertyCategory.ParentPropertyId), property.Id, false);
            var categoriesCount = (int)_db.SearchCount(categoriesQuery);
            var categories = _db.Search(categoriesQuery.AddPaging(categoriesCount, 0))
                .ToList();
            var categoryPropertyIds = categories.Select(s => s.Id).ToList();

            // Clear any permissions tied to categories.
            if (categoryPropertyIds.Count > 0)
            {
                permissionsQuery = new DBQuery<WorldPropertyPermission>()
                    .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), categoryPropertyIds);
                permissionsCount = (int)_db.SearchCount(permissionsQuery);
                permissions = _db.Search(permissionsQuery.AddPaging(permissionsCount, 0))
                    .ToList();

                foreach (var permission in permissions)
                {
                    _db.Delete<WorldPropertyPermission>(permission.Id);
                    Log.Write(LogGroup.Property, $"Deleted property permission for category '{permission.PropertyId}'.");
                }
            }

            // Clear the actual categories (and any associated items)
            foreach (var category in categories)
            {
                _db.Delete<WorldPropertyCategory>(category.Id);
                Log.Write(LogGroup.Property, $"Deleted property category '{category.Name}', id: '{category.Id}' from property '{category.ParentPropertyId}'");
            }

            // Clear any citizenship assignments on players who may be citizens of this property.
            var citizenQuery = new DBQuery<Player>()
                .AddFieldSearch(nameof(Player.CitizenPropertyId), property.Id, false);
            var citizenCount = (int)_db.SearchCount(citizenQuery);
            var citizens = _db.Search(citizenQuery.AddPaging(citizenCount, 0));

            foreach (var citizen in citizens)
            {
                Log.Write(LogGroup.Property, $"Citizenship revoked for player '{citizen.Name}' ({citizen.Id}) on property '{property.CustomName}' ({property.Id})");
                citizen.CitizenPropertyId = string.Empty;
                citizen.PropertyOwedTaxes = 0;
                _db.Set(citizen);
            }

            _loader.ForgetProperty(property.Id);

            // Finally delete the entire property.
            _db.Delete<WorldProperty>(property.Id);
            Log.Write(LogGroup.Property, $"Property '{property.CustomName}' deleted.");

            EventsPlugin.PushEventData("PROPERTY_ID", property.Id);
            EventsPlugin.SignalEvent("SWLOR_DELETE_PROPERTY", GetModule());
        }

        /// <summary>
        /// Removes a starship from the docked ship lists of the player starports it is registered to.
        /// </summary>
        /// <param name="starship">The starship being deleted.</param>
        private void UnregisterStarshipFromStarports(WorldProperty starship)
        {
            if (!starship.ChildPropertyIds.TryGetValue(PropertyChildType.RegisteredStarport, out var starportIds))
                return;

            foreach (var starportId in starportIds)
            {
                var starport = _db.Get<WorldProperty>(starportId);
                if (starport == null ||
                    !starport.ChildPropertyIds.TryGetValue(PropertyChildType.Starship, out var starshipIds) ||
                    !starshipIds.Remove(starship.Id))
                    continue;

                _db.Set(starport);
                Log.Write(LogGroup.Property, $"Unregistered deleted starship '{starship.CustomName}' ({starship.Id}) from starport '{starport.CustomName}' ({starport.Id}).");
            }
        }

        /// <summary>
        /// When the module loads, update the permissions list on all properties to reflect any changes.
        /// </summary>
        private void RefreshPermissions()
        {
            foreach (var (type, permissions) in _catalog.PermissionsByPropertyType)
            {
                var propertyQuery = new DBQuery<WorldProperty>()
                    .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)type);
                var propertyCount = (int)_db.SearchCount(propertyQuery);
                var dbProperties = _db.Search(propertyQuery.AddPaging(propertyCount, 0))
                    .ToList();

                foreach (var property in dbProperties)
                {
                    var permissionQuery = new DBQuery<WorldPropertyPermission>()
                        .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), property.Id, false);
                    var permissionCount = (int)_db.SearchCount(permissionQuery);
                    var dbPropertyPermissions = _db.Search(permissionQuery.AddPaging(permissionCount, 0))
                        .ToList();

                    foreach (var propertyPermission in dbPropertyPermissions)
                    {
                        // Perform a refresh of permissions (adding/removing as needed)
                        // If changes occurred, save them.
                        if (RefreshPermissions(property.OwnerPlayerId, propertyPermission, permissions))
                        {
                            _db.Set(propertyPermission);
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
        private bool RefreshPermissions(string propertyOwnerId, WorldPropertyPermission dbPermission, List<PropertyPermissionType> masterList)
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
                    dbPermission.GrantPermissions.Remove(grantPermission);
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
        /// Creates a list of default item categories for use in freshly made properties.
        /// </summary>
        /// <returns>A list of categories</returns>
        private List<WorldPropertyCategory> CreateDefaultCategories(string parentPropertyId)
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

        private WorldProperty CreateProperty(
            uint creatorPlayer,
            string ownerPlayerId,
            string propertyName,
            PropertyType type,
            PropertyLayoutType layout,
            uint targetArea = OBJECT_INVALID,
            Action<WorldProperty> constructionAction = null)
        {
            var creatorPlayerId = GetObjectUUID(creatorPlayer);
            var layoutDetail = GetLayoutByType(layout);
            var propertyDetail = _catalog.GetPropertyDetail(type);

            var property = new WorldProperty
            {
                CustomName = propertyName,
                IsPubliclyAccessible = propertyDetail.PublicSetting == PropertyPublicType.AlwaysPublic,
                PropertyType = type,
                OwnerPlayerId = ownerPlayerId,
                Layout = layout,
                ItemStorageCount = layoutDetail.ItemStorageLimit
            };

            constructionAction?.Invoke(property);

            var ownerPermissions = new WorldPropertyPermission
            {
                PropertyId = property.Id,
                PlayerId = ownerPlayerId
            };

            var creatorPermissions = creatorPlayerId == ownerPlayerId
                ? null
                : new WorldPropertyPermission
                {
                    PropertyId = property.Id,
                    PlayerId = creatorPlayerId
                };

            foreach (var permission in _catalog.GetPermissionsByPropertyType(type))
            {
                ownerPermissions.Permissions[permission] = true;
                ownerPermissions.GrantPermissions[permission] = true;

                if (creatorPermissions != null)
                {
                    creatorPermissions.Permissions[permission] = true;
                    creatorPermissions.GrantPermissions[permission] = true;
                }
            }

            _db.Set(property);
            _db.Set(ownerPermissions);
            if(creatorPermissions != null)
                _db.Set(creatorPermissions);

            if (propertyDetail.HasStorage)
            {
                // Create the default item storage categories and give permission to the owner for all categories.
                foreach (var category in CreateDefaultCategories(property.Id))
                {
                    _db.Set(category);

                    var categoryPermission = new WorldPropertyPermission
                    {
                        PropertyId = category.Id,
                        PlayerId = ownerPlayerId
                    };

                    foreach (var permission in _catalog.GetPermissionsByPropertyType(PropertyType.Category))
                    {
                        categoryPermission.Permissions[permission] = true;
                        categoryPermission.GrantPermissions[permission] = true;
                    }

                    _db.Set(categoryPermission);
                }
            }

            if (_loader.IsPropertyOnDemand(property))
            {
                _loader.SetPropertyLoadState(property.Id, PropertyLoadState.Unloaded);
            }
            else
            {
                _loader.SpawnIntoWorld(property, targetArea);
            }

            Log.Write(LogGroup.Property, $"{GetName(creatorPlayer)} ({GetPCPlayerName(creatorPlayer)} / {GetPCPublicCDKey(creatorPlayer)}) placed {propertyDetail.Name}.");

            return property;
        }

        /// <summary>
        /// Creates a new apartment in the database for a given player.
        /// </summary>
        /// <param name="player">The player to associate the apartment with.</param>
        /// <param name="layout">The layout to use.</param>
        /// <returns>The new world property.</returns>
        public WorldProperty CreateApartment(uint player, PropertyLayoutType layout)
        {
            var playerId = GetObjectUUID(player);
            var propertyName = "Apartment";
            return CreateProperty(player, playerId, propertyName, PropertyType.Apartment, layout, OBJECT_INVALID, property =>
            {
                property.Dates[PropertyDateType.Lease] = DateTime.UtcNow.AddDays(7);
            });
        }

        /// <summary>
        /// Creates a new starship in the database for a given player and returns the world property.
        /// </summary>
        /// <param name="player">The player to associate the starship with.</param>
        /// <param name="layout">The layout to use.</param>
        /// <param name="planetType">The planet where this starship is being created.</param>
        /// <param name="spaceLocation">Location of the space transfer point (when a player is converted to a ship)</param>
        /// <param name="landingLocation">Location of the ground transfer point (when a player is converted back to normal)</param>
        /// <returns>The new world property.</returns>
        public WorldProperty CreateStarship(
            uint player,
            PropertyLayoutType layout,
            PlanetType planetType,
            Location spaceLocation,
            Location landingLocation)
        {
            var spacePosition = GetPositionFromLocation(spaceLocation);
            var spaceOrientation = GetFacingFromLocation(spaceLocation);
            var spaceArea = GetAreaFromLocation(spaceLocation);
            var spaceAreaResref = GetResRef(spaceArea);

            var landingPosition = GetPositionFromLocation(landingLocation);
            var landingOrientation = GetFacingFromLocation(landingLocation);
            var landingArea = GetAreaFromLocation(landingLocation);
            var landingAreaResref = GetResRef(landingArea);
            var landingPropertyId = GetPropertyId(landingArea);

            // In the event the starport a ship is located at is destroyed or otherwise disappears,
            // we need to know the location of the planet's NPC starport so the ship can be returned there.
            // If we don't capture this correctly, the ship will be lost in limbo and the players won't be
            // able to access it.
            var planet = _planet.GetPlanetByType(planetType);
            var npcLandingWaypoint = GetWaypointByTag(planet.LandingWaypointTag);
            var npcLandingPosition = GetPosition(npcLandingWaypoint);
            var npcLandingOrientation = GetFacing(npcLandingWaypoint);
            var npcLandingArea = GetArea(npcLandingWaypoint);
            var npcLandingResref = GetResRef(npcLandingArea);
            var npcOrbitWaypoint = GetWaypointByTag(planet.SpaceOrbitWaypointTag);
            var npcOrbitPosition = GetPosition(npcOrbitWaypoint);
            var npcOrbitOrientation = GetFacing(npcOrbitWaypoint);
            var npcOrbitResref = GetResRef(GetArea(npcOrbitWaypoint));

            var playerId = GetObjectUUID(player);
            var propertyName = "Starship";

            return CreateProperty(player, playerId, propertyName, PropertyType.Starship, layout, OBJECT_INVALID, property =>
            {
                property.Positions[PropertyLocationType.LastNPCDockPosition] = new PropertyLocation
                {
                    X = npcLandingPosition.X,
                    Y = npcLandingPosition.Y,
                    Z = npcLandingPosition.Z,
                    Orientation = npcLandingOrientation,
                    AreaResref = npcLandingResref
                };

                property.Positions[PropertyLocationType.LastNPCSpacePosition] = new PropertyLocation
                {
                    X = npcOrbitPosition.X,
                    Y = npcOrbitPosition.Y,
                    Z = npcOrbitPosition.Z,
                    Orientation = npcOrbitOrientation,
                    AreaResref = npcOrbitResref
                };

                property.Positions[PropertyLocationType.DockPosition] = new PropertyLocation
                {
                    X = landingPosition.X,
                    Y = landingPosition.Y,
                    Z = landingPosition.Z,
                    Orientation = landingOrientation,
                    AreaResref = string.IsNullOrWhiteSpace(landingPropertyId) ? landingAreaResref : string.Empty,
                    InstancePropertyId = !string.IsNullOrWhiteSpace(landingPropertyId) ? landingPropertyId : string.Empty
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

        /// <summary>
        /// Creates a new city in the specified area and assigns the specified player to become the owner.
        /// A city hall structure and interior will also be spawned at the specified location.
        /// The specified item will be destroyed.
        /// </summary>
        /// <param name="player">The player who will become mayor.</param>
        /// <param name="area">The area to claim.</param>
        /// <param name="item">The item used to place the city hall</param>
        /// <param name="location">The location to spawn city hall.</param>
        public void CreateCity(uint player, uint area, uint item, Location location)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            var propertyName = "Player City";
            var now = DateTime.UtcNow;
            var city = CreateProperty(player, playerId, propertyName, PropertyType.City, PropertyLayoutType.City, area, property =>
            {
                property.ParentPropertyId = GetResRef(area);
                AssignPropertyId(area, property.Id);

                // Maintenance fees will be collected one week from now.
                property.Dates[PropertyDateType.Upkeep] = now.AddDays(7);

                // The first voting cycle will begin three weeks from now.
                property.Dates[PropertyDateType.ElectionStart] = now.AddDays(21);

                // The property will be cleaned up on the first server reset after 18 hours have passed
                // if there are less than the required number of citizens registered.
                property.Dates[PropertyDateType.BelowRequiredCitizens] = now.AddHours(18);

                // Initialize taxes at zero.
                property.Taxes[PropertyTaxType.Citizenship] = 0;
                property.Taxes[PropertyTaxType.Transportation] = 0;

                // Upgrades
                property.Upgrades[PropertyUpgradeType.CityLevel] = 1;
                property.Upgrades[PropertyUpgradeType.MedicalCenterLevel] = 1;
                property.Upgrades[PropertyUpgradeType.StarportLevel] = 1;
                property.Upgrades[PropertyUpgradeType.CantinaLevel] = 1;
            });

            CreateBuilding(
                player,
                item,
                city.Id,
                PropertyType.CityHall,
                PropertyLayoutType.CityHallStyle1,
                StructureType.CityHall,
                location);

            dbPlayer.CitizenPropertyId = city.Id;
            // A new citizenship must not inherit taxes from a previously deleted city.
            dbPlayer.PropertyOwedTaxes = 0;
            _db.Set(dbPlayer);

            Log.Write(LogGroup.Property, $"{GetName(player)} ({GetPCPlayerName(player)} / {GetPCPublicCDKey(player)}) founded a new city in {GetName(area)}.");
            _city.BroadcastPropertyEvent(
                city,
                "New Player City Founded",
                $"A new player city has been founded.\n" +
                $"**First Upkeep Check**: {_city.FormatUtcDate(city.Dates[PropertyDateType.Upkeep])}\n" +
                $"**First Election Opens**: {_city.FormatUtcDate(city.Dates[PropertyDateType.ElectionStart])}\n" +
                $"**Citizen Requirement Grace Ends**: {_city.FormatUtcDate(city.Dates[PropertyDateType.BelowRequiredCitizens])}");
        }

        /// <summary>
        /// Creates a new structure and interior property associated with the building.
        /// </summary>
        /// <param name="player">The player to associate the building with.</param>
        /// <param name="parentCityId">The parent city Id.</param>
        /// <param name="propertyType">The type of property to create</param>
        /// <param name="layout">The layout to use.</param>
        /// <param name="item">The item used to create the building.</param>
        /// <param name="structureType">The type of structure to create.</param>
        /// <param name="location">The location to spawn the structure.</param>
        /// <returns>The new world property.</returns>
        public void CreateBuilding(
            uint player,
            uint item,
            string parentCityId,
            PropertyType propertyType,
            PropertyLayoutType layout,
            StructureType structureType,
            Location location)
        {
            var layoutDetail = GetLayoutByType(layout);
            var propertyName = layoutDetail.Name;
            var city = _db.Get<WorldProperty>(parentCityId);

            // Hierarchy goes:
            //      City  (Top Level)
            //          -> Contains: Structure (buildings)
            //              -> Contains: Building interiors
            var buildingStructure = CreateStructure(parentCityId, item, structureType, location);

            var interior = CreateProperty(
                player,
                city.OwnerPlayerId,
                propertyName,
                propertyType,
                layout,
                OBJECT_INVALID,
                interiorProperty =>
            {
                interiorProperty.ParentPropertyId = buildingStructure.Id;
                interiorProperty.CustomName = buildingStructure.CustomName;
            });

            if (!buildingStructure.ChildPropertyIds.ContainsKey(PropertyChildType.Interior))
                buildingStructure.ChildPropertyIds[PropertyChildType.Interior] = new List<string>();

            buildingStructure.ChildPropertyIds[PropertyChildType.Interior].Add(interior.Id);
            _db.Set(buildingStructure);

            var buildingPlaceable = GetPlaceableByPropertyId(buildingStructure.Id);
            if (GetIsObjectValid(buildingPlaceable))
            {
                RunStructureChangedEvent(structureType, StructureChangeType.PositionChanged, buildingStructure, buildingPlaceable);
            }
        }

        /// <summary>
        /// Creates a structure inside a specific property.
        /// </summary>
        /// <param name="parentPropertyId">The parent property to associate this structure with.</param>
        /// <param name="item">The item used to spawn the structure.</param>
        /// <param name="type">The type of structure to spawn.</param>
        /// <param name="location">The location to spawn the structure at.</param>
        public WorldProperty CreateStructure(
            string parentPropertyId,
            uint item,
            StructureType type,
            Location location)
        {
            var structureDetail = GetStructureByType(type);
            var area = GetAreaFromLocation(location);
            var areaResref = GetResRef(area);
            var position = GetPositionFromLocation(location);
            var parentProperty = _db.Get<WorldProperty>(parentPropertyId);
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
                OwnerPlayerId = parentProperty.OwnerPlayerId,
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

            if (!parentProperty.ChildPropertyIds.ContainsKey(PropertyChildType.Structure))
                parentProperty.ChildPropertyIds[PropertyChildType.Structure] = new List<string>();

            parentProperty.ChildPropertyIds[PropertyChildType.Structure].Add(structure.Id);
            parentProperty.ItemStorageCount += structureItemStorage;

            _db.Set(structure);
            _db.Set(parentProperty);

            // Now spawn it within the game world.
            var placeable = CreateObject(ObjectType.Placeable, structureDetail.Resref, location);
            SetPlotFlag(placeable, true);
            AssignPropertyId(placeable, structure.Id);

            _loader.RegisterStructurePlaceable(structure.Id, placeable);

            DestroyObject(item);
            RunStructureChangedEvent(type, StructureChangeType.PositionChanged, structure, placeable);

            return structure;
        }

        /// <summary>
        /// Retrieves a list of permissions associated with the item storage of a property for a given player.
        /// </summary>
        /// <param name="playerId">The player Id to search for</param>
        /// <param name="propertyId">The property Id to search for</param>
        /// <returns>A list of permissions</returns>
        public List<WorldPropertyPermission> GetCategoryPermissions(string playerId, string propertyId)
        {
            var categoriesQuery = new DBQuery<WorldPropertyCategory>()
                .AddFieldSearch(nameof(WorldPropertyCategory.ParentPropertyId), propertyId, false);
            var categories = _db.Search(categoriesQuery).ToList();
            var categoryIds = categories.Select(s => s.Id).ToList();

            var permissionQuery = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), categoryIds)
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false);
            var permissions = _db.Search(permissionQuery);

            return permissions.ToList();
        }

        /// <inheritdoc />
        public PropertyLayout GetLayoutByType(PropertyLayoutType type)
        {
            return _catalog.GetLayoutByType(type);
        }

        /// <inheritdoc />
        public StructureAttribute GetStructureByType(StructureType structure)
        {
            return _catalog.GetStructureByType(structure);
        }

        /// <inheritdoc />
        public List<PropertyLayoutType> GetAllLayoutsByPropertyType(PropertyType type)
        {
            return _catalog.GetAllLayoutsByPropertyType(type);
        }

        /// <inheritdoc />
        public Vector4 GetEntrancePosition(PropertyLayoutType type)
        {
            return _catalog.GetEntrancePosition(type);
        }

        /// <inheritdoc />
        public uint GetInstanceTemplate(PropertyLayoutType type)
        {
            return _catalog.GetInstanceTemplate(type);
        }

        /// <inheritdoc />
        public PropertyPermissionAttribute GetPermissionByType(PropertyPermissionType permission)
        {
            return _catalog.GetPermissionByType(permission);
        }

        /// <inheritdoc />
        public List<PropertyPermissionType> GetPermissionsByPropertyType(PropertyType type)
        {
            return _catalog.GetPermissionsByPropertyType(type);
        }

        /// <inheritdoc />
        public PropertyTypeAttribute GetPropertyDetail(PropertyType type)
        {
            return _catalog.GetPropertyDetail(type);
        }

        /// <inheritdoc />
        public uint GetPlaceableByPropertyId(string propertyId)
        {
            return _loader.GetPlaceableByPropertyId(propertyId);
        }

        /// <inheritdoc />
        public List<StructureType> GetStructuresByInteriorPropertyType(PropertyType propertyType)
        {
            return _catalog.GetStructuresByInteriorPropertyType(propertyType);
        }

        /// <summary>
        /// When an apartment terminal is used, open the Apartment NUI
        /// </summary>
        public void StartApartmentConversation()
        {
            var player = GetLastUsedBy();
            var terminal = OBJECT_SELF;

            TogglePlayerWindow(player, GuiWindowType.ManageApartment, null, terminal);
        }

        /// <summary>
        /// Determines whether a player has a specific permission.
        /// This will always return true for DMs.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <param name="propertyId">The property Id to check.</param>
        /// <param name="permission">The type of permission to check.</param>
        /// <returns>true if player has permission, false otherwise</returns>
        public bool HasPropertyPermission(uint player, string propertyId, PropertyPermissionType permission)
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
            var permissions = _db.Search(query).FirstOrDefault();

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
        public bool CanGrantPermission(uint player, string propertyId, PropertyPermissionType permission)
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
            var permissions = _db.Search(query).FirstOrDefault();

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
        public void PreviewProperty(uint player, PropertyLayoutType layout)
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

        /// <inheritdoc />
        public void EnterPropertyInstance()
        {
            _loader.EnterPropertyInstance();
        }

        /// <inheritdoc />
        public void ExitPropertyInstance()
        {
            _loader.ExitPropertyInstance();
        }

        /// <summary>
        /// Sends a player to a specific property's instance.
        /// </summary>
        /// <param name="player">The player to send.</param>
        /// <param name="propertyId">The property Id</param>
        public void EnterProperty(uint player, string propertyId)
        {
            if (!HasPropertyPermission(player, propertyId, PropertyPermissionType.EnterProperty))
            {
                FloatingTextStringOnCreature("You do not have permission to access that property.", player, false);
                return;
            }

            var property = _db.Get<WorldProperty>(propertyId);
            if (property == null)
            {
                _loader.SendPropertyLoadFailedMessage(player);
                return;
            }

            var entrance = _catalog.GetEntrancePosition(property.Layout);
            if (!TryResolveEnterableInstance(player, property.Id, out var instance))
                return;

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
        public void StoreOriginalLocation(uint player)
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
        public void JumpToOriginalLocation(uint player)
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
        public void PropertyMenu()
        {
            var feat = (FeatType)Convert.ToInt32(EventsPlugin.GetEventData("FEAT_ID"));

            if (feat != FeatType.PropertyMenu) return;

            var player = OBJECT_SELF;

            if (Gui.IsWindowOpen(player, GuiWindowType.ManageStructures))
            {
                TogglePlayerWindow(player, GuiWindowType.ManageStructures);
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
            var permission = _db.Search(permissionQuery).FirstOrDefault();

            var categoryQuery = new DBQuery<WorldPropertyCategory>()
                .AddFieldSearch(nameof(WorldPropertyCategory.ParentPropertyId), propertyId, false);
            var categoryIds = _db.Search(categoryQuery).Select(s => s.Id).ToList();
            long categoryPermissionCount = 0;

            if (categoryIds.Count > 0)
            {
                permissionQuery = new DBQuery<WorldPropertyPermission>()
                    .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                    .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), categoryIds);
                categoryPermissionCount = _db.SearchCount(permissionQuery);
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

            TogglePlayerWindow(player, GuiWindowType.ManageStructures);
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
        public void PlaceStructure()
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
                ConversationMenu.Start(player, player, nameof(PlaceCityHallDialog));
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
            var layout = GetLayoutByType(property.Layout);
            int structureLimit;
            var structureDetail = GetStructureByType(structureType);
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
                structureCount = structures.Count(x => GetStructureByType(x.StructureType).LayoutType != PropertyLayoutType.Invalid);
                structureLimit = layout.BuildingLimit;
                fixtureName = "Building";
            }
            else
            {
                if (structureDetail.Category == StructureCategoryType.Structure)
                {
                    structureCount = structures.Count(x =>
                    {
                        var structureByType = GetStructureByType(x.StructureType);
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
                        var structureByType = GetStructureByType(x.StructureType);
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
                CreateStructure(propertyId, item, structureType, location);
            }
            // Otherwise we have a layout which means an interior must be spawned.
            else
            {
                var structureLayout = GetLayoutByType(structureDetail.LayoutType);
                CreateBuilding(
                    player,
                    item,
                    propertyId,
                    structureLayout.PropertyType,
                    structureDetail.LayoutType,
                    structureType, location);
            }

            SendMessageToPC(player, $"{fixtureName} Limit: {structureCount+1} / {structureLimit}");
        }

        /// <summary>
        /// When a building entrance is used, port the player inside the instance if they have permission
        /// or display an error message saying they don't have permission to enter.
        /// </summary>
        public void EnterBuilding()
        {
            var player = GetLastUsedBy();
            var playerId = GetObjectUUID(player);
            var door = OBJECT_SELF;

            // Buildings only ever have one child which is the interior area instance
            var buildingId = GetPropertyId(door);
            var building = _db.Get<WorldProperty>(buildingId);
            if (building == null ||
                !building.ChildPropertyIds.ContainsKey(PropertyChildType.Interior))
            {
                SendMessageToPC(player, "This building is not ready yet. Please try again shortly.");
                return;
            }

            var interiorIds = building.ChildPropertyIds[PropertyChildType.Interior];
            if (interiorIds.Count != 1)
            {
                SendMessageToPC(player, "This building is not ready yet. Please try again shortly.");
                return;
            }

            var interiorId = interiorIds.Single();
            var interior = _db.Get<WorldProperty>(interiorId);
            if (interior == null)
            {
                _loader.SendPropertyLoadFailedMessage(player);
                return;
            }

            var permission = _db.Search(new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), interiorId, false))
                .SingleOrDefault();

            // Building is publicly accessible or the player has permission to enter.
            if (interior.IsPubliclyAccessible ||
                !interior.IsPubliclyAccessible && permission != null && permission.Permissions[PropertyPermissionType.EnterProperty])
            {
                if (!TryResolveEnterableInstance(player, interior.Id, out var instance))
                    return;

                var entrance = GetEntrancePosition(interior.Layout);
                var position = Vector3(entrance.X, entrance.Y, entrance.Z);
                var location = Location(instance.Area, position, entrance.W);

                StoreOriginalLocation(player);
                AssignCommand(player, () => ActionJumpToLocation(location));
            }
            else
            {
                SendMessageToPC(player, "You do not have permission to enter.");
            }
        }

        /// <inheritdoc />
        public void RunStructureChangedEvent(StructureType structureType, StructureChangeType changeType, WorldProperty property, uint placeable)
        {
            _catalog.RunStructureChangedEvent(structureType, changeType, property, placeable);
        }

        /// <summary>
        /// When the Citizenship terminal is used, open the Manage Citizenship UI.
        /// </summary>
        public void OpenCitizenshipMenu()
        {
            var player = GetLastUsedBy();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
            {
                SendMessageToPC(player, "Only players may use this terminal.");
                return;
            }

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            var terminal = OBJECT_SELF;
            var area = GetArea(terminal);
            var propertyId = GetPropertyId(area);
            var dbProperty = _db.Get<WorldProperty>(propertyId);
            var dbBuilding = _db.Get<WorldProperty>(dbProperty.ParentPropertyId);

            // Player is a citizen of another city. Their citizenship needs to be revoked before
            // they're able to access this terminal.
            if (!string.IsNullOrWhiteSpace(dbPlayer.CitizenPropertyId) &&
                dbPlayer.CitizenPropertyId != Guid.Empty.ToString() &&
                dbBuilding.ParentPropertyId != dbPlayer.CitizenPropertyId)
            {
                SendMessageToPC(player, ColorToken.Red("You are a citizen of another city. You cannot access this terminal unless you revoke your citizenship first."));
            }
            else
            {
                TogglePlayerWindow(player, GuiWindowType.ManageCitizenship, null, terminal);
            }
        }

        /// <summary>
        /// When the City Management terminal is used, open the City Management UI.
        /// </summary>
        public void OpenCityManagementMenu()
        {
            var player = GetLastUsedBy();
            var playerId = GetObjectUUID(player);
            var terminal = OBJECT_SELF;
            var area = GetArea(terminal);
            var propertyId = GetPropertyId(area);
            var dbProperty = _db.Get<WorldProperty>(propertyId);
            var dbBuilding = _db.Get<WorldProperty>(dbProperty.ParentPropertyId);

            var permission = _db.Search(new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), dbBuilding.ParentPropertyId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false))
                .SingleOrDefault();

            // Player has at least one permission. Display the window.
            if (permission != null && (permission.Permissions[PropertyPermissionType.RenameProperty] ||
                                       permission.Permissions[PropertyPermissionType.EditTaxes] ||
                                       permission.Permissions[PropertyPermissionType.AccessTreasury] ||
                                       permission.Permissions[PropertyPermissionType.ManageUpgrades] ||
                                       permission.Permissions[PropertyPermissionType.ManageUpkeep]))
            {
                TogglePlayerWindow(player, GuiWindowType.ManageCity, null, terminal);
            }
            else
            {
                SendMessageToPC(player, ColorToken.Red("You do not have permission to access this terminal."));
            }
        }

        /// <inheritdoc />
        public string GetCityLevelName(int level)
        {
            return _city.GetCityLevelName(level);
        }

        /// <inheritdoc />
        public int GetCitizensRequiredForNextCityLevel(int level)
        {
            return _city.GetCitizensRequiredForNextCityLevel(level);
        }

        /// <inheritdoc />
        public int GetEffectiveUpgradeLevel(string cityId, PropertyUpgradeType upgradeType)
        {
            return _city.GetEffectiveUpgradeLevel(cityId, upgradeType);
        }

        /// <summary>
        /// Shows or hides a window for a player. The window table of <c>Gui</c> is filled by the module load, so a unit
        /// test replaces this.
        /// </summary>
        /// <param name="player">The player to show the window to.</param>
        /// <param name="type">The type of window.</param>
        /// <param name="payload">An optional payload for the window's view model.</param>
        /// <param name="tetherObject">The object the window is tethered to.</param>
        protected virtual void TogglePlayerWindow(uint player, GuiWindowType type, GuiPayloadBase payload = null, uint tetherObject = OBJECT_INVALID)
        {
            Gui.TogglePlayerWindow(player, type, payload, tetherObject);
        }
    }
}
