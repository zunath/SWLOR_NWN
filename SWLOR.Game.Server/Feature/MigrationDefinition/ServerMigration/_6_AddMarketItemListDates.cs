using System;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _6_AddMarketItemListDates : IServerMigration
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        
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
