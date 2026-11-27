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
    /// Property maintenance: the boot clean-up of expired leases and properties queued for deletion, the refresh of permissions,
    /// deleting a property with everything it owns, and returning a starship to a safe dock. Split from <c>PropertyService</c>;
    /// the members moved unchanged.
    /// </summary>
    public class PropertyMaintenanceService : IPropertyMaintenanceService
    {
        private readonly IDBService _db;
        private readonly IAreaService _area;
        private readonly IPlanetService _planet;
        private readonly IPropertyCatalogService _catalog;
        private readonly IPropertyLoaderService _loader;

        public PropertyMaintenanceService(
            IDBService db,
            IAreaService area,
            IPlanetService planet,
            IPropertyCatalogService catalog,
            IPropertyLoaderService loader)
        {
            _db = db;
            _area = area;
            _planet = planet;
            _catalog = catalog;
            _loader = loader;
        }

        /// <summary>
        /// When the module loads, update the permissions list on all properties to reflect any changes.
        /// </summary>
        public void RefreshPermissions()
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
        /// When the module loads, remove all data marked for deletion and any properties with expired leases.
        /// </summary>
        public void CleanUpData()
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
    }
}
