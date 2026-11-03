using System;
using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Moves incubation enzymes from their 2x2 base items to the 1x1 <see cref="BaseItem.Enzyme"/>, with the model that
/// selects each color's inventory icon (<c>iit_enzyme_NNN</c>). The saved-data rule for characters and stored items.
/// </summary>
public static class EnzymeBaseItemMigration
{
    private static readonly Dictionary<string, (BaseItem Previous, int Model)> Enzymes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["hydrolase_blue"] = (BaseItem.Electronics5, 1),
        ["hydrolase_orange"] = (BaseItem.Electronics5, 2),
        ["hydrolase_purple"] = (BaseItem.Electronics5, 3),
        ["hydrolase_red"] = (BaseItem.Electronics5, 4),
        ["hydrolase_white"] = (BaseItem.Electronics5, 5),
        ["lyase_black"] = (BaseItem.Electronics5, 6),
        ["lyase_blue"] = (BaseItem.Electronics5, 7),
        ["lyase_green"] = (BaseItem.Electronics5, 8),
        ["lyase_orange"] = (BaseItem.Electronics5, 9),
        ["lyase_red"] = (BaseItem.Electronics5, 10),
        ["isomerase_blue"] = (BaseItem.Electronics4, 11),
        ["isomerase_green"] = (BaseItem.Electronics4, 12),
        ["isomerase_orange"] = (BaseItem.Electronics4, 13),
        ["isomerase_red"] = (BaseItem.Electronics4, 14),
        ["isomerase_yellow"] = (BaseItem.Electronics4, 15),
    };

    private static readonly string[] LegacyIconPrefixes = { "iit_ess4_", "iit_ess5_" };

    public static bool MigrateSavedObject(string original, out string migrated) =>
        StoredObjectData.MigrateEnzymeData(original, out migrated);

    /// <summary>True for an enzyme still on its 2x2 base item; <paramref name="model"/> is its new appearance.</summary>
    internal static bool TryGetConversion(string resref, BaseItem baseItem, out int model)
    {
        model = 0;
        if (resref == null || !Enzymes.TryGetValue(resref, out var enzyme) || enzyme.Previous != baseItem)
            return false;
        model = enzyme.Model;
        return true;
    }

    /// <summary>Points a stored listing's icon at the new artwork when it still shows the old 2x2 icon.</summary>
    internal static bool UpdateListingIcon(string resref, string icon, Action<string> setIcon)
    {
        if (resref == null || icon == null || !Enzymes.TryGetValue(resref, out var enzyme) ||
            !Array.Exists(LegacyIconPrefixes, icon.StartsWith))
            return false;
        setIcon($"iit_enzyme_{enzyme.Model:000}");
        return true;
    }
}
