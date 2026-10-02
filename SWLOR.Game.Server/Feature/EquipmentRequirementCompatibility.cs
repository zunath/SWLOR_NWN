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

    public static bool Normalize(uint obj) => EquipmentRequirementMigration.MigrateObject(obj);
}
