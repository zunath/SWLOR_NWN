using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.MigrationDefinition;

namespace SWLOR.Game.Server.Feature;

/// <summary>
/// Converts retired ship deeds and modules that reach a player after their one-time migration (stale world copies, stores,
/// containers): on login and on acquire, through the same rule as the stored-item sweep.
/// </summary>
public static class LegacySpaceItemCompatibility
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
    /// Replaces retired ship deeds and modules in or on the object with their new items. True when anything changed.
    /// </summary>
    public static bool Normalize(uint obj) => LegacySpaceItemMigration.MigrateObject(obj, out _);
}
