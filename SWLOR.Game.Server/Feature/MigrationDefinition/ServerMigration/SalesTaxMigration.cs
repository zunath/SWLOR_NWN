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
/// The Sales tax part of the Player City Revamp server migration (<see cref="_32_PlayerCityRevamp"/>): gives every city
/// the Sales tax key, at 0, so no existing city starts charging. Stores read a missing key as 0 as well, so the step
/// only keeps the saved taxes complete.
/// </summary>
public static class SalesTaxMigration
{
    /// <summary>
    /// Adds the Sales tax key at 0 to a city that has none. Returns true if it changed. A city that already has the key
    /// keeps its rate, so running the migration again changes nothing.
    /// </summary>
    public static bool MigrateCity(WorldProperty city)
    {
        if (city.Taxes == null)
            city.Taxes = new Dictionary<PropertyTaxType, int>();

        if (city.Taxes.ContainsKey(PropertyTaxType.Sales))
            return false;

        city.Taxes[PropertyTaxType.Sales] = 0;
        return true;
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

        Log.Write(LogGroup.Migration, $"Migration #{version}: Sales tax key added to {changedCities} of {cities.Count} cities.", true);
    }
}
