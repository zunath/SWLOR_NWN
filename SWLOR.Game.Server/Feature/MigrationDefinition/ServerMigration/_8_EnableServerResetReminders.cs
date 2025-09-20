using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _8_EnableServerResetReminders: ServerMigrationBase, IServerMigration
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        
        public int Version => 8;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;
        public void Migrate()
        {
            var query = new DBQuery<Player>();
            var playerCount = (int)_db.SearchCount(query);
            var dbPlayers = _db.Search(query
                .AddPaging(playerCount, 0));

            foreach (var dbPlayer in dbPlayers)
            {
                dbPlayer.Settings.DisplayServerResetReminders = true;
                _db.Set(dbPlayer);
            }
        }
    }
}
