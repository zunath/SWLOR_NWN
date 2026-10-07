using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>Updates saved item artwork independently of previously completed item-schema migrations.</summary>
public sealed class _23_UpdateItemIcons : IServerMigration
{
    public int Version => 23;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

    public void Migrate()
    {
        MigrateRecords<InventoryItem>(item =>
        {
            var changed = UpdateData(item.Data, value => item.Data = value);
            changed |= UpdateIcon(item.Resref, item.IconResref, value => item.IconResref = value);
            return changed;
        });
        MigrateRecords<MarketItem>(item =>
        {
            var changed = UpdateData(item.Data, value => item.Data = value);
            changed |= UpdateIcon(item.Resref, item.IconResref, value => item.IconResref = value);
            return changed;
        });
        MigrateRecords<WorldPropertyCategory>(category =>
        {
            var changed = false;
            if (category.Items == null) return false;
            foreach (var item in category.Items.Values)
            {
                changed |= UpdateData(item.Data, value => item.Data = value);
                changed |= UpdateIcon(item.Resref, item.IconResref, value => item.IconResref = value);
            }
            return changed;
        });
        MigrateRecords<WorldProperty>(item => UpdateData(item.SerializedItem, value => item.SerializedItem = value));
        MigrateRecords<ResearchJob>(job => UpdateData(job.SerializedItem, value => job.SerializedItem = value));
        MigrateRecords<PlayerOutfit>(outfit => UpdateData(outfit.Data, value => outfit.Data = value));
        MigrateRecords<DMCreature>(creature => UpdateData(creature.Data, value => creature.Data = value));
        MigrateRecords<PlayerShip>(ship =>
        {
            var changed = UpdateData(ship.SerializedItem, value => ship.SerializedItem = value);
            changed |= UpdateModules(ship.Status?.HighPowerModules);
            changed |= UpdateModules(ship.Status?.LowPowerModules);
            changed |= UpdateModules(ship.Status?.ConfigurationModules);
            return changed;
        });
    }

    private static bool UpdateModules(Dictionary<int, ShipStatus.ShipStatusModule> modules)
    {
        if (modules == null) return false;
        var changed = false;
        foreach (var module in modules.Values)
            changed |= UpdateData(module.SerializedItem, value => module.SerializedItem = value);
        return changed;
    }

    private static bool UpdateData(string data, Action<string> setData)
    {
        if (!ItemIconMigration.MigrateSerializedObject(data, out var updated)) return false;
        setData(updated);
        return true;
    }

    private static bool UpdateIcon(string resref, string icon, Action<string> setIcon)
    {
        var updated = ItemIconMigration.GetUpdatedIcon(resref, icon);
        if (updated == icon) return false;
        setIcon(updated);
        return true;
    }

    private static void MigrateRecords<T>(Func<T, bool> migrate) where T : EntityBase
    {
        var query = new DBQuery<T>();
        var count = (int)DB.SearchCount(query);
        var changed = 0;
        foreach (var entity in DB.Search(query.AddPaging(count, 0)))
        {
            if (!migrate(entity)) continue;
            DB.Set(entity);
            changed++;
        }
        Log.Write(LogGroup.Migration, $"Migration #23: Updated item artwork in {changed}/{count} {typeof(T).Name} records.", true);
    }
}
