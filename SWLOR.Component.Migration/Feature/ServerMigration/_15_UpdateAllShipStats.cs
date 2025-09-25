using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.Enums;
using SWLOR.Component.Migration.Model;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Space.Contracts;

namespace SWLOR.Component.Migration.Feature.ServerMigration
{
    public class _15_UpdateAllShipStats : ServerMigrationBase, IServerMigration
    {
        public _15_UpdateAllShipStats(ILogger logger, IDatabaseService db, ISpaceService spaceService) : base(logger, db, spaceService)
        {
        }

        public int Version => 15;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;
        public void Migrate()
        {
            RecalculateAllShipStats();
        }
    }
}
