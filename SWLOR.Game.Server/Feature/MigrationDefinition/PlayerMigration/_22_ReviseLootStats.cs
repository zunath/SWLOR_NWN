namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;

/// <summary>Moves a character's equipped and carried loot onto the revised loot stats.</summary>
public sealed class _22_ReviseLootStats : PlayerMigrationBase
{
    public override int Version => 22;

    public override void Migrate(uint player) => LootStatRevisionMigration.MigrateObject(player);
}
