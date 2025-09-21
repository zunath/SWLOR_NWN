using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _3_AddRacialStatsAndGrantRebuild: ServerMigrationBase, IServerMigration
    {
        public _3_AddRacialStatsAndGrantRebuild(ILogger logger, IDatabaseService db) : base(logger, db)
        {
        }
        
        public int Version => 3;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;
        public void Migrate()
        {
            var query = new DBQuery<Player>();
            var playerCount = (int)_db.SearchCount(query);
            var players = _db.Search(query.AddPaging(playerCount, 0));

            foreach (var player in players)
            {
                player.RacialStat = AbilityType.Invalid;

                _db.Set(player);
            }
        }
    }
}
