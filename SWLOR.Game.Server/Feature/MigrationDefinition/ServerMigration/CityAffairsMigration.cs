using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>
/// The City Affairs part of the Player City Revamp server migration (<see cref="_32_PlayerCityRevamp"/>): gives every
/// city permission record the Handle City Affairs key (true for the mayor, false for everyone else) and starts each
/// city's officers, modifiers, and neglect counts empty. Situations are a new record type, so none exist yet.
/// </summary>
public static class CityAffairsMigration
{
    /// <summary>
    /// Adds the Handle City Affairs key to a city permission record if it has none. Returns true if it changed. A record
    /// that already has the key is left alone, so running the migration again changes nothing.
    /// </summary>
    /// <param name="permission">The permission record of a player in a city.</param>
    /// <param name="mayorPlayerId">The city's mayor.</param>
    public static bool MigratePermission(WorldPropertyPermission permission, string mayorPlayerId)
    {
        var isMayor = permission.PlayerId == mayorPlayerId;
        var changed = false;

        if (!permission.Permissions.ContainsKey(PropertyPermissionType.HandleCityAffairs))
        {
            permission.Permissions[PropertyPermissionType.HandleCityAffairs] = isMayor;
            changed = true;
        }

        if (!permission.GrantPermissions.ContainsKey(PropertyPermissionType.HandleCityAffairs))
        {
            permission.GrantPermissions[PropertyPermissionType.HandleCityAffairs] = isMayor;
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// Starts a city's officers and modifiers empty and its neglect and recall counts at zero when the record has
    /// none. Returns true if it changed.
    /// </summary>
    public static bool MigrateCity(WorldProperty city)
    {
        var changed = false;

        if (city.Officers == null)
        {
            city.Officers = new Dictionary<string, CityOfficer>();
            changed = true;
        }

        if (city.Modifiers == null)
        {
            city.Modifiers = new Dictionary<CityModifierType, CityModifier>();
            changed = true;
        }

        if (city.NeglectCount < 0)
        {
            city.NeglectCount = 0;
            changed = true;
        }

        if (city.RecallStage < 0)
        {
            city.RecallStage = 0;
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
        var permissionsChanged = 0;

        foreach (var city in cities)
        {
            var cityChanged = MigrateCity(city);
            if (cityChanged)
                DB.Set(city);

            var permissionQuery = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), city.Id, false);
            var permissionCount = checked((int)DB.SearchCount(permissionQuery));
            if (permissionCount > 0)
            {
                foreach (var permission in DB.Search(permissionQuery.AddPaging(permissionCount, 0)).ToList())
                {
                    if (!MigratePermission(permission, city.OwnerPlayerId))
                        continue;

                    DB.Set(permission);
                    permissionsChanged++;
                }
            }

            progress.RecordProcessed(cityChanged);
        }

        progress.FinishSection();

        Log.Write(LogGroup.Migration, $"Migration #{version}: City Affairs permission added to {permissionsChanged} permission records of {cities.Count} cities.", true);
    }
}
