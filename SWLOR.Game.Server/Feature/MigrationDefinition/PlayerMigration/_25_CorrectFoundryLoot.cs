namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;

/// <summary>Reaches characters whose earlier item migrations were already checkpointed.</summary>
public sealed class _25_CorrectFoundryLoot : PlayerMigrationBase
{
    public override int Version => 25;

    public override void Migrate(uint player)
    {
        if (GetLocalInt(player, FoundryLootStatRevisionCatalog.RevisionVariable) < FoundryLootStatRevisionCatalog.CurrentRevision)
            LootStatRevisionMigration.MigrateObject(player);
    }
}
