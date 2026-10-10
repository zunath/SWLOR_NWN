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

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

public sealed class _25_ReplaceDungeonSabers : IServerMigration
{
    public int Version => 25;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

    public void Migrate()
    {
        MigrateRecords<InventoryItem>(item => MigrateData(item.Data, value => item.Data = value, obj =>
        {
            item.Resref = GetResRef(obj);
            item.Tag = GetTag(obj);
            item.Name = GetName(obj);
            item.IconResref = Item.GetIconResref(obj);
        }));
        MigrateRecords<MarketItem>(item => MigrateData(item.Data, value => item.Data = value, obj =>
        {
            item.Resref = GetResRef(obj);
            item.Tag = GetTag(obj);
            item.Name = GetName(obj);
            item.IconResref = Item.GetIconResref(obj);
            item.Category = PlayerMarket.GetItemMarketCategory(obj);
            // The seller must choose a price for the replacement before it can be purchased.
            item.IsListed = false;
            item.DateListed = null;
        }));
        MigrateRecords<QuestContract>(contract => MigrateContractItems(contract.RewardItems));
        MigrateRecords<QuestContractDelivery>(delivery => MigrateContractItems(delivery.Items));
        MigrateRecords<WorldPropertyCategory>(category =>
        {
            var changed = false;
            if (category.Items == null) return false;
            foreach (var item in category.Items.Values)
                changed |= MigrateData(item.Data, value => item.Data = value, obj =>
                {
                    item.Resref = GetResRef(obj);
                    item.Tag = GetTag(obj);
                    item.Name = GetName(obj);
                    item.IconResref = Item.GetIconResref(obj);
                });
            return changed;
        });
        MigrateRecords<WorldProperty>(item => MigrateData(item.SerializedItem, value => item.SerializedItem = value));
        MigrateRecords<ResearchJob>(item => MigrateData(item.SerializedItem, value => item.SerializedItem = value));
        MigrateRecords<PlayerOutfit>(item => MigrateData(item.Data, value => item.Data = value));
        MigrateRecords<DMCreature>(item => MigrateData(item.Data, value => item.Data = value));
        MigrateRecords<PlayerShip>(ship =>
        {
            var changed = MigrateData(ship.SerializedItem, value => ship.SerializedItem = value);
            changed |= MigrateModules(ship.Status?.HighPowerModules);
            changed |= MigrateModules(ship.Status?.LowPowerModules);
            changed |= MigrateModules(ship.Status?.ConfigurationModules);
            return changed;
        });
    }

    private static bool MigrateContractItems(List<QuestContractItem> items)
    {
        if (items == null) return false;
        var changed = false;
        foreach (var item in items)
            changed |= MigrateData(item.Data, value => item.Data = value, obj =>
            {
                item.Resref = GetResRef(obj);
                item.Name = GetName(obj);
                item.IconResref = Item.GetIconResref(obj);
            });
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

    private static bool MigrateData(string data, Action<string> save, Action<uint> updateMetadata = null)
    {
        if (!DungeonSaberMigration.MigrateSerializedObject(data, out var migrated)) return false;
        if (updateMetadata != null)
        {
            var obj = MigrationObject.Deserialize(migrated);
            try { updateMetadata(obj); }
            finally { MigrationObject.DestroyTemporaryObject(obj); }
        }
        save(migrated);
        return true;
    }

    private static void MigrateRecords<T>(Func<T, bool> migrate) where T : EntityBase
    {
        var query = new DBQuery<T>();
        var count = checked((int)DB.SearchCount(query));
        var records = DB.Search(query.AddPaging(count, 0)).ToList();
        var changed = 0;
        foreach (var record in records)
        {
            try
            {
                if (!migrate(record)) continue;
                DB.Set(record);
                changed++;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Dungeon saber migration failed for {typeof(T).Name} {record.Id}.", ex);
            }
        }
        Log.Write(LogGroup.Migration, $"Dungeon sabers: {typeof(T).Name} completed, {changed}/{count} records changed.", true);
    }
}
