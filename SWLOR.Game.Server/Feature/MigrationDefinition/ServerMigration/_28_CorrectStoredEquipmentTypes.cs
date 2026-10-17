using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>Updates stored artwork, loot stats, wearable types and visor models in one stored-item pass.</summary>
public sealed class _28_CorrectStoredEquipmentTypes : IOfflineServerMigration
{
    public int Version => 28;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;
    public void Migrate() => _27_UpdateStoredItems.Migrate(Version);
    public void MigrateOffline(OfflineMigrationOptions options) => _27_UpdateStoredItems.MigrateOffline(Version, options);
}
