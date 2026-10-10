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
        var changed = 0;
        changed += MigrateEntities<InventoryItem>(x => x.Data, (x, value) => x.Data = value);
        changed += MigrateEntities<MarketItem>(x => x.Data, (x, value) => x.Data = value);
        changed += MigrateEntities<WorldProperty>(x => x.SerializedItem, (x, value) => x.SerializedItem = value);
        changed += MigrateEntities<ResearchJob>(x => x.SerializedItem, (x, value) => x.SerializedItem = value);
        changed += MigrateEntities<PlayerOutfit>(x => x.Data, (x, value) => x.Data = value);
        foreach (var category in SearchAll<WorldPropertyCategory>())
        {
            var categoryChanged = false;
            if (category.Items != null)
                foreach (var item in category.Items.Values)
                    categoryChanged |= MigrateField(item.Data, value => item.Data = value);
            if (!categoryChanged)
                continue;
            DB.Set(category);
            changed++;
        }
        foreach (var ship in SearchAll<PlayerShip>())
        {
            var shipChanged = MigrateField(ship.SerializedItem, value => ship.SerializedItem = value);
            shipChanged |= MigrateModules(ship.Status?.HighPowerModules);
            shipChanged |= MigrateModules(ship.Status?.LowPowerModules);
            shipChanged |= MigrateModules(ship.Status?.ConfigurationModules);
            if (!shipChanged)
                continue;
            DB.Set(ship);
            changed++;
        }
        Log.Write(LogGroup.Migration, $"Migration #{Version}: Removed player weapon stat overrides in {changed} stored records.", true);
    }

    private static List<T> SearchAll<T>() where T : EntityBase
    {
        var query = new DBQuery<T>();
        var count = (int)DB.SearchCount(query);
        return DB.Search(query.AddPaging(count, 0)).ToList();
    }

    private static int MigrateEntities<T>(Func<T, string> getData, Action<T, string> setData) where T : EntityBase
    {
        var changed = 0;
        foreach (var entity in SearchAll<T>())
        {
            if (!MigrateField(getData(entity), value => setData(entity, value)))
                continue;
            DB.Set(entity);
            changed++;
        }
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
