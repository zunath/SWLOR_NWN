using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.Enums;
using SWLOR.Component.Migration.Model;
using SWLOR.NWN.API.NWNX;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Common.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;

namespace SWLOR.Component.Migration.Feature.ServerMigration
{
    public class _2_LegacyServerMigration: LegacyMigrationBase, IServerMigration
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();
        
        public _2_LegacyServerMigration(IDatabaseService db, IServiceProvider serviceProvider)
        {
            _db = db;
            // Services are now lazy-loaded via IServiceProvider
        }
        
        public int Version => 2;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;
        public void Migrate()
        {
            MigratePersistentStorageItems();
        }

        private void MigratePersistentStorageItems()
        {
            var query = new DBQuery<InventoryItem>();
            var itemCount = (int)_db.SearchCount(query);
            var items = _db.Search(query.AddPaging(itemCount, 0)).ToList();
            var tempStorage = GetObjectByTag("MIGRATION_STORAGE");

            foreach (var item in items)
            {
                if (item.StorageId != "BANK_CZ220" && item.StorageId != "BANK_VELES" && item.StorageId != "BANK_MONCALA")
                    continue;

                if (item.IconResref != "unknown_item")
                    continue;

                var deserialized = ObjectPlugin.Deserialize(item.Data);
                if (!GetIsObjectValid(deserialized))
                    continue;

                ObjectPlugin.AcquireItem(tempStorage, deserialized);

                WipeItemProperties(deserialized);
                ItemService.MarkLegacyItem(deserialized);
                WipeDescription(deserialized);
                WipeVariables(deserialized);
                CleanItemName(deserialized);

                item.Data = ObjectPlugin.Serialize(deserialized);
                _db.Set(item);

                DestroyObject(deserialized);
            }
        }
    }
}
