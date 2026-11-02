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
/// The Edict part of the Player City Revamp server migration (<see cref="_32_PlayerCityRevamp"/>): gives every city
/// permission record the Manage Edicts key, true for the mayor and false for everyone else. A city's Edicts, mayor
/// record, restricted treasury and escrow start empty on their own, since an older record loads with those defaults.
/// </summary>
public static class EdictMigration
{
    /// <summary>
    /// Adds the Manage Edicts permission and grant keys to a city permission record if it lacks them. Returns true if it
    /// changed. A key that already exists is left alone, so running the migration again changes nothing.
    /// </summary>
    /// <param name="permission">The permission record of one player on one city.</param>
    /// <param name="mayorPlayerId">The player id of the city's mayor.</param>
    public static bool MigratePermission(WorldPropertyPermission permission, string mayorPlayerId)
    {
        var isMayor = !string.IsNullOrWhiteSpace(mayorPlayerId) && permission.PlayerId == mayorPlayerId;
        var changed = false;

        if (!permission.Permissions.ContainsKey(PropertyPermissionType.ManageEdicts))
        {
            permission.Permissions[PropertyPermissionType.ManageEdicts] = isMayor;
            changed = true;
        }

        if (!permission.GrantPermissions.ContainsKey(PropertyPermissionType.ManageEdicts))
        {
            permission.GrantPermissions[PropertyPermissionType.ManageEdicts] = isMayor;
            changed = true;
        }

        return changed;
    }

    public static void Run(int version)
    {
        var cityQuery = new DBQuery<WorldProperty>()
            .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.City);
        var cityCount = checked((int)DB.SearchCount(cityQuery));
        var progress = new ServerMigrationProgress(version, ("WorldProperty", cityCount));
        progress.BeginSection<WorldProperty>();

        var cities = cityCount > 0
            ? DB.Search(cityQuery.AddPaging(cityCount, 0)).ToList()
            : new List<WorldProperty>();
        var changedPermissions = 0;

        foreach (var city in cities)
        {
            var permissionQuery = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), city.Id, false);
            var permissionCount = checked((int)DB.SearchCount(permissionQuery));
            var changedForCity = false;

            if (permissionCount > 0)
            {
                foreach (var permission in DB.Search(permissionQuery.AddPaging(permissionCount, 0)).ToList())
                {
                    if (!MigratePermission(permission, city.OwnerPlayerId))
                        continue;

                    DB.Set(permission);
                    changedPermissions++;
                    changedForCity = true;
                }
            }

            progress.RecordProcessed(changedForCity);
        }

        progress.FinishSection();
        Log.Write(LogGroup.Migration, $"Migration #{version}: Manage Edicts added to {changedPermissions} city permission records across {cities.Count} cities.", true);
    }
}
