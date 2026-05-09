using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _21_SetDefaultOutfitAndMarketLimits : ServerMigrationBase, IServerMigration
    {
        public int Version => 21;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

        public void Migrate()
        {
            var query = new DBQuery<Player>();
            var count = (int)DB.SearchCount(query);
            var dbPlayers = DB.Search(query
                .AddPaging(count, 0));

            foreach (var player in dbPlayers)
            {
                var isModified = false;

                if (player.OutfitSlotLimit <= 0)
                {
                    player.OutfitSlotLimit = Entity.Player.DefaultOutfitSlotLimit;
                    isModified = true;
                }

                if (player.MarketListingLimit <= 0)
                {
                    player.MarketListingLimit = Entity.Player.DefaultMarketListingLimit;
                    isModified = true;
                }

                if (isModified)
                {
                    DB.Set(player);
                }
            }
        }
    }
}
