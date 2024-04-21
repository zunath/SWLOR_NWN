using SWLOR.Core.Entity;
using SWLOR.Core.Service;
using SWLOR.Core.Service.DBService;
using SWLOR.Core.Service.MigrationService;

namespace SWLOR.Core.Feature.MigrationDefinition.ServerMigration
{
    public class _8_EnableServerResetReminders: ServerMigrationBase, IServerMigration
    {
        public int Version => 8;
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
