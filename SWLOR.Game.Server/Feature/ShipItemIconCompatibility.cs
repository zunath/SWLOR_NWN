using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.MigrationDefinition;

namespace SWLOR.Game.Server.Feature;

/// <summary>Refreshes artwork when legacy world copies reach a player's inventory.</summary>
public static class ShipItemIconCompatibility
{
    [NWNEventHandler(ScriptName.OnModuleEnter)]
    public static void OnEnter()
    {
        var player = GetEnteringObject();
        if (GetIsPC(player)) ShipItemIconMigration.MigrateObject(player);
    }

    [NWNEventHandler(ScriptName.OnModuleAcquire)]
    public static void OnAcquire() => ShipItemIconMigration.MigrateObject(GetModuleItemAcquired());
}
