using System;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PlayerMarketService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>Listing metadata for a stored item whose root changed equipment type or appearance during the sweep.</summary>
internal readonly record struct RootListing(string IconResref, MarketCategoryType Category);

/// <summary>Applies the stored-item sweep's item rules to one saved object.</summary>
internal interface IStoredItemConverter
{
    /// <param name="rootChanged">Called when the saved root changed equipment type or appearance.</param>
    bool MigrateStoredObjectData(string original, out string migrated, bool repairHeadgear, Action<RootListing> rootChanged);
}

/// <summary>
/// Loads each saved object into the game engine and migrates the live object. The sweep uses
/// <see cref="SavedDataStoredItemConverter"/>; this is the reference its engine test compares against.
/// </summary>
internal sealed class EngineStoredItemConverter : IStoredItemConverter
{
    public static readonly EngineStoredItemConverter Instance = new();

    public bool MigrateStoredObjectData(string original, out string migrated, bool repairHeadgear, Action<RootListing> rootChanged)
        => LootStatRevisionMigration.MigrateStoredObjectData(original, out migrated, repairHeadgear, rootChanged == null ? null :
            item => rootChanged(new RootListing(Item.GetIconResref(item), PlayerMarket.GetItemMarketCategory(item))));
}

/// <summary>Edits the saved GFF directly, with no game engine; safe to call from several threads.</summary>
internal sealed class SavedDataStoredItemConverter : IStoredItemConverter
{
    public static readonly SavedDataStoredItemConverter Instance = new();

    public bool MigrateStoredObjectData(string original, out string migrated, bool repairHeadgear, Action<RootListing> rootChanged)
        => StoredObjectData.MigrateStoredItemData(original, out migrated, repairHeadgear, rootChanged);
}
