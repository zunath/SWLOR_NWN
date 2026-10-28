using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.MigrationDefinition;

namespace SWLOR.Game.Server.Feature;

/// <summary>Repairs equipment acquired from stale world copies after the player's one-time migration.</summary>
public static class EquipmentRequirementCompatibility
{
    [NWNEventHandler(ScriptName.OnModuleEnter)]
    public static void OnEnter()
    {
        var player = GetEnteringObject();
        if (GetIsPC(player) && !GetIsDM(player))
            Normalize(player);
    }

    [NWNEventHandler(ScriptName.OnModuleAcquire)]
    public static void OnAcquire()
    {
        var owner = GetModuleItemAcquiredBy();
        if (GetIsPC(owner) && !GetIsDM(owner))
            Normalize(GetModuleItemAcquired());
    }

    /// <summary>
    /// Replaces retired ship deeds and modules with their new items first (the retired Piloting perk stamps they carry
    /// never become skill requirements), then moves legacy requirements on everything else to skill ranks.
    /// </summary>
    public static bool Normalize(uint obj)
    {
        var converted = LegacySpaceItemMigration.MigrateObject(obj, out var replacedRoot);
        if (replacedRoot)
            return converted;
        return EquipmentRequirementMigration.MigrateObject(obj) | converted;
    }
}
