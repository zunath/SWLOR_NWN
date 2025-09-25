using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.Enums;
using SWLOR.Component.Migration.Model;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Space.Contracts;

namespace SWLOR.Component.Migration.Feature.ServerMigration
{
    public class _8_EnableServerResetReminders: ServerMigrationBase, IServerMigration
    {
        public _8_EnableServerResetReminders(ILogger logger, IDatabaseService db, ISpaceService spaceService) : base(logger, db, spaceService)
        {
        }
        
        public int Version => 8;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;
        public void Migrate()
        {
            var query = new DBQuery<Player>();
            var playerCount = (int)DB.SearchCount(query);
            var dbPlayers = DB.Search(query
                .AddPaging(playerCount, 0));

            foreach (var dbPlayer in dbPlayers)
            {
                dbPlayer.Settings.DisplayServerResetReminders = true;
                DB.Set(dbPlayer);
            }
        }
    }
}
