using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>Updates stored enzyme sizes and building inventory icons in one pass.</summary>
public sealed class _31_ShrinkEnzymes : IServerMigration
{
    public int Version => 31;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

    public void Migrate() => StoredItemSweep.MigrateEnzymes(Version);
}
