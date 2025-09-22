using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.Enums;
using SWLOR.Component.Migration.Model;

namespace SWLOR.Component.Migration.Feature.ServerMigration
{
    public class _12_RefundStabling : ServerMigrationBase, IServerMigration
    {
        private readonly Dictionary<(PerkType, int), int> _refundMap = new()
        {
            {(PerkType.Stabling, 1), 1},
            {(PerkType.Stabling, 2), 2}
        };

        public int Version => 12;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;
        public void Migrate()
        {
            RefundPerksByMapping(_refundMap);
        }
    }
}
