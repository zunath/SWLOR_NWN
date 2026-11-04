using System;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

internal sealed class EnzymeStoredItemConverter : IStoredItemConverter
{
    public static readonly EnzymeStoredItemConverter Instance = new();

    public bool MigrateStoredObjectData(string original, out string migrated, bool repairHeadgear, Action<RootListing> rootChanged) =>
        EnzymeBaseItemMigration.MigrateSavedObject(original, out migrated);
}
