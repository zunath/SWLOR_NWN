using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.Enums;
using SWLOR.Component.Migration.Model;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Migration.Feature.ServerMigration
{
    public class _14_UpdateSpaceStats: ServerMigrationBase, IServerMigration
    {
        public _14_UpdateSpaceStats(ILogger logger, IDatabaseService db, IServiceProvider serviceProvider) : base(logger, db, serviceProvider)
        {
        }
        
        public int Version => 14;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;
        public void Migrate()
        {
            RecalculateAllShipStats();
        }
    }
}
