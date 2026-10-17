using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>Revisits stored items so already-checkpointed records receive the wearable type corrections.</summary>
public sealed class _28_CorrectStoredEquipmentTypes : IServerMigration
{
    public int Version => 28;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;
    public void Migrate() => _27_UpdateStoredItems.Migrate(Version);
}
