using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>
/// The next release's server migration, empty until a change needs it. Put that release's database changes here;
/// a stored-item change calls <see cref="StoredItemSweep"/> once with the release's converter, and a change that
/// needs item blueprints keeps <see cref="MigrationExecutionType.PostCacheLoad"/>. Rename the class after what it
/// does (<c>Migrations.md</c>).
/// </summary>
public sealed class _32_NextReleaseServerMigration : IServerMigration
{
    public int Version => 32;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

    public void Migrate() { }
}
