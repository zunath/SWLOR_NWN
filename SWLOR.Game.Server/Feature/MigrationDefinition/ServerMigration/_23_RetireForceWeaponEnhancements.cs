using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.QuestContractService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public sealed class _23_RetireForceWeaponEnhancements : IServerMigration
    {
        public int Version => 23;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

        public void Migrate()
        {
            MigrateRecords<InventoryItem>(item => MigrateData(item.Data, value => item.Data = value,
                () => item.Name = ForceWeaponEnhancementMigration.GetReplacementName(item.Name)));
            MigrateRecords<MarketItem>(item => MigrateData(item.Data, value => item.Data = value,
                () => item.Name = ForceWeaponEnhancementMigration.GetReplacementName(item.Name)));
            MigrateRecords<QuestContract>(contract => MigrateContractItems(contract.RewardItems));
            MigrateRecords<QuestContractDelivery>(delivery => MigrateContractItems(delivery.Items));
            MigrateRecords<WorldPropertyCategory>(category =>
            {
                var changed = false;
                if (category.Items == null) return false;
                foreach (var item in category.Items.Values)
                    changed |= MigrateData(item.Data, value => item.Data = value,
                        () => item.Name = ForceWeaponEnhancementMigration.GetReplacementName(item.Name));
                return changed;
            });
            MigrateRecords<WorldProperty>(property =>
                MigrateData(property.SerializedItem, value => property.SerializedItem = value));
            MigrateRecords<ResearchJob>(job =>
            {
                var changed = MigrateData(job.SerializedItem, value => job.SerializedItem = value);
                var replacement = ForceWeaponEnhancementMigration.GetReplacementRecipe(job.Recipe);
                if (replacement != job.Recipe)
                {
                    job.Recipe = replacement;
                    changed = true;
                }
                return changed;
            });
            MigrateRecords<PlayerOutfit>(outfit => MigrateData(outfit.Data, value => outfit.Data = value));
            MigrateRecords<DMCreature>(creature => MigrateData(creature.Data, value => creature.Data = value));
            MigrateRecords<PlayerShip>(ship =>
            {
                var changed = MigrateData(ship.SerializedItem, value => ship.SerializedItem = value);
                changed |= MigrateModules(ship.Status?.HighPowerModules);
                changed |= MigrateModules(ship.Status?.LowPowerModules);
                changed |= MigrateModules(ship.Status?.ConfigurationModules);
                return changed;
            });
            MigrateRecords<Player>(ForceWeaponEnhancementMigration.MigrateRecipeKnowledge);
        }

        /// <summary>
        /// Updates escrowed item payloads and default display names without changing delivery or reward metadata.
        /// </summary>
        private static bool MigrateContractItems(List<QuestContractItem> items)
        {
            if (items == null) return false;
            var changed = false;
            foreach (var item in items)
                changed |= MigrateData(item.Data, value => item.Data = value,
                    () => item.Name = ForceWeaponEnhancementMigration.GetReplacementName(item.Name));
            return changed;
        }

        private static bool MigrateModules(Dictionary<int, ShipStatus.ShipStatusModule> modules)
        {
            if (modules == null) return false;
            var changed = false;
            foreach (var module in modules.Values)
                changed |= MigrateData(module.SerializedItem, value => module.SerializedItem = value);
            return changed;
        }

        private static bool MigrateData(string data, Action<string> save, Action updateMetadata = null)
        {
            var migrated = ForceWeaponEnhancementMigration.MigrateSerializedObject(data);
            if (data == migrated) return false;
            save(migrated);
            updateMetadata?.Invoke();
            return true;
        }

        private static void MigrateRecords<T>(Func<T, bool> migrate) where T : EntityBase
        {
            var query = new DBQuery<T>();
            var count = checked((int)DB.SearchCount(query));
            // DB.Search defaults to 50 results; explicitly load the complete surface.
            var records = DB.Search(query.AddPaging(count, 0)).ToList();
            var changed = 0;
            for (var index = 0; index < records.Count; index++)
            {
                var record = records[index];
                try
                {
                    if (migrate(record))
                    {
                        DB.Set(record);
                        changed++;
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Force weapon enhancement migration failed for {typeof(T).Name} {record.Id}.", ex);
                }
                if ((index + 1) % 100 == 0)
                    Log.Write(LogGroup.Migration,
                        $"Force weapon enhancements: {typeof(T).Name} {index + 1}/{records.Count} scanned, {changed} changed.", true);
            }
            Log.Write(LogGroup.Migration,
                $"Force weapon enhancements: {typeof(T).Name} completed, {changed}/{records.Count} records changed.", true);
        }
    }
}
