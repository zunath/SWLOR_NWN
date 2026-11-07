using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>Moves incubation enzymes in stored items to the 1x1 enzyme base item (<see cref="EnzymeBaseItemMigration"/>), and armor
/// parts off retired style 2 (<see cref="RetiredArmorStyleMigration"/>), in one stored-item pass.</summary>
public sealed class _31_ShrinkEnzymesAndRetireArmorStyle : IServerMigration
{
    public int Version => 31;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

    public void Migrate() => StoredItemSweep.MigrateEnzymesAndArmorStyles(Version);
}
