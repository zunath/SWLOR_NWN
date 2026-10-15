using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.QuestContractService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    private const string LootCategory = "LootStatRevisionMigration";
    internal const string RevisedGloves = "triagewardencr";
    internal const string RevisedPistol = "slw_lastwitness";
    private const string RevisedGreatsword = "absdef_l1";

    // A crafted Attack enhancement and a Defense enhancement merged into the gloves' own properties.
    internal static readonly Dictionary<(ItemPropertyType, int), int> GloveEnhancements = new()
    {
        [(ItemPropertyType.Attack, 0)] = 5,
        [(ItemPropertyType.Defense, (int)CombatDamageType.Physical)] = 4,
    };

    internal static readonly Dictionary<(ItemPropertyType, int), int> NoEnhancements = new();

    [EngineTest("Player loot stat migration revises equipped, carried and nested loot and keeps the wearer's totals", Category = LootCategory, TimeoutSeconds = 60f)]
    public static async Task PlayerLootStatRevision(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        var gloves = await CreateItemAsync(ctx, player, RevisedGloves, player);
        var pistol = await CreateItemAsync(ctx, player, RevisedPistol, player);
        var bag = await CreateItemAsync(ctx, player, "bag_sm", player);
        var greatsword = await CreateItemAsync(ctx, player, RevisedGreatsword, bag);
        var current = await CreateItemAsync(ctx, player, RevisedGloves, player);
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            StampLegacyLoot(gloves, GloveEnhancements);
            RemoveRequirement(gloves);
            StampLegacyLoot(pistol, NoEnhancements);
            StampLegacyLoot(greatsword, NoEnhancements);
        });

        var baseline = ReadEquipmentTotals(player);
        await EquipAsync(ctx, player, gloves, InventorySlot.Arms);
        var legacyTotals = ReadEquipmentTotals(player);
        ctx.AssertEqual(baseline.Attack + LegacyValue(RevisedGloves, ItemPropertyType.Attack, 0, GloveEnhancements), legacyTotals.Attack,
            "The legacy gloves grant their shipped Attack plus the merged enhancement");

        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            new _22_ReviseLootStats().Migrate(player);
            AssertRevisedLoot(ctx, gloves, GloveEnhancements);
            AssertRevisedLoot(ctx, pistol, NoEnhancements);
            AssertRevisedLoot(ctx, greatsword, NoEnhancements);
            AssertRevisedLoot(ctx, current, NoEnhancements);
            ctx.AssertEqual(gloves, GetItemInSlot(InventorySlot.Arms, player), "The gloves stay equipped");
            ctx.AssertEqual(bag, GetItemPossessor(greatsword, true), "The nested greatsword stays in its bag");
            ctx.Assert(!LootStatRevisionMigration.MigrateObject(player), "A retry makes no further changes");
        });

        var revisedTotals = ReadEquipmentTotals(player);
        AssertTotals(ctx, baseline, revisedTotals, GloveEnhancements, "after the migration");

        for (var cycle = 1; cycle <= 2; cycle++)
        {
            await UnequipAsync(ctx, player, gloves);
            var unequipped = ReadEquipmentTotals(player);
            ctx.AssertEqual(baseline.Attack, unequipped.Attack, $"Cycle {cycle}: unequipping returns Attack to its baseline");
            ctx.AssertEqual(baseline.PhysicalDefense, unequipped.PhysicalDefense, $"Cycle {cycle}: unequipping returns Defense to its baseline");
            ctx.AssertEqual(baseline.ForceDefense, unequipped.ForceDefense, $"Cycle {cycle}: unequipping returns Force Defense to its baseline");
            ctx.AssertEqual(baseline.MaxHP, unequipped.MaxHP, $"Cycle {cycle}: unequipping returns max HP to its baseline");
            await EquipAsync(ctx, player, gloves, InventorySlot.Arms);
            AssertTotals(ctx, baseline, ReadEquipmentTotals(player), GloveEnhancements, $"after re-equip cycle {cycle}");
        }

        string savedBag = null;
        string savedGloves = null;
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            savedBag = ObjectPlugin.Serialize(bag);
            savedGloves = ObjectPlugin.Serialize(gloves);
        });
        for (var cycle = 1; cycle <= 2; cycle++)
        {
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                var restoredGloves = Deserialize(ctx, savedGloves);
                AssertRevisedLoot(ctx, restoredGloves, GloveEnhancements);
                var restoredBag = Deserialize(ctx, savedBag);
                var contents = new List<uint>();
                for (var item = GetFirstItemInInventory(restoredBag); GetIsObjectValid(item); item = GetNextItemInInventory(restoredBag))
                    contents.Add(item);
                ctx.AssertEqual(1, contents.Count, $"Reload cycle {cycle}: the bag still holds exactly one item");
                AssertRevisedLoot(ctx, contents[0], NoEnhancements);
                ctx.AssertEqual(1, GetItemStackSize(contents[0]), $"Reload cycle {cycle}: the stack size is unchanged");
                ctx.Assert(!LootStatRevisionMigration.MigrateObject(restoredBag), $"Reload cycle {cycle}: reloaded loot needs no further change");
                savedGloves = ObjectPlugin.Serialize(restoredGloves);
                savedBag = ObjectPlugin.Serialize(restoredBag);
                DestroyObject(restoredGloves);
                DestroyObject(restoredBag);
            });
            await ctx.WaitFrameAsync();
        }
    }

    [EngineTest("Loot stat migration revises legacy loot a player acquires after the character migration", Category = LootCategory)]
    public static async Task AcquiredLootStatRevision(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        var owner = ctx.SpawnCreature("nw_rat001");
        await ctx.WaitFrameAsync();
        var legacy = await CreateItemAsync(ctx, owner, RevisedPistol, owner);
        var copy = OBJECT_INVALID;
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            StampLegacyLoot(legacy, NoEnhancements);
            ctx.Assert(!LootStatRevisionMigration.MigrateObject(owner), "NPC inventories keep their gear");
            ctx.AssertEqual(0, GetLocalInt(legacy, LootStatRevisionMigration.RevisionVariable), "The NPC's copy stays legacy");
        });
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            copy = CopyItem(legacy, player, true);
            ctx.Track(copy);
        });
        await ctx.WaitUntilAsync(() => GetLocalInt(copy, LootStatRevisionMigration.RevisionVariable) == LootStatRevisionMigration.CurrentRevision,
            5f, "the acquired legacy pistol to be revised");
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            AssertRevisedLoot(ctx, copy, NoEnhancements);
            ctx.AssertEqual(player, GetItemPossessor(copy), "The player keeps the acquired pistol");
        });
    }

    [EngineTest("Player loot stat migration keeps current HP and stamina when equipped pools are revised", Category = LootCategory, TimeoutSeconds = 60f)]
    public static async Task PlayerLootStatRevisionKeepsPools(EngineTestContext ctx)
    {
        const string chestResref = "absdef_w2";
        const string beltResref = "apexbite_w2";
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        var chest = await CreateItemAsync(ctx, player, chestResref, player);
        var belt = await CreateItemAsync(ctx, player, beltResref, player);
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            foreach (var item in new[] { chest, belt })
            {
                StampLegacyLoot(item, NoEnhancements);
                RemoveRequirement(item);
            }
        });
        await EquipAsync(ctx, player, chest, InventorySlot.Chest);
        await EquipAsync(ctx, player, belt, InventorySlot.Belt);

        var maxHP = 0;
        var recordMaxHP = 0;
        var maxStamina = 0;
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            // Native max HP is capped by the fixture's few levels; the player record keeps the full total.
            maxHP = GetMaxHitPoints(player);
            recordMaxHP = DB.Get<Player>(GetObjectUUID(player)).MaxHP;
            SetCurrentHitPoints(player, maxHP);
            maxStamina = Stat.GetMaxStamina(player);
            fixture.Update(record => record.Stamina = maxStamina);
        });
        await ctx.WaitFrameAsync();

        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            new _22_ReviseLootStats().Migrate(player);
            AssertRevisedLoot(ctx, chest, NoEnhancements);
            AssertRevisedLoot(ctx, belt, NoEnhancements);
            var hpDifference = PoolDifference(chestResref, ItemPropertyType.HPBonus) + PoolDifference(beltResref, ItemPropertyType.HPBonus);
            var staminaDifference = PoolDifference(beltResref, ItemPropertyType.Stamina);
            ctx.AssertEqual(recordMaxHP + hpDifference, DB.Get<Player>(GetObjectUUID(player)).MaxHP, "Max HP moves by exactly the revised HP");
            ctx.AssertEqual(maxHP, ObjectPlugin.GetCurrentHitPoints(player), "Raising max HP keeps the player's current HP");
            ctx.AssertEqual(maxStamina + staminaDifference, Stat.GetMaxStamina(player), "Max STM moves by exactly the revised STM");
            ctx.AssertEqual(System.Math.Min(maxStamina, maxStamina + staminaDifference), DB.Get<Player>(GetObjectUUID(player)).Stamina,
                "Current STM is clamped only by the revised drop, not by the old item's whole bonus");
        });
    }

    private static int PoolDifference(string resref, ItemPropertyType type) =>
        LootStatRevisionCatalog.Changes[resref].Where(x => x.Type == type).Sum(x => x.Current - x.Previous);

    [EngineTest("Loot stat migration leaves revised blueprints and unrelated items unchanged", Category = LootCategory)]
    public static async Task UnrelatedLootStatRevision(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("nw_rat001");
        await ctx.WaitFrameAsync();
        var current = await CreateItemAsync(ctx, owner, RevisedGloves, owner);
        var unrelated = await CreateItemAsync(ctx, owner, "b_longsword", owner);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            var before = DescribeProperties(current);
            var unrelatedBefore = DescribeProperties(unrelated);
            ctx.Assert(!LootStatRevisionMigration.MigrateObject(current), "A revised blueprint is already current");
            ctx.Assert(!LootStatRevisionMigration.MigrateObject(unrelated), "An item outside the catalog is ignored");
            ctx.AssertEqual(before, DescribeProperties(current), "The revised blueprint keeps its properties");
            ctx.AssertEqual(unrelatedBefore, DescribeProperties(unrelated), "The unrelated item keeps its properties");
            AssertRevisedLoot(ctx, current, NoEnhancements);
        });
    }

    [EngineTest("Stored loot stat migration reaches more than fifty bank records, market, property storage, contracts and droid inventory controllers", Category = LootCategory, TimeoutSeconds = 60f)]
    public static async Task StoredLootStatRevision(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("nw_rat001");
        await ctx.WaitFrameAsync();
        var gloves = await CreateItemAsync(ctx, owner, RevisedGloves, owner);
        var controller = await CreateItemAsync(ctx, owner, Droid.DroidControlItemResref, owner);
        string saved = null;
        string identity = null;
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            StampLegacyLoot(gloves, GloveEnhancements);
            identity = GetObjectUUID(gloves);
            saved = ObjectPlugin.Serialize(gloves);
            DestroyObject(gloves);
        });
        await ctx.DelaySecondsAsync(0.3f);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            ctx.Assert(LootStatRevisionMigration.MigrateSerializedObject(saved, out var migrated), "Saved legacy gloves need revision");
            var restored = Deserialize(ctx, migrated);
            AssertRevisedLoot(ctx, restored, GloveEnhancements);
            ctx.AssertEqual(identity, GetObjectUUID(restored), "The saved identity survives the revision");
            ctx.Assert(!LootStatRevisionMigration.MigrateSerializedObject(migrated, out var retried), "A revised payload needs no change");
            ctx.AssertEqual(migrated, retried, "An unchanged payload stays byte-for-byte intact");

            SetLocalString(controller, "CONSTRUCTED_DROID", JsonConvert.SerializeObject(new ConstructedDroid
            {
                EquippedItems = { [InventorySlot.Arms] = saved },
                Inventory = { ["spare"] = saved }
            }));
            ctx.Assert(LootStatRevisionMigration.MigrateObject(controller), "Droid controller equipment needs revision");
            var droid = JsonConvert.DeserializeObject<ConstructedDroid>(GetLocalString(controller, "CONSTRUCTED_DROID"));
            AssertRevisedLoot(ctx, Deserialize(ctx, droid.EquippedItems[InventorySlot.Arms]), GloveEnhancements);
            AssertRevisedLoot(ctx, Deserialize(ctx, droid.Inventory["spare"]), GloveEnhancements);
            ctx.Assert(!LootStatRevisionMigration.MigrateObject(controller), "A revised controller needs no change");

            var records = new List<InventoryItem>();
            var market = new MarketItem { Data = saved, Name = "Keep listing name", Quantity = 1, Price = 321 };
            var category = new WorldPropertyCategory
            {
                Items = { ["gloves"] = new WorldPropertyItem { Data = saved, Quantity = 1, Name = "Keep storage name" } }
            };
            var contract = new QuestContract { RewardItems = { new QuestContractItem { Data = saved, Resref = RevisedGloves, StackSize = 1 } } };
            try
            {
                for (var i = 0; i < 55; i++)
                {
                    var record = new InventoryItem { Data = saved, Name = "Keep bank name", Quantity = 1 };
                    records.Add(record);
                    DB.Set(record);
                }
                DB.Set(market);
                DB.Set(category);
                DB.Set(contract);
                for (var run = 1; run <= 2; run++)
                {
                    new _30_ReviseLootStats().Migrate();
                    foreach (var record in records)
                    {
                        var stored = DB.Get<InventoryItem>(record.Id);
                        AssertRevisedLoot(ctx, Deserialize(ctx, stored.Data), GloveEnhancements);
                        ctx.AssertEqual(1, stored.Quantity, $"Run {run}: bank quantity survives");
                        ctx.AssertEqual("Keep bank name", stored.Name, $"Run {run}: bank metadata survives");
                    }
                    var storedMarket = DB.Get<MarketItem>(market.Id);
                    AssertRevisedLoot(ctx, Deserialize(ctx, storedMarket.Data), GloveEnhancements);
                    ctx.AssertEqual(321, storedMarket.Price, $"Run {run}: market price survives");
                    var storedProperty = DB.Get<WorldPropertyCategory>(category.Id).Items["gloves"];
                    AssertRevisedLoot(ctx, Deserialize(ctx, storedProperty.Data), GloveEnhancements);
                    ctx.AssertEqual(1, storedProperty.Quantity, $"Run {run}: property storage quantity survives");
                    var storedContract = DB.Get<QuestContract>(contract.Id).RewardItems.Single();
                    AssertRevisedLoot(ctx, Deserialize(ctx, storedContract.Data), GloveEnhancements);
                }
            }
            finally
            {
                foreach (var record in records)
                    DB.Delete<InventoryItem>(record.Id);
                DB.Delete<MarketItem>(market.Id);
                DB.Delete<WorldPropertyCategory>(category.Id);
                DB.Delete<QuestContract>(contract.Id);
            }
        });
    }

    internal static void StampLegacyLoot(uint item, Dictionary<(ItemPropertyType, int), int> enhancements)
    {
        // Rebuild the item as it shipped before the revision, with any crafted enhancement merged in.
        foreach (var change in LootStatRevisionCatalog.Changes[GetResRef(item)])
        {
            RemoveMatching(item, change.Type, change.Subtype);
            var legacy = change.Previous + enhancements.GetValueOrDefault((change.Type, change.Subtype));
            if (legacy > 0)
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(change.Type, LootSubtype(change), legacy), item);
        }
        DeleteLocalInt(item, LootStatRevisionMigration.RevisionVariable);
    }

    internal static void AssertRevisedLoot(EngineTestContext ctx, uint item, Dictionary<(ItemPropertyType, int), int> enhancements)
    {
        var resref = GetResRef(item);
        foreach (var change in LootStatRevisionCatalog.Changes[resref])
        {
            var expected = change.Current + enhancements.GetValueOrDefault((change.Type, change.Subtype));
            ctx.AssertEqual(expected, SumProperty(item, change.Type, change.Subtype), $"{resref} {change.Type} is revised");
        }
        ctx.AssertEqual(LootStatRevisionMigration.CurrentRevision, GetLocalInt(item, LootStatRevisionMigration.RevisionVariable),
            $"{resref} is marked as revised");
    }

    internal static int LegacyValue(string resref, ItemPropertyType type, int subtype, Dictionary<(ItemPropertyType, int), int> enhancements)
        => LootStatRevisionCatalog.Changes[resref].Single(x => x.Type == type && x.Subtype == subtype).Previous +
           enhancements.GetValueOrDefault((type, subtype));

    private static int LootSubtype(LootStatChange change) => change.Type == ItemPropertyType.Defense ? change.Subtype : -1;

    private static int SumProperty(uint item, ItemPropertyType type, int subtype)
    {
        var total = 0;
        for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            if (GetItemPropertyType(ip) == type && (type != ItemPropertyType.Defense || GetItemPropertySubType(ip) == subtype))
                total += GetItemPropertyCostTableValue(ip);
        return total;
    }

    private static void RemoveMatching(uint item, ItemPropertyType type, int subtype)
    {
        var matches = new List<SWLOR.NWN.API.Engine.ItemProperty>();
        for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            if (GetItemPropertyType(ip) == type && (type != ItemPropertyType.Defense || GetItemPropertySubType(ip) == subtype))
                matches.Add(ip);
        foreach (var ip in matches)
            Invoke("MigrationObject", "RemoveProperty", item, ip);
    }

    internal static void RemoveRequirement(uint item)
    {
        // The fixture has no skill ranks; the revision under test doesn't depend on the requirement.
        RemoveMatching(item, ItemPropertyType.RequiresSkill, 0);
    }

    private static async Task EquipAsync(EngineTestContext ctx, uint creature, uint item, InventorySlot slot)
    {
        // The equip validator cancels swaps, so an occupied slot (such as starting clothes) is emptied first.
        var existing = GetItemInSlot(slot, creature);
        if (GetIsObjectValid(existing) && existing != item)
            await UnequipAsync(ctx, creature, existing);

        AssignCommand(creature, () =>
        {
            ClearAllActions();
            ActionEquipItem(item, slot);
        });
        await ctx.WaitUntilAsync(() => GetItemInSlot(slot, creature) == item, 10f, $"{GetResRef(item)} to be equipped");
        await ctx.WaitFrameAsync();
    }

    private static async Task UnequipAsync(EngineTestContext ctx, uint creature, uint item)
    {
        AssignCommand(creature, () =>
        {
            ClearAllActions();
            ActionUnequipItem(item);
        });
        await ctx.WaitUntilAsync(() => GetItemPossessor(item) == creature &&
                                       Enumerable.Range(0, NumberOfInventorySlots).All(slot => GetItemInSlot((InventorySlot)slot, creature) != item),
            10f, $"{GetResRef(item)} to be unequipped");
        await ctx.WaitFrameAsync();
    }

    private static (int Attack, int PhysicalDefense, int ForceDefense, int MaxHP) ReadEquipmentTotals(uint player)
    {
        var record = DB.Get<Player>(GetObjectUUID(player));
        return (record.Attack,
            record.Defenses.GetValueOrDefault(CombatDamageType.Physical),
            record.Defenses.GetValueOrDefault(CombatDamageType.Force),
            record.MaxHP);
    }

    private static void AssertTotals(EngineTestContext ctx,
        (int Attack, int PhysicalDefense, int ForceDefense, int MaxHP) baseline,
        (int Attack, int PhysicalDefense, int ForceDefense, int MaxHP) actual,
        Dictionary<(ItemPropertyType, int), int> enhancements, string when)
    {
        int Revised(ItemPropertyType type, int subtype)
        {
            var current = LootStatRevisionCatalog.Changes[RevisedGloves]
                .Where(x => x.Type == type && x.Subtype == subtype)
                .Select(x => x.Current)
                .SingleOrDefault();
            return current + enhancements.GetValueOrDefault((type, subtype));
        }
        ctx.AssertEqual(baseline.Attack + Revised(ItemPropertyType.Attack, 0), actual.Attack, $"Attack counts the revised gloves {when}");
        ctx.AssertEqual(baseline.PhysicalDefense + Revised(ItemPropertyType.Defense, (int)CombatDamageType.Physical), actual.PhysicalDefense,
            $"Defense counts the revised gloves {when}");
        ctx.AssertEqual(baseline.ForceDefense + Revised(ItemPropertyType.Defense, (int)CombatDamageType.Force), actual.ForceDefense,
            $"Force Defense counts the revised gloves {when}");
        ctx.AssertEqual(baseline.MaxHP + Revised(ItemPropertyType.HPBonus, 0), actual.MaxHP, $"Max HP counts the revised gloves {when}");
    }
}
