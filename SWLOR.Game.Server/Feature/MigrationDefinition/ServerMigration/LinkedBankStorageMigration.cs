using System.Linq;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    internal static class LinkedBankStorageMigration
    {
        public static void RemoveRetiredUpgradeData()
        {
            var validUpgradeKeys = Enum.GetNames(typeof(PropertyUpgradeType)).ToHashSet();
            var validNumericUpgradeKeys = Enum.GetValues(typeof(PropertyUpgradeType))
                .Cast<PropertyUpgradeType>()
                .Select(value => ((int)value).ToString())
                .ToHashSet();
            var query = new DBQuery<WorldProperty>();
            var count = (int)DB.SearchCount(query);
            var rawProperties = DB.SearchRawJson(query.AddPaging(count, 0)).ToList();
            var modifiedCount = 0;

            foreach (var rawProperty in rawProperties)
            {
                var jObject = JObject.Parse(rawProperty);
                var upgrades = jObject[nameof(WorldProperty.Upgrades)] as JObject;

                if (upgrades == null)
                    continue;

                var removedUpgradeKeys = upgrades.Properties()
                    .Where(property => !validUpgradeKeys.Contains(property.Name) &&
                                       !validNumericUpgradeKeys.Contains(property.Name))
                    .ToList();

                if (removedUpgradeKeys.Count <= 0)
                    continue;

                foreach (var removedUpgradeKey in removedUpgradeKeys)
                {
                    removedUpgradeKey.Remove();
                }

                var property = jObject.ToObject<WorldProperty>();
                DB.Set(property);
                modifiedCount++;
            }

            Log.Write(LogGroup.Migration, $"Removed retired bank upgrade data from {modifiedCount}/{rawProperties.Count} world properties.");
        }

        public static void MigrateInventoryItemsToGlobalBank()
        {
            var query = new DBQuery<InventoryItem>();
            var count = (int)DB.SearchCount(query);
            var items = DB.Search(query.AddPaging(count, 0)).ToList();
            var modifiedCount = 0;

            foreach (var item in items)
            {
                if (!Bank.NormalizeStorageId(item))
                    continue;

                DB.Set(item);
                modifiedCount++;
            }

            Log.Write(LogGroup.Migration, $"Moved {modifiedCount}/{items.Count} bank inventory items to global bank storage.");
        }
    }
}
