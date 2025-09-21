using System;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
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
