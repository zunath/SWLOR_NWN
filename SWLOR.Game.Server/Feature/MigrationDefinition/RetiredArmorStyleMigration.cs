using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Moves armor parts saved on style 2 to style 1. Both draw the wearer's own body part, and no part list offers 2
/// any more because its preview pictured a model the armor never shows. Style 1 is listed on every one of these
/// slots in both the general and droid catalogs, so it suits any wearer. Shoulders, belts and robes keep their
/// saved styles. The saved-data rule for characters and stored items.
/// </summary>
public static class RetiredArmorStyleMigration
{
    public const int Removed = 2;
    public const int Replacement = 1;

    public static readonly IReadOnlyList<AppearanceArmor> Parts = new[]
    {
        AppearanceArmor.Neck, AppearanceArmor.Torso, AppearanceArmor.Pelvis,
        AppearanceArmor.LeftBicep, AppearanceArmor.RightBicep, AppearanceArmor.LeftForearm, AppearanceArmor.RightForearm,
        AppearanceArmor.LeftHand, AppearanceArmor.RightHand, AppearanceArmor.LeftThigh, AppearanceArmor.RightThigh,
        AppearanceArmor.LeftShin, AppearanceArmor.RightShin, AppearanceArmor.LeftFoot, AppearanceArmor.RightFoot,
    };

    public static bool MigrateSavedObject(string original, out string migrated) =>
        StoredObjectData.MigrateRetiredArmorStyleData(original, out migrated);

    /// <summary>Moves the armor a character wears, carries or holds in bags and droids.</summary>
    internal static void MigrateCharacter(SavedCharacter character)
    {
        var visited = new HashSet<StoredObjectData.Node>();
        StoredObjectData.MigrateRetiredArmorStyleNode(character.Root, false, visited);
        // Items waiting in overflow storage hang off the character separately from its inventory lists.
        foreach (var item in character.Carried.Concat(character.Equipped))
            StoredObjectData.MigrateRetiredArmorStyleNode(item.Node, true, visited);
    }
}
