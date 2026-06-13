using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _34_RemoveObsoleteBiblePerks : ServerMigrationBase, IServerMigration
    {
        private static readonly Dictionary<PerkType, int[]> PlayerRemovedPerks = new()
        {
            { PerkType.DemolitionExpert, new[] { 1, 2, 3 } },
            { PerkType.FlashbangGrenade, new[] { 2, 3, 3 } },
            { PerkType.KoltoGrenade, new[] { 2, 3, 3 } },
            { PerkType.KoltoBomb, new[] { 2, 3, 3 } },
            { PerkType.IncendiaryBomb, new[] { 2, 3, 3 } },
            { PerkType.GasBomb, new[] { 2, 3, 3 } },
            { PerkType.StealthGenerator, new[] { 2, 3, 3 } },

            { PerkType.RangedHealing, new[] { 2, 3, 4, 5 } },
            { PerkType.FrugalMedic, new[] { 1, 2, 2 } },
            { PerkType.KoltoRecovery, new[] { 3, 4, 5 } },
            { PerkType.StasisField, new[] { 2, 3, 4 } },
            { PerkType.CombatEnhancement, new[] { 3, 3, 4 } },

            { PerkType.ForceHeal, new[] { 2, 2, 2, 3, 3 } },
            { PerkType.ForceBurst, new[] { 2, 2, 3, 3 } },
            { PerkType.Disturbance, new[] { 2, 2, 2 } },
            { PerkType.ForceValor, new[] { 2, 3 } },
            { PerkType.ThrowRock, new[] { 1, 2, 2, 2, 3 } },
            { PerkType.BurstOfSpeed, new[] { 2, 2 } },
            { PerkType.ThrowLightsaber, new[] { 2, 2, 2 } },
            { PerkType.ForceStun, new[] { 2, 2, 3 } },
            { PerkType.BattleInsight, new[] { 2, 2 } },
            { PerkType.ForceMind, new[] { 3, 4 } },
            { PerkType.Premonition, new[] { 2, 2 } },
            { PerkType.ForceInspiration, new[] { 2, 3, 4 } },
        };

        private static readonly Dictionary<PerkType, (int MaxLevel, int[] PricesByLevel)> PlayerTrimmedPerks = new()
        {
            { PerkType.IonGrenade, (2, new[] { 2, 3, 3 }) },
            { PerkType.AdhesiveGrenade, (2, new[] { 2, 3, 3 }) },
            { PerkType.MedKit, (4, new[] { 1, 2, 3, 4, 4 }) },
            { PerkType.Resuscitation, (2, new[] { 4, 4, 4 }) },
            { PerkType.Shielding, (3, new[] { 2, 3, 3, 4 }) },
        };

        private static readonly Dictionary<PerkType, int[]> BeastRemovedPerks = new()
        {
            { PerkType.FlameBreath, new[] { 2, 2, 2, 3, 3 } },
            { PerkType.ShockingSlash, new[] { 1, 1, 1, 2, 2 } },
            { PerkType.DiseasedTouch, new[] { 2, 2, 2, 2, 2 } },
            { PerkType.Clip, new[] { 2, 2, 2, 2, 2 } },
            { PerkType.SpinningClaw, new[] { 2, 2, 2, 2, 2 } },
            { PerkType.BeastSpeed, new[] { 3, 3, 3 } },
            { PerkType.BolsterArmor, new[] { 1, 1, 1, 2, 2 } },
        };

        private static readonly Dictionary<PerkType, (int MaxLevel, int[] PricesByLevel)> BeastTrimmedPerks = new()
        {
            { PerkType.Bite, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.Claw, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.BolsterAttack, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.Hasten, (2, new[] { 4, 4, 4 }) },
            { PerkType.PoisonBreath, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.IceBreath, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.EvasiveManeuver, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.Assault, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.ForceTouch, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.Innervate, (3, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.Anger, (2, new[] { 1, 1, 1, 2, 2 }) },
            { PerkType.FocusAttention, (3, new[] { 2, 2, 2, 3, 3 }) },
        };

        // Numeric ids are intentional: the old enum names were deleted and some ids have been reused by new Bible abilities.
        private static readonly HashSet<RecastGroup> ObsoleteRecastGroups = new()
        {
            (RecastGroup)1, // BurstOfSpeed
            (RecastGroup)2, // ForceHeal
            (RecastGroup)3, // ForcePush
            (RecastGroup)4, // ThrowLightsaber
            (RecastGroup)5, // ForceStun
            (RecastGroup)6, // BattleInsight
            (RecastGroup)7, // ComprehendSpeech
            (RecastGroup)8, // MindTrick
            (RecastGroup)9, // ForceBurst
            (RecastGroup)10, // ForceBody
            (RecastGroup)11, // ForceDrain
            (RecastGroup)12, // ForceLightning
            (RecastGroup)13, // ForceMind
            (RecastGroup)14, // ForceLeap
            (RecastGroup)15, // FragGrenade
            (RecastGroup)25, // MedKit
            (RecastGroup)26, // KoltoRecovery
            (RecastGroup)27, // Resuscitation
            (RecastGroup)28, // TreatmentKit
            (RecastGroup)29, // StasisField
            (RecastGroup)30, // CombatEnhancement
            (RecastGroup)31, // Shielding
            (RecastGroup)32, // Bombs
            (RecastGroup)33, // StealthGenerator
            (RecastGroup)34, // Flamethrower
            (RecastGroup)35, // WristRocket
            (RecastGroup)36, // DeflectorShield
            (RecastGroup)39, // Premonition
            (RecastGroup)40, // Disturbance
            (RecastGroup)41, // Benevolence
            (RecastGroup)42, // ForceValor
            (RecastGroup)43, // ForceSpark
            (RecastGroup)44, // CreepingTerror
            (RecastGroup)45, // FuryStance
            (RecastGroup)46, // Furor
            (RecastGroup)47, // ThrowRock
            (RecastGroup)48, // ForceInspiration
            (RecastGroup)49, // RousingShout
            (RecastGroup)61, // Infusion
            (RecastGroup)64, // ConcussionGrenade
            (RecastGroup)65, // FlashbangGrenade
            (RecastGroup)66, // IonGrenade
            (RecastGroup)67, // KoltoGrenade
            (RecastGroup)68, // AdhesiveGrenade
            (RecastGroup)70, // KoltoBomb
            (RecastGroup)71, // IncendiaryBomb
            (RecastGroup)72, // GasBomb
            (RecastGroup)79, // DiseasedTouch
            (RecastGroup)80, // Clip
            (RecastGroup)81, // SpinningClaw
            (RecastGroup)82, // FlameBreath
            (RecastGroup)83, // ShockingSlash
            (RecastGroup)84, // BolsterArmor
            (RecastGroup)85, // Anger
            (RecastGroup)86, // AOEAnger
            (RecastGroup)87, // Claw
            (RecastGroup)88, // BolsterAttack
            (RecastGroup)89, // Hasten
            (RecastGroup)90, // PoisonBreath
            (RecastGroup)91, // IceBreath
            (RecastGroup)92, // EvasiveManeuver
            (RecastGroup)93, // Assault
            (RecastGroup)94, // ForceTouch
            (RecastGroup)95, // Innervate
            (RecastGroup)96, // ForceRestore
            (RecastGroup)97, // AdrenalStim
        };

        public int Version => 34;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

        public void Migrate()
        {
            var (playersMigrated, playerSpRefunded) = MigratePlayers();
            var (beastsMigrated, beastSpRefunded) = MigrateBeasts();
            var (storedItemRecordsMigrated, storedItemsRemoved, droidPerksMigrated) = MigrateStoredObsoleteItems();

            Log.Write(
                LogGroup.Migration,
                $"Removed obsolete Bible perks from {playersMigrated} players and {beastsMigrated} beasts. Refunded {playerSpRefunded} player SP and {beastSpRefunded} beast SP. Removed {storedItemsRemoved} obsolete instruction disc items and migrated {droidPerksMigrated} stored droid perk sets across {storedItemRecordsMigrated} stored records.");
        }

        private static (int EntityCount, int SpRefunded) MigratePlayers()
        {
            var query = new DBQuery<Player>();
            var playerCount = (int)DB.SearchCount(query);
            var players = DB.Search(query.AddPaging(playerCount, 0));
            var migratedCount = 0;
            var totalRefund = 0;

            foreach (var player in players)
            {
                var refund = CleanPerks(player.Perks, PlayerRemovedPerks, PlayerTrimmedPerks, out var changed);
                changed |= RemoveUnlockedPerks(player);
                changed |= RemoveRecastTimes(player);

                if (refund > 0)
                {
                    player.UnallocatedSP += refund;
                    totalRefund += refund;
                    changed = true;
                }

                if (!changed)
                    continue;

                DB.Set(player);
                migratedCount++;
            }

            return (migratedCount, totalRefund);
        }

        private static (int EntityCount, int SpRefunded) MigrateBeasts()
        {
            var query = new DBQuery<Beast>();
            var beastCount = (int)DB.SearchCount(query);
            var beasts = DB.Search(query.AddPaging(beastCount, 0));
            var migratedCount = 0;
            var totalRefund = 0;

            foreach (var beast in beasts)
            {
                var refund = CleanPerks(beast.Perks, BeastRemovedPerks, BeastTrimmedPerks, out var changed);

                if (refund > 0)
                {
                    beast.UnallocatedSP += refund;
                    totalRefund += refund;
                    changed = true;
                }

                if (!changed)
                    continue;

                DB.Set(beast);
                migratedCount++;
            }

            return (migratedCount, totalRefund);
        }

        private static int CleanPerks(
            Dictionary<PerkType, int> perks,
            Dictionary<PerkType, int[]> removedPerks,
            Dictionary<PerkType, (int MaxLevel, int[] PricesByLevel)> trimmedPerks,
            out bool changed)
        {
            changed = false;
            if (perks == null)
                return 0;

            var refund = 0;

            foreach (var (perkType, pricesByLevel) in removedPerks)
            {
                if (!perks.TryGetValue(perkType, out var purchasedLevel))
                    continue;

                refund += CalculateRefund(pricesByLevel, 1, purchasedLevel);
                perks.Remove(perkType);
                changed = true;
            }

            foreach (var (perkType, trim) in trimmedPerks)
            {
                if (!perks.TryGetValue(perkType, out var purchasedLevel) ||
                    purchasedLevel <= trim.MaxLevel)
                    continue;

                refund += CalculateRefund(trim.PricesByLevel, trim.MaxLevel + 1, purchasedLevel);
                perks[perkType] = trim.MaxLevel;
                changed = true;
            }

            return refund;
        }

        private static int CalculateRefund(int[] pricesByLevel, int fromLevel, int purchasedLevel)
        {
            var refund = 0;
            var maxLevel = purchasedLevel > pricesByLevel.Length
                ? pricesByLevel.Length
                : purchasedLevel;

            for (var level = fromLevel; level <= maxLevel; level++)
            {
                refund += pricesByLevel[level - 1];
            }

            return refund;
        }

        private static bool RemoveUnlockedPerks(Player player)
        {
            if (player.UnlockedPerks == null)
                return false;

            var changed = false;
            foreach (var perkType in PlayerRemovedPerks.Keys)
            {
                changed |= player.UnlockedPerks.Remove(perkType);
            }

            return changed;
        }

        private static bool RemoveRecastTimes(Player player)
        {
            if (player.RecastTimes == null)
                return false;

            var changed = false;
            foreach (var recastGroup in ObsoleteRecastGroups)
            {
                changed |= player.RecastTimes.Remove(recastGroup);
            }

            return changed;
        }

        private static (int EntityCount, int ItemsRemoved, int DroidPerksMigrated) MigrateStoredObsoleteItems()
        {
            var migratedRecords = 0;
            var removedItems = 0;
            var droidPerksMigrated = 0;

            removedItems += MigrateInventoryItems(ref migratedRecords, ref droidPerksMigrated);
            removedItems += MigrateMarketItems(ref migratedRecords, ref droidPerksMigrated);
            removedItems += MigrateWorldPropertyCategories(ref migratedRecords, ref droidPerksMigrated);
            removedItems += MigrateSerializedField<WorldProperty>(x => x.SerializedItem, (x, value) => x.SerializedItem = value, ref migratedRecords, ref droidPerksMigrated);
            removedItems += MigrateSerializedField<ResearchJob>(x => x.SerializedItem, (x, value) => x.SerializedItem = value, ref migratedRecords, ref droidPerksMigrated);
            removedItems += MigrateSerializedField<PlayerOutfit>(x => x.Data, (x, value) => x.Data = value, ref migratedRecords, ref droidPerksMigrated);
            removedItems += MigrateSerializedField<DMCreature>(x => x.Data, (x, value) => x.Data = value, ref migratedRecords, ref droidPerksMigrated);
            removedItems += MigratePlayerShips(ref migratedRecords, ref droidPerksMigrated);

            return (migratedRecords, removedItems, droidPerksMigrated);
        }

        private static int MigrateInventoryItems(ref int migratedRecords, ref int droidPerksMigrated)
        {
            var removedItems = 0;

            foreach (var item in SearchAll<InventoryItem>())
            {
                var metadataRootObsolete = IsObsoleteItemRecord(item.Resref, item.Tag);
                var serializedChanged = ObsoleteItemMigration.RemoveObsoleteItemsFromSerializedObject(
                    item.Data,
                    out var migratedData,
                    out var removedRoot,
                    out var serializedRemovedItems,
                    out var serializedDroidPerks);

                if (metadataRootObsolete || removedRoot)
                {
                    DB.Delete<InventoryItem>(item.Id);
                    removedItems += CountRemovedItemStack(item.Quantity, serializedRemovedItems);
                    migratedRecords++;
                    continue;
                }

                if (!serializedChanged)
                    continue;

                item.Data = migratedData;
                DB.Set(item);
                removedItems += serializedRemovedItems;
                droidPerksMigrated += serializedDroidPerks;
                migratedRecords++;
            }

            return removedItems;
        }

        private static int MigrateMarketItems(ref int migratedRecords, ref int droidPerksMigrated)
        {
            var removedItems = 0;

            foreach (var item in SearchAll<MarketItem>())
            {
                var metadataRootObsolete = IsObsoleteItemRecord(item.Resref, item.Tag);
                var serializedChanged = ObsoleteItemMigration.RemoveObsoleteItemsFromSerializedObject(
                    item.Data,
                    out var migratedData,
                    out var removedRoot,
                    out var serializedRemovedItems,
                    out var serializedDroidPerks);

                if (metadataRootObsolete || removedRoot)
                {
                    DB.Delete<MarketItem>(item.Id);
                    removedItems += CountRemovedItemStack(item.Quantity, serializedRemovedItems);
                    migratedRecords++;
                    continue;
                }

                if (!serializedChanged)
                    continue;

                item.Data = migratedData;
                DB.Set(item);
                removedItems += serializedRemovedItems;
                droidPerksMigrated += serializedDroidPerks;
                migratedRecords++;
            }

            return removedItems;
        }

        private static int MigrateWorldPropertyCategories(ref int migratedRecords, ref int droidPerksMigrated)
        {
            var removedItems = 0;

            foreach (var category in SearchAll<WorldPropertyCategory>())
            {
                if (category.Items == null || category.Items.Count <= 0)
                    continue;

                var changed = false;

                foreach (var itemId in category.Items.Keys.ToList())
                {
                    var item = category.Items[itemId];
                    var metadataRootObsolete = IsObsoleteItemRecord(item.Resref, item.Tag);
                    var serializedChanged = ObsoleteItemMigration.RemoveObsoleteItemsFromSerializedObject(
                        item.Data,
                        out var migratedData,
                        out var removedRoot,
                        out var serializedRemovedItems,
                        out var serializedDroidPerks);

                    if (metadataRootObsolete || removedRoot)
                    {
                        category.Items.Remove(itemId);
                        removedItems += CountRemovedItemStack(item.Quantity, serializedRemovedItems);
                        changed = true;
                        continue;
                    }

                    if (!serializedChanged)
                        continue;

                    item.Data = migratedData;
                    removedItems += serializedRemovedItems;
                    droidPerksMigrated += serializedDroidPerks;
                    changed = true;
                }

                if (!changed)
                    continue;

                DB.Set(category);
                migratedRecords++;
            }

            return removedItems;
        }

        private static int MigrateSerializedField<T>(
            Func<T, string> getSerializedObject,
            Action<T, string> setSerializedObject,
            ref int migratedRecords,
            ref int droidPerksMigrated)
            where T : EntityBase
        {
            var removedItems = 0;

            foreach (var entity in SearchAll<T>())
            {
                var changed = ObsoleteItemMigration.RemoveObsoleteItemsFromSerializedObject(
                    getSerializedObject(entity),
                    out var migratedData,
                    out var removedRoot,
                    out var serializedRemovedItems,
                    out var serializedDroidPerks);

                if (!changed)
                    continue;

                setSerializedObject(entity, removedRoot ? string.Empty : migratedData);
                DB.Set(entity);
                removedItems += serializedRemovedItems;
                droidPerksMigrated += serializedDroidPerks;
                migratedRecords++;
            }

            return removedItems;
        }

        private static int MigratePlayerShips(ref int migratedRecords, ref int droidPerksMigrated)
        {
            var removedItems = 0;

            foreach (var ship in SearchAll<PlayerShip>())
            {
                var changed = ObsoleteItemMigration.RemoveObsoleteItemsFromSerializedObject(
                    ship.SerializedItem,
                    out var migratedData,
                    out var removedRoot,
                    out var serializedRemovedItems,
                    out var serializedDroidPerks);

                if (changed)
                {
                    ship.SerializedItem = removedRoot ? string.Empty : migratedData;
                    removedItems += serializedRemovedItems;
                    droidPerksMigrated += serializedDroidPerks;
                }

                changed |= MigrateShipModules(ship.Status?.HighPowerModules, ref removedItems, ref droidPerksMigrated);
                changed |= MigrateShipModules(ship.Status?.LowPowerModules, ref removedItems, ref droidPerksMigrated);
                changed |= MigrateShipModules(ship.Status?.ConfigurationModules, ref removedItems, ref droidPerksMigrated);

                if (!changed)
                    continue;

                DB.Set(ship);
                migratedRecords++;
            }

            return removedItems;
        }

        private static bool MigrateShipModules(
            Dictionary<int, ShipStatus.ShipStatusModule> modules,
            ref int removedItems,
            ref int droidPerksMigrated)
        {
            if (modules == null || modules.Count <= 0)
                return false;

            var changed = false;
            foreach (var module in modules.Values)
            {
                var moduleChanged = ObsoleteItemMigration.RemoveObsoleteItemsFromSerializedObject(
                    module.SerializedItem,
                    out var migratedData,
                    out var removedRoot,
                    out var serializedRemovedItems,
                    out var serializedDroidPerks);

                if (!moduleChanged)
                    continue;

                module.SerializedItem = removedRoot ? string.Empty : migratedData;
                removedItems += serializedRemovedItems;
                droidPerksMigrated += serializedDroidPerks;
                changed = true;
            }

            return changed;
        }

        private static bool IsObsoleteItemRecord(string resref, string tag)
        {
            return ObsoleteItemMigration.IsObsoleteResRef(resref) ||
                   ObsoleteItemMigration.IsObsoleteResRef(tag);
        }

        private static int CountRemovedItemStack(int quantity, int serializedRemovedItems)
        {
            return serializedRemovedItems > 0
                ? Math.Max(serializedRemovedItems, quantity)
                : Math.Max(1, quantity);
        }

        private static List<T> SearchAll<T>()
            where T : EntityBase
        {
            var query = new DBQuery<T>();
            var count = (int)DB.SearchCount(query);
            return DB.Search(query.AddPaging(count, 0)).ToList();
        }
    }
}
