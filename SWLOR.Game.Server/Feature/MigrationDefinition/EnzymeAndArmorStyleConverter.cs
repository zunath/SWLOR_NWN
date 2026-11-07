using System;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>The release's stored-item rules: enzymes and retired armor styles, in one decode per record.</summary>
internal sealed class EnzymeAndArmorStyleConverter : IStoredItemConverter
{
    public static readonly EnzymeAndArmorStyleConverter Instance = new();

    public bool MigrateStoredObjectData(string original, out string migrated, bool repairHeadgear, Action<RootListing> rootChanged) =>
        StoredObjectData.MigrateEnzymeAndArmorStyleData(original, out migrated);
}
