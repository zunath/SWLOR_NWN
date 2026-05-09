using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using System;
using SWLOR.Game.Server.Service.LogService;
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
            Log.Write(LogGroup.Migration, $"Starting migration _21_SetDefaultOutfitAndMarketLimits: totalPlayers={count}");

            var modifiedCount = 0;

            try
            {
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
                        modifiedCount++;
                        DB.Set(player);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Migration, $"Migration _21_SetDefaultOutfitAndMarketLimits failed unexpectedly. Exception: {ex}", true);
                throw;
            }

            Log.Write(LogGroup.Migration, $"Completed migration _21_SetDefaultOutfitAndMarketLimits: modifiedPlayers={modifiedCount}, totalPlayers={count}");
        }
    }
}
