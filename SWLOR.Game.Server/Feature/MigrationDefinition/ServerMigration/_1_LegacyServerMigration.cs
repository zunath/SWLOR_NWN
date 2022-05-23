using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _1_LegacyServerMigration: LegacyMigrationBase, IServerMigration
    {
        public int Version => 1;
        public void Migrate()
        {
            MigratePersistentStorageItems();
        }

        private void MigratePersistentStorageItems()
        {
            var items = DB.Search(new DBQuery<InventoryItem>());

            foreach (var item in items)
            {
                var deserialized = ObjectPlugin.Deserialize(item.Data);

                WipeItemProperties(deserialized);
                Item.MarkLegacyItem(deserialized);
                WipeDescription(deserialized);
                WipeVariables(deserialized);
                CleanItemName(deserialized);

                item.Data = ObjectPlugin.Serialize(deserialized);
                DB.Set(item);

                DestroyObject(deserialized);
            }
        }
    }
}
