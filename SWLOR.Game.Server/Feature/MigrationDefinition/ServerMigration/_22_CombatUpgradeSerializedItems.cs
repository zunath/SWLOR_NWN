using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _22_CombatUpgradeSerializedItems : ServerMigrationBase, IServerMigration
    {
        public int Version => 22;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

        public void Migrate()
        {
            MigrateInventoryItems();
            MigrateMarketItems();
            MigrateWorldPropertyCategories();
            MigrateWorldProperties();
            MigrateResearchJobs();
            MigratePlayerOutfits();
            MigratePlayerShips();
            MigrateDMCreatures();
        }

        private static List<T> SearchAll<T>()
            where T : EntityBase
        {
            var query = new DBQuery<T>();
            var count = (int)DB.SearchCount(query);
            return DB.Search(query.AddPaging(count, 0)).ToList();
        }

        private static void MigrateInventoryItems()
        {
            foreach (var item in SearchAll<InventoryItem>())
            {
                if (!EquipmentRequirementMigration.MigrateSerializedObject(item.Data, out var migratedData))
                    continue;

                item.Data = migratedData;
                DB.Set(item);
            }
        }

        private static void MigrateMarketItems()
        {
            foreach (var item in SearchAll<MarketItem>())
            {
                if (!EquipmentRequirementMigration.MigrateSerializedObject(item.Data, out var migratedData))
                    continue;

                item.Data = migratedData;
                DB.Set(item);
            }
        }

        private static void MigrateWorldPropertyCategories()
        {
            foreach (var category in SearchAll<WorldPropertyCategory>())
            {
                if (category.Items == null)
                    continue;

                var wasMigrated = false;
                foreach (var item in category.Items.Values)
                {
                    if (!EquipmentRequirementMigration.MigrateSerializedObject(item.Data, out var migratedData))
                        continue;

                    item.Data = migratedData;
                    wasMigrated = true;
                }

                if (wasMigrated)
                    DB.Set(category);
            }
        }

        private static void MigrateWorldProperties()
        {
            foreach (var property in SearchAll<WorldProperty>())
            {
                if (!EquipmentRequirementMigration.MigrateSerializedObject(property.SerializedItem, out var migratedData))
                    continue;

                property.SerializedItem = migratedData;
                DB.Set(property);
            }
        }

        private static void MigrateResearchJobs()
        {
            foreach (var job in SearchAll<ResearchJob>())
            {
                if (!EquipmentRequirementMigration.MigrateSerializedObject(job.SerializedItem, out var migratedData))
                    continue;

                job.SerializedItem = migratedData;
                DB.Set(job);
            }
        }

        private static void MigratePlayerOutfits()
        {
            foreach (var outfit in SearchAll<PlayerOutfit>())
            {
                if (!EquipmentRequirementMigration.MigrateSerializedObject(outfit.Data, out var migratedData))
                    continue;

                outfit.Data = migratedData;
                DB.Set(outfit);
            }
        }

        private static void MigratePlayerShips()
        {
            foreach (var ship in SearchAll<PlayerShip>())
            {
                var wasMigrated = false;

                if (EquipmentRequirementMigration.MigrateSerializedObject(ship.SerializedItem, out var migratedShipItem))
                {
                    ship.SerializedItem = migratedShipItem;
                    wasMigrated = true;
                }

                if (ship.Status != null)
                {
                    wasMigrated |= MigrateShipModules(ship.Status.HighPowerModules);
                    wasMigrated |= MigrateShipModules(ship.Status.LowPowerModules);
                    wasMigrated |= MigrateShipModules(ship.Status.ConfigurationModules);
                }

                if (wasMigrated)
                    DB.Set(ship);
            }
        }

        private static bool MigrateShipModules(Dictionary<int, ShipStatus.ShipStatusModule> modules)
        {
            if (modules == null)
                return false;

            var wasMigrated = false;
            foreach (var module in modules.Values)
            {
                if (!EquipmentRequirementMigration.MigrateSerializedObject(module.SerializedItem, out var migratedData))
                    continue;

                module.SerializedItem = migratedData;
                wasMigrated = true;
            }

            return wasMigrated;
        }

        private static void MigrateDMCreatures()
        {
            foreach (var creature in SearchAll<DMCreature>())
            {
                if (!EquipmentRequirementMigration.MigrateSerializedObject(creature.Data, out var migratedData))
                    continue;

                creature.Data = migratedData;
                DB.Set(creature);
            }
        }
    }
}
