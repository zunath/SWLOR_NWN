using SWLOR.Core.Entity;
using SWLOR.Core.Service;
using SWLOR.Core.Service.DBService;
using SWLOR.Core.Service.MigrationService;

namespace SWLOR.Core.Feature.MigrationDefinition.ServerMigration
{
    public class _13_StinkyWompratsQuestFix: ServerMigrationBase, IServerMigration
    {
        public int Version => 13;
        public void Migrate()
        {
            var query = new DBQuery<Player>();
            var count = (int)DB.SearchCount(query);
            var dbPlayers = DB.Search(query
                .AddPaging(count, 0));

            foreach (var dbPlayer in dbPlayers)
            {
                if (dbPlayer.Quests.ContainsKey("stinky_womprats"))
                {
                    if (dbPlayer.Quests["stinky_womprats"].CurrentState >= 2)
                    {
                        dbPlayer.Quests["stinky_womprats"].CurrentState = 1;

                        if (!dbPlayer.Quests["stinky_womprats"].ItemProgresses.ContainsKey("womprathide") ||
                            dbPlayer.Quests["stinky_womprats"].ItemProgresses["womprathide"] < 1)
                            dbPlayer.Quests["stinky_womprats"].ItemProgresses["womprathide"] = 1;
                    }
                    
                    DB.Set(dbPlayer);
                }
            }
            
        }
    }
}
