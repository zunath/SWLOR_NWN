using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.MigrationDefinition;
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

    // A crafted Attack enhancement and a Defense enhancement merged into the gloves' own properties.
    internal static readonly Dictionary<(ItemPropertyType, int), int> GloveEnhancements = new()
    {
        [(ItemPropertyType.Attack, 0)] = 5,
        [(ItemPropertyType.Defense, (int)CombatDamageType.Physical)] = 4,
    };

    internal static readonly Dictionary<(ItemPropertyType, int), int> NoEnhancements = new();

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
                    StoredItemSweepRunner.Run();
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
        if (RareLootStatRevisionCatalog.Changes.TryGetValue(GetResRef(item), out var corrections))
            foreach (var change in corrections)
                RemoveMatching(item, change.Type, change.Subtype);
        // Rebuild the item as it shipped before the revision, with any crafted enhancement merged in.
        foreach (var change in LootStatRevisionCatalog.Changes[GetResRef(item)])
        {
            RemoveMatching(item, change.Type, change.Subtype);
            var legacy = change.Previous + enhancements.GetValueOrDefault((change.Type, change.Subtype));
            if (legacy > 0)
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(change.Type, LootSubtype(change), legacy), item);
        }
        DeleteLocalInt(item, LootStatRevisionMigration.RevisionVariable);
        DeleteLocalInt(item, RareLootStatRevisionCatalog.RevisionVariable);
        DeleteLocalInt(item, FoundryLootStatRevisionCatalog.RevisionVariable);
    }

    internal static void AssertRevisedLoot(EngineTestContext ctx, uint item, Dictionary<(ItemPropertyType, int), int> enhancements)
    {
        var resref = GetResRef(item);
        var final = LootStatRevisionCatalog.Changes[resref].ToDictionary(x => (x.Type, x.Subtype));
        if (RareLootStatRevisionCatalog.Changes.TryGetValue(resref, out var corrections))
        {
            foreach (var change in corrections)
                final[(change.Type, change.Subtype)] = change;
            ctx.AssertEqual(RareLootStatRevisionCatalog.CurrentRevision,
                GetLocalInt(item, RareLootStatRevisionCatalog.RevisionVariable), $"{resref} is marked as corrected");
        }
        if (FoundryLootStatRevisionCatalog.Changes.TryGetValue(resref, out var foundryCorrections))
        {
            foreach (var change in foundryCorrections)
                final[(change.Type, change.Subtype)] = change;
            ctx.AssertEqual(FoundryLootStatRevisionCatalog.CurrentRevision,
                GetLocalInt(item, FoundryLootStatRevisionCatalog.RevisionVariable), $"{resref} is marked as Foundry-corrected");
        }
        foreach (var change in final.Values)
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

    private static (int Attack, int PhysicalDefense, int ForceDefense, int MaxHP) ReadEquipmentTotals(uint player) =>
        ReadEquipmentTotals(DB.Get<Player>(GetObjectUUID(player)));

    private static (int Attack, int PhysicalDefense, int ForceDefense, int MaxHP) ReadEquipmentTotals(Player record)
    {
        return (record.Attack,
            record.Defenses.GetValueOrDefault(CombatDamageType.Physical),
            record.Defenses.GetValueOrDefault(CombatDamageType.Force),
            record.MaxHP);
    }

}
