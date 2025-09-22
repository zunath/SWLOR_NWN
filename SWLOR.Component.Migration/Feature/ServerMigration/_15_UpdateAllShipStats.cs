using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.Enums;
using SWLOR.Component.Migration.Model;

namespace SWLOR.Component.Migration.Feature.ServerMigration
{
    public class _15_UpdateAllShipStats : ServerMigrationBase, IServerMigration
    {

        public int Version => 15;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;
        public void Migrate()
        {
            RecalculateAllShipStats();
        }
    }
}
