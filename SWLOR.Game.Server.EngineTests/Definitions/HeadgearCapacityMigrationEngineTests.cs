using System;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Full inventory sends a converted visor to overflow storage without losing it and retries without drift", Category = HeadgearCategory, TimeoutSeconds = 120)]
    public static async Task HeadgearCapacityOverflowsToStorage(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        var oldChest = GetItemInSlot(InventorySlot.Chest, player);
        if (GetIsObjectValid(oldChest))
        {
            await UnequipAsync(ctx, player, oldChest);
            DestroyObject(oldChest);
            await ctx.WaitFrameAsync();
        }
        var visor = await CreateItemAsync(ctx, player, "ruptorvanecr", player);
        var head = await CreateItemAsync(ctx, player, "charm_cowl", player);
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            StampLegacyVisor(visor);
            StampLegacyLoot(head, NoEnhancements);
            RemoveRequirement(head);
        });
        var baseline = ReadEquipmentTotals(player);
        await EquipAsync(ctx, player, head, InventorySlot.Head);
        await EquipAsync(ctx, player, visor, InventorySlot.Chest);
        var fillers = 0;
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            // A 2x3 outfit fills six cells; sixty of them cover the 10x36 inventory a character file holds.
            for (; fillers < 60; fillers++)
            {
                var carried = CreateItemOnObject("nw_maletatcivout", player);
                if (!GetIsObjectValid(carried) || GetItemPossessor(carried) != player)
                    break;
            }
        });
        ctx.AssertEqual(60, fillers, "The fixture fills its whole inventory");

        Player migratedRecord = null;
        byte[] migratedFile = null;
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            var upgraded = SavedCharacterMigration.Run(player, DB.Get<Player>(fixture.Id), "_22_ReviseLootStats");
            migratedRecord = upgraded.Record;
            migratedFile = upgraded.File;
            ctx.AssertEqual(1, upgraded.Overflow.Count, "The visor that fits nowhere goes to overflow storage");
            ctx.AssertEqual("ruptorvanecr", upgraded.Overflow[0].ResRef, "Overflow storage keeps the converted visor");
            var character = SavedCharacterMigration.Load(ctx, upgraded);
            ctx.AssertEqual(OBJECT_INVALID, GetItemInSlot(InventorySlot.Chest, character), "The legacy visor leaves the chest slot");
            var restoredHead = GetItemInSlot(InventorySlot.Head, character);
            ctx.AssertEqual("charm_cowl", GetResRef(restoredHead), "The existing helmet stays equipped");
            ctx.AssertEqual(0, SavedCharacterMigration.FindItems(character, "ruptorvanecr").Count,
                "The visor is not duplicated into the character file");
            AssertRevisedLoot(ctx, restoredHead, NoEnhancements);

            var overflowVisor = Deserialize(ctx, upgraded.Overflow[0].Data);
            ctx.AssertEqual(BaseItem.Helmet, GetBaseItemType(overflowVisor), "The stored visor is converted to headgear");
            // The upgrade revises items it sends to overflow storage, so claiming one leaves the acquire rule nothing to do.
            AssertRevisedLoot(ctx, overflowVisor, VisorEnhancements);
            ctx.Assert(!LootStatRevisionMigration.MigrateObject(overflowVisor), "The claimed visor needs no further revision");
            ctx.AssertEqual("Customized salvage", GetName(overflowVisor), "Custom name survives");
            ctx.AssertEqual("fixture-crafter", GetLocalString(overflowVisor, "CRAFTED_BY"), "Crafting identity survives");
            ctx.AssertEqual(9, SumProperty(overflowVisor, ItemPropertyType.FP, 0), "Custom FP property survives");
            ctx.AssertEqual(11, SumProperty(overflowVisor, ItemPropertyType.Stamina, 0), "Custom stamina property survives");

            // Only the equipped helmet contributes Attack once the visor is stowed.
            var expectedAttack = baseline.Attack + SumProperty(restoredHead, ItemPropertyType.Attack, 0);
            ctx.AssertEqual(expectedAttack, upgraded.Record.Attack, "Only the equipped helmet contributes Attack");
        });

        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            var retried = SavedCharacterMigration.Run(migratedFile, migratedRecord, "_22_ReviseLootStats");
            ctx.Assert(retried.File.SequenceEqual(migratedFile), "A retry leaves the character file unchanged");
            ctx.AssertEqual(0, retried.Overflow.Count, "A retry does not store the visor again");
            ctx.AssertEqual(migratedRecord.Attack, retried.Record.Attack, "A retry has no persisted Attack drift");
            ctx.AssertEqual(migratedRecord.MaxHP, retried.Record.MaxHP, "A retry has no persisted HP drift");
        });
    }
}
