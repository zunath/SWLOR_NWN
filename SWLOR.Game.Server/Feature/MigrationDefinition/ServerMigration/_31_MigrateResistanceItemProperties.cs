using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _31_MigrateResistanceItemProperties : ServerMigrationBase, IServerMigration
    {
        public int Version => 31;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

        public void Migrate()
        {
            var migratedCount = 0;

            migratedCount += MigrateEntityItems(SearchAll<InventoryItem>(), item => item.Data, (item, data) => item.Data = data);
            migratedCount += MigrateEntityItems(SearchAll<MarketItem>(), item => item.Data, (item, data) => item.Data = data);
            migratedCount += MigrateWorldPropertyCategories();
            migratedCount += MigrateEntityItems(SearchAll<WorldProperty>(), item => item.SerializedItem, (item, data) => item.SerializedItem = data);
            migratedCount += MigrateEntityItems(SearchAll<ResearchJob>(), item => item.SerializedItem, (item, data) => item.SerializedItem = data);
            migratedCount += MigrateEntityItems(SearchAll<PlayerOutfit>(), item => item.Data, (item, data) => item.Data = data);
            migratedCount += MigrateEntityItems(SearchAll<DMCreature>(), item => item.Data, (item, data) => item.Data = data);
            migratedCount += MigratePlayerShips();

            Log.Write(LogGroup.Migration, $"Migrated resistance and weapon damage item properties on {migratedCount} serialized records.");
        }

        private static List<T> SearchAll<T>()
            where T : EntityBase
        {
            var query = new DBQuery<T>();
            var count = (int)DB.SearchCount(query);
            return DB.Search(query.AddPaging(count, 0)).ToList();
        }

        private static int MigrateEntityItems<T>(
            IEnumerable<T> entities,
            System.Func<T, string> getSerializedData,
            System.Action<T, string> setSerializedData)
            where T : EntityBase
        {
            var migratedCount = 0;

            foreach (var entity in entities)
            {
                if (!MigrateSerializedObject(getSerializedData(entity), out var migratedData))
                    continue;

                setSerializedData(entity, migratedData);
                DB.Set(entity);
                migratedCount++;
            }

            return migratedCount;
        }

        private static bool MigrateSerializedObject(string serializedObject, out string migratedSerializedObject)
        {
            var migrated = false;
            migratedSerializedObject = serializedObject;

            if (SerializedItemResistanceMigration.MigrateSerializedObject(migratedSerializedObject, out var resistanceData))
            {
                migratedSerializedObject = resistanceData;
                migrated = true;
            }

            if (SerializedItemWeaponDamageTypeMigration.MigrateSerializedObject(migratedSerializedObject, out var weaponDamageData))
            {
                migratedSerializedObject = weaponDamageData;
                migrated = true;
            }

            return migrated;
        }

        private static int MigrateWorldPropertyCategories()
        {
            var migratedCount = 0;

            foreach (var category in SearchAll<WorldPropertyCategory>())
            {
                if (category.Items == null)
                    continue;

                var migrated = false;
                foreach (var item in category.Items.Values)
                {
                    if (!MigrateSerializedObject(item.Data, out var migratedData))
                        continue;

                    item.Data = migratedData;
                    migrated = true;
                }

                if (!migrated)
                    continue;

                DB.Set(category);
                migratedCount++;
            }

            return migratedCount;
        }

        private static int MigratePlayerShips()
        {
            var migratedCount = 0;

            foreach (var ship in SearchAll<PlayerShip>())
            {
                var migrated = false;

                if (MigrateSerializedObject(ship.SerializedItem, out var migratedItem))
                {
                    ship.SerializedItem = migratedItem;
                    migrated = true;
                }

                migrated |= MigrateShipStatusModules(ship.Status);

                if (!migrated)
                    continue;

                DB.Set(ship);
                migratedCount++;
            }

            return migratedCount;
        }

        private static bool MigrateShipStatusModules(ShipStatus status)
        {
            if (status == null)
                return false;

            var migrated = false;
            migrated |= MigrateShipStatusModuleDictionary(status.HighPowerModules);
            migrated |= MigrateShipStatusModuleDictionary(status.LowPowerModules);
            migrated |= MigrateShipStatusModuleDictionary(status.ConfigurationModules);

            return migrated;
        }

        private static bool MigrateShipStatusModuleDictionary(Dictionary<int, ShipStatus.ShipStatusModule> modules)
        {
            if (modules == null)
                return false;

            var migrated = false;
            foreach (var module in modules.Values)
            {
                if (!MigrateSerializedObject(module.SerializedItem, out var migratedItem))
                    continue;

                module.SerializedItem = migratedItem;
                migrated = true;
            }

            return migrated;
        }
    }
}
