using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _3_AddRacialStatsAndGrantRebuild: ServerMigrationBase, IServerMigration
    {
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
