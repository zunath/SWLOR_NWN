namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;

/// <summary>Applies the rare-loot correction even when the first loot revision already ran.</summary>
public sealed class _23_ReviseRareLootStats : PlayerMigrationBase
{
    public override int Version => 23;

    public override void Migrate(uint player) => LootStatRevisionMigration.MigrateObject(player);
}
