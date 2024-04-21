using SWLOR.Core.Entity;
using SWLOR.Core.Service;
using SWLOR.Core.Service.DBService;
using SWLOR.Core.Service.MigrationService;

namespace SWLOR.Core.Feature.MigrationDefinition.ServerMigration
{
    public class _6_AddMarketItemListDates : IServerMigration
    {
        public int Version => 6;
        public void Migrate()
        {
            var query = new DBQuery<MarketItem>()
                .AddFieldSearch(nameof(MarketItem.IsListed), true);
            var count = (int)DB.SearchCount(query);
            var listings = DB.Search(query
                .AddPaging(count, 0));
            var now = DateTime.UtcNow;


            foreach (var listing in listings)
            {
                listing.DateListed = now;
                DB.Set(listing);
            }
        }
    }
}
