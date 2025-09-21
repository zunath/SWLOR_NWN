using System.Linq;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWNX;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Core.Data.Entity;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _2_LegacyServerMigration: LegacyMigrationBase, IServerMigration
    {
        private readonly IDatabaseService _db;
        private readonly IItemService _itemService;
        
        public _2_LegacyServerMigration(IDatabaseService db, IItemService itemService)
        {
            _db = db;
            _itemService = itemService;
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
                _itemService.MarkLegacyItem(deserialized);
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
