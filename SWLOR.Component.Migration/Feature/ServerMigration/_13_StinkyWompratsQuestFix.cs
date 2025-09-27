using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.Enums;
using SWLOR.Component.Migration.Model;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Component.Migration.Feature.ServerMigration
{
    public class _13_StinkyWompratsQuestFix: ServerMigrationBase, IServerMigration
    {
        public _13_StinkyWompratsQuestFix(ILogger logger, IDatabaseService db, IServiceProvider serviceProvider) : base(logger, db, serviceProvider)
        {
        }
        
        public int Version => 13;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;
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
