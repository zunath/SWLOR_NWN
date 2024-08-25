using System.Collections.Generic;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
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
