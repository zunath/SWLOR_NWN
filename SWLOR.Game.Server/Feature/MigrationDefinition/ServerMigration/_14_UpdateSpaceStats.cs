using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
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
