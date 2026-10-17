namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;

public sealed class _19_UpdateItemIcons : PlayerMigrationBase
{
    public override int Version => 19;

    // Keep the checkpoint; unreleased artwork changes share migration 22's item visit.
    public override void Migrate(uint player) { }
}
