using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PlayerMarketService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static partial class MigrationEngineTests
    {
        [EngineTest("Migration persists blueprint alternatives and resumes partial bank market and research writes", Category = "Migration")]
        public static async Task PersistedBlueprintRetries(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var blueprint = await CreateItemAsync(ctx, owner, "blueprint", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () => SetLocalInt(blueprint, "BLUEPRINT_RECIPE_ID", 3488));
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var data = ObjectPlugin.Serialize(blueprint);
                var date = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var bank = new InventoryItem
                {
                    PlayerId = "migration-owner", StorageId = "migration-bank", Name = "Legacy blueprint",
                    Resref = "blueprint", Tag = "blueprint", Quantity = 7, IconResref = "test-icon", Data = data
                };
                var market = new MarketItem
                {
                    PlayerId = "migration-owner", MarketId = "migration-market", MarketName = "Test market",
                    SellerName = "Test seller", SellerCDKey = "test-key", Price = 1234, IsListed = true, DateListed = date,
                    Category = MarketCategoryType.Invalid, Name = "Legacy blueprint", Resref = "blueprint", Tag = "blueprint",
                    Quantity = 9, IconResref = "test-icon", Data = data
                };
                var job = new ResearchJob
                {
                    ParentPropertyId = "migration-lab", PlayerId = "migration-owner", DateStarted = date,
                    DateCompleted = date.AddDays(2), Level = 3, Recipe = (RecipeType)3488, SerializedItem = data
                };
                var category = new WorldPropertyCategory
                {
                    ParentPropertyId = "migration-property", Name = "Test storage",
                    Items = { ["original"] = new WorldPropertyItem
                    {
                        Name = "Legacy blueprint", Resref = "blueprint", Tag = "blueprint",
                        Quantity = 11, IconResref = "test-icon", Data = data
                    } }
                };
                var originals = new[] { JObject.FromObject(bank), JObject.FromObject(market), JObject.FromObject(job) };
                var bankCount = DB.SearchCount(new DBQuery<InventoryItem>());
                var marketCount = DB.SearchCount(new DBQuery<MarketItem>());
                var jobCount = DB.SearchCount(new DBQuery<ResearchJob>());
                try
                {
                    DB.Set(bank);
                    DB.Set(market);
                    DB.Set(job);
                    DB.Set(category);
                    RunStoredMigration();
                    AssertRecords();
                    var categoryKeys = ReadPersisted<WorldPropertyCategory>(category.Id).Items.Keys.OrderBy(key => key).ToArray();

                    // Recreate an interrupted expansion: one alternative persisted, the
                    // other did not, and the source still contains its legacy recipe.
                    DB.Delete<InventoryItem>($"{bank.Id}-recipe-4801");
                    DB.Delete<MarketItem>($"{market.Id}-recipe-4801");
                    DB.Delete<ResearchJob>($"{job.Id}-recipe-4801");
                    DB.Set(originals[0].ToObject<InventoryItem>());
                    DB.Set(originals[1].ToObject<MarketItem>());
                    DB.Set(originals[2].ToObject<ResearchJob>());
                    RunStoredMigration();
                    AssertRecords();
                    RunStoredMigration();
                    AssertRecords();
                    ctx.Assert(categoryKeys.SequenceEqual(ReadPersisted<WorldPropertyCategory>(category.Id).Items.Keys.OrderBy(key => key)),
                        "Retry must preserve the property storage item identities");
                }
                finally
                {
                    foreach (var suffix in new[] { "", "-recipe-4800", "-recipe-4801" })
                    {
                        DB.Delete<InventoryItem>(bank.Id + suffix);
                        DB.Delete<MarketItem>(market.Id + suffix);
                        DB.Delete<ResearchJob>(job.Id + suffix);
                    }
                    DB.Delete<WorldPropertyCategory>(category.Id);
                }

                void AssertRecords()
                {
                    ctx.AssertEqual(bankCount + 3, DB.SearchCount(new DBQuery<InventoryItem>()), "Exactly three bank variants");
                    ctx.AssertEqual(marketCount + 3, DB.SearchCount(new DBQuery<MarketItem>()), "Exactly three market variants");
                    ctx.AssertEqual(jobCount + 3, DB.SearchCount(new DBQuery<ResearchJob>()), "Exactly three research variants");
                    foreach (var recipe in new[] { 4799, 4800, 4801 })
                    {
                        var suffix = recipe == 4799 ? "" : $"-recipe-{recipe}";
                        var savedBank = ReadPersisted<InventoryItem>(bank.Id + suffix);
                        var savedMarket = ReadPersisted<MarketItem>(market.Id + suffix);
                        var savedJob = ReadPersisted<ResearchJob>(job.Id + suffix);
                        AssertPreservedMetadata(ctx, originals[0], savedBank, "Id", "Name", "Data");
                        AssertPreservedMetadata(ctx, originals[1], savedMarket, "Id", "Name", "Data");
                        AssertPreservedMetadata(ctx, originals[2], savedJob, "Id", "Recipe", "SerializedItem");
                        AssertBlueprint(ctx, savedBank.Data, recipe, savedBank.Name);
                        AssertBlueprint(ctx, savedMarket.Data, recipe, savedMarket.Name);
                        AssertBlueprint(ctx, savedJob.SerializedItem, recipe);
                        ctx.AssertEqual((RecipeType)recipe, savedJob.Recipe, "Research target agrees with its serialized blueprint");
                    }
                    var savedCategory = ReadPersisted<WorldPropertyCategory>(category.Id);
                    ctx.AssertEqual(3, savedCategory.Items.Count, "All property storage alternatives");
                    var recipes = new List<int>();
                    foreach (var item in savedCategory.Items.Values)
                    {
                        var nativeItem = Deserialize(ctx, item.Data);
                        recipes.Add(GetLocalInt(nativeItem, "BLUEPRINT_RECIPE_ID"));
                        ctx.AssertEqual(11, item.Quantity, "Property stack quantity");
                        ctx.AssertEqual(GetName(nativeItem), item.Name, "Property blueprint display name");
                    }
                    ctx.Assert(recipes.OrderBy(id => id).SequenceEqual(new[] { 4799, 4800, 4801 }), "Distinct property recipes");
                }
            });
        }

        [EngineTest("Migration updates persisted property outfit creature and every ship module surface", Category = "Migration")]
        public static async Task PersistedItemSurfaces(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var kit = await CreateItemAsync(ctx, owner, "saber_upg1", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var data = ObjectPlugin.Serialize(kit);
                var property = new WorldProperty { SerializedItem = data };
                var outfit = new PlayerOutfit { PlayerId = "migration-owner", Name = "Test outfit", Data = data };
                var creature = new DMCreature("Test creature", "migration-creature", ObjectPlugin.Serialize(owner));
                var ship = new PlayerShip
                {
                    OwnerPlayerId = "migration-owner", PropertyId = property.Id, SerializedItem = data,
                    Status = new ShipStatus
                    {
                        HighPowerModules = { [1] = new ShipStatus.ShipStatusModule { SerializedItem = data, ModuleBonus = 7 } },
                        LowPowerModules = { [2] = new ShipStatus.ShipStatusModule { SerializedItem = data, ModuleBonus = 8 } },
                        ConfigurationModules = { [3] = new ShipStatus.ShipStatusModule { SerializedItem = data, ModuleBonus = 9 } }
                    }
                };
                var emptyShip = new PlayerShip { OwnerPlayerId = "migration-owner", SerializedItem = "", Status = null };
                try
                {
                    DB.Set(property);
                    DB.Set(outfit);
                    DB.Set(creature);
                    DB.Set(ship);
                    DB.Set(emptyShip);
                    for (var run = 0; run < 2; run++)
                    {
                        RunStoredMigration();
                        AssertKit(ReadPersisted<WorldProperty>(property.Id).SerializedItem, "Property structure");
                        AssertKit(ReadPersisted<PlayerOutfit>(outfit.Id).Data, "Outfit");
                        var savedCreature = Deserialize(ctx, ReadPersisted<DMCreature>(creature.Id).Data);
                        var contents = new List<string>();
                        for (var item = GetFirstItemInInventory(savedCreature); GetIsObjectValid(item); item = GetNextItemInInventory(savedCreature))
                            contents.Add(GetResRef(item));
                        ctx.Assert(contents.SequenceEqual(new[] { "saber_upg2" }), "Stored creature inventory has exactly the replacement kit");
                        var savedShip = ReadPersisted<PlayerShip>(ship.Id);
                        AssertKit(savedShip.SerializedItem, "Ship root");
                        AssertKit(savedShip.Status.HighPowerModules[1].SerializedItem, "High power module");
                        AssertKit(savedShip.Status.LowPowerModules[2].SerializedItem, "Low power module");
                        AssertKit(savedShip.Status.ConfigurationModules[3].SerializedItem, "Configuration module");
                        ctx.AssertEqual(7, savedShip.Status.HighPowerModules[1].ModuleBonus, "Unrelated module state");
                        ctx.Assert(ReadPersisted<PlayerShip>(emptyShip.Id).Status == null, "Ship without module state remains valid");
                    }
                }
                finally
                {
                    DB.Delete<WorldProperty>(property.Id);
                    DB.Delete<PlayerOutfit>(outfit.Id);
                    DB.Delete<DMCreature>(creature.Id);
                    DB.Delete<PlayerShip>(ship.Id);
                    DB.Delete<PlayerShip>(emptyShip.Id);
                }

                void AssertKit(string serialized, string surface) =>
                    ctx.AssertEqual("saber_upg2", GetResRef(Deserialize(ctx, serialized)), surface);
            });
        }

        [EngineTest("Migration rejects corrupt stored items without overwriting their records", Category = "Migration")]
        public static async Task CorruptStoredItem(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var record = new InventoryItem { PlayerId = "migration-owner", StorageId = "migration-bank", Data = "invalid serialized object", Quantity = 7 };
                try
                {
                    DB.Set(record);
                    var before = DB.GetRawJson<InventoryItem>(record.Id);
                    var failed = false;
                    try { RunStoredMigration(); }
                    catch (TargetInvocationException) { failed = true; }
                    ctx.Assert(failed, "Corrupt data must fail the migration instead of saving a missing item");
                    ctx.AssertEqual(before, DB.GetRawJson<InventoryItem>(record.Id), "Original corrupt record is preserved for repair");
                }
                finally { DB.Delete<InventoryItem>(record.Id); }
            });
        }

        [EngineTest("Migration replaces matching permanent properties without removing other subtypes or temporary bonuses", Category = "Migration")]
        public static async Task PropertyReplacementPreservesOtherBonuses(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var item = await CreateItemAsync(ctx, owner, "b_longsword", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ClearProperties(item);
                AddItemProperty(DurationType.Permanent, ItemPropertyAbilityBonus(AbilityType.Might, 2), item);
                AddItemProperty(DurationType.Permanent, ItemPropertyAbilityBonus(AbilityType.Perception, 3), item);
                AddItemProperty(DurationType.Temporary, ItemPropertyAbilityBonus(AbilityType.Might, 1), item, 600);
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                for (var run = 0; run < 2; run++)
                {
                    Invoke("MigrationObject", "AddProperty", item, ItemPropertyAbilityBonus(AbilityType.Might, 4), AddItemPropertyPolicy.ReplaceExisting);
                    var bonuses = new List<(int Subtype, int Value, DurationType Duration)>();
                    for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                        if (GetItemPropertyType(ip) == ItemPropertyType.AbilityBonus)
                            bonuses.Add((GetItemPropertySubType(ip), GetItemPropertyCostTableValue(ip), GetItemPropertyDurationType(ip)));
                    ctx.AssertEqual(3, bonuses.Count, "Replacement must not duplicate or discard unrelated bonuses");
                    ctx.Assert(bonuses.Contains(((int)AbilityType.Might, 4, DurationType.Permanent)), "New permanent bonus");
                    ctx.Assert(bonuses.Contains(((int)AbilityType.Perception, 3, DurationType.Permanent)), "Other subtype survives");
                    ctx.Assert(bonuses.Contains(((int)AbilityType.Might, 1, DurationType.Temporary)), "Temporary bonus survives");
                }
            });
        }

        private static void RunStoredMigration() => Invoke("ServerMigration.StoredItemDataMigration", "Migrate");

        // Read Redis directly so these assertions cannot pass on a mutated cache object
        // whose database write was omitted.
        private static T ReadPersisted<T>(string id) where T : EntityBase =>
            JsonConvert.DeserializeObject<T>(DB.GetRawJson<T>(id));

        private static void AssertBlueprint(EngineTestContext ctx, string data, int recipe, string name = null)
        {
            var item = Deserialize(ctx, data);
            ctx.AssertEqual(recipe, GetLocalInt(item, "BLUEPRINT_RECIPE_ID"), "Persisted blueprint recipe");
            var definition = Craft.GetRecipe((RecipeType)recipe);
            ctx.Assert(definition != null, "Replacement recipe must be loaded");
            ctx.AssertEqual($"Blueprint: {Cache.GetItemNameByResref(definition.Resref)}", GetName(item), "Recipe-specific blueprint name");
            if (name != null)
                ctx.AssertEqual(GetName(item), name, "Stored name matches native item name");
        }

        private static void AssertPreservedMetadata(EngineTestContext ctx, JObject original, object saved, params string[] changedFields)
        {
            var expected = (JObject)original.DeepClone();
            var actual = JObject.FromObject(saved);
            if (!JToken.DeepEquals(expected["Id"], actual["Id"]))
            {
                // Newly created alternatives have their own creation timestamp;
                // the original record must retain its original timestamp.
                expected.Remove("DateCreated");
                actual.Remove("DateCreated");
            }
            foreach (var field in changedFields)
            {
                expected.Remove(field);
                actual.Remove(field);
            }
            var changed = expected.Properties().Where(field => !JToken.DeepEquals(field.Value, actual[field.Name])).Select(field => field.Name);
            ctx.Assert(JToken.DeepEquals(expected, actual), $"Migration must preserve unrelated persisted metadata: {string.Join(", ", changed)}");
        }
    }
}
