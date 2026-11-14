namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>The saved-data conversion of retired space items in a stored item or creature (UTI, BIC or UTC, base64).</summary>
public static class LegacySpaceItemConversion
{
    /// <summary>
    /// Converts every retired hull and module in the saved object and removes its configuration modules, adding what
    /// it found to <paramref name="tally"/>. A saved configuration module is left unchanged and reported on the tally.
    /// </summary>
    public static bool Migrate(string data, out string migrated, LegacySpaceItemTally tally) =>
        StoredObjectData.MigrateLegacySpaceItems(data, out migrated, tally);
}
