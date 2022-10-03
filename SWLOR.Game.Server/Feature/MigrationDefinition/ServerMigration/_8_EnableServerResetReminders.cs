using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
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
