using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _8_EnableServerResetReminders: ServerMigrationBase, IServerMigration
    {
        public _8_EnableServerResetReminders(ILogger logger, IDatabaseService db) : base(logger, db)
        {
        }
        
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
