using System;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

internal sealed class PelvisStoredItemConverter : IStoredItemConverter
{
    public static readonly PelvisStoredItemConverter Instance = new();

    public bool MigrateStoredObjectData(string original, out string migrated, bool repairHeadgear, Action<RootListing> rootChanged) =>
        PelvisAppearanceMigration.MigrateSavedObject(original, out migrated);
}
