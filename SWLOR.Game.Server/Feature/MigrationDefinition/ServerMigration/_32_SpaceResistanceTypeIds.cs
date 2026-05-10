using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _32_SpaceResistanceTypeIds : ServerMigrationBase, IServerMigration
    {
        private static readonly IReadOnlyDictionary<string, ResistanceType> ResistanceKeyMap =
            new Dictionary<string, ResistanceType>
            {
                { ((int)ResistanceType.Fire).ToString(), ResistanceType.Fire },
                { ((int)ResistanceType.Poison).ToString(), ResistanceType.Poison },
                { ((int)ResistanceType.Electrical).ToString(), ResistanceType.Electrical },
                { ((int)ResistanceType.Ice).ToString(), ResistanceType.Ice },
                { "5", ResistanceType.Mind },
                { "6", ResistanceType.Mobility },
                { "7", ResistanceType.Trauma },
                { "8", ResistanceType.Disruption },
                { ((int)ResistanceType.Mind).ToString(), ResistanceType.Mind },
                { ((int)ResistanceType.Mobility).ToString(), ResistanceType.Mobility },
                { ((int)ResistanceType.Trauma).ToString(), ResistanceType.Trauma },
                { ((int)ResistanceType.Disruption).ToString(), ResistanceType.Disruption },
            };

        public int Version => 32;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

        public void Migrate()
        {
            var playerCount = MigrateResistanceDictionaryEntities<Player>(nameof(Player.Resistances));
            var beastCount = MigrateResistanceDictionaryEntities<Beast>(nameof(Beast.ResistancePurities));
            var incubationJobCount = MigrateResistanceDictionaryEntities<IncubationJob>(nameof(IncubationJob.ResistancePurities));
            var itemCount = MigrateSerializedItems();

            Log.Write(
                LogGroup.Migration,
                $"Spaced resistance type IDs for {playerCount} players, {beastCount} beasts, {incubationJobCount} incubation jobs, and {itemCount} serialized item records.");
        }

        private static int MigrateResistanceDictionaryEntities<TEntity>(string propertyName)
            where TEntity : EntityBase
        {
            var query = new DBQuery<TEntity>();
            var count = (int)DB.SearchCount(query);
            var entities = DB.SearchRawJson(query.AddPaging(count, 0));
            var migratedCount = 0;

            foreach (var rawEntity in entities)
            {
                var jObject = JObject.Parse(rawEntity);
                if (!NormalizeResistanceDictionary(jObject, propertyName))
                    continue;

                var entity = jObject.ToObject<TEntity>();
                DB.Set(entity);
                migratedCount++;
            }

            return migratedCount;
        }

        private static bool NormalizeResistanceDictionary(JObject entity, string propertyName)
        {
            var migrated = false;
            var resistances = entity[propertyName] as JObject;

            if (resistances == null)
            {
                resistances = new JObject();
                entity[propertyName] = resistances;
                migrated = true;
            }

            foreach (var pair in ResistanceKeyMap)
            {
                migrated |= MoveResistanceValue(resistances, pair.Key, pair.Value.ToString());
            }

            foreach (var type in Resistance.GetAllResistanceTypes())
            {
                var key = type.ToString();
                if (resistances[key] != null)
                    continue;

                resistances[key] = 0;
                migrated = true;
            }

            return migrated;
        }

        private static bool MoveResistanceValue(JObject resistances, string sourceKey, string targetKey)
        {
            if (sourceKey == targetKey)
                return false;

            var sourceToken = resistances[sourceKey];
            if (sourceToken == null)
                return false;

            if (resistances[targetKey] == null)
            {
                resistances[targetKey] = sourceToken.DeepClone();
            }
            else
            {
                resistances[targetKey] = Math.Max(GetInt(sourceToken), GetInt(resistances[targetKey]));
            }

            resistances.Remove(sourceKey);
            return true;
        }

        private static int GetInt(JToken token)
        {
            return int.TryParse(token?.ToString(), out var value)
                ? value
                : 0;
        }

        private static int MigrateSerializedItems()
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

            return migratedCount;
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
            Func<T, string> getSerializedData,
            Action<T, string> setSerializedData)
            where T : EntityBase
        {
            var migratedCount = 0;

            foreach (var entity in entities)
            {
                if (!SerializedItemResistanceMigration.MigrateSerializedObject(getSerializedData(entity), out var migratedData))
                    continue;

                setSerializedData(entity, migratedData);
                DB.Set(entity);
                migratedCount++;
            }

            return migratedCount;
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
                    if (!SerializedItemResistanceMigration.MigrateSerializedObject(item.Data, out var migratedData))
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

                if (SerializedItemResistanceMigration.MigrateSerializedObject(ship.SerializedItem, out var migratedItem))
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
                if (!SerializedItemResistanceMigration.MigrateSerializedObject(module.SerializedItem, out var migratedItem))
                    continue;

                module.SerializedItem = migratedItem;
                migrated = true;
            }

            return migrated;
        }
    }
}
