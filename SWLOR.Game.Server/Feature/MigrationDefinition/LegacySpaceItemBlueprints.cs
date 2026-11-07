using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// The blueprints of every item a retired space item becomes, read once on the main thread so the saved-data
/// conversions can run on worker threads.
/// </summary>
public static class LegacySpaceItemBlueprints
{
    private static IReadOnlyDictionary<string, LegacySpaceItemBlueprint> _blueprints;

    public static bool IsLoaded => _blueprints != null;

    /// <summary>Main thread only: creates each replacement item once and keeps its saved form and listing.</summary>
    public static void Load()
    {
        if (IsLoaded)
            return;
        var storage = GetObjectByTag("TEMP_ITEM_STORAGE");
        var blueprints = new Dictionary<string, LegacySpaceItemBlueprint>(StringComparer.OrdinalIgnoreCase);
        foreach (var resref in LegacySpaceItemCatalog.ReplacementResrefs)
        {
            var item = CreateItemOnObject(resref, storage);
            if (!GetIsObjectValid(item))
                throw new InvalidOperationException($"Space item blueprint '{resref}' could not be created.");
            blueprints[resref] = new LegacySpaceItemBlueprint(ObjectPlugin.Serialize(item), GetName(item),
                Item.GetIconResref(item), PlayerMarket.GetItemMarketCategory(item));
            DestroyObject(item);
        }
        _blueprints = blueprints;
    }

    /// <summary>Supplies the blueprints directly, for tests.</summary>
    public static void Load(IReadOnlyDictionary<string, LegacySpaceItemBlueprint> blueprints) =>
        _blueprints = new Dictionary<string, LegacySpaceItemBlueprint>(blueprints, StringComparer.OrdinalIgnoreCase);

    public static LegacySpaceItemBlueprint Get(string resref)
    {
        if (_blueprints == null)
            throw new InvalidOperationException("Space item blueprints are not loaded.");
        return _blueprints.TryGetValue(resref, out var blueprint)
            ? blueprint
            : throw new InvalidOperationException($"No space item blueprint is loaded for '{resref}'.");
    }
}
