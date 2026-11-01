using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

public sealed class _30_ConsolidatePelvisAppearance : IServerMigration
{
    public int Version => 30;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;
    public void Migrate() => StoredItemSweep.MigratePelvis(Version);
}
