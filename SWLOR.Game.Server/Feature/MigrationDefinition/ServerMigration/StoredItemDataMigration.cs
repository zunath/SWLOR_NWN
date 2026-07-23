using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PlayerMarketService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    internal static class StoredItemDataMigration
    {
        public static void Migrate()
        {
            var marketItemCount = CountAll<MarketItem>();

            MigrateMarketCategories(marketItemCount);
            MigrateResearchJobRecipes(CountAll<ResearchJob>());

            var categories = SearchAll<WorldPropertyCategory>();
            var ships = SearchAll<PlayerShip>();
            var researchJobs = SearchAll<ResearchJob>();
            var totalSerializedObjects =
                CountAll<InventoryItem>() +
                marketItemCount +
                categories.Sum(x => x.Items?.Count ?? 0) +
                CountAll<WorldProperty>() +
                researchJobs.Count +
                CountAll<PlayerOutfit>() +
                CountAll<DMCreature>() +
                ships.Count +
                ships.Sum(CountShipModules);

            var progress = new MigrationProgress(totalSerializedObjects, "serialized objects");
            LogProgress($"Starting stored item data migration ({totalSerializedObjects} serialized objects to scan).");

            MigrateInventoryItems(progress);
            MigrateMarketItems(progress);
            MigrateWorldPropertyCategories(categories, progress);
            MigrateEntityItems(SearchAll<WorldProperty>(), "world property structure items", x => x.SerializedItem, (x, value) => x.SerializedItem = value, progress);
            MigrateEntityItems(researchJobs, "research jobs", x => x.SerializedItem, (x, value) => x.SerializedItem = value, progress);
            MigrateEntityItems(SearchAll<PlayerOutfit>(), "player outfits", x => x.Data, (x, value) => x.Data = value, progress);
            MigrateEntityItems(SearchAll<DMCreature>(), "DM creatures", x => x.Data, (x, value) => x.Data = value, progress);
            MigratePlayerShips(ships, progress);

            LogProgress($"Finished stored item data migration ({progress.MigratedCount}/{progress.ProcessedCount} serialized objects changed).");
        }

        private static int CountAll<T>()
            where T : EntityBase
        {
            var query = new DBQuery<T>();
            return (int)DB.SearchCount(query);
        }

        private static List<T> SearchAll<T>()
            where T : EntityBase
        {
            var query = new DBQuery<T>();
            var count = (int)DB.SearchCount(query);
            return DB.Search(query.AddPaging(count, 0)).ToList();
        }

        private static void MigrateMarketCategories(int itemCount)
        {
            var query = new DBQuery<MarketItem>();
            var items = DB.SearchRawJson(query.AddPaging(itemCount, 0));
            var migratedCount = 0;
            var progress = new MigrationProgress(itemCount, "records");
            progress.BeginSection("market category records", itemCount);

            foreach (var rawItem in items)
            {
                var jObject = JObject.Parse(rawItem);
                var categoryToken = jObject[nameof(MarketItem.Category)];

                if (!TryMapMarketCategory(categoryToken, out var category))
                {
                    progress.RecordProcessed(false);
                    continue;
                }

                jObject[nameof(MarketItem.Category)] = (int)category;

                var item = jObject.ToObject<MarketItem>();
                DB.Set(item);
                migratedCount++;
                progress.RecordProcessed(true);
            }

            Log.Write(LogGroup.Migration, $"Migration #22: Migrated market categories for {migratedCount} listings.", true);
            progress.FinishSection($"{migratedCount}/{itemCount} market category records changed.");
        }

        private static bool TryMapMarketCategory(JToken categoryToken, out MarketCategoryType category)
        {
            return TryMapArmorCategory(categoryToken, out category) ||
                   TryMapWeaponCategory(categoryToken, out category);
        }

        private static bool TryMapArmorCategory(JToken categoryToken, out MarketCategoryType category)
        {
            category = MarketCategoryType.Invalid;

            if (categoryToken == null)
                return false;

            return categoryToken.Type == JTokenType.Integer
                ? TryMapArmorCategory(categoryToken.Value<int>(), out category)
                : TryMapArmorCategory(categoryToken.Value<string>(), out category);
        }

        private static bool TryMapArmorCategory(int categoryId, out MarketCategoryType category)
        {
            return TryMapArmorCategory(categoryId.ToString(), out category);
        }

        private static bool TryMapArmorCategory(string categoryNameOrId, out MarketCategoryType category)
        {
            category = MarketCategoryType.Invalid;

            if (string.IsNullOrWhiteSpace(categoryNameOrId))
                return false;

            switch (categoryNameOrId)
            {
                case "16":
                case "Breastplate":
                case "Tunic":
                    category = MarketCategoryType.Armor;
                    return true;
                case "17":
                case "21":
                case "Helmet":
                case "Cap":
                    category = MarketCategoryType.Helmet;
                    return true;
                case "18":
                case "22":
                case "Bracer":
                case "Glove":
                    category = MarketCategoryType.Glove;
                    return true;
                case "19":
                case "23":
                case "Legging":
                case "Boot":
                    category = MarketCategoryType.Boot;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryMapWeaponCategory(JToken categoryToken, out MarketCategoryType category)
        {
            category = MarketCategoryType.Invalid;

            if (categoryToken == null)
                return false;

            if (categoryToken.Type == JTokenType.Integer)
                return TryMapWeaponCategory(categoryToken.Value<int>().ToString(), out category);

            if (categoryToken.Type != JTokenType.String)
                return false;

            return TryMapWeaponCategory(categoryToken.Value<string>(), out category);
        }

        private static bool TryMapWeaponCategory(string categoryNameOrId, out MarketCategoryType category)
        {
            category = MarketCategoryType.Invalid;

            switch (categoryNameOrId)
            {
                case "2":
                case "Fin. Vibroblade":
                case "FinesseVibroblade":
                    category = MarketCategoryType.Vibroknife;
                    return true;
                case "4":
                case "Polearm":
                    category = MarketCategoryType.Spear;
                    return true;
                default:
                    return false;
            }
        }

        private static void MigrateResearchJobRecipes(int jobCount)
        {
            var query = new DBQuery<ResearchJob>();
            var jobs = DB.SearchRawJson(query.AddPaging(jobCount, 0));
            var migratedCount = 0;
            var progress = new MigrationProgress(jobCount, "records");
            progress.BeginSection("research job recipe records", jobCount);

            foreach (var rawJob in jobs)
            {
                var jObject = JObject.Parse(rawJob);
                var recipe = jObject[nameof(ResearchJob.Recipe)];

                if (!DroidBoostRecipeMigration.TryGetReplacementRecipeTypes(recipe, out var newRecipeTypes))
                {
                    progress.RecordProcessed(false);
                    continue;
                }

                jObject[nameof(ResearchJob.Recipe)] = (int)newRecipeTypes[0];
                jObject.Remove("MigratedRecipeTargets");

                var job = jObject.ToObject<ResearchJob>();
                var serializedItems = BuildResearchJobSerializedItems(job.SerializedItem, newRecipeTypes);
                job.SerializedItem = serializedItems[newRecipeTypes[0]];
                DB.Set(job);
                migratedCount++;

                foreach (var recipeType in newRecipeTypes.Skip(1))
                {
                    var clonedJob = new ResearchJob
                    {
                        ParentPropertyId = job.ParentPropertyId,
                        PlayerId = job.PlayerId,
                        DateStarted = job.DateStarted,
                        DateCompleted = job.DateCompleted,
                        SerializedItem = serializedItems[recipeType],
                        Level = job.Level,
                        Recipe = recipeType
                    };

                    DB.Set(clonedJob);
                    migratedCount++;
                }

                progress.RecordProcessed(true);
            }

            Log.Write(LogGroup.Migration, $"Migration #22: Migrated droid boost research jobs for {migratedCount} jobs.", true);
            progress.FinishSection($"{migratedCount} research job records written from {jobCount} source jobs.");
        }

        private static Dictionary<RecipeType, string> BuildResearchJobSerializedItems(
            string serializedItem,
            RecipeType[] recipeTypes)
        {
            var serializedItems = recipeTypes
                .Distinct()
                .ToDictionary(recipeType => recipeType, _ => serializedItem ?? string.Empty);

            if (TryMigrateRootBlueprintVariants(
                    serializedItem,
                    out var blueprintVariants,
                    out var recipeIds,
                    false))
            {
                for (var index = 0; index < blueprintVariants.Count; index++)
                {
                    var recipeType = (RecipeType)recipeIds[index];
                    if (serializedItems.ContainsKey(recipeType))
                        serializedItems[recipeType] = blueprintVariants[index];
                }
            }
            else
            {
                if (MigrateDroidBoostSerializedObject(serializedItem, out var migratedData))
                {
                    foreach (var recipeType in recipeTypes)
                    {
                        serializedItems[recipeType] = migratedData;
                    }
                }
            }

            return serializedItems;
        }

        private static void MigrateInventoryItems(MigrationProgress progress)
        {
            var items = SearchAll<InventoryItem>();
            var migratedCount = 0;
            var removedItems = 0;
            var droidPerksMigrated = 0;
            var saberTokens = 0;
            progress.BeginSection("inventory items", items.Count);

            foreach (var item in items)
            {
                var metadataRootObsolete = IsObsoleteItemRecord(item.Resref, item.Tag);
                if (metadataRootObsolete)
                {
                    var obsoleteResult = MigrateSerializedObject(item.Data);
                    DB.Delete<InventoryItem>(item.Id);
                    removedItems += CountRemovedItemStack(item.Quantity, obsoleteResult.RemovedItems);
                    droidPerksMigrated += obsoleteResult.MigratedDroidPerks;
                    migratedCount++;
                    progress.RecordProcessed(true);
                    continue;
                }

                if (TryMigrateRootBlueprintVariants(item.Data, out var blueprintVariants, out var recipeIds))
                {
                    item.Data = blueprintVariants[0];
                    item.Name = GetBlueprintName(recipeIds[0], item.Name);
                    DB.Set(item);

                    for (var index = 1; index < blueprintVariants.Count; index++)
                    {
                        var clonedItem = new InventoryItem
                        {
                            StorageId = item.StorageId,
                            PlayerId = item.PlayerId,
                            Name = GetBlueprintName(recipeIds[index], item.Name),
                            Tag = item.Tag,
                            Resref = item.Resref,
                            Quantity = item.Quantity,
                            Data = blueprintVariants[index],
                            IconResref = item.IconResref
                        };

                        DB.Set(clonedItem);
                    }

                    migratedCount++;
                    progress.RecordProcessed(true);
                    continue;
                }

                var result = MigrateSerializedObject(item.Data);

                if (result.RemovedRoot)
                {
                    DB.Delete<InventoryItem>(item.Id);
                    removedItems += CountRemovedItemStack(item.Quantity, result.RemovedItems);
                    droidPerksMigrated += result.MigratedDroidPerks;
                    migratedCount++;
                    progress.RecordProcessed(true);
                    continue;
                }

                var migrated = ApplySerializedResult(result, value => item.Data = value);
                migrated |= TryMigrateCombatReadinessName(item.Resref, item.Name, value => item.Name = value);

                if (!migrated)
                {
                    progress.RecordProcessed(false);
                    continue;
                }

                DB.Set(item);
                removedItems += result.RemovedItems;
                droidPerksMigrated += result.MigratedDroidPerks;
                saberTokens += result.NormalizedSabers;
                migratedCount++;
                progress.RecordProcessed(true);
            }

            progress.FinishSection($"{migratedCount}/{items.Count} inventory item records changed. Removed {removedItems} items, migrated {droidPerksMigrated} stored droid perk sets, and normalized {saberTokens} legacy sabers.");
        }

        private static void MigrateMarketItems(MigrationProgress progress)
        {
            var items = SearchAll<MarketItem>();
            var migratedCount = 0;
            var removedItems = 0;
            var droidPerksMigrated = 0;
            var saberTokens = 0;
            progress.BeginSection("market items", items.Count);

            foreach (var item in items)
            {
                var metadataRootObsolete = IsObsoleteItemRecord(item.Resref, item.Tag);
                if (metadataRootObsolete)
                {
                    var obsoleteResult = MigrateSerializedObject(item.Data);
                    DB.Delete<MarketItem>(item.Id);
                    removedItems += CountRemovedItemStack(item.Quantity, obsoleteResult.RemovedItems);
                    droidPerksMigrated += obsoleteResult.MigratedDroidPerks;
                    migratedCount++;
                    progress.RecordProcessed(true);
                    continue;
                }

                if (TryMigrateRootBlueprintVariants(item.Data, out var blueprintVariants, out var recipeIds))
                {
                    item.Data = blueprintVariants[0];
                    item.Name = GetBlueprintName(recipeIds[0], item.Name);
                    DB.Set(item);

                    for (var index = 1; index < blueprintVariants.Count; index++)
                    {
                        var clonedItem = new MarketItem
                        {
                            MarketId = item.MarketId,
                            MarketName = item.MarketName,
                            PlayerId = item.PlayerId,
                            SellerName = item.SellerName,
                            SellerCDKey = item.SellerCDKey,
                            Price = item.Price,
                            IsListed = item.IsListed,
                            Name = GetBlueprintName(recipeIds[index], item.Name),
                            Tag = item.Tag,
                            Resref = item.Resref,
                            Data = blueprintVariants[index],
                            Quantity = item.Quantity,
                            IconResref = item.IconResref,
                            Category = item.Category,
                            DateListed = item.DateListed
                        };

                        DB.Set(clonedItem);
                    }

                    migratedCount++;
                    progress.RecordProcessed(true);
                    continue;
                }

                var result = MigrateSerializedObject(item.Data);

                if (result.RemovedRoot)
                {
                    DB.Delete<MarketItem>(item.Id);
                    removedItems += CountRemovedItemStack(item.Quantity, result.RemovedItems);
                    droidPerksMigrated += result.MigratedDroidPerks;
                    migratedCount++;
                    progress.RecordProcessed(true);
                    continue;
                }

                var migrated = ApplySerializedResult(result, value => item.Data = value);
                migrated |= TryMigrateCombatReadinessName(item.Resref, item.Name, value => item.Name = value);

                if (!migrated)
                {
                    progress.RecordProcessed(false);
                    continue;
                }

                DB.Set(item);
                removedItems += result.RemovedItems;
                droidPerksMigrated += result.MigratedDroidPerks;
                saberTokens += result.NormalizedSabers;
                migratedCount++;
                progress.RecordProcessed(true);
            }

            progress.FinishSection($"{migratedCount}/{items.Count} market item records changed. Removed {removedItems} items, migrated {droidPerksMigrated} stored droid perk sets, and normalized {saberTokens} legacy sabers.");
        }

        private static void MigrateWorldPropertyCategories(
            IReadOnlyCollection<WorldPropertyCategory> categories,
            MigrationProgress progress)
        {
            var categoryItemCount = categories.Sum(x => x.Items?.Count ?? 0);
            var migratedCategoryCount = 0;
            var migratedItemCount = 0;
            var removedItems = 0;
            var droidPerksMigrated = 0;
            var saberTokens = 0;
            progress.BeginSection("world property category storage", categoryItemCount);

            foreach (var category in categories)
            {
                if (category.Items == null)
                    continue;

                var categoryMigrated = false;
                var additionalItems = new Dictionary<string, WorldPropertyItem>();

                foreach (var itemId in category.Items.Keys.ToList())
                {
                    var item = category.Items[itemId];
                    var metadataRootObsolete = IsObsoleteItemRecord(item.Resref, item.Tag);
                    if (metadataRootObsolete)
                    {
                        var obsoleteResult = MigrateSerializedObject(item.Data);
                        category.Items.Remove(itemId);
                        removedItems += CountRemovedItemStack(item.Quantity, obsoleteResult.RemovedItems);
                        droidPerksMigrated += obsoleteResult.MigratedDroidPerks;
                        categoryMigrated = true;
                        migratedItemCount++;
                        progress.RecordProcessed(true);
                        continue;
                    }

                    if (TryMigrateRootBlueprintVariants(item.Data, out var blueprintVariants, out var recipeIds))
                    {
                        item.Data = blueprintVariants[0];
                        item.Name = GetBlueprintName(recipeIds[0], item.Name);

                        for (var index = 1; index < blueprintVariants.Count; index++)
                        {
                            var clonedItem = new WorldPropertyItem
                            {
                                Name = GetBlueprintName(recipeIds[index], item.Name),
                                Tag = item.Tag,
                                Resref = item.Resref,
                                IconResref = item.IconResref,
                                Quantity = item.Quantity,
                                Data = blueprintVariants[index]
                            };

                            additionalItems[Guid.NewGuid().ToString()] = clonedItem;
                        }

                        categoryMigrated = true;
                        migratedItemCount++;
                        progress.RecordProcessed(true);
                        continue;
                    }

                    var result = MigrateSerializedObject(item.Data);

                    if (result.RemovedRoot)
                    {
                        category.Items.Remove(itemId);
                        removedItems += CountRemovedItemStack(item.Quantity, result.RemovedItems);
                        droidPerksMigrated += result.MigratedDroidPerks;
                        categoryMigrated = true;
                        migratedItemCount++;
                        progress.RecordProcessed(true);
                        continue;
                    }

                    var itemMigrated = ApplySerializedResult(result, value => item.Data = value);
                    itemMigrated |= TryMigrateCombatReadinessName(item.Resref, item.Name, value => item.Name = value);

                    if (!itemMigrated)
                    {
                        progress.RecordProcessed(false);
                        continue;
                    }

                    removedItems += result.RemovedItems;
                    droidPerksMigrated += result.MigratedDroidPerks;
                    saberTokens += result.NormalizedSabers;
                    categoryMigrated = true;
                    migratedItemCount++;
                    progress.RecordProcessed(true);
                }

                foreach (var (itemId, item) in additionalItems)
                {
                    category.Items[itemId] = item;
                }

                if (!categoryMigrated)
                    continue;

                migratedCategoryCount++;
                DB.Set(category);
            }

            progress.FinishSection($"{migratedItemCount}/{categoryItemCount} category items changed across {migratedCategoryCount}/{categories.Count} categories. Removed {removedItems} items, migrated {droidPerksMigrated} stored droid perk sets, and normalized {saberTokens} legacy sabers.");
        }

        private static void MigrateEntityItems<T>(
            IReadOnlyCollection<T> entities,
            string sectionName,
            Func<T, string> getSerializedData,
            Action<T, string> setSerializedData,
            MigrationProgress progress)
            where T : EntityBase
        {
            var migratedCount = 0;
            var removedItems = 0;
            var droidPerksMigrated = 0;
            progress.BeginSection(sectionName, entities.Count);

            foreach (var entity in entities)
            {
                var result = MigrateSerializedObject(getSerializedData(entity));
                if (!result.Changed)
                {
                    progress.RecordProcessed(false);
                    continue;
                }

                setSerializedData(entity, result.RemovedRoot ? string.Empty : result.Data);
                DB.Set(entity);
                removedItems += result.RemovedItems;
                droidPerksMigrated += result.MigratedDroidPerks;
                migratedCount++;
                progress.RecordProcessed(true);
            }

            progress.FinishSection($"{migratedCount}/{entities.Count} {sectionName} records changed. Removed {removedItems} items and migrated {droidPerksMigrated} stored droid perk sets.");
        }

        private static void MigratePlayerShips(
            IReadOnlyCollection<PlayerShip> ships,
            MigrationProgress progress)
        {
            var sectionTotal = ships.Count + ships.Sum(CountShipModules);
            var migratedCount = 0;
            var removedItems = 0;
            var droidPerksMigrated = 0;
            progress.BeginSection("player ships", sectionTotal);

            foreach (var ship in ships)
            {
                var migrated = false;
                var result = MigrateSerializedObject(ship.SerializedItem);
                if (result.Changed)
                {
                    ship.SerializedItem = result.RemovedRoot ? string.Empty : result.Data;
                    removedItems += result.RemovedItems;
                    droidPerksMigrated += result.MigratedDroidPerks;
                    migrated = true;
                }

                progress.RecordProcessed(result.Changed);

                migrated |= MigrateShipStatusModuleDictionary(ship.Status?.HighPowerModules, progress, ref removedItems, ref droidPerksMigrated);
                migrated |= MigrateShipStatusModuleDictionary(ship.Status?.LowPowerModules, progress, ref removedItems, ref droidPerksMigrated);
                migrated |= MigrateShipStatusModuleDictionary(ship.Status?.ConfigurationModules, progress, ref removedItems, ref droidPerksMigrated);

                if (!migrated)
                    continue;

                DB.Set(ship);
                migratedCount++;
            }

            progress.FinishSection($"{migratedCount}/{ships.Count} player ship records changed. Removed {removedItems} items and migrated {droidPerksMigrated} stored droid perk sets.");
        }

        private static bool MigrateShipStatusModuleDictionary(
            Dictionary<int, ShipStatus.ShipStatusModule> modules,
            MigrationProgress progress,
            ref int removedItems,
            ref int droidPerksMigrated)
        {
            if (modules == null || modules.Count <= 0)
                return false;

            var migrated = false;
            foreach (var module in modules.Values)
            {
                var result = MigrateSerializedObject(module.SerializedItem);
                if (!result.Changed)
                {
                    progress.RecordProcessed(false);
                    continue;
                }

                module.SerializedItem = result.RemovedRoot ? string.Empty : result.Data;
                removedItems += result.RemovedItems;
                droidPerksMigrated += result.MigratedDroidPerks;
                migrated = true;
                progress.RecordProcessed(true);
            }

            return migrated;
        }

        private static int CountShipModules(PlayerShip ship)
        {
            return CountShipModules(ship.Status?.HighPowerModules) +
                   CountShipModules(ship.Status?.LowPowerModules) +
                   CountShipModules(ship.Status?.ConfigurationModules);
        }

        private static int CountShipModules(Dictionary<int, ShipStatus.ShipStatusModule> modules)
        {
            return modules?.Count ?? 0;
        }

        private static bool ApplySerializedResult(SerializedObjectMigrationResult result, Action<string> setData)
        {
            if (!result.Changed || result.RemovedRoot)
                return false;

            setData(result.Data);
            return true;
        }

        private static SerializedObjectMigrationResult MigrateSerializedObject(string serializedObject)
        {
            var result = new SerializedObjectMigrationResult
            {
                Data = serializedObject
            };

            if (string.IsNullOrWhiteSpace(serializedObject))
                return result;

            var obj = ObjectPlugin.Deserialize(serializedObject);
            if (!GetIsObjectValid(obj))
                return result;

            if (GetObjectType(obj) == ObjectType.Item)
            {
                var resref = GetResRef(obj);
                if (ObsoleteItemMigration.TryGetConversionResRef(resref, out var replacementResRef))
                {
                    DestroyObject(obj);
                    var tempStorage = GetObjectByTag("TEMP_ITEM_STORAGE");
                    var replacement = CreateItemOnObject(replacementResRef, tempStorage);
                    result.Changed = true;
                    result.Data = ObjectPlugin.Serialize(replacement);
                    result.RemovedItems = 1;
                    DestroyObject(replacement);
                    return result;
                }

                if (ObsoleteItemMigration.IsObsoleteResRef(resref))
                {
                    DestroyObject(obj);
                    result.Changed = true;
                    result.RemovedRoot = true;
                    result.RemovedItems = 1;
                    return result;
                }
            }

            result.Changed = MigrateStoredObject(obj, result);
            if (result.Changed)
                result.Data = ObjectPlugin.Serialize(obj);

            DestroyObject(obj);
            return result;
        }

        private static bool MigrateDroidBoostSerializedObject(
            string serializedObject,
            out string migratedSerializedObject)
        {
            migratedSerializedObject = serializedObject;
            if (string.IsNullOrWhiteSpace(serializedObject))
                return false;

            var obj = ObjectPlugin.Deserialize(serializedObject);
            if (!GetIsObjectValid(obj))
                return false;

            var migrated = DroidBoostStoredItemMigration.MigrateObject(obj);
            if (migrated)
                migratedSerializedObject = ObjectPlugin.Serialize(obj);

            DestroyObject(obj);
            return migrated;
        }

        private static bool MigrateStoredObject(uint obj, SerializedObjectMigrationResult result)
        {
            var migrated = false;
            migrated |= EquipmentRequirementMigration.MigrateObject(obj);
            migrated |= DroidBoostStoredItemMigration.MigrateObject(obj);
            migrated |= SerializedItemResistanceMigration.MigrateObject(obj);
            migrated |= SerializedItemWeaponDamageTypeMigration.MigrateObject(obj);
            migrated |= CombatReadinessMigration.MigrateObject(obj);
            migrated |= PistolBaseItemMigration.MigrateStoredObject(obj);
            migrated |= ObsoleteItemMigration.RemoveObsoleteItemsFromObject(
                obj,
                out var removedItems,
                out var migratedDroidPerks);
            migrated |= LegacySaberMigration.MigrateStoredObject(obj, out var normalizedSabers);

            result.RemovedItems += removedItems;
            result.MigratedDroidPerks += migratedDroidPerks;
            result.NormalizedSabers += normalizedSabers;

            return migrated;
        }

        private static bool TryMigrateRootBlueprintVariants(
            string serializedObject,
            out List<string> migratedSerializedObjects,
            out int[] newRecipeIds,
            bool migrateStoredData = true)
        {
            migratedSerializedObjects = null;
            newRecipeIds = null;

            if (string.IsNullOrWhiteSpace(serializedObject))
                return false;

            var obj = ObjectPlugin.Deserialize(serializedObject);
            if (!GetIsObjectValid(obj))
                return false;

            if (GetObjectType(obj) != ObjectType.Item ||
                ObsoleteItemMigration.IsObsoleteResRef(GetResRef(obj)) ||
                !DroidBoostStoredItemMigration.TryGetReplacementBlueprintRecipeIds(obj, out newRecipeIds) ||
                newRecipeIds.Length <= 1)
            {
                DestroyObject(obj);
                return false;
            }

            if (migrateStoredData)
            {
                var result = new SerializedObjectMigrationResult();
                MigrateStoredObject(obj, result);
            }
            else
            {
                DroidBoostStoredItemMigration.MigrateObject(obj);
            }

            migratedSerializedObjects = new List<string>();
            foreach (var newRecipeId in newRecipeIds)
            {
                SetLocalInt(obj, DroidBoostStoredItemMigration.BlueprintRecipeIdVariable, newRecipeId);
                migratedSerializedObjects.Add(ObjectPlugin.Serialize(obj));
            }

            DestroyObject(obj);
            return true;
        }

        private static bool TryMigrateCombatReadinessName(
            string resref,
            string currentName,
            Action<string> setName)
        {
            if (!CombatReadinessMigration.TryGetCombatReadinessItemName(resref, out var combatReadinessName) ||
                currentName == combatReadinessName)
            {
                return false;
            }

            setName(combatReadinessName);
            return true;
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

        private static string GetBlueprintName(int recipeId, string fallback)
        {
            var recipe = Craft.GetRecipe((RecipeType)recipeId);
            return recipe == null
                ? fallback
                : $"Blueprint: {Cache.GetItemNameByResref(recipe.Resref)}";
        }

        private static void LogProgress(string message)
        {
            Log.Write(LogGroup.Migration, $"Migration #22: {message}", true);
        }

        private sealed class SerializedObjectMigrationResult
        {
            public string Data { get; set; }
            public bool Changed { get; set; }
            public bool RemovedRoot { get; set; }
            public int RemovedItems { get; set; }
            public int MigratedDroidPerks { get; set; }
            public int NormalizedSabers { get; set; }
        }

        private sealed class MigrationProgress
        {
            private const int PercentReportStep = 5;
            private const int RecordReportStep = 500;

            private readonly int _totalCount;
            private int _nextPercentReport = PercentReportStep;
            private int _lastRecordReport;
            private string _sectionName;
            private int _sectionTotal;
            private int _sectionProcessed;
            private int _sectionMigrated;

            public int ProcessedCount { get; private set; }
            public int MigratedCount { get; private set; }

            private readonly string _unitName;

            public MigrationProgress(int totalCount, string unitName)
            {
                _totalCount = totalCount;
                _unitName = unitName;
            }

            public void BeginSection(string sectionName, int sectionTotal)
            {
                _sectionName = sectionName;
                _sectionTotal = sectionTotal;
                _sectionProcessed = 0;
                _sectionMigrated = 0;

                LogProgress($"Scanning {sectionName} ({sectionTotal} {_unitName}). {BuildOverallProgressText()}");
            }

            public void RecordProcessed(bool migrated)
            {
                ProcessedCount++;
                _sectionProcessed++;

                if (migrated)
                {
                    MigratedCount++;
                    _sectionMigrated++;
                }

                if (ShouldReportProgress())
                    ReportProgress();
            }

            public void FinishSection(string details)
            {
                LogProgress($"Finished {_sectionName}. {details} {BuildOverallProgressText()}");
            }

            private bool ShouldReportProgress()
            {
                if (_totalCount <= 0)
                    return false;

                var percent = GetOverallPercent();
                if (percent >= _nextPercentReport)
                {
                    while (_nextPercentReport <= percent)
                    {
                        _nextPercentReport += PercentReportStep;
                    }

                    return true;
                }

                if (ProcessedCount - _lastRecordReport < RecordReportStep)
                    return false;

                _lastRecordReport = ProcessedCount;
                return true;
            }

            private void ReportProgress()
            {
                _lastRecordReport = ProcessedCount;
                LogProgress(
                    $"{BuildOverallProgressText()} Current surface: {_sectionName} {_sectionProcessed}/{_sectionTotal} ({GetSectionPercent():0.0}%), {_sectionMigrated} changed.");
            }

            private string BuildOverallProgressText()
            {
                return _totalCount <= 0
                    ? $"Current migration progress: 0/0 {_unitName} (100.0%)."
                    : $"Current migration progress: {ProcessedCount}/{_totalCount} {_unitName} ({GetOverallPercent():0.0}%), {MigratedCount} changed.";
            }

            private double GetOverallPercent()
            {
                return _totalCount <= 0
                    ? 100.0
                    : ProcessedCount * 100.0 / _totalCount;
            }

            private double GetSectionPercent()
            {
                return _sectionTotal <= 0
                    ? 100.0
                    : _sectionProcessed * 100.0 / _sectionTotal;
            }
        }

        private static class DroidBoostStoredItemMigration
        {
            public const string BlueprintRecipeIdVariable = "BLUEPRINT_RECIPE_ID";

            private static readonly Dictionary<int, int[]> LegacyDroidStatMap = new()
            {
                { 12, new[] { 115, 116, 117 } },
                { 13, new[] { 118, 119, 120, 121 } },
                { 14, new[] { 122, 123 } },
                { 15, new[] { 124, 125, 126 } },
            };

            private static readonly Dictionary<int, int[]> LegacyDroidEnhancementMap = new()
            {
                { 111, new[] { 115, 116, 117 } },
                { 112, new[] { 118, 119, 120, 121 } },
                { 113, new[] { 122, 123 } },
                { 114, new[] { 124, 125, 126 } },
            };

            public static bool MigrateObject(uint obj)
            {
                if (!GetIsObjectValid(obj))
                    return false;

                var wasMigrated = false;
                if (GetObjectType(obj) == ObjectType.Item)
                    wasMigrated |= MigrateItem(obj);

                if (GetHasInventory(obj))
                {
                    for (var item = GetFirstItemInInventory(obj); GetIsObjectValid(item); item = GetNextItemInInventory(obj))
                    {
                        wasMigrated |= MigrateObject(item);
                    }
                }

                return wasMigrated;
            }

            private static bool MigrateItem(uint item)
            {
                var wasMigrated = MigrateRecipeLocalVariable(item);
                wasMigrated |= MigrateBlueprintRecipeLocalVariable(item);
                wasMigrated |= MigrateConstructedDroidLocalVariable(item);
                var replacements = new List<(ItemProperty Property, ItemPropertyType Type, int Value, int[] SubTypes)>();
                var tier = 0;
                var hasArmorStat = false;
                var isDroidCpu = false;

                for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                {
                    var propertyType = GetItemPropertyType(ip);
                    var subType = GetItemPropertySubType(ip);
                    var value = GetItemPropertyCostTableValue(ip);

                    if (propertyType == ItemPropertyType.DroidStat)
                    {
                        if (subType == (int)DroidStatSubType.Tier)
                            tier = value;
                        else if (subType == (int)DroidStatSubType.Armor)
                            hasArmorStat = true;

                        if (LegacyDroidStatMap.TryGetValue(subType, out var droidStatSubTypes))
                            replacements.Add((ip, propertyType, value, droidStatSubTypes));
                    }
                    else if (propertyType == ItemPropertyType.DroidEnhancement &&
                             LegacyDroidEnhancementMap.TryGetValue(subType, out var enhancementSubTypes))
                    {
                        replacements.Add((ip, propertyType, value, enhancementSubTypes));
                    }
                    else if (propertyType == ItemPropertyType.DroidPart &&
                             subType == (int)DroidPartItemPropertySubType.CPU)
                    {
                        isDroidCpu = true;
                    }
                }

                foreach (var (property, _, _, _) in replacements)
                {
                    RemoveItemProperty(item, property);
                }

                foreach (var (_, propertyType, value, subTypes) in replacements)
                {
                    foreach (var subType in subTypes)
                    {
                        AddItemProperty(DurationType.Permanent, ItemPropertyCustom(propertyType, subType, value), item);
                    }
                }

                var isConstructedDroidController =
                    GetResRef(item) == Droid.DroidControlItemResref &&
                    !string.IsNullOrWhiteSpace(GetLocalString(item, "CONSTRUCTED_DROID"));
                var addedArmorStat = AddArmorSkillRankIfNeeded(item, isDroidCpu || isConstructedDroidController, hasArmorStat, tier);

                return wasMigrated || replacements.Count > 0 || addedArmorStat;
            }

            private static bool AddArmorSkillRankIfNeeded(uint item, bool isDroidStatSource, bool hasArmorStat, int tier)
            {
                if (!isDroidStatSource || hasArmorStat)
                    return false;

                var armorRank = GetTierArmorSkillRank(tier);
                if (armorRank <= 0)
                    return false;

                AddItemProperty(
                    DurationType.Permanent,
                    ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Armor, armorRank),
                    item);
                return true;
            }

            private static int GetTierArmorSkillRank(int tier)
            {
                return tier switch
                {
                    1 => 5,
                    2 => 15,
                    3 => 25,
                    4 => 35,
                    5 => 45,
                    _ => 0
                };
            }

            private static bool MigrateConstructedDroidLocalVariable(uint item)
            {
                var serialized = GetLocalString(item, "CONSTRUCTED_DROID");
                if (string.IsNullOrWhiteSpace(serialized))
                    return false;

                var droid = JsonConvert.DeserializeObject<ConstructedDroid>(serialized);
                if (droid == null)
                    return false;

                var migrated = false;
                migrated |= MigrateSerializedObjectField(droid.SerializedCPU, value => droid.SerializedCPU = value);
                migrated |= MigrateSerializedObjectField(droid.SerializedHead, value => droid.SerializedHead = value);
                migrated |= MigrateSerializedObjectField(droid.SerializedBody, value => droid.SerializedBody = value);
                migrated |= MigrateSerializedObjectField(droid.SerializedArms, value => droid.SerializedArms = value);
                migrated |= MigrateSerializedObjectField(droid.SerializedLegs, value => droid.SerializedLegs = value);

                if (droid.EquippedItems != null)
                {
                    foreach (var key in droid.EquippedItems.Keys.ToList())
                    {
                        var value = droid.EquippedItems[key];
                        if (!MigrateSerializedObject(value, out var migratedValue))
                            continue;

                        droid.EquippedItems[key] = migratedValue;
                        migrated = true;
                    }
                }

                if (droid.Inventory != null)
                {
                    foreach (var key in droid.Inventory.Keys.ToList())
                    {
                        var value = droid.Inventory[key];
                        if (!MigrateSerializedObject(value, out var migratedValue))
                            continue;

                        droid.Inventory[key] = migratedValue;
                        migrated = true;
                    }
                }

                if (!migrated)
                    return false;

                SetLocalString(item, "CONSTRUCTED_DROID", JsonConvert.SerializeObject(droid));
                return true;
            }

            private static bool MigrateSerializedObjectField(string serializedObject, Action<string> setSerializedObject)
            {
                if (!MigrateSerializedObject(serializedObject, out var migratedSerializedObject))
                    return false;

                setSerializedObject(migratedSerializedObject);
                return true;
            }

            private static bool MigrateSerializedObject(string serializedObject, out string migratedSerializedObject)
            {
                migratedSerializedObject = serializedObject;
                if (string.IsNullOrWhiteSpace(serializedObject))
                    return false;

                var obj = ObjectPlugin.Deserialize(serializedObject);
                if (!GetIsObjectValid(obj))
                    return false;

                var wasMigrated = EquipmentRequirementMigration.MigrateObject(obj);
                wasMigrated |= MigrateObject(obj);
                if (wasMigrated)
                    migratedSerializedObject = ObjectPlugin.Serialize(obj);

                DestroyObject(obj);
                return wasMigrated;
            }

            private static bool MigrateRecipeLocalVariable(uint item)
            {
                var recipeList = GetLocalString(item, "RECIPES");
                if (string.IsNullOrWhiteSpace(recipeList))
                    return false;

                var migrated = false;
                var newRecipeIds = new List<int>();

                foreach (var recipeId in recipeList.Split(','))
                {
                    if (!int.TryParse(recipeId, out var parsedId) ||
                        !DroidBoostRecipeMigration.TryGetReplacementRecipeNames(parsedId.ToString(), out var newRecipeNames))
                    {
                        if (int.TryParse(recipeId, out parsedId))
                            newRecipeIds.Add(parsedId);
                        continue;
                    }

                    foreach (var newRecipeName in newRecipeNames)
                    {
                        newRecipeIds.AddRange(DroidBoostRecipeMigration.GetReplacementRecipeIds(new[] { newRecipeName }));
                    }

                    migrated = true;
                }

                if (!migrated)
                    return false;

                SetLocalString(item, "RECIPES", string.Join(",", newRecipeIds.Distinct()));
                return true;
            }

            private static bool MigrateBlueprintRecipeLocalVariable(uint item)
            {
                if (!TryGetReplacementBlueprintRecipeIds(item, out var newRecipeIds))
                    return false;

                SetLocalInt(item, BlueprintRecipeIdVariable, newRecipeIds[0]);
                return true;
            }

            public static bool TryGetReplacementBlueprintRecipeIds(uint item, out int[] newRecipeIds)
            {
                newRecipeIds = null;

                var recipeId = GetLocalInt(item, BlueprintRecipeIdVariable);
                if (recipeId <= 0 ||
                    !DroidBoostRecipeMigration.TryGetReplacementRecipeNames(recipeId.ToString(), out var newRecipeNames))
                {
                    return false;
                }

                newRecipeIds = DroidBoostRecipeMigration.GetReplacementRecipeIds(newRecipeNames).ToArray();
                return newRecipeIds.Length > 0;
            }
        }
    }
}
