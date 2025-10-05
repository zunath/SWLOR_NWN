using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.Enums;
using SWLOR.Component.Migration.Model;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Migration.Definitions.ServerMigration
{
    public class _19_CombatUpgrade : ServerMigrationBase, IServerMigration
    {
        public _19_CombatUpgrade(
            ILogger logger, 
            IDatabaseService db, 
            IServiceProvider serviceProvider) 
            : base(logger, db, serviceProvider)
        {
        }

        public int Version => 19;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;
        public void Migrate()
        {

        }
    }
}
