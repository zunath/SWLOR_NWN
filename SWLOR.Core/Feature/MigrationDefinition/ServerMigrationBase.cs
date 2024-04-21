using SWLOR.Core.Entity;
using SWLOR.Core.LogService;
using SWLOR.Core.Service;
using SWLOR.Core.Service.CurrencyService;
using SWLOR.Core.Service.DBService;
using SWLOR.Core.Service.PerkService;

namespace SWLOR.Core.Feature.MigrationDefinition
{
    public abstract class ServerMigrationBase
    {
        protected void GrantRebuildTokenToAllPlayers()
        {
            var query = new DBQuery<Player>();
            var count = (int)DB.SearchCount(query);
            var dbPlayers = DB.Search(query
                .AddPaging(count, 0));

            foreach (var dbPlayer in dbPlayers)
            {
                if (!dbPlayer.Currencies.ContainsKey(CurrencyType.RebuildToken))
                    dbPlayer.Currencies[CurrencyType.RebuildToken] = 0;

                dbPlayer.Currencies[CurrencyType.RebuildToken]++;

                DB.Set(dbPlayer);
            }
        }

        protected void RefundPerksByMapping(Dictionary<(PerkType, int), int> refundMap)
        {
            var dbQuery = new DBQuery<Player>();
            var playerCount = (int)DB.SearchCount(dbQuery);

            var dbPlayers = DB.Search(dbQuery
                .AddPaging(playerCount, 0));

            foreach (var dbPlayer in dbPlayers)
            {
                var refundAmount = 0;

                // Calculate the refund amount first.
                foreach (var ((type, level), sp) in refundMap)
                {
                    if (dbPlayer.Perks.ContainsKey(type) && dbPlayer.Perks[type] >= level)
                    {
                        refundAmount += sp;
                    }
                }

                // Then remove the perks being refunded.
                foreach (var ((type, _), _) in refundMap)
                {
                    if (dbPlayer.Perks.ContainsKey(type))
                    {
                        dbPlayer.Perks.Remove(type);
                    }
                }

                if (refundAmount > 0)
                {
                    dbPlayer.UnallocatedSP += refundAmount;

                    Log.Write(LogGroup.Migration, $"{dbPlayer.Name} ({dbPlayer.Id}) refunded {refundAmount} SP.");

                    DB.Set(dbPlayer);
                }
            }
        }
    }
}
