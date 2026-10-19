using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>Removes the Carapace Bracer's retired Poison protection from stored copies in one stored-item pass.</summary>
public sealed class _29_CorrectStoredCarapaceBracers : IServerMigration
{
    public int Version => 29;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;
    public void Migrate() => StoredItemSweep.Migrate(Version);
}
