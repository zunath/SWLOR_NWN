using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.QuestContractService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Updates item artwork, loot stats, droid weapon skills, renamed furniture and legacy headgear slots across every stored item (banks, market,
/// property storage, research, outfits, saved creatures, ships, contracts) in one pass. Each stored object
/// is loaded and saved once, with one item traversal. Server migrations that change stored items run it.
/// The pass edits saved data directly instead of loading each item into the engine, and converts records on
/// worker threads, so a production sweep takes about a minute at boot (<see cref="StoredObjectData"/>'s item sweep).
/// </summary>
internal static class StoredItemSweep
{
    /// <summary>The first server migration whose sweep also converts retired ship deeds and modules.</summary>
    internal const int LegacySpaceItemsVersion = 31;

    internal static void Migrate(int version)
    {
        var progress = new ServerMigrationProgress(version, CountSections());
        var space = version >= LegacySpaceItemsVersion ? new LegacySpaceItemSweep(version) : null;
        var changed = Sweep(new ParallelStoredItemPass(progress), SavedDataStoredItemConverter.Instance, space);
        Log.Write(LogGroup.Migration, $"Migration #{version}: Updated item artwork, loot stats and equipment slots in {changed} stored records.", true);
        if (space == null)
            return;
        var paid = space.Apply(version);
        var totals = space.Totals;
        Log.Write(LogGroup.Migration, $"Migration #{version}: Stored space items: {string.Join(", ", totals.Items.OrderBy(x => x.Key).Select(x => $"{x.Key} {x.Value}"))}; " +
            $"{totals.ConfigurationModulesRefunded} configuration modules refunded for {totals.Credits} credits; {paid} owners paid or given designs.", true);
    }

    internal static void MigratePelvis(int version)
    {
        var sections = CountSections().Append(ServerMigrationProgress.CountRecords<OverflowItem>()).ToArray();
        var progress = new ServerMigrationProgress(version, sections);
        var pass = new ParallelStoredItemPass(progress);
        var converter = PelvisStoredItemConverter.Instance;
        var changed = Sweep(pass, converter, null);
        changed += pass.Process<OverflowItem>(item => MigrateData(converter, item.Data, value => item.Data = value));
        Log.Write(LogGroup.Migration, $"Migration #{version}: Consolidated pelvis appearances in {changed} stored records.", true);
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
    /// <param name="space">The retired space item rule, or null for a sweep before <see cref="LegacySpaceItemsVersion"/>.
    /// Registered ships are converted by their own pass first (<see cref="PlayerShipConversion"/>).</param>
    private static int Sweep(ParallelStoredItemPass pass, IStoredItemConverter converter, LegacySpaceItemSweep space)
    {
        var legacyMetadata = converter is not PelvisStoredItemConverter;
        var changed = 0;
        changed += pass.Process<InventoryItem>(item =>
        {
            var itemChanged = MigrateItem(converter, item.Resref, item.Data, value => item.Data = value, item.IconResref, value => item.IconResref = value)
                | (legacyMetadata && RenameListing(item.Resref, item.Name, value => item.Name = value));
            return itemChanged | (space?.Migrate(item.Data, value => item.Data = value, item.PlayerId, $"{nameof(InventoryItem)}:{item.Id}",
                (blueprint, resref) => (item.Name, item.Tag, item.Resref, item.IconResref) = (blueprint.Name, resref, resref, blueprint.IconResref),
                credits => space.QueueRemoval<InventoryItem>(item.Id, item.PlayerId, credits)) ?? false);
        }, space == null ? null : space.Flush);
        changed += pass.Process<MarketItem>(item =>
        {
            var itemChanged = MigrateItem(converter, item.Resref, item.Data, value => item.Data = value, item.IconResref, value => item.IconResref = value,
                listing => item.Category = listing.Category) | (legacyMetadata && RenameListing(item.Resref, item.Name, value => item.Name = value));
            return itemChanged | (space?.Migrate(item.Data, value => item.Data = value, item.PlayerId, $"{nameof(MarketItem)}:{item.Id}",
                (blueprint, resref) => (item.Name, item.Tag, item.Resref, item.IconResref, item.Category) =
                    (blueprint.Name, resref, resref, blueprint.IconResref, blueprint.Category),
                credits => space.QueueRemoval<MarketItem>(item.Id, item.PlayerId, credits)) ?? false);
        }, space == null ? null : space.Flush);
        changed += pass.Process<WorldPropertyCategory>(category =>
        {
            var categoryChanged = false;
            if (category.Items != null)
                foreach (var (key, item) in category.Items.ToList())
                {
                    categoryChanged |= MigrateItem(converter, item.Resref, item.Data, value => item.Data = value, item.IconResref, value => item.IconResref = value)
                        | (legacyMetadata && RenameListing(item.Resref, item.Name, value => item.Name = value));
                    if (space == null)
                        continue;
                    var removed = false;
                    var owner = LegacySpaceItemSweep.ForProperty(category.ParentPropertyId);
                    var sourceKey = $"{nameof(WorldPropertyCategory)}:{category.Id}:{key}";
                    categoryChanged |= space.Migrate(item.Data, value => item.Data = value, owner, sourceKey,
                        (blueprint, resref) => (item.Name, item.Tag, item.Resref, item.IconResref) = (blueprint.Name, resref, resref, blueprint.IconResref),
                        credits =>
                        {
                            removed = category.Items.Remove(key);
                            space.QueueRefund(owner, sourceKey, credits);
                        });
                    categoryChanged |= removed;
                }
            return categoryChanged;
        }, space == null ? null : space.Flush);
        changed += pass.Process<WorldProperty>(property => MigrateData(converter, property.SerializedItem, value => property.SerializedItem = value) |
            (legacyMetadata && FurnitureRenameMigration.RenameStructure(property)) |
            (space?.Migrate(property.SerializedItem, value => property.SerializedItem = value, null) ?? false));
        changed += pass.Process<ResearchJob>(job => MigrateData(converter, job.SerializedItem, value => job.SerializedItem = value) |
            (space?.Migrate(job.SerializedItem, value => job.SerializedItem = value, null) ?? false));
        // Cosmetic chest templates retain Armor so ApplyOutfit can still load them.
        changed += pass.Process<PlayerOutfit>(outfit =>
            MigrateData(converter, outfit.Data, value => outfit.Data = value, repairHeadgear: false) |
            MigrateData(converter, outfit.HelmetData, value => outfit.HelmetData = value) |
            (!legacyMetadata && PelvisAppearanceMigration.MigrateOutfitListing(outfit)));
        changed += pass.Process<DMCreature>(creature => MigrateData(converter, creature.Data, value => creature.Data = value) |
            (space?.Migrate(creature.Data, value => creature.Data = value, null) ?? false));
        changed += pass.Process<PlayerShip>(ship =>
        {
            var shipChanged = MigrateData(converter, ship.SerializedItem, value => ship.SerializedItem = value);
            shipChanged |= MigrateModules(converter, ship.Status?.HighPowerModules);
            shipChanged |= MigrateModules(converter, ship.Status?.MidPowerModules);
            shipChanged |= MigrateModules(converter, ship.Status?.LowPowerModules);
            shipChanged |= MigrateModules(converter, ship.Status?.ConfigurationModules);
            return shipChanged;
        });
        changed += pass.Process<QuestContract>(contract => MigrateContractItems(converter, contract.RewardItems, space));
        changed += pass.Process<QuestContractDelivery>(delivery => MigrateContractItems(converter, delivery.Items, space));
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
        if (converter is PelvisStoredItemConverter)
            return changed;
        var updatedIcon = rootIcon ?? ItemIconMigration.GetUpdatedIcon(resref, icon);
        if (updatedIcon == icon)
            return changed;
        setIcon(updatedIcon);
        return true;
    }

    /// <summary>Keeps a listing's saved name in step with its renamed item (<see cref="FurnitureRenameMigration"/>).</summary>
    private static bool RenameListing(string resref, string name, Action<string> setName)
    {
        var renamed = FurnitureRenameMigration.GetRenamedItemText(resref, name);
        if (renamed == name)
            return false;
        setName(renamed);
        return true;
    }

    private static bool MigrateContractItems(IStoredItemConverter converter, List<QuestContractItem> items, LegacySpaceItemSweep space)
    {
        var changed = false;
        if (items != null)
            foreach (var item in items)
            {
                changed |= MigrateItem(converter, item.Resref, item.Data, value => item.Data = value, item.IconResref, value => item.IconResref = value)
                    | (converter is not PelvisStoredItemConverter && RenameListing(item.Resref, item.Name, value => item.Name = value));
                changed |= space?.Migrate(item.Data, value => item.Data = value, null, null,
                    (blueprint, resref) => (item.Name, item.Resref, item.IconResref) = (blueprint.Name, resref, blueprint.IconResref)) ?? false;
            }
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
}
