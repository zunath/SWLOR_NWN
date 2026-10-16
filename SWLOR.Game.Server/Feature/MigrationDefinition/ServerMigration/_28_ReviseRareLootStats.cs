using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>Corrects rare loot in storage using the existing complete item traversal.</summary>
public sealed class _28_ReviseRareLootStats : IServerMigration
{
    public int Version => 28;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

    public void Migrate() => _27_UpdateStoredItems.MigrateStoredItems(Version);
}
