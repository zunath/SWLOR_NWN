using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _1_LegacyServerMigration: IServerMigration
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

                Item.MarkLegacyItem(deserialized);
                for (var ip = GetFirstItemProperty(deserialized); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(deserialized))
                {
                    RemoveItemProperty(deserialized, ip);
                }

                item.Data = ObjectPlugin.Serialize(deserialized);
                DB.Set(item);

                DestroyObject(deserialized);
            }
        }
    }
}
