using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    private const string HeadgearCategory = "HeadgearSlotMigration";
    internal static readonly Dictionary<(ItemPropertyType, int), int> VisorEnhancements = new()
    {
        [(ItemPropertyType.Attack, 0)] = 5,
        [(ItemPropertyType.Defense, 1)] = 4,
        [(ItemPropertyType.HPBonus, 0)] = 17,
    };

    [EngineTest("Unmodified legacy blueprint body armor visors convert without losing their native data", Category = HeadgearCategory, TimeoutSeconds = 60)]
    public static async Task OriginalLegacyVisorBlueprints(EngineTestContext ctx)
    {
        foreach (var data in new[] { LegacyHeadgearFixtures.Ruptor, LegacyHeadgearFixtures.Thermal })
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.DelaySecondsAsync(0.3f);
            var visor = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                visor = Deserialize(ctx, data);
                ctx.AssertEqual(BaseItem.Armor, GetBaseItemType(visor), "The unmodified historical blueprint loads as native body armor");
                ctx.Assert(ItemPlugin.MoveTo(visor, owner), "Legacy fixture is owned by the creature");
                ctx.Log($"Historical {GetResRef(visor)} simple model before conversion: {GetItemAppearance(visor, ItemAppearanceType.SimpleModel, 0)}");
            });
            await EquipAsync(ctx, owner, visor, InventorySlot.Chest);
            var properties = DescribeProperties(visor);
            var uuid = GetObjectUUID(visor);
            var attack = Stat.GetNPCSkinStats(GetItemInSlot(InventorySlot.CreatureArmor, owner)).Attack;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ctx.Assert(HeadgearSlotMigration.MigrateObject(visor), "The actual conversion repairs the historical fixture");
                ctx.AssertEqual(visor, GetItemInSlot(InventorySlot.Head, owner), "The historical visor now occupies the head slot");
                ctx.AssertEqual(OBJECT_INVALID, GetItemInSlot(InventorySlot.Chest, owner), "The old chest slot is clear");
                ctx.AssertEqual(uuid, GetObjectUUID(visor), "Native fixture identity survives in place");
                ctx.AssertEqual(properties, DescribeProperties(visor), "Slot compatibility alone preserves every historical property");
                ctx.AssertEqual(110, GetItemAppearance(visor, ItemAppearanceType.SimpleModel, 0), "Missing historical helmet model uses the corrected blueprint model");
                ctx.AssertEqual(attack, Stat.GetNPCSkinStats(GetItemInSlot(InventorySlot.CreatureArmor, owner)).Attack, "Moving slots neither doubles nor drops the equipment contribution");
                ctx.Assert(!HeadgearSlotMigration.MigrateObject(visor), "Historical native fixture conversion is idempotent");
            });
        }
    }

    [EngineTest("Legacy chest visor converts before loot revision and survives bank reloads", Category = HeadgearCategory, TimeoutSeconds = 90)]
    public static Task ChestVisorSlotFirst(EngineTestContext ctx) => VerifyChestVisor(ctx, false, false);

    [EngineTest("Legacy chest visor converts after loot revision and survives bank reloads", Category = HeadgearCategory, TimeoutSeconds = 90)]
    public static Task ChestVisorStatsFirst(EngineTestContext ctx) => VerifyChestVisor(ctx, true, false);

    [EngineTest("Occupied helmet survives legacy visor conversion before loot revision", Category = HeadgearCategory, TimeoutSeconds = 90)]
    public static Task OccupiedHeadVisorSlotFirst(EngineTestContext ctx) => VerifyChestVisor(ctx, false, true);

    [EngineTest("Occupied helmet survives legacy visor conversion after loot revision", Category = HeadgearCategory, TimeoutSeconds = 90)]
    public static Task OccupiedHeadVisorStatsFirst(EngineTestContext ctx) => VerifyChestVisor(ctx, true, true);

    private static async Task VerifyChestVisor(EngineTestContext ctx, bool statsFirst, bool occupied)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        var visor = await CreateItemAsync(ctx, player, "ruptorvanecr", player);
        await ctx.ExecuteInCreatureContextAsync(player, () => StampLegacyVisor(visor));
        await ctx.WaitFrameAsync();
        var chest = GetItemInSlot(InventorySlot.Chest, player);
        if (GetIsObjectValid(chest)) await UnequipAsync(ctx, player, chest);
        var head = GetItemInSlot(InventorySlot.Head, player);
        if (GetIsObjectValid(head)) await UnequipAsync(ctx, player, head);
        if (occupied)
        {
            head = await CreateItemAsync(ctx, player, "charm_cowl", player);
            await ctx.ExecuteInCreatureContextAsync(player, () => RemoveRequirement(head));
            await EquipAsync(ctx, player, head, InventorySlot.Head);
        }
        // Login rebuilds max FP, max STM and Evasion from worn gear, so start from records the gear explains.
        fixture.Update(record =>
        {
            record.MaxFP = Stat.BaseFP;
            record.MaxStamina = Stat.BaseSTM;
            record.Evasion = 0;
        });
        await ctx.ExecuteInCreatureContextAsync(player, () => Stat.RefreshPlayerEquipmentStats(player));
        var baseline = ReadEquipmentTotals(player);
        var baseFP = Stat.GetMaxFP(player);
        var baseSTM = Stat.GetMaxStamina(player);
        await EquipAsync(ctx, player, visor, InventorySlot.Chest);
        var hp = 0;
        var fp = 0;
        var stm = 0;
        SavedCharacterMigration.Upgraded upgraded = null;
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            ctx.AssertEqual(BaseItem.Armor, GetBaseItemType(visor), "Fixture is genuinely legacy body armor before actual conversion");
            ctx.AssertEqual(visor, GetItemInSlot(InventorySlot.Chest, player), "Legacy fixture occupies chest");
            ctx.AssertEqual(baseline.Attack + 34, ReadEquipmentTotals(player).Attack, "Old Attack 29 and crafted Attack 5 are counted once");
            hp = GetMaxHitPoints(player);
            SetCurrentHitPoints(player, hp);
            fp = Stat.GetMaxFP(player);
            stm = Stat.GetMaxStamina(player);
            fixture.Update(record => { record.FP = fp; record.Stamina = stm; });

            if (statsFirst) Invoke("LootStatRevisionMigration", "MigrateItem", visor);
            upgraded = SavedCharacterMigration.Run(player, DB.Get<Player>(fixture.Id), "_22_ReviseLootStats");
            var character = SavedCharacterMigration.Load(ctx, upgraded);
            var migrated = SavedCharacterMigration.FindItem(character, "ruptorvanecr");
            AssertVisorCustomization(ctx, migrated, null);
            AssertRevisedLoot(ctx, migrated, VisorEnhancements);
            ctx.AssertEqual(OBJECT_INVALID, GetItemInSlot(InventorySlot.Chest, character), "No stale chest occupancy remains");
            ctx.AssertEqual(occupied ? "charm_cowl" : "ruptorvanecr", GetResRef(GetItemInSlot(InventorySlot.Head, character)), "Head slot is preserved or safely equipped");
            ctx.AssertEqual(0, upgraded.Overflow.Count, "A roomy inventory needs no overflow storage");
            var worn = !occupied;
            AssertVisorTotals(ctx, upgraded.Record, baseline, worn);
            ctx.AssertEqual(GetMaxHitPoints(character), ObjectPlugin.GetCurrentHitPoints(character), "A character at full HP is saved at full HP for its final cap");
            var retried = SavedCharacterMigration.Run(upgraded.File, upgraded.Record, "_22_ReviseLootStats");
            ctx.Assert(retried.File.SequenceEqual(upgraded.File), "Repeated conversion makes no change");
            ctx.Assert(!LootStatRevisionMigration.MigrateObject(migrated), "Loot revision retry makes no change");
            ctx.Assert(!HeadgearSlotMigration.MigrateObject(migrated), "Headgear repair retry makes no change");
        });

        // The player logs in with the converted character and moves the visor through the bank twice.
        using var login = await PlayerAbilityFixture.CreateFromCharacterAsync(ctx, upgraded.File, upgraded.Record, 3f);
        player = login.Creature;
        var loginWorn = !occupied;
        visor = FindHeadgear(player, "ruptorvanecr");
        var uuid = GetObjectUUID(visor);
        ctx.AssertEqual(baseFP + (loginWorn ? 9 : 0), Stat.GetMaxFP(player), "FP bonus is counted only while worn");
        ctx.AssertEqual(baseSTM + (loginWorn ? 11 : 0), Stat.GetMaxStamina(player), "Stamina bonus is counted only while worn");
        ctx.AssertEqual(System.Math.Min(fp, Stat.GetMaxFP(player)), DB.Get<Player>(login.Id).FP, "Current FP survives within final cap");
        ctx.AssertEqual(System.Math.Min(stm, Stat.GetMaxStamina(player)), DB.Get<Player>(login.Id).Stamina, "Current stamina survives within final cap");
        AssertVisorTotals(ctx, player, baseline, loginWorn);

        // Use the actual bank persistence entry points. Remove, save, destroy,
        // reload, acquire and equip this same customized item twice.
        for (var cycle = 1; cycle <= 2; cycle++)
        {
            if (GetItemInSlot(InventorySlot.Head, player) == visor) await UnequipAsync(ctx, player, visor);
            AssertVisorTotals(ctx, player, baseline, false);
            InventoryItem stored = null;
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                stored = Bank.DepositItem(player, visor);
                ctx.AssertEqual(login.Id, stored.PlayerId, $"Cycle {cycle}: bank ownership survives");
                ctx.AssertEqual(1, stored.Quantity, $"Cycle {cycle}: exactly one visor is saved");
            });
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(visor), 5f, "deposited visor to leave the live inventory");
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                StoredItemSweepRunner.Run();
                Bank.WithdrawItem(player, stored.Id);
            });
            await ctx.WaitFrameAsync();
            visor = FindHeadgear(player, "ruptorvanecr");
            ctx.Assert(GetIsObjectValid(visor), $"Cycle {cycle}: bank withdrawal restores the visor");
            ctx.AssertEqual(1, CountHeadgear(player, "ruptorvanecr"), $"Cycle {cycle}: no duplicate copy exists");
            AssertVisorCustomization(ctx, visor, uuid);
            AssertRevisedLoot(ctx, visor, VisorEnhancements);
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                ctx.Assert(DB.Get<InventoryItem>(stored.Id) == null, "Withdrawal removes the stored copy");
                ctx.Assert(!HeadgearSlotMigration.MigrateObject(visor), "Reload/login retry is stable");
                ctx.Assert(!LootStatRevisionMigration.MigrateObject(visor), "Acquisition does not revise stats twice");
            });
            if (!occupied)
            {
                await EquipAsync(ctx, player, visor, InventorySlot.Head);
                AssertVisorTotals(ctx, player, baseline, true);
            }
            else AssertVisorTotals(ctx, player, baseline, false);
        }
    }

    internal static void StampLegacyVisor(uint visor)
    {
        // Reconstruct the pre-8382511cf item, including its pre-revision loot
        // stats. No compatibility API is invoked until the test asserts this state.
        StampLegacyLoot(visor, VisorEnhancements);
        RemoveRequirement(visor);
        ItemPlugin.SetBaseItemType(visor, BaseItem.Armor);
        ItemPlugin.SetItemAppearance(visor, ItemAppearanceType.SimpleModel, 0, 0);
        ItemPlugin.SetItemAppearance(visor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso, 186);
        ItemPlugin.SetItemAppearance(visor, ItemAppearanceType.ArmorColor, 0, 7);
        SetName(visor, "Customized salvage");
        SetDescription(visor, "Crafted by a player, with retained enhancements.");
        SetLocalString(visor, "CRAFTED_BY", "fixture-crafter");
        SetLocalInt(visor, "CUSTOM_INT", 42);
        SetLocalFloat(visor, "CUSTOM_FLOAT", 1.25f);
        AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.FP, -1, 9), visor);
        AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Stamina, -1, 11), visor);
    }

    internal static void AssertVisorCustomization(EngineTestContext ctx, uint visor, string uuid, int expectedModel = 110)
    {
        ctx.AssertEqual(BaseItem.Helmet, GetBaseItemType(visor), "The copy is headgear");
        // Items saved inside a character file carry no UUID, so only live and stored copies are checked for one.
        if (uuid != null)
            ctx.AssertEqual(uuid, GetObjectUUID(visor), "The item's UUID is preserved");
        ctx.AssertEqual(1, GetItemStackSize(visor), "Quantity is preserved");
        ctx.AssertEqual("Customized salvage", GetName(visor), "Custom name survives");
        ctx.AssertEqual("Crafted by a player, with retained enhancements.", GetDescription(visor), "Custom description survives");
        ctx.AssertEqual("fixture-crafter", GetLocalString(visor, "CRAFTED_BY"), "Crafting identity survives");
        ctx.AssertEqual(42, GetLocalInt(visor, "CUSTOM_INT"), "Integer local survives");
        ctx.AssertEqual(1.25f, GetLocalFloat(visor, "CUSTOM_FLOAT"), "Float local survives");
        ctx.AssertEqual(7, GetItemAppearance(visor, ItemAppearanceType.ArmorColor, 0), "Custom color survives");
        ctx.AssertEqual(expectedModel, GetItemAppearance(visor, ItemAppearanceType.SimpleModel, 0), "Converted headgear retains its expected model");
        ctx.AssertEqual(9, SumProperty(visor, ItemPropertyType.FP, 0), "Custom FP property survives");
        ctx.AssertEqual(11, SumProperty(visor, ItemPropertyType.Stamina, 0), "Custom stamina property survives");
    }

    private static void AssertVisorTotals(EngineTestContext ctx, uint player,
        (int Attack, int PhysicalDefense, int ForceDefense, int MaxHP) baseline, bool worn) =>
        AssertVisorTotals(ctx, DB.Get<Player>(GetObjectUUID(player)), baseline, worn);

    private static void AssertVisorTotals(EngineTestContext ctx, Player record,
        (int Attack, int PhysicalDefense, int ForceDefense, int MaxHP) baseline, bool worn)
    {
        var actual = ReadEquipmentTotals(record);
        ctx.AssertEqual(baseline.Attack + (worn ? 10 : 0), actual.Attack, "Revised Attack plus enhancement counts exactly once");
        ctx.AssertEqual(baseline.PhysicalDefense + (worn ? 17 : 0), actual.PhysicalDefense, "Physical Defense counts exactly once");
        ctx.AssertEqual(baseline.ForceDefense + (worn ? 8 : 0), actual.ForceDefense, "Force Defense counts exactly once");
        ctx.AssertEqual(baseline.MaxHP + (worn ? 67 : 0), actual.MaxHP, "Max HP counts exactly once");
    }

    internal static uint FindHeadgear(uint owner, string resref)
    {
        for (var item = GetFirstItemInInventory(owner); GetIsObjectValid(item); item = GetNextItemInInventory(owner))
            if (GetResRef(item) == resref) return item;
        for (var slot = 0; slot < NumberOfInventorySlots; slot++)
        {
            var item = GetItemInSlot((InventorySlot)slot, owner);
            if (GetResRef(item) == resref) return item;
        }
        return OBJECT_INVALID;
    }

    private static int CountHeadgear(uint owner, string resref)
    {
        var items = new HashSet<uint>();
        for (var item = GetFirstItemInInventory(owner); GetIsObjectValid(item); item = GetNextItemInInventory(owner))
            if (GetResRef(item) == resref) items.Add(item);
        for (var slot = 0; slot < NumberOfInventorySlots; slot++)
        {
            var item = GetItemInSlot((InventorySlot)slot, owner);
            if (GetResRef(item) == resref) items.Add(item);
        }
        return items.Count;
    }

    [EngineTest("All legacy headgear resrefs convert in nested bank storage and retain quantities", Category = HeadgearCategory, TimeoutSeconds = 90)]
    public static async Task NestedStoredHeadgear(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        var bags = new Dictionary<string, uint>();
        var records = new Dictionary<string, InventoryItem>();
        var items = new Dictionary<string, string>();
        foreach (var resref in HeadgearSlotMigration.LegacyHeadgear.Keys)
        {
            // A legacy armor copy takes more repository space than a helmet.
            // Give each resref its own real container instead of overfilling one.
            var bag = await CreateItemAsync(ctx, player, "bag_sm", player);
            var item = await CreateItemAsync(ctx, player, resref, bag);
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                StampLegacyVisor(item);
                SetLocalString(bag, "HEADGEAR_FIXTURE_BAG", resref);
                bags[resref] = bag;
                items[resref] = GetObjectUUID(item);
                ctx.AssertEqual(bag, GetItemPossessor(item, true), $"{resref}: legacy fixture is actually inside its container before migration");
                ctx.AssertEqual(BaseItem.Armor, GetBaseItemType(item), "Nested fixture starts as body armor");
            });
        }
        var correct = await CreateItemAsync(ctx, player, "ruptorvanecr", player);
        var armor = await CreateItemAsync(ctx, player, "absdef_w2", player);
        var correctData = ObjectPlugin.Serialize(correct);
        var armorData = ObjectPlugin.Serialize(armor);
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            foreach (var (resref, bag) in bags) records[resref] = Bank.DepositItem(player, bag);
        });
        await ctx.WaitUntilAsync(() => bags.Values.All(bag => !GetIsObjectValid(bag)), 5f, "the legacy containers to be stored");
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            StoredItemSweepRunner.Run();
            foreach (var stored in records.Values)
            {
                var data = DB.Get<InventoryItem>(stored.Id).Data;
                ctx.Assert(!HeadgearSlotMigration.MigrateSerializedObject(data, out _), "Stored nested gear is already corrected");
                Bank.WithdrawItem(player, stored.Id);
            }
            ctx.AssertEqual(correctData, ObjectPlugin.Serialize(correct), "A correct new visor is untouched");
            ctx.AssertEqual(armorData, ObjectPlugin.Serialize(armor), "Ordinary body armor is untouched");
        });
        await ctx.WaitFrameAsync();
        void CheckBags()
        {
            var found = new HashSet<string>();
            for (var bag = GetFirstItemInInventory(player); GetIsObjectValid(bag); bag = GetNextItemInInventory(player))
            {
                var resref = GetLocalString(bag, "HEADGEAR_FIXTURE_BAG");
                if (!items.ContainsKey(resref)) continue;
                ctx.Assert(found.Add(resref), "Each legacy container survives only once");
                bags[resref] = bag;
                ctx.AssertEqual(1, Item.GetInventoryItemCount(bag), "Each container retains exactly its original visor");
                var item = GetFirstItemInInventory(bag);
                ctx.AssertEqual(resref, GetResRef(item), "The exact nested resref survives");
                AssertVisorCustomization(ctx, item, items[resref], HeadgearSlotMigration.LegacyHeadgear[resref]);
                AssertRevisedLoot(ctx, item, VisorEnhancements);
                ctx.AssertEqual(HeadgearSlotMigration.LegacyHeadgear[resref], GetItemAppearance(item, ItemAppearanceType.SimpleModel, 0), "Each missing legacy helmet model uses its own corrected blueprint");
                ctx.AssertEqual(bag, GetItemPossessor(item, true), "Nested container ownership survives");
                ctx.AssertEqual(player, GetItemPossessor(item), "Ultimate player ownership survives");
                ctx.Assert(!HeadgearSlotMigration.MigrateObject(bag), "Repeated nested migration is stable");
            }
            ctx.AssertEqual(13, found.Count, "All thirteen nested copies survive exactly once");
        }
        CheckBags();
        for (var cycle = 0; cycle < 2; cycle++)
        {
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                foreach (var (resref, bag) in bags) records[resref] = Bank.DepositItem(player, bag);
            });
            await ctx.WaitUntilAsync(() => bags.Values.All(bag => !GetIsObjectValid(bag)), 5f, "nested deposit destruction");
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                foreach (var stored in records.Values) Bank.WithdrawItem(player, stored.Id);
            });
            await ctx.WaitFrameAsync();
            CheckBags();
        }
    }

    [EngineTest("Legacy visor acquired after migration converts once and keeps crafted properties", Category = HeadgearCategory)]
    public static async Task AcquiredLegacyHeadgear(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var owner = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        var item = await CreateItemAsync(ctx, owner, "thermlancercr", owner);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            StampLegacyVisor(item);
            ItemPlugin.SetItemAppearance(item, ItemAppearanceType.SimpleModel, 0, 33);
        });
        var uuid = GetObjectUUID(item);
        await ctx.ExecuteInCreatureContextAsync(fixture.Creature, () =>
            ctx.Assert(ItemPlugin.MoveTo(item, fixture.Creature), "Transfer a real legacy instance to the player"));
        await ctx.WaitUntilAsync(() => GetBaseItemType(item) == BaseItem.Helmet &&
            GetLocalInt(item, LootStatRevisionMigration.RevisionVariable) == LootStatRevisionMigration.CurrentRevision, 5f, "acquisition compatibility handlers");
        AssertVisorCustomization(ctx, item, uuid, 33);
        ctx.AssertEqual(33, GetItemAppearance(item, ItemAppearanceType.SimpleModel, 0), "A valid customized helmet model is preserved");
        AssertRevisedLoot(ctx, item, VisorEnhancements);
        ctx.AssertEqual(fixture.Creature, GetItemPossessor(item), "Acquisition preserves ownership");
        ctx.Assert(!HeadgearSlotMigration.MigrateObject(item), "Repeated acquisition normalization is stable");
    }
}
