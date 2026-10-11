using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>Repairs saved weapons even when the previous item migration has already completed.</summary>
public sealed class _24_RemoveWeaponStatOverrides : IServerMigration
{
    public int Version => 24;
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
            ServerMigrationProgress.CountRecords<PlayerShip>());
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
        Log.Write(LogGroup.Migration, $"Migration #{Version}: Removed player weapon stat overrides in {changed} stored records.", true);
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
        if (!WeaponStatOverrideMigration.MigrateSerializedObject(data, out var migrated))
            return false;
        setData(migrated);
        return true;
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
