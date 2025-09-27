using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.Enums;
using SWLOR.Component.Migration.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Component.Migration.Feature.ServerMigration
{
    public class _3_AddRacialStatsAndGrantRebuild: ServerMigrationBase, IServerMigration
    {
        public _3_AddRacialStatsAndGrantRebuild(ILogger logger, IDatabaseService db, IServiceProvider serviceProvider) : base(logger, db, serviceProvider)
        {
        }
        
        public int Version => 3;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;
        public void Migrate()
        {
            var query = new DBQuery<Player>();
            var playerCount = (int)DB.SearchCount(query);
            var players = DB.Search(query.AddPaging(playerCount, 0));

            foreach (var player in players)
            {
                player.RacialStat = AbilityType.Invalid;

                DB.Set(player);
            }
        }
    }
}
