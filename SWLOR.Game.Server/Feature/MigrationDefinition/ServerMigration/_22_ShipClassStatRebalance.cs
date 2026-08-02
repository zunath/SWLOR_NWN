using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _22_ShipClassStatRebalance : ServerMigrationBase, IServerMigration
    {
        public int Version => 22;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

        public void Migrate()
        {
            RecalculateAllShipStats();
        }
    }
}
