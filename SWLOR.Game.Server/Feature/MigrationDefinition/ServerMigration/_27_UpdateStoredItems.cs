using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.QuestContractService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>
/// Updates item artwork, loot stats and legacy headgear slots across every stored item (banks, market,
/// property storage, research, outfits, saved creatures, ships, contracts) in one pass. Each stored object
/// is loaded and saved once, with one item traversal. Player records are updated by <see cref="_27_UpdatePlayerRecords"/>.
/// The pass edits saved data directly instead of loading each item into the engine, and converts records on
/// worker threads, so a production sweep takes about a minute at boot (<see cref="StoredObjectData"/>'s item sweep).
/// </summary>
public sealed class _27_UpdateStoredItems : IServerMigration
{
    public int Version => 27;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

    public void Migrate() => Migrate(Version);

    internal static void Migrate(int version)
    {
        var progress = new ServerMigrationProgress(version, CountSections());
        var changed = Sweep(new ParallelStoredItemPass(progress), SavedDataStoredItemConverter.Instance);
        Log.Write(LogGroup.Migration, $"Migration #{version}: Updated item artwork, loot stats and equipment slots in {changed} stored records.", true);
    }

    private static (string Name, int Count)[] CountSections() => new[]
    {
        ServerMigrationProgress.CountRecords<InventoryItem>(),
        ServerMigrationProgress.CountRecords<MarketItem>(),
        ServerMigrationProgress.CountRecords<WorldPropertyCategory>(),
        ServerMigrationProgress.CountRecords<WorldProperty>(),
        ServerMigrationProgress.CountRecords<ResearchJob>(),
        ServerMigrationProgress.CountRecords<PlayerOutfit>(),
        ServerMigrationProgress.CountRecords<DMCreature>(),
        ServerMigrationProgress.CountRecords<PlayerShip>(),
        ServerMigrationProgress.CountRecords<QuestContract>(),
        ServerMigrationProgress.CountRecords<QuestContractDelivery>(),
    };

    /// <summary>Every stored item record and the fields holding its items.</summary>
    private static int Sweep(ParallelStoredItemPass pass, IStoredItemConverter converter)
    {
        var changed = 0;
        changed += pass.Process<InventoryItem>(item =>
            MigrateItem(converter, item.Resref, item.Data, value => item.Data = value, item.IconResref, value => item.IconResref = value));
        changed += pass.Process<MarketItem>(item =>
        {
            return MigrateItem(converter, item.Resref, item.Data, value => item.Data = value, item.IconResref, value => item.IconResref = value,
                listing => item.Category = listing.Category);
        });
        changed += pass.Process<WorldPropertyCategory>(category =>
        {
            var categoryChanged = false;
            if (category.Items != null)
                foreach (var item in category.Items.Values)
                    categoryChanged |= MigrateItem(converter, item.Resref, item.Data, value => item.Data = value, item.IconResref, value => item.IconResref = value);
            return categoryChanged;
        });
        changed += pass.Process<WorldProperty>(property => MigrateData(converter, property.SerializedItem, value => property.SerializedItem = value));
        changed += pass.Process<ResearchJob>(job => MigrateData(converter, job.SerializedItem, value => job.SerializedItem = value));
        // Cosmetic chest templates retain Armor so ApplyOutfit can still load them.
        changed += pass.Process<PlayerOutfit>(outfit => MigrateData(converter, outfit.Data, value => outfit.Data = value, repairHeadgear: false));
        changed += pass.Process<DMCreature>(creature => MigrateData(converter, creature.Data, value => creature.Data = value));
        changed += pass.Process<PlayerShip>(ship =>
        {
            var shipChanged = MigrateData(converter, ship.SerializedItem, value => ship.SerializedItem = value);
            shipChanged |= MigrateModules(converter, ship.Status?.HighPowerModules);
            shipChanged |= MigrateModules(converter, ship.Status?.LowPowerModules);
            shipChanged |= MigrateModules(converter, ship.Status?.ConfigurationModules);
            return shipChanged;
        });
        changed += pass.Process<QuestContract>(contract => MigrateContractItems(converter, contract.RewardItems));
        changed += pass.Process<QuestContractDelivery>(delivery => MigrateContractItems(converter, delivery.Items));
        return changed;
    }

    private static bool MigrateItem(IStoredItemConverter converter, string resref, string data, Action<string> setData,
        string icon, Action<string> setIcon, Action<RootListing> rootChanged = null)
    {
        string rootIcon = null;
        var changed = MigrateData(converter, data, setData, rootChanged: listing =>
        {
            rootIcon = listing.IconResref;
            rootChanged?.Invoke(listing);
        });
        var updatedIcon = rootIcon ?? ItemIconMigration.GetUpdatedIcon(resref, icon);
        if (updatedIcon == icon)
            return changed;
        setIcon(updatedIcon);
        return true;
    }

    private static bool MigrateContractItems(IStoredItemConverter converter, List<QuestContractItem> items)
    {
        var changed = false;
        if (items != null)
            foreach (var item in items)
                changed |= MigrateItem(converter, item.Resref, item.Data, value => item.Data = value, item.IconResref, value => item.IconResref = value);
        return changed;
    }

    private static bool MigrateModules(IStoredItemConverter converter, Dictionary<int, ShipStatus.ShipStatusModule> modules)
    {
        var changed = false;
        if (modules != null)
            foreach (var module in modules.Values)
                changed |= MigrateData(converter, module.SerializedItem, value => module.SerializedItem = value);
        return changed;
    }

    private static bool MigrateData(IStoredItemConverter converter, string data, Action<string> setData,
        bool repairHeadgear = true, Action<RootListing> rootChanged = null)
    {
        if (!converter.MigrateStoredObjectData(data, out var migrated, repairHeadgear, rootChanged))
            return false;
        setData(migrated);
        return true;
    }

    /// <summary>Loads a stored object once and applies artwork, loot stats and headgear repair in one item traversal.</summary>
    public static bool MigrateSerializedObject(string serialized, out string migrated)
        => LootStatRevisionMigration.MigrateStoredObjectData(serialized, out migrated);
}
