using System.Collections.Generic;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _12_RefundStabling : ServerMigrationBase, IServerMigration
    {
        private readonly Dictionary<(PerkType, int), int> _refundMap = new()
        {
            {(PerkType.Stabling, 1), 1},
            {(PerkType.Stabling, 2), 2}
        };

        public int Version => 12;
        public void Migrate()
        {
            RefundPerksByMapping(_refundMap);
        }
    }
}
