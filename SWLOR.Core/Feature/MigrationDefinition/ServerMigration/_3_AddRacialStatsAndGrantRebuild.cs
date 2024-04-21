using SWLOR.Core.Entity;
using SWLOR.Core.NWScript.Enum;
using SWLOR.Core.Service;
using SWLOR.Core.Service.DBService;

namespace SWLOR.Core.Feature.MigrationDefinition.ServerMigration
{
    public class _3_AddRacialStatsAndGrantRebuild: ServerMigrationBase
    {
        public int Version => 3;
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
