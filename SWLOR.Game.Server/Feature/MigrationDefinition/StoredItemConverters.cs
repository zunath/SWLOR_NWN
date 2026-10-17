using System;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PlayerMarketService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>Listing metadata for a stored item whose root changed equipment type during the sweep.</summary>
internal readonly record struct ConvertedEquipment(string IconResref, MarketCategoryType Category);

/// <summary>Applies the stored-item sweep's item rules to one saved object.</summary>
internal interface IStoredItemConverter
{
    /// <param name="converted">Called when the saved root was converted to another equipment type.</param>
    bool MigrateStoredObjectData(string original, out string migrated, bool repairHeadgear, Action<ConvertedEquipment> converted);
}

/// <summary>Loads each saved object into the game engine and migrates the live object.</summary>
internal sealed class EngineStoredItemConverter : IStoredItemConverter
{
    public static readonly EngineStoredItemConverter Instance = new();

    public bool MigrateStoredObjectData(string original, out string migrated, bool repairHeadgear, Action<ConvertedEquipment> converted)
        => LootStatRevisionMigration.MigrateStoredObjectData(original, out migrated, repairHeadgear, converted == null ? null :
            item => converted(new ConvertedEquipment(Item.GetIconResref(item), PlayerMarket.GetItemMarketCategory(item))));
}

/// <summary>Edits the saved GFF directly, with no game engine; safe to call from several threads.</summary>
internal sealed class OfflineStoredItemConverter : IStoredItemConverter
{
    public static readonly OfflineStoredItemConverter Instance = new();

    public bool MigrateStoredObjectData(string original, out string migrated, bool repairHeadgear, Action<ConvertedEquipment> converted)
        => StoredObjectData.MigrateStoredItemOffline(original, out migrated, repairHeadgear, converted);
}
