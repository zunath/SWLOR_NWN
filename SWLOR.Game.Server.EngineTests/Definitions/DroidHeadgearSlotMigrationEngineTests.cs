using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class DroidInventoryEngineTests
{
    [EngineTest("Migration repairs saved droid chest visor before loot revision through two resummons", Category = "DroidInventory", TimeoutSeconds = 90)]
    public static Task HeadgearSavedSlotFirst(EngineTestContext ctx) => VerifySavedHeadgear(ctx, false);

    [EngineTest("Migration repairs saved droid chest visor after loot revision through two resummons", Category = "DroidInventory", TimeoutSeconds = 90)]
    public static Task HeadgearSavedStatsFirst(EngineTestContext ctx) => VerifySavedHeadgear(ctx, true);

    private static async Task VerifySavedHeadgear(EngineTestContext ctx, bool statsFirst)
    {
        var owner = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        var controller = await CreateController(ctx, owner);
        var visor = await CreateLoot(ctx, owner, "ruptorvanecr");
        var carried = await CreateLoot(ctx, owner, "thermlancercr");
        var head = await CreateLoot(ctx, owner, "charm_cowl");
        var uuid = GetObjectUUID(visor);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            MigrationEngineTests.StampLegacyVisor(visor);
            MigrationEngineTests.StampLegacyVisor(carried);
            MigrationEngineTests.RemoveRequirement(head);
            foreach (var item in new[] { visor, carried, head })
                AddItemProperty(DurationType.Permanent, ItemPropertyLimitUseByRace(RacialType.Droid), item);
            SetLocalString(head, "DROID_ITEM_ID", "collision");
            SetLocalString(visor, "DROID_ITEM_ID", "collision");
            Droid.SaveConstructedDroid(controller, new ConstructedDroid
            {
                Name = "Headgear fixture",
                EquippedItems = { [InventorySlot.Chest] = ObjectPlugin.Serialize(visor), [InventorySlot.Head] = ObjectPlugin.Serialize(head) },
                Inventory = { ["collision"] = ObjectPlugin.Serialize(carried) },
            });
            DestroyObject(visor);
            DestroyObject(carried);
            DestroyObject(head);
            if (statsFirst) LootStatRevisionMigration.MigrateObject(controller);
            ctx.Assert(HeadgearSlotMigration.MigrateObject(controller), "A genuinely legacy controller is repaired");
            if (!statsFirst) LootStatRevisionMigration.MigrateObject(controller);
            ctx.Assert(!HeadgearSlotMigration.MigrateObject(controller), "Controller retry is stable");
            var saved = Droid.LoadConstructedDroid(controller);
            ctx.Assert(!saved.EquippedItems.ContainsKey(InventorySlot.Chest), "No saved chest visor remains");
            ctx.Assert(saved.EquippedItems.ContainsKey(InventorySlot.Head), "Existing saved helmet is preserved");
            ctx.AssertEqual(2, saved.Inventory.Count, "Both carried and stowed copies are retained despite colliding historical IDs");
        });
        await ctx.DelaySecondsAsync(0.3f);
        var droid = await Spawn(ctx, owner, controller);
        try
        {
            for (var cycle = 0; cycle <= 2; cycle++)
            {
                visor = MigrationEngineTests.FindHeadgear(droid, "ruptorvanecr");
                head = GetItemInSlot(InventorySlot.Head, droid);
                ctx.AssertEqual("charm_cowl", GetResRef(head), "The original head slot is preserved after summon");
                ctx.AssertEqual(OBJECT_INVALID, GetItemInSlot(InventorySlot.Chest, droid), "The chest slot is clear");
                ctx.AssertEqual(3, Items(droid).Count(), "Exactly three items survive each save/reload cycle");
                ctx.AssertEqual(droid, GetItemPossessor(visor), "The droid owns the converted copy");
                MigrationEngineTests.AssertVisorCustomization(ctx, visor, uuid);
                MigrationEngineTests.AssertRevisedLoot(ctx, visor, MigrationEngineTests.VisorEnhancements);
                await Unequip(ctx, droid, head);
                var baseline = SkinAttack(droid);
                await Equip(ctx, droid, visor, InventorySlot.Head);
                ctx.AssertEqual(baseline + 10, SkinAttack(droid), "Equip adds revised Attack and crafted bonus exactly once");
                await ctx.ExecuteInCreatureContextAsync(droid, () =>
                {
                    ctx.Assert(!HeadgearSlotMigration.MigrateObject(droid), "Live droid retry does not revise equipment again");
                    ctx.Assert(!LootStatRevisionMigration.MigrateObject(droid), "Live loot retry is stable");
                });
                await Unequip(ctx, droid, visor);
                ctx.AssertEqual(baseline, SkinAttack(droid), "Unequip removes precisely the visor's contribution");
                await Equip(ctx, droid, head, InventorySlot.Head);
                if (cycle == 2) break;
                await Dismiss(ctx, owner, droid);
                droid = await Spawn(ctx, owner, controller);
            }
        }
        finally { if (GetIsObjectValid(droid)) await Dismiss(ctx, owner, droid); }
    }

    [EngineTest("Migration repairs summoned droid legacy chest gear and saves the live copies", Category = "DroidInventory", TimeoutSeconds = 90)]
    public static Task HeadgearSummonedDroid(EngineTestContext ctx) => VerifySummonedHeadgear(ctx, false);

    [EngineTest("Migration preserves occupied droid headgear and clamps visor resource pools across reloads", Category = "DroidInventory", TimeoutSeconds = 90)]
    public static Task HeadgearSummonedDroidOccupiedHead(EngineTestContext ctx) => VerifySummonedHeadgear(ctx, true);

    private static async Task VerifySummonedHeadgear(EngineTestContext ctx, bool occupied)
    {
        var owner = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        var controller = await CreateController(ctx, owner);
        var droid = await Spawn(ctx, owner, controller);
        if (occupied)
        {
            var head = await CreateLoot(ctx, droid, "charm_cowl");
            await ctx.ExecuteInCreatureContextAsync(droid, () =>
            {
                MigrationEngineTests.RemoveRequirement(head);
                AddItemProperty(DurationType.Permanent, ItemPropertyLimitUseByRace(RacialType.Droid), head);
            });
            await Equip(ctx, droid, head, InventorySlot.Head);
        }
        var visor = await CreateLoot(ctx, droid, "ruptorvanecr");
        var baseline = SkinAttack(droid);
        await ctx.ExecuteInCreatureContextAsync(droid, () =>
        {
            MigrationEngineTests.StampLegacyVisor(visor);
            AddItemProperty(DurationType.Permanent, ItemPropertyLimitUseByRace(RacialType.Droid), visor);
        });
        await ctx.WaitFrameAsync();
        await Equip(ctx, droid, visor, InventorySlot.Chest);
        ctx.AssertEqual(baseline + 34, SkinAttack(droid), "Live legacy armor contributes its original Attack once");
        var uuid = GetObjectUUID(visor);
        await ctx.ExecuteInCreatureContextAsync(droid, () =>
        {
            var hp = GetMaxHitPoints(droid);
            var fp = Stat.GetMaxFP(droid);
            var stamina = Stat.GetMaxStamina(droid);
            SetCurrentHitPoints(droid, hp);
            SetLocalInt(droid, "FP", fp);
            SetLocalInt(droid, "STAMINA", stamina);
            ctx.Assert(HeadgearSlotMigration.MigrateObject(droid), "Live legacy droid equipment converts");
            LootStatRevisionMigration.MigrateObject(droid);
            if (occupied)
                ctx.AssertEqual("charm_cowl", GetResRef(GetItemInSlot(InventorySlot.Head, droid)), "The droid keeps its existing helmet");
            else
                ctx.AssertEqual(visor, GetItemInSlot(InventorySlot.Head, droid), "The live droid can equip the converted visor");
            ctx.AssertEqual(OBJECT_INVALID, GetItemInSlot(InventorySlot.Chest, droid), "Live chest is cleared");
            ctx.AssertEqual(baseline + (occupied ? 0 : 10), SkinAttack(droid), "Migration removes the old contribution and counts only worn gear");
            ctx.AssertEqual(System.Math.Min(hp, GetMaxHitPoints(droid)), ObjectPlugin.GetCurrentHitPoints(droid), "Droid HP stays within the final cap");
            ctx.AssertEqual(System.Math.Min(fp, Stat.GetMaxFP(droid)), Stat.GetCurrentFP(droid), "Droid FP stays within the final cap");
            ctx.AssertEqual(System.Math.Min(stamina, Stat.GetMaxStamina(droid)), Stat.GetCurrentStamina(droid), "Droid stamina stays within the final cap");
        });
        try
        {
            for (var cycle = 0; cycle < 2; cycle++)
            {
                if (occupied)
                {
                    await Dismiss(ctx, owner, droid);
                    droid = await Spawn(ctx, owner, controller);
                    visor = MigrationEngineTests.FindHeadgear(droid, "ruptorvanecr");
                    MigrationEngineTests.AssertVisorCustomization(ctx, visor, uuid);
                    ctx.AssertEqual(2, Items(droid).Count(), "The original helmet and stowed visor both survive reload");
                    ctx.AssertEqual("charm_cowl", GetResRef(GetItemInSlot(InventorySlot.Head, droid)), "Reload keeps the original helmet equipped");
                    ctx.AssertEqual(baseline, SkinAttack(droid), "The inventory visor grants no Attack after reload");
                    continue;
                }
                await Unequip(ctx, droid, visor);
                ctx.AssertEqual(baseline, SkinAttack(droid), "Repeated unequip has no stale Attack");
                await Equip(ctx, droid, visor, InventorySlot.Head);
                await Dismiss(ctx, owner, droid);
                droid = await Spawn(ctx, owner, controller);
                visor = GetItemInSlot(InventorySlot.Head, droid);
                MigrationEngineTests.AssertVisorCustomization(ctx, visor, uuid);
                ctx.AssertEqual(1, Items(droid).Count(), "Dismissal and resummon preserve one owned item");
                ctx.AssertEqual(baseline + 10, SkinAttack(droid), "Reload reconstructs the same legitimate Attack");
            }
        }
        finally { if (GetIsObjectValid(droid)) await Dismiss(ctx, owner, droid); }
    }
}
