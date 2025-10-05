using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.Enums;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.Migration.Feature.ServerMigration
{
    public class _6_AddMarketItemListDates : IServerMigration
    {
        private readonly IDatabaseService _db;
        private readonly IMarketItemRepository _marketItemRepository;

        public _6_AddMarketItemListDates(IDatabaseService db, IMarketItemRepository marketItemRepository)
        {
            _db = db;
            _marketItemRepository = marketItemRepository;
        }
        
        public int Version => 6;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;
        public void Migrate()
        {
            var count = (int)_marketItemRepository.GetListedCount();
            var listings = _marketItemRepository.GetListedItems();
            var now = DateTime.UtcNow;

            foreach (var listing in listings)
            {
                listing.DateListed = now;
                _marketItemRepository.Save(listing);
            }
        }
    }
}
