using System.Collections.Generic;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _19_RefundForceLeap : ServerMigrationBase, IServerMigration
    {
        private readonly Dictionary<(PerkType, int), int> _refundMap = new()
        {
            {(PerkType.ForceLeap, 1), 3},
            {(PerkType.ForceLeap, 2), 3}, 
            {(PerkType.ForceLeap, 3), 3}  
        };
        public int Version => 19;

        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

        public void Migrate()
        {
            RefundPerksByMapping(_refundMap);
        }
    }
}