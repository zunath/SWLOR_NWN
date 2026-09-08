using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class MigrationEngineTests
    {
        [EngineTest("Migration removes equipped item properties and their native effects", Category = "Migration")]
        public static async Task EquippedPropertyRemoval(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.DelaySecondsAsync(0.5f);
            var weapon = await ctx.EquipItemAsync(owner, "nw_wswls001", InventorySlot.RightHand);
            var original = GetAbilityScore(owner, AbilityType.Might);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
                AddItemProperty(DurationType.Permanent, ItemPropertyAbilityBonus(AbilityType.Might, 4), weapon));
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(original + 4, GetAbilityScore(owner, AbilityType.Might), "Equipped fixture applies its native ability bonus");
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                for (var property = GetFirstItemProperty(weapon); GetIsItemPropertyValid(property); property = GetNextItemProperty(weapon))
                {
                    if (GetItemPropertyType(property) != ItemPropertyType.AbilityBonus)
                        continue;
                    Invoke("MigrationObject", "RemoveProperty", weapon, property);
                    break;
                }
                ctx.AssertEqual(-1, PropertyValue(weapon, ItemPropertyType.AbilityBonus), "The property is removed immediately");
            });
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(original, GetAbilityScore(owner, AbilityType.Might), "Its equipped effect is removed too");
        }

        [EngineTest("Migration clears every legacy local variable", Category = "Migration")]
        public static async Task ClearLegacyVariables(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var item = await CreateItemAsync(ctx, owner, "b_longsword", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                SetLocalInt(item, "OLD_INT", 12);
                SetLocalString(item, "OLD_STRING", "legacy");
                SetLocalFloat(item, "OLD_FLOAT", 1.5f);
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                typeof(_2_LegacyServerMigration).BaseType
                    .GetMethod("WipeVariables", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(new _2_LegacyServerMigration(), new object[] { item });
                ctx.AssertEqual(0, ObjectPlugin.GetLocalVariableCount(item), "No legacy locals remain");
            });
        }

        [EngineTest("Migration converts stored weapon requirements after cache load", Category = "Migration")]
        public static async Task StoredWeaponRequirements(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var weapon = await CreateItemAsync(ctx, owner, "b_longsword", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ClearProperties(weapon);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.UseLimitationPerk, 6, 5), weapon);
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ctx.AssertEqual(5, PropertyValue(weapon, ItemPropertyType.UseLimitationPerk, 6), "Legacy fixture perk requirement");
                var migrated = MigrateSerialized(ctx, weapon);
                ctx.AssertEqual(40, PropertyValue(migrated, ItemPropertyType.RequiresSkill, (int)SkillType.Vibroblade), "Weapon skill rank");
                ctx.AssertEqual(-1, PropertyValue(migrated, ItemPropertyType.RequiresSkill, 0), "No invalid skill requirement");
            });
        }

        [EngineTest("Migration updates carried droid CPU and nested weapon data", Category = "Migration")]
        public static async Task DroidContents(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var cpu = await CreateItemAsync(ctx, owner, "d_bl_cpu1_m", owner);
            var weapon = await CreateItemAsync(ctx, owner, "b_longsword", owner);
            var controller = await CreateItemAsync(ctx, owner, Droid.DroidControlItemResref, owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ClearProperties(cpu);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DroidPart, (int)DroidPartItemPropertySubType.CPU), cpu);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Tier, 1), cpu);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DroidStat, 12, 5), cpu);

                ClearProperties(weapon);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DMG, (int)CombatDamageType.Physical, 6), weapon);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.UseLimitationPerk, 6, 5), weapon);
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                SetLocalString(controller, "CONSTRUCTED_DROID", JsonConvert.SerializeObject(new ConstructedDroid
                {
                    SerializedCPU = ObjectPlugin.Serialize(cpu),
                    EquippedItems = { [InventorySlot.RightHand] = ObjectPlugin.Serialize(weapon) }
                }));

                Invoke("ServerMigration.StoredItemDataMigration", "MigrateDroidItems", owner);
                Invoke("SerializedItemWeaponDamageTypeMigration", "MigrateObject", owner);
                ctx.Log($"Carried CPU properties: {DescribeProperties(cpu)}; owner inventory: {Item.GetInventoryItemCount(owner)}");
                ctx.AssertEqual(5, PropertyValue(cpu, ItemPropertyType.DroidStat, (int)DroidStatSubType.Armor), "Carried CPU armor skill");
                ctx.AssertEqual(5, PropertyValue(cpu, ItemPropertyType.DroidStat, (int)DroidStatSubType.Vibroblade), "Carried CPU weapon skill");
                var droid = JsonConvert.DeserializeObject<ConstructedDroid>(GetLocalString(controller, "CONSTRUCTED_DROID"));
                var migratedWeapon = Deserialize(ctx, droid.EquippedItems[InventorySlot.RightHand]);
                ctx.AssertEqual(5, PropertyValue(migratedWeapon, ItemPropertyType.DMG), "Droid weapon damage");
                ctx.AssertEqual(40, PropertyValue(migratedWeapon, ItemPropertyType.RequiresSkill, (int)SkillType.Vibroblade), "Droid weapon requirement");
                var migratedCpu = Deserialize(ctx, droid.SerializedCPU);
                ctx.AssertEqual(5, PropertyValue(migratedCpu, ItemPropertyType.DroidStat, (int)DroidStatSubType.Armor), "Stored CPU armor skill");

                Invoke("SerializedItemWeaponDamageTypeMigration", "MigrateObject", owner);
                droid = JsonConvert.DeserializeObject<ConstructedDroid>(GetLocalString(controller, "CONSTRUCTED_DROID"));
                ctx.AssertEqual(5, PropertyValue(Deserialize(ctx, droid.EquippedItems[InventorySlot.RightHand]), ItemPropertyType.DMG), "Repeat migration damage");

                var storedController = MigrateSerialized(ctx, controller);
                var storedDroid = JsonConvert.DeserializeObject<ConstructedDroid>(GetLocalString(storedController, "CONSTRUCTED_DROID"));
                var storedCpu = Deserialize(ctx, storedDroid.SerializedCPU);
                ctx.AssertEqual(5, PropertyValue(storedCpu, ItemPropertyType.DroidStat, (int)DroidStatSubType.Armor), "Full storage pipeline preserves CPU armor");
                ctx.AssertEqual(5, PropertyValue(Deserialize(ctx, storedDroid.EquippedItems[InventorySlot.RightHand]), ItemPropertyType.DMG), "Full storage pipeline preserves weapon damage");
            });
        }

        [EngineTest("Migration expands nested carried blueprints without losing alternatives", Category = "Migration")]
        public static async Task NestedBlueprints(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var bag = await CreateItemAsync(ctx, owner, "bag_sm", owner);
            var blueprint = await CreateItemAsync(ctx, owner, "blueprint", bag);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                SetLocalInt(blueprint, "BLUEPRINT_RECIPE_ID", 3488);
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                Invoke("ServerMigration.StoredItemDataMigration", "MigrateDroidItems", owner);
                AssertBlueprintRecipes(ctx, bag);
                Invoke("ServerMigration.StoredItemDataMigration", "MigrateDroidItems", owner);
                AssertBlueprintRecipes(ctx, bag);
            });
        }

        [EngineTest("Migration expands blueprint alternatives inside serialized droid inventories", Category = "Migration")]
        public static async Task DroidBlueprints(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var blueprint = await CreateItemAsync(ctx, owner, "blueprint", owner);
            var controller = await CreateItemAsync(ctx, owner, Droid.DroidControlItemResref, owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                SetLocalInt(blueprint, "BLUEPRINT_RECIPE_ID", 3488);
                SetLocalString(blueprint, "DROID_ITEM_ID", "original");
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ctx.Log("Serializing the legacy blueprint into its droid controller.");
                SetLocalString(controller, "CONSTRUCTED_DROID", JsonConvert.SerializeObject(new ConstructedDroid
                {
                    Inventory = { ["original"] = ObjectPlugin.Serialize(blueprint) }
                }));
                ctx.Log("Expanding the controller's serialized blueprint.");
                Invoke("ServerMigration.StoredItemDataMigration", "MigrateDroidItems", controller);
                ctx.Log("Checking the expanded serialized blueprints.");
                var droid = JsonConvert.DeserializeObject<ConstructedDroid>(GetLocalString(controller, "CONSTRUCTED_DROID"));
                ctx.AssertEqual(3, droid.Inventory.Count, "Expanded droid blueprint count");
                var recipes = new List<int>();
                foreach (var pair in droid.Inventory)
                {
                    var item = Deserialize(ctx, pair.Value);
                    recipes.Add(GetLocalInt(item, "BLUEPRINT_RECIPE_ID"));
                    ctx.AssertEqual(pair.Key, GetLocalString(item, "DROID_ITEM_ID"), "Droid item identity");
                }
                ctx.Assert(recipes.OrderBy(value => value).SequenceEqual(new[] { 4799, 4800, 4801 }), "All replacement recipes should survive.");
            });
        }

        [EngineTest("Migration preserves storage quantities and replacement metadata", Category = "Migration")]
        public static async Task ReplacementStacksAndMetadata(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var kit = await CreateItemAsync(ctx, owner, "saber_upg1", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ctx.AssertEqual(1, GetItemStackSize(kit), "Kits use separate items; storage records aggregate their quantity");
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var data = ObjectPlugin.Serialize(kit);
                var record = new InventoryItem { PlayerId = "migration-test", StorageId = "migration-test", Name = "Old kit", Resref = "saber_upg1", Tag = "saber_upg1", Quantity = 7, Data = data };
                try
                {
                    DB.Set(record);
                    Invoke("ServerMigration.StoredItemDataMigration", "Migrate");
                    var migrated = DB.Get<InventoryItem>(record.Id);
                    ctx.AssertEqual("saber_upg2", migrated.Resref, "Replacement metadata resref");
                    ctx.AssertEqual("saber_upg2", migrated.Tag, "Replacement metadata tag");
                    var item = Deserialize(ctx, migrated.Data);
                    ctx.AssertEqual(GetName(item), migrated.Name, "Replacement metadata name");
                    ctx.AssertEqual(Item.GetIconResref(item), migrated.IconResref, "Replacement metadata icon");
                    ctx.AssertEqual(7, migrated.Quantity, "Stored stack quantity");
                    ctx.AssertEqual(1, GetItemStackSize(item), "Serialized individual kit quantity");
                    Invoke("ServerMigration.StoredItemDataMigration", "Migrate");
                    ctx.AssertEqual(7, DB.Get<InventoryItem>(record.Id).Quantity, "Retry stack quantity");
                }
                finally
                {
                    DB.Delete<InventoryItem>(record.Id);
                }
            });
        }

        [EngineTest("Migration removes obsolete contents before serializing their container", Category = "Migration")]
        public static async Task ObsoleteContainerContents(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var bag = await CreateItemAsync(ctx, owner, "bag_sm", owner);
            await CreateItemAsync(ctx, owner, "recipe_saberupg1", bag);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var migrated = MigrateSerialized(ctx, bag);
                ctx.Assert(!GetIsObjectValid(GetFirstItemInInventory(migrated)), $"The serialized bag must not retain a destroyed obsolete item; first remaining item: {GetResRef(GetFirstItemInInventory(migrated))}.");
            });
        }

        [EngineTest("Migration normalizes sabers nested in droid containers", Category = "Migration")]
        public static async Task NestedDroidSabers(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var bag = await CreateItemAsync(ctx, owner, "bag_sm", owner);
            var saber = await CreateItemAsync(ctx, owner, "b_longsword", bag);
            var controller = await CreateItemAsync(ctx, owner, Droid.DroidControlItemResref, owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ItemPlugin.SetBaseItemType(saber, BaseItem.Lightsaber);
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                SetLocalString(controller, "CONSTRUCTED_DROID", JsonConvert.SerializeObject(new ConstructedDroid
                {
                    Inventory = { ["bag"] = ObjectPlugin.Serialize(bag) }
                }));
                var migrated = MigrateSerialized(ctx, controller);
                var droid = JsonConvert.DeserializeObject<ConstructedDroid>(GetLocalString(migrated, "CONSTRUCTED_DROID"));
                var migratedSaber = GetFirstItemInInventory(Deserialize(ctx, droid.Inventory["bag"]));
                ctx.Log($"Nested saber properties: {DescribeProperties(migratedSaber)}");
                ctx.AssertEqual(5, GetLocalInt(migratedSaber, "SABER_TIER"), "Nested saber tier");
                ctx.AssertEqual(21, PropertyValue(migratedSaber, ItemPropertyType.DMG), "Nested saber damage");
            });
        }

        [EngineTest("Migration keeps resistance vulnerabilities when merging old properties", Category = "Migration")]
        public static async Task ResistanceVulnerability(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var item = await CreateItemAsync(ctx, owner, "ch_armor", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                AddLegacyProperty(item, ItemPropertyType.Resistance, (int)ResistanceType.Fire, 54, 112);
                // Retired elemental defense subtypes are absent from today's 2DA,
                // but can still occur in serialized pre-upgrade items.
                AddLegacyProperty(item, ItemPropertyType.Defense, (int)CombatDamageType.Fire, 35, 5);
                item = Deserialize(ctx, ObjectPlugin.Serialize(item));
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ctx.Log($"Resistance fixture: {DescribeProperties(item)}; table: {Get2DAString("itempropdef", "Label", 133)} / {Get2DAString("itempropdef", "CostTableResRef", 133)}");
                ctx.AssertEqual(112, PropertyValue(item, ItemPropertyType.Resistance, (int)ResistanceType.Fire), "Fixture vulnerability row");
                ctx.AssertEqual(5, PropertyValue(item, ItemPropertyType.Defense, (int)CombatDamageType.Fire), "Fixture legacy defense row");
                Invoke("SerializedItemResistanceMigration", "MigrateObject", item);
                var value = PropertyValue(item, ItemPropertyType.Resistance, (int)ResistanceType.Fire);
                ctx.AssertEqual(5, value, "The legacy defense wins over vulnerability, without becoming immunity");
                ctx.AssertEqual(-1, PropertyValue(item, ItemPropertyType.Defense, (int)CombatDamageType.Fire), "Legacy elemental defense is removed");
            });
        }

        private static async Task<uint> CreateItemAsync(EngineTestContext ctx, uint actor, string resref, uint possessor)
        {
            var item = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                item = CreateItemOnObject(resref, possessor);
                ctx.Assert(GetIsObjectValid(item), $"Could not create test item {resref}.");
                ctx.Track(item);
            });
            await ctx.WaitFrameAsync();
            return item;
        }
        private static uint Deserialize(EngineTestContext ctx, string data)
        {
            var item = ObjectPlugin.Deserialize(data);
            ctx.Assert(GetIsObjectValid(item), "Serialized test item must deserialize.");
            ctx.Track(item);
            return item;
        }

        private static uint MigrateSerialized(EngineTestContext ctx, uint item)
        {
            var result = Invoke("ServerMigration.StoredItemDataMigration", "MigrateSerializedObject", ObjectPlugin.Serialize(item));
            return Deserialize(ctx, (string)result.GetType().GetProperty("Data").GetValue(result));
        }

        private static object Invoke(string type, string method, params object[] args) =>
            typeof(_22_CombatSystemReplacement).Assembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition." + type)
                .GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args);

        private static void ClearProperties(uint item)
        {
            var properties = new List<SWLOR.NWN.API.Engine.ItemProperty>();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                properties.Add(ip);
            foreach (var ip in properties)
                RemoveItemProperty(item, ip);
        }

        private static void AddLegacyProperty(uint item, ItemPropertyType type, int subtype, int table, int value)
        {
            // Stamp the serialized property list directly; current item-property
            // constructors reject some retired subtype and cost-table combinations.
            var nativeItem = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(item).AsNWSItem();
            var template = nativeItem.GetPassiveProperty(0);
            using var property = new global::NWN.Native.API.CNWItemProperty
            {
                m_nPropertyName = (ushort)type, m_nSubType = (ushort)subtype,
                m_nCostTable = (byte)table, m_nCostTableValue = (ushort)value,
                m_nParam1 = 255, m_nParam1Value = 255, m_nChanceOfAppearing = 100,
                m_nDurationType = template.m_nDurationType, m_bUseable = template.m_bUseable,
                m_nID = 1000000 + (ulong)type
            };
            nativeItem.AddPassiveProperty(property);
        }

        private static int PropertyValue(uint item, ItemPropertyType type, int? subtype = null)
        {
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                if (GetItemPropertyType(ip) == type && (!subtype.HasValue || GetItemPropertySubType(ip) == subtype.Value))
                    return GetItemPropertyCostTableValue(ip);
            return -1;
        }

        private static string DescribeProperties(uint item)
        {
            var properties = new List<string>();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                properties.Add($"{GetItemPropertyType(ip)}:{GetItemPropertySubType(ip)}:{GetItemPropertyCostTableValue(ip)}");
            return string.Join(", ", properties);
        }

        private static void AssertBlueprintRecipes(EngineTestContext ctx, uint container)
        {
            var recipes = new List<int>();
            for (var item = GetFirstItemInInventory(container); GetIsObjectValid(item); item = GetNextItemInInventory(container))
                recipes.Add(GetLocalInt(item, "BLUEPRINT_RECIPE_ID"));
            ctx.Assert(recipes.OrderBy(value => value).SequenceEqual(new[] { 4799, 4800, 4801 }), $"A migrated container should hold exactly all three new blueprints; found: {string.Join(", ", recipes)}.");
        }
    }
}
