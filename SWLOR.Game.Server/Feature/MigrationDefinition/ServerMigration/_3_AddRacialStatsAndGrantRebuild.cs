using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _3_AddRacialStatsAndGrantRebuild: ServerMigrationBase, IServerMigration
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        
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
