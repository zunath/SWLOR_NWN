using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>
/// The garden part of the Player City Revamp server migration (<see cref="_32_PlayerCityRevamp"/>): adds the Tend Gardens
/// permission to every existing House and City permission record, true for the property's owner (a house's owner, a city's
/// mayor) and false for everyone else. Permission checks index the dictionary directly, so a record without the key would
/// throw. A record that already has the key is left alone, so running the migration again changes nothing.
/// </summary>
public static class GardenPermissionMigration
{
    /// <summary>
    /// Gives a permission record the Tend Gardens permission and its grant right if it lacks them. Returns true if it changed.
    /// </summary>
    /// <param name="permission">The record, for one player on one House or City.</param>
    /// <param name="ownerPlayerId">The property's owner: the house's owner or the city's mayor.</param>
    public static bool Migrate(WorldPropertyPermission permission, string ownerPlayerId)
    {
        var holdsIt = !string.IsNullOrWhiteSpace(ownerPlayerId) && permission.PlayerId == ownerPlayerId;
        var changed = false;

        if (!permission.Permissions.ContainsKey(PropertyPermissionType.TendGardens))
        {
            permission.Permissions[PropertyPermissionType.TendGardens] = holdsIt;
            changed = true;
        }

        if (!permission.GrantPermissions.ContainsKey(PropertyPermissionType.TendGardens))
        {
            permission.GrantPermissions[PropertyPermissionType.TendGardens] = holdsIt;
            changed = true;
        }

        return changed;
    }

    public static void Run(int version)
    {
        var properties = new List<WorldProperty>();
        foreach (var type in new[] { PropertyType.House, PropertyType.City })
        {
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)type);
            var count = checked((int)DB.SearchCount(query));
            if (count > 0)
                properties.AddRange(DB.Search(query.AddPaging(count, 0)).ToList());
        }

        var progress = new ServerMigrationProgress(version, ("WorldProperty", properties.Count));
        progress.BeginSection<WorldProperty>();

        var changedRecords = 0;
        foreach (var property in properties)
        {
            var permissionQuery = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), property.Id, false);
            var permissionCount = checked((int)DB.SearchCount(permissionQuery));
            var changedHere = false;
            if (permissionCount > 0)
            {
                foreach (var permission in DB.Search(permissionQuery.AddPaging(permissionCount, 0)).ToList())
                {
                    if (!Migrate(permission, property.OwnerPlayerId))
                        continue;

                    DB.Set(permission);
                    changedRecords++;
                    changedHere = true;
                }
            }

            progress.RecordProcessed(changedHere);
        }

        progress.FinishSection();
        Log.Write(LogGroup.Migration, $"Migration #{version}: Tend Gardens added to {changedRecords} permission records on {properties.Count} houses and cities.", true);
    }
}
