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
/// The Acting Mayor part of the Player City Revamp server migration (<see cref="_32_PlayerCityRevamp"/>): starts every
/// city's Acting Mayor and step-down empty. The story request and council summons start empty on their own, since an older
/// record loads with none.
/// </summary>
public static class ActingMayorMigration
{
    /// <summary>
    /// Starts a city's Acting Mayor id empty and its end time null when the record has none, and clears an end time left
    /// without a player. A city that already has an Acting Mayor, a step-down, or a story request keeps it, so running the
    /// migration again changes nothing. Returns true if it changed.
    /// </summary>
    public static bool MigrateCity(WorldProperty city)
    {
        var changed = false;

        if (city.ActingMayorPlayerId == null)
        {
            city.ActingMayorPlayerId = string.Empty;
            changed = true;
        }

        if (city.ActingMayorEndsUtc.HasValue && string.IsNullOrWhiteSpace(city.ActingMayorPlayerId))
        {
            city.ActingMayorEndsUtc = null;
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
        var changedCities = 0;

        foreach (var city in cities)
        {
            var changed = MigrateCity(city);
            if (changed)
            {
                DB.Set(city);
                changedCities++;
            }

            progress.RecordProcessed(changed);
        }

        progress.FinishSection();
        Log.Write(LogGroup.Migration, $"Migration #{version}: Acting Mayor started empty on {changedCities} of {cities.Count} cities.", true);
    }
}
