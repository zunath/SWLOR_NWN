using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Player migration removes equipped, carried and nested weapon stat overrides", Category = "WeaponStatOverrides")]
    public static async Task PlayerWeaponStatOverrides(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        var equipped = await ctx.EquipItemAsync(player, "b_longsword", InventorySlot.RightHand);
        var carried = await CreateItemAsync(ctx, player, "b_longsword", player);
        var bag = await CreateItemAsync(ctx, player, "bag_sm", player);
        var nested = await CreateItemAsync(ctx, player, "b_longsword", bag);
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            foreach (var weapon in new[] { equipped, carried, nested })
                StampWeaponStatOverrides(weapon);
            new _17_RemoveWeaponStatOverrides().Migrate(player);
            foreach (var weapon in new[] { equipped, carried, nested })
                AssertWeaponOverridesRemoved(ctx, weapon);
            ctx.AssertEqual(equipped, GetItemInSlot(InventorySlot.RightHand, player), "Weapon stays equipped");
            ctx.Assert(!WeaponStatOverrideMigration.MigrateObject(player), "Retry makes no further changes");
        });
    }

    [EngineTest("Stored weapon repair preserves identity, bonuses and nested droid equipment", Category = "WeaponStatOverrides")]
    public static async Task StoredWeaponStatOverrides(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("nw_rat001");
        await ctx.WaitFrameAsync();
        var weapon = await CreateItemAsync(ctx, owner, "b_longsword", owner);
        var controller = await CreateItemAsync(ctx, owner, Droid.DroidControlItemResref, owner);
        var bag = await CreateItemAsync(ctx, owner, "bag_sm", owner);
        var nested = await CreateItemAsync(ctx, owner, "b_longsword", bag);
        string saved = null;
        string identity = null;
        string savedBag = null;
        string nestedIdentity = null;
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            StampWeaponStatOverrides(weapon);
            identity = GetObjectUUID(weapon);
            saved = ObjectPlugin.Serialize(weapon);
            StampWeaponStatOverrides(nested);
            nestedIdentity = GetObjectUUID(nested);
            savedBag = ObjectPlugin.Serialize(bag);
            DestroyObject(weapon);
        });
        await ctx.DelaySecondsAsync(0.3f);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            ctx.Assert(WeaponStatOverrideMigration.MigrateSerializedObject(saved, out var migrated), "Saved weapon requires repair");
            var restored = Deserialize(ctx, migrated);
            AssertWeaponOverridesRemoved(ctx, restored);
            ctx.AssertEqual(identity, GetObjectUUID(restored), "Saved identity survives repair");
            ctx.Assert(!WeaponStatOverrideMigration.MigrateSerializedObject(migrated, out var retried), "Saved retry is unchanged");
            ctx.AssertEqual(migrated, retried, "Unchanged payload remains byte-for-byte intact");

            ctx.Assert(WeaponStatOverrideMigration.MigrateSerializedObject(savedBag, out var migratedBag), "Saved nested weapon requires repair");
            ctx.Assert(ContainsIdentity(migratedBag, nestedIdentity), "Nested saved identity survives alongside its live original");
            var restoredBag = Deserialize(ctx, migratedBag);
            var restoredNested = GetFirstItemInInventory(restoredBag);
            AssertWeaponOverridesRemoved(ctx, restoredNested);
            ctx.Assert(HasProperty(nested, ItemPropertyType.AccuracyStat), "Repairing the archive leaves the live original untouched");

            SetLocalString(controller, "CONSTRUCTED_DROID", JsonConvert.SerializeObject(new ConstructedDroid
            {
                EquippedItems = { [InventorySlot.RightHand] = saved },
                Inventory = { ["spare"] = saved }
            }));
            ctx.Assert(WeaponStatOverrideMigration.MigrateObject(controller), "Controller equipment requires repair");
            var droid = JsonConvert.DeserializeObject<ConstructedDroid>(GetLocalString(controller, "CONSTRUCTED_DROID"));
            AssertWeaponOverridesRemoved(ctx, Deserialize(ctx, droid.EquippedItems[InventorySlot.RightHand]));
            AssertWeaponOverridesRemoved(ctx, Deserialize(ctx, droid.Inventory["spare"]));
            ctx.Assert(!WeaponStatOverrideMigration.MigrateObject(controller), "Controller retry is unchanged");
        });
    }

    [EngineTest("Weapon stat repair preserves NPC, creature and nonweapon overrides", Category = "WeaponStatOverrides")]
    public static async Task ProtectedWeaponStatOverrides(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("nw_rat001");
        await ctx.WaitFrameAsync();
        var weapon = await CreateItemAsync(ctx, owner, "b_longsword", owner);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            StampWeaponStatOverrides(weapon);
            ctx.Assert(!WeaponStatOverrideMigration.MigrateObject(owner), "NPC inventory is excluded");
            ctx.Assert(HasProperty(weapon, ItemPropertyType.DamageStat), "NPC weapon override survives");
            SetLocalInt(weapon, "NO_ECONOMY", 1);
            ctx.Assert(!WeaponStatOverrideMigration.MigrateObject(weapon), "NPC-only weapon is excluded");
            DeleteLocalInt(weapon, "NO_ECONOMY");
            ItemPlugin.SetBaseItemType(weapon, Item.CreatureBaseItemTypes[0]);
            ctx.Assert(!WeaponStatOverrideMigration.MigrateObject(weapon), "Beast natural weapons are excluded");
            ItemPlugin.SetBaseItemType(weapon, BaseItem.Amulet);
            ctx.Assert(!WeaponStatOverrideMigration.MigrateObject(weapon), "Nonweapons are excluded");
            ctx.Assert(HasProperty(weapon, ItemPropertyType.AccuracyStat), "Protected overrides remain intact");
        });
    }

    [EngineTest("Stored weapon repair reaches more than fifty bank records and market and property storage", Category = "WeaponStatOverrides")]
    public static async Task StoredWeaponStatOverrideRecords(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("nw_rat001");
        await ctx.WaitFrameAsync();
        var weapon = await CreateItemAsync(ctx, owner, "b_longsword", owner);
        string saved = null;
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            StampWeaponStatOverrides(weapon);
            saved = ObjectPlugin.Serialize(weapon);
            DestroyObject(weapon);
        });
        await ctx.DelaySecondsAsync(0.3f);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            var records = new List<InventoryItem>();
            var market = new MarketItem { Data = saved, Name = "Keep listing name", Quantity = 3, Price = 123 };
            var category = new WorldPropertyCategory
            {
                Items = { ["weapon"] = new WorldPropertyItem { Data = saved, Quantity = 2, Name = "Keep storage name" } }
            };
            try
            {
                for (var i = 0; i < 55; i++)
                {
                    var record = new InventoryItem { Data = saved, Name = "Keep bank name", Quantity = 2 };
                    records.Add(record);
                    DB.Set(record);
                }
                DB.Set(market);
                DB.Set(category);
                new _24_RemoveWeaponStatOverrides().Migrate();
                foreach (var record in records)
                {
                    var repaired = DB.Get<InventoryItem>(record.Id);
                    AssertWeaponOverridesRemoved(ctx, Deserialize(ctx, repaired.Data));
                    ctx.AssertEqual(2, repaired.Quantity, "Stored quantity survives");
                    ctx.AssertEqual("Keep bank name", repaired.Name, "Stored metadata survives");
                }
                var repairedMarket = DB.Get<MarketItem>(market.Id);
                AssertWeaponOverridesRemoved(ctx, Deserialize(ctx, repairedMarket.Data));
                ctx.AssertEqual(123, repairedMarket.Price, "Market price survives");
                ctx.AssertEqual(3, repairedMarket.Quantity, "Market quantity survives");
                var stored = DB.Get<WorldPropertyCategory>(category.Id).Items["weapon"];
                AssertWeaponOverridesRemoved(ctx, Deserialize(ctx, stored.Data));
                ctx.AssertEqual(2, stored.Quantity, "Property storage quantity survives");
            }
            finally
            {
                foreach (var record in records)
                    DB.Delete<InventoryItem>(record.Id);
                DB.Delete<MarketItem>(market.Id);
                DB.Delete<WorldPropertyCategory>(category.Id);
            }
        });
    }

    private static void StampWeaponStatOverrides(uint weapon)
    {
        RemovePropertiesImmediately(weapon);
        AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DMG, -1, 23), weapon);
        AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Accuracy, -1, 10), weapon);
        AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Delay, -1, (int)ItemPropertyAttackDelay.Delay230), weapon);
        AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Fire), weapon);
        AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.AccuracyStat, (int)AbilityType.Agility), weapon);
        AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DamageStat, (int)AbilityType.Perception), weapon);
        SetLocalInt(weapon, "KEEP_LOCAL", 17);
    }

    private static void AssertWeaponOverridesRemoved(EngineTestContext ctx, uint weapon)
    {
        ctx.Assert(!HasProperty(weapon, ItemPropertyType.AccuracyStat), "Accuracy stat override is removed");
        ctx.Assert(!HasProperty(weapon, ItemPropertyType.DamageStat), "Damage stat override is removed");
        ctx.AssertEqual(23, PropertyValue(weapon, ItemPropertyType.DMG), "DMG stays intact");
        ctx.AssertEqual(10, PropertyValue(weapon, ItemPropertyType.Accuracy), "Accuracy bonus stays intact");
        ctx.AssertEqual((int)ItemPropertyAttackDelay.Delay230, PropertyValue(weapon, ItemPropertyType.Delay), "Delay stays intact");
        ctx.AssertEqual((int)CombatDamageType.Fire, FindPropertySubtype(weapon, ItemPropertyType.WeaponDamageType), "Element stays intact");
        ctx.AssertEqual(17, GetLocalInt(weapon, "KEEP_LOCAL"), "Other locals stay intact");
    }
}
