using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>Moves incubation enzymes in stored items to the 1x1 enzyme base item (<see cref="EnzymeBaseItemMigration"/>).</summary>
public sealed class _33_ShrinkEnzymes : IServerMigration
{
    public int Version => 33;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

    public void Migrate() => StoredItemSweep.MigrateEnzymes(Version);
}
