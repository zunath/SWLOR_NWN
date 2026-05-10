using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _25_RenameDroidBoostRecipes : ServerMigrationBase, IServerMigration
    {
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

        private const string BlueprintRecipeIdVariable = "BLUEPRINT_RECIPE_ID";

        public int Version => 25;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

        public void Migrate()
        {
            MigratePlayers();
            MigrateResearchJobs();
            MigrateSerializedItems();
        }

        private static List<T> SearchAll<T>()
            where T : EntityBase
        {
            var query = new DBQuery<T>();
            var count = (int)DB.SearchCount(query);
            return DB.Search(query.AddPaging(count, 0)).ToList();
        }

        private static void MigratePlayers()
        {
            var query = new DBQuery<Player>();
            var count = (int)DB.SearchCount(query);
            var players = DB.SearchRawJson(query.AddPaging(count, 0));
            var droidRecipeCount = 0;
            var weaponBlueprintCount = 0;
            var weaponBlueprintRefundTotal = 0;

            foreach (var rawPlayer in players)
            {
                var jObject = JObject.Parse(rawPlayer);
                var migratedDroidRecipes = DroidBoostRecipeMigration.ExpandPlayerRecipeDictionaries(jObject);
                var migratedWeaponBlueprints = WeaponBlueprintPerkMigration.CollapsePlayerPerks(jObject, out var weaponBlueprintRefundAmount);

                if (!migratedDroidRecipes && !migratedWeaponBlueprints)
                    continue;

                var player = jObject.ToObject<Player>();
                if (weaponBlueprintRefundAmount > 0)
                {
                    player.UnallocatedSP += weaponBlueprintRefundAmount;
                    weaponBlueprintRefundTotal += weaponBlueprintRefundAmount;
                }

                DB.Set(player);

                if (migratedDroidRecipes)
                    droidRecipeCount++;

                if (migratedWeaponBlueprints)
                    weaponBlueprintCount++;
            }

            Log.Write(LogGroup.Migration, $"Migrated droid boost recipe unlocks for {droidRecipeCount} players and collapsed weapon blueprint perks for {weaponBlueprintCount} players. Refunded {weaponBlueprintRefundTotal} SP from duplicate legacy weapon blueprint perks.");
        }

        private static void MigrateResearchJobs()
        {
            var query = new DBQuery<ResearchJob>();
            var count = (int)DB.SearchCount(query);
            var jobs = DB.SearchRawJson(query.AddPaging(count, 0));
            var migratedCount = 0;

            foreach (var rawJob in jobs)
            {
                var jObject = JObject.Parse(rawJob);
                var recipe = jObject[nameof(ResearchJob.Recipe)];

                if (!DroidBoostRecipeMigration.TryGetReplacementRecipeTypes(recipe, out var newRecipeTypes))
                    continue;

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
            }

            Log.Write(LogGroup.Migration, $"Migrated droid boost research jobs for {migratedCount} jobs.");
        }

        private static Dictionary<RecipeType, string> BuildResearchJobSerializedItems(
            string serializedItem,
            RecipeType[] recipeTypes)
        {
            var serializedItems = recipeTypes
                .Distinct()
                .ToDictionary(recipeType => recipeType, _ => serializedItem ?? string.Empty);

            if (TryMigrateRootBlueprintVariants(serializedItem, out var blueprintVariants, out var recipeIds))
            {
                for (var index = 0; index < blueprintVariants.Count; index++)
                {
                    var recipeType = (RecipeType)recipeIds[index];
                    if (serializedItems.ContainsKey(recipeType))
                        serializedItems[recipeType] = blueprintVariants[index];
                }
            }
            else if (MigrateSerializedObject(serializedItem, out var migratedData))
            {
                foreach (var recipeType in recipeTypes)
                {
                    serializedItems[recipeType] = migratedData;
                }
            }

            return serializedItems;
        }

        private static void MigrateSerializedItems()
        {
            MigrateInventoryItems();
            MigrateMarketItems();
            MigrateEntityItems(SearchAll<PlayerOutfit>(), item => item.Data, (item, data) => item.Data = data);
            MigrateEntityItems(SearchAll<DMCreature>(), item => item.Data, (item, data) => item.Data = data);
            MigrateEntityItems(SearchAll<WorldProperty>(), item => item.SerializedItem, (item, data) => item.SerializedItem = data);
            MigrateEntityItems(SearchAll<ResearchJob>(), item => item.SerializedItem, (item, data) => item.SerializedItem = data);
            MigrateEntityItems(SearchAll<PlayerShip>(), item => item.SerializedItem, (item, data) => item.SerializedItem = data);

            foreach (var category in SearchAll<WorldPropertyCategory>())
            {
                if (category.Items == null)
                    continue;

                var migrated = false;
                var additionalItems = new Dictionary<string, WorldPropertyItem>();

                foreach (var (_, item) in category.Items.ToList())
                {
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

                            additionalItems[System.Guid.NewGuid().ToString()] = clonedItem;
                        }

                        migrated = true;
                        continue;
                    }

                    if (!MigrateSerializedObject(item.Data, out var migratedData))
                        continue;

                    item.Data = migratedData;
                    migrated = true;
                }

                foreach (var (itemId, item) in additionalItems)
                {
                    category.Items[itemId] = item;
                }

                if (migrated)
                    DB.Set(category);
            }
        }

        private static void MigrateInventoryItems()
        {
            foreach (var item in SearchAll<InventoryItem>())
            {
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

                    continue;
                }

                if (!MigrateSerializedObject(item.Data, out var migratedData))
                    continue;

                item.Data = migratedData;
                DB.Set(item);
            }
        }

        private static void MigrateMarketItems()
        {
            foreach (var item in SearchAll<MarketItem>())
            {
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

                    continue;
                }

                if (!MigrateSerializedObject(item.Data, out var migratedData))
                    continue;

                item.Data = migratedData;
                DB.Set(item);
            }
        }

        private static void MigrateEntityItems<T>(
            IEnumerable<T> entities,
            System.Func<T, string> getSerializedData,
            System.Action<T, string> setSerializedData)
            where T : EntityBase
        {
            foreach (var entity in entities)
            {
                if (!MigrateSerializedObject(getSerializedData(entity), out var migratedData))
                    continue;

                setSerializedData(entity, migratedData);
                DB.Set(entity);
            }
        }

        private static bool MigrateSerializedObject(string serializedObject, out string migratedSerializedObject)
        {
            migratedSerializedObject = serializedObject;
            if (string.IsNullOrWhiteSpace(serializedObject))
                return false;

            var obj = ObjectPlugin.Deserialize(serializedObject);
            if (!GetIsObjectValid(obj))
                return false;

            var wasMigrated = MigrateObject(obj);
            if (wasMigrated)
                migratedSerializedObject = ObjectPlugin.Serialize(obj);

            DestroyObject(obj);
            return wasMigrated;
        }

        private static bool MigrateObject(uint obj)
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

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var propertyType = GetItemPropertyType(ip);
                var subType = GetItemPropertySubType(ip);
                var value = GetItemPropertyCostTableValue(ip);

                if (propertyType == ItemPropertyType.DroidStat &&
                    LegacyDroidStatMap.TryGetValue(subType, out var droidStatSubTypes))
                {
                    replacements.Add((ip, propertyType, value, droidStatSubTypes));
                }
                else if (propertyType == ItemPropertyType.DroidEnhancement &&
                         LegacyDroidEnhancementMap.TryGetValue(subType, out var enhancementSubTypes))
                {
                    replacements.Add((ip, propertyType, value, enhancementSubTypes));
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

            return wasMigrated || replacements.Count > 0;
        }

        private static bool MigrateConstructedDroidLocalVariable(uint item)
        {
            var serialized = GetLocalString(item, "CONSTRUCTED_DROID");
            if (string.IsNullOrWhiteSpace(serialized))
                return false;

            var droid = Newtonsoft.Json.JsonConvert.DeserializeObject<ConstructedDroid>(serialized);
            if (droid == null)
                return false;

            var migrated = false;
            migrated |= MigrateSerializedObjectField(droid.SerializedCPU, value => droid.SerializedCPU = value);
            migrated |= MigrateSerializedObjectField(droid.SerializedHead, value => droid.SerializedHead = value);
            migrated |= MigrateSerializedObjectField(droid.SerializedBody, value => droid.SerializedBody = value);
            migrated |= MigrateSerializedObjectField(droid.SerializedArms, value => droid.SerializedArms = value);
            migrated |= MigrateSerializedObjectField(droid.SerializedLegs, value => droid.SerializedLegs = value);

            foreach (var key in droid.EquippedItems.Keys.ToList())
            {
                var value = droid.EquippedItems[key];
                if (!MigrateSerializedObject(value, out var migratedValue))
                    continue;

                droid.EquippedItems[key] = migratedValue;
                migrated = true;
            }

            foreach (var key in droid.Inventory.Keys.ToList())
            {
                var value = droid.Inventory[key];
                if (!MigrateSerializedObject(value, out var migratedValue))
                    continue;

                droid.Inventory[key] = migratedValue;
                migrated = true;
            }

            if (!migrated)
                return false;

            SetLocalString(item, "CONSTRUCTED_DROID", Newtonsoft.Json.JsonConvert.SerializeObject(droid));
            return true;
        }

        private static bool MigrateSerializedObjectField(string serializedObject, System.Action<string> setSerializedObject)
        {
            if (!MigrateSerializedObject(serializedObject, out var migratedSerializedObject))
                return false;

            setSerializedObject(migratedSerializedObject);
            return true;
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

        private static bool TryMigrateRootBlueprintVariants(
            string serializedObject,
            out List<string> migratedSerializedObjects,
            out int[] newRecipeIds)
        {
            migratedSerializedObjects = null;
            newRecipeIds = null;

            if (string.IsNullOrWhiteSpace(serializedObject))
                return false;

            var obj = ObjectPlugin.Deserialize(serializedObject);
            if (!GetIsObjectValid(obj))
                return false;

            if (GetObjectType(obj) != ObjectType.Item ||
                !TryGetReplacementBlueprintRecipeIds(obj, out newRecipeIds) ||
                newRecipeIds.Length <= 1)
            {
                DestroyObject(obj);
                return false;
            }

            MigrateObject(obj);

            migratedSerializedObjects = new List<string>();
            foreach (var newRecipeId in newRecipeIds)
            {
                SetLocalInt(obj, BlueprintRecipeIdVariable, newRecipeId);
                migratedSerializedObjects.Add(ObjectPlugin.Serialize(obj));
            }

            DestroyObject(obj);
            return true;
        }

        private static bool TryGetReplacementBlueprintRecipeIds(uint item, out int[] newRecipeIds)
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

        private static string GetBlueprintName(int recipeId, string fallback)
        {
            var recipe = Craft.GetRecipe((RecipeType)recipeId);
            return recipe == null
                ? fallback
                : $"Blueprint: {Cache.GetItemNameByResref(recipe.Resref)}";
        }
    }
}
