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
    /// Who may use a property and getting into it: the permission checks, entering a property or a building, previewing a layout,
    /// and remembering where a player came from. Split from <c>PropertyService</c>; the members moved unchanged.
    /// </summary>
    public class PropertyAccessService : IPropertyAccessService
    {
        private readonly IDBService _db;
        private readonly IPropertyCatalogService _catalog;
        private readonly IPropertyLoadRegistryService _registry;
        private readonly IPropertyLoaderService _loader;

        public PropertyAccessService(
            IDBService db,
            IPropertyCatalogService catalog,
            IPropertyLoadRegistryService registry,
            IPropertyLoaderService loader)
        {
            _db = db;
            _catalog = catalog;
            _registry = registry;
            _loader = loader;
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
            var entrance = _catalog.GetEntrancePosition(layout);
            var area = _catalog.GetInstanceTemplate(layout);
            var position = new Vector3(entrance.X, entrance.Y, entrance.Z);
            var location = Location(area, position, entrance.W);

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
                _registry.SendPropertyLoadFailedMessage(player);
                return;
            }

            var entrance = _catalog.GetEntrancePosition(property.Layout);
            if (!_loader.TryResolveEnterableInstance(player, property.Id, out var instance))
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
        /// When a building entrance is used, port the player inside the instance if they have permission
        /// or display an error message saying they don't have permission to enter.
        /// </summary>
        [NWNEventHandler(ScriptName.OnEnterProperty)]
        public void EnterBuilding()
        {
            var player = GetLastUsedBy();
            var playerId = GetObjectUUID(player);
            var door = OBJECT_SELF;

            // Buildings only ever have one child which is the interior area instance
            var buildingId = _registry.GetPropertyId(door);
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
                _registry.SendPropertyLoadFailedMessage(player);
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
                if (!_loader.TryResolveEnterableInstance(player, interior.Id, out var instance))
                    return;

                var entrance = _catalog.GetEntrancePosition(interior.Layout);
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
    }
}
