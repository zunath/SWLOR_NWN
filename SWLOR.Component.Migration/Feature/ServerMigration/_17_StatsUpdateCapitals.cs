using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.Enums;
using SWLOR.Component.Migration.Model;

namespace SWLOR.Component.Migration.Feature.ServerMigration
{
    public class _17_StatsUpdateCapitals : ServerMigrationBase, IServerMigration
    {
        public int Version => 17;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;
        public void Migrate()
        {
            RecalculateAllShipStats();
        }
    }
}
