using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.MigrationDefinition;

namespace SWLOR.Game.Server.Feature;

/// <summary>Recalibrates legacy sabers and double weapons acquired after the player's item migration.</summary>
public static class SaberRecalibrationCompatibility
{
    [NWNEventHandler(ScriptName.OnModuleEnter)]
    public static void OnEnter()
    {
        var player = GetEnteringObject();
        if (GetIsPC(player) && !GetIsDM(player))
        {
            SerializedItemWeaponDamageTypeMigration.MigrateDoubleWeapons(player);
            LegacySaberMigration.MigratePlayer(player);
        }
    }

    [NWNEventHandler(ScriptName.OnModuleAcquire)]
    public static void OnAcquire()
    {
        var owner = GetModuleItemAcquiredBy();
        if (GetIsPC(owner) && !GetIsDM(owner))
        {
            var item = GetModuleItemAcquired();
            SerializedItemWeaponDamageTypeMigration.MigrateDoubleWeapons(item);
            LegacySaberMigration.MigrateStoredObject(item, out _);
        }
    }
}
