using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _14_UpdateSpaceStats: ServerMigrationBase, IServerMigration
    {
        public int Version => 14;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;
        public void Migrate()
        {
            RecalculateAllShipStats();
        }
    }
}
