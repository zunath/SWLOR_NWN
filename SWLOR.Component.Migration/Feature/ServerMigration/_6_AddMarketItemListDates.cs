using SWLOR.Component.Market.Entity;
using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.Enums;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;

namespace SWLOR.Component.Migration.Feature.ServerMigration
{
    public class _6_AddMarketItemListDates : IServerMigration
    {
        private readonly IDatabaseService _db;

        public _6_AddMarketItemListDates(IDatabaseService db)
        {
            _db = db;
        }
        
        public int Version => 6;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;
        public void Migrate()
        {
            var query = new DBQuery<MarketItem>()
                .AddFieldSearch(nameof(MarketItem.IsListed), true);
            var count = (int)_db.SearchCount(query);
            var listings = _db.Search(query
                .AddPaging(count, 0));
            var now = DateTime.UtcNow;


            foreach (var listing in listings)
            {
                listing.DateListed = now;
                _db.Set(listing);
            }
        }
    }
}
