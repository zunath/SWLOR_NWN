using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>Reuses the combined stored-item conversion for servers already checkpointed through 28.</summary>
public sealed class _29_CorrectStoredFoundryLoot : IServerMigration
{
    public int Version => 29;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

    public void Migrate()
    {
        // The server checkpoint advances only after all post-cache migrations.
        // A pending 27/28 already visits these items with the Foundry correction.
        if ((DB.Get<ServerConfiguration>("SWLOR_CONFIG")?.MigrationVersion ?? 0) >= 28)
            _27_UpdateStoredItems.Migrate(Version);
    }
}
