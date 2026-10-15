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

/// <summary>Repairs legacy headgear in banks, markets, properties, research, outfits, ships and contracts.</summary>
public sealed class _31_RepairHeadgearSlots : IServerMigration
{
    public int Version => 31;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

    public void Migrate()
    {
        var progress = new ServerMigrationProgress(Version,
            ServerMigrationProgress.CountRecords<InventoryItem>(),
            ServerMigrationProgress.CountRecords<MarketItem>(),
            ServerMigrationProgress.CountRecords<WorldProperty>(),
            ServerMigrationProgress.CountRecords<ResearchJob>(),
            ServerMigrationProgress.CountRecords<PlayerOutfit>(),
            ServerMigrationProgress.CountRecords<WorldPropertyCategory>(),
            ServerMigrationProgress.CountRecords<PlayerShip>(),
            ServerMigrationProgress.CountRecords<QuestContract>(),
            ServerMigrationProgress.CountRecords<QuestContractDelivery>());
        var changed = 0;
        changed += MigrateEntities<InventoryItem>(progress, x => x.Data, (x, value) => x.Data = value);
        changed += MigrateEntities<MarketItem>(progress, x => x.Data, (x, value) => x.Data = value);
        changed += MigrateEntities<WorldProperty>(progress, x => x.SerializedItem, (x, value) => x.SerializedItem = value);
        changed += MigrateEntities<ResearchJob>(progress, x => x.SerializedItem, (x, value) => x.SerializedItem = value);
        changed += MigrateEntities<PlayerOutfit>(progress, x => x.Data, (x, value) => x.Data = value);
        changed += MigrateRecords<WorldPropertyCategory>(progress, category =>
        {
            var categoryChanged = false;
            if (category.Items != null)
                foreach (var item in category.Items.Values)
                    categoryChanged |= MigrateField(item.Data, value => item.Data = value);
            return categoryChanged;
        });
        changed += MigrateRecords<PlayerShip>(progress, ship =>
        {
            var shipChanged = MigrateField(ship.SerializedItem, value => ship.SerializedItem = value);
            shipChanged |= MigrateModules(ship.Status?.HighPowerModules);
            shipChanged |= MigrateModules(ship.Status?.LowPowerModules);
            shipChanged |= MigrateModules(ship.Status?.ConfigurationModules);
            return shipChanged;
        });
        changed += MigrateRecords<QuestContract>(progress, contract => MigrateContractItems(contract.RewardItems));
        changed += MigrateRecords<QuestContractDelivery>(progress, delivery => MigrateContractItems(delivery.Items));
        Log.Write(LogGroup.Migration, $"Migration #{Version}: Repaired headgear slots in {changed} stored records.", true);
    }

    private static int MigrateEntities<T>(ServerMigrationProgress progress, Func<T, string> getData, Action<T, string> setData) where T : EntityBase
        => MigrateRecords<T>(progress, entity => MigrateField(getData(entity), value => setData(entity, value)));

    private static int MigrateRecords<T>(ServerMigrationProgress progress, Func<T, bool> migrate) where T : EntityBase
    {
        var count = progress.BeginSection<T>();
        var records = DB.Search(new DBQuery<T>().AddPaging(count, 0)).ToList();
        var changed = 0;
        foreach (var record in records)
        {
            var recordChanged = migrate(record);
            if (recordChanged)
            {
                DB.Set(record);
                changed++;
            }
            progress.RecordProcessed(recordChanged);
        }
        progress.FinishSection();
        return changed;
    }

    private static bool MigrateField(string data, Action<string> setData)
    {
        if (!HeadgearSlotMigration.MigrateSerializedObject(data, out var migrated))
            return false;
        setData(migrated);
        return true;
    }

    private static bool MigrateContractItems(List<QuestContractItem> items)
    {
        var changed = false;
        if (items != null)
            foreach (var item in items)
                changed |= MigrateField(item.Data, value => item.Data = value);
        return changed;
    }

    private static bool MigrateModules(Dictionary<int, ShipStatus.ShipStatusModule> modules)
    {
        var changed = false;
        if (modules != null)
            foreach (var module in modules.Values)
                changed |= MigrateField(module.SerializedItem, value => module.SerializedItem = value);
        return changed;
    }
}
