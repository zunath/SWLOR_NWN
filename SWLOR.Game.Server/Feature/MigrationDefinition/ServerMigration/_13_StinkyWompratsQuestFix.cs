using SWLOR.Game.Server.Entity;

using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _13_StinkyWompratsQuestFix: ServerMigrationBase, IServerMigration
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        
        public int Version => 13;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;
        public void Migrate()
        {
            var query = new DBQuery<Player>();
            var count = (int)_db.SearchCount(query);
            var dbPlayers = _db.Search(query
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
                    
                    _db.Set(dbPlayer);
                }
            }
            
        }
    }
}
