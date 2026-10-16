using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.QuestContractService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>
/// Updates item artwork and moves loot onto the revised loot stats across every stored item (banks, market,
/// property storage, research, outfits, saved creatures, ships, contracts) in one pass. Each stored object
/// is loaded and saved once for both updates. Player records are updated by <see cref="_27_UpdatePlayerRecords"/>.
/// </summary>
public sealed class _27_UpdateStoredItems : IServerMigration
{
    public int Version => 27;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

    public void Migrate()
    {
        var progress = new ServerMigrationProgress(Version,
            ServerMigrationProgress.CountRecords<InventoryItem>(),
            ServerMigrationProgress.CountRecords<MarketItem>(),
            ServerMigrationProgress.CountRecords<WorldPropertyCategory>(),
            ServerMigrationProgress.CountRecords<WorldProperty>(),
            ServerMigrationProgress.CountRecords<ResearchJob>(),
            ServerMigrationProgress.CountRecords<PlayerOutfit>(),
            ServerMigrationProgress.CountRecords<DMCreature>(),
            ServerMigrationProgress.CountRecords<PlayerShip>(),
            ServerMigrationProgress.CountRecords<QuestContract>(),
            ServerMigrationProgress.CountRecords<QuestContractDelivery>());
        var changed = 0;
        changed += MigrateRecords<InventoryItem>(progress, item =>
            MigrateItem(item.Resref, item.Data, value => item.Data = value, item.IconResref, value => item.IconResref = value));
        changed += MigrateRecords<MarketItem>(progress, item =>
            MigrateItem(item.Resref, item.Data, value => item.Data = value, item.IconResref, value => item.IconResref = value));
        changed += MigrateRecords<WorldPropertyCategory>(progress, category =>
        {
            var categoryChanged = false;
            if (category.Items != null)
                foreach (var item in category.Items.Values)
                    categoryChanged |= MigrateItem(item.Resref, item.Data, value => item.Data = value, item.IconResref, value => item.IconResref = value);
            return categoryChanged;
        });
        changed += MigrateRecords<WorldProperty>(progress, property => MigrateData(property.SerializedItem, value => property.SerializedItem = value));
        changed += MigrateRecords<ResearchJob>(progress, job => MigrateData(job.SerializedItem, value => job.SerializedItem = value));
        changed += MigrateRecords<PlayerOutfit>(progress, outfit => MigrateData(outfit.Data, value => outfit.Data = value));
        changed += MigrateRecords<DMCreature>(progress, creature => MigrateData(creature.Data, value => creature.Data = value));
        changed += MigrateRecords<PlayerShip>(progress, ship =>
        {
            var shipChanged = MigrateData(ship.SerializedItem, value => ship.SerializedItem = value);
            shipChanged |= MigrateModules(ship.Status?.HighPowerModules);
            shipChanged |= MigrateModules(ship.Status?.LowPowerModules);
            shipChanged |= MigrateModules(ship.Status?.ConfigurationModules);
            return shipChanged;
        });
        changed += MigrateRecords<QuestContract>(progress, contract => MigrateContractItems(contract.RewardItems));
        changed += MigrateRecords<QuestContractDelivery>(progress, delivery => MigrateContractItems(delivery.Items));
        Log.Write(LogGroup.Migration, $"Migration #{Version}: Updated item artwork and loot stats in {changed} stored records.", true);
    }

    private static int MigrateRecords<T>(ServerMigrationProgress progress, Func<T, bool> migrate) where T : EntityBase
    {
        var count = progress.BeginSection<T>();
        var changed = 0;
        foreach (var record in DB.Search(new DBQuery<T>().AddPaging(count, 0)))
        {
            bool recordChanged;
            try
            {
                recordChanged = migrate(record);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Stored item migration failed for {typeof(T).Name} {record.Id}.", ex);
            }
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

    private static bool MigrateItem(string resref, string data, Action<string> setData, string icon, Action<string> setIcon)
    {
        var changed = MigrateData(data, setData);
        var updatedIcon = ItemIconMigration.GetUpdatedIcon(resref, icon);
        if (updatedIcon == icon)
            return changed;
        setIcon(updatedIcon);
        return true;
    }

    private static bool MigrateContractItems(List<QuestContractItem> items)
    {
        var changed = false;
        if (items != null)
            foreach (var item in items)
                changed |= MigrateItem(item.Resref, item.Data, value => item.Data = value, item.IconResref, value => item.IconResref = value);
        return changed;
    }

    private static bool MigrateModules(Dictionary<int, ShipStatus.ShipStatusModule> modules)
    {
        var changed = false;
        if (modules != null)
            foreach (var module in modules.Values)
                changed |= MigrateData(module.SerializedItem, value => module.SerializedItem = value);
        return changed;
    }

    private static bool MigrateData(string data, Action<string> setData)
    {
        if (!MigrateSerializedObject(data, out var migrated))
            return false;
        setData(migrated);
        return true;
    }

    /// <summary>Loads a stored object once, applies both updates, and saves it only when either changed it.</summary>
    public static bool MigrateSerializedObject(string serialized, out string migrated)
    {
        // Saved creatures keep their authored gear, so only their artwork changes.
        if (string.IsNullOrWhiteSpace(serialized) || StoredObjectData.IsCreatureData(serialized))
            return ItemIconMigration.MigrateSerializedObject(serialized, out migrated);

        migrated = serialized;
        var inventory = StoredObjectData.ReadInventory(serialized);
        var obj = MigrationObject.Deserialize(inventory?.PrepareForNativeLoad(null) ?? serialized);
        try
        {
            var changed = ItemIconMigration.MigrateObject(obj);
            changed |= LootStatRevisionMigration.MigrateObject(obj);
            if (!changed)
                return false;
            migrated = MigrationObject.Serialize(obj, serialized);
            if (inventory != null)
                migrated = inventory.CopyMigratedInventory(migrated);
            return true;
        }
        finally
        {
            MigrationObject.DestroyTemporaryObject(obj);
        }
    }
}
