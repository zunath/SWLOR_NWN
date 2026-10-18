using System;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Character migration 23 repairs version 22 worn and nested visors and artwork before two bank reloads", Category = HeadgearCategory, TimeoutSeconds = 120)]
    public static async Task ReservedVisorModelsAndArtwork(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        var visor = await CreateItemAsync(ctx, player, "ruptorvanecr", player);
        var bag = await CreateItemAsync(ctx, player, "bag_sm", player);
        var nested = await CreateItemAsync(ctx, player, "thermlancercr", bag);
        var custom = await CreateItemAsync(ctx, player, "precision_optic", bag);
        var material = await CreateItemAsync(ctx, player, "aen_hp1", bag);
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            RemoveRequirement(visor);
            foreach (var item in new[] { visor, nested })
                ItemPlugin.SetItemAppearance(item, ItemAppearanceType.SimpleModel, 0, 223);
            ItemPlugin.SetItemAppearance(custom, ItemAppearanceType.SimpleModel, 0, 33);
            ItemPlugin.SetItemAppearance(material, ItemAppearanceType.SimpleModel, 0, 88);
            SetLocalString(visor, "CRAFTED_BY", "visor-crafter");
            SetName(visor, "Customized visor");
        });
        var oldHead = GetItemInSlot(InventorySlot.Head, player);
        if (GetIsObjectValid(oldHead)) await UnequipAsync(ctx, player, oldHead);
        await EquipAsync(ctx, player, visor, InventorySlot.Head);
        var properties = DescribeProperties(visor);
        var totals = ReadEquipmentTotals(player);
        SavedCharacterMigration.Upgraded upgraded = null;
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            const string checkpointVariable = "PLAYER_MIGRATION_VERSION";
            var record = DB.Get<Player>(fixture.Id);
            record.Version = 22;
            DB.Set(record);
            SetLocalInt(player, checkpointVariable, 22);
            ctx.Assert(SavedCharacterMigration.Versions.Where(version => version > 22).SequenceEqual(new[] { 23 }),
                "A released version 22 character receives exactly one combined item migration");
            ctx.AssertEqual(223, GetItemAppearance(visor, ItemAppearanceType.SimpleModel, 0), "Fixture reproduces the shipped reserved helmet model");
            upgraded = SavedCharacterMigration.Upgrade(SavedCharacterMigration.SaveFile(player), record);
            ctx.Assert(upgraded != null, "The character upgrade repairs a version 22 character");
            ctx.AssertEqual(23, upgraded.Record.Version, "The record checkpoint advances after repair");
            ctx.AssertEqual(23, upgraded.FileVersion, "The saved character contains the new checkpoint");

            var character = SavedCharacterMigration.Load(ctx, upgraded);
            var migratedVisor = GetItemInSlot(InventorySlot.Head, character);
            var migratedBag = SavedCharacterMigration.FindItem(character, "bag_sm");
            var migratedNested = SavedCharacterMigration.FindItem(character, "thermlancercr");
            var migratedCustom = SavedCharacterMigration.FindItem(character, "precision_optic");
            var migratedMaterial = SavedCharacterMigration.FindItem(character, "aen_hp1");
            ctx.AssertEqual("ruptorvanecr", GetResRef(migratedVisor), "Appearance repair keeps the same item equipped");
            ctx.AssertEqual(110, GetItemAppearance(migratedVisor, ItemAppearanceType.SimpleModel, 0), "The combined character migration repairs the worn helmet");
            ctx.AssertEqual(110, GetItemAppearance(migratedNested, ItemAppearanceType.SimpleModel, 0), "The same traversal repairs nested headgear");
            ctx.AssertEqual(33, GetItemAppearance(migratedCustom, ItemAppearanceType.SimpleModel, 0), "Valid customized visor models survive");
            ctx.AssertEqual(ItemIconMigration.GetUpdatedModel("aen_hp1", (int)GetBaseItemType(migratedMaterial), 88),
                GetItemAppearance(migratedMaterial, ItemAppearanceType.SimpleModel, 0), "The same traversal updates other item artwork");
            ctx.AssertEqual(migratedBag, GetItemPossessor(migratedNested, true), "The nested visor stays in its bag");
            ctx.AssertEqual(totals, ReadEquipmentTotals(upgraded.Record), "Appearance-only repair leaves equipment totals unchanged");
            ctx.Assert(SavedCharacterMigration.Upgrade(upgraded.File, upgraded.Record) == null,
                "A completed migration retry succeeds without walking items again");
            DeleteLocalInt(player, checkpointVariable);
        });

        // The player logs in with the repaired character and moves the visor through the bank twice.
        using var login = await PlayerAbilityFixture.CreateFromCharacterAsync(ctx, upgraded.File, upgraded.Record, 3f);
        player = login.Creature;
        visor = GetItemInSlot(InventorySlot.Head, player);
        var identity = GetObjectUUID(visor);
        ctx.AssertEqual(properties, DescribeProperties(visor), "Appearance-only repair leaves the visor's properties unchanged");
        ctx.AssertEqual(totals, ReadEquipmentTotals(player), "The logged-in character holds the same equipment contribution");
        for (var cycle = 1; cycle <= 2; cycle++)
        {
            await UnequipAsync(ctx, player, visor);
            InventoryItem stored = null;
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                SetDescription(visor, "Changed before bank cycle " + cycle);
                stored = Bank.DepositItem(player, visor);
                ctx.AssertEqual(login.Id, stored.PlayerId, "Bank ownership is retained");
                ctx.AssertEqual(1, stored.Quantity, "Exactly one visor is saved");
            });
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(visor), 5f, "the saved visor to leave live inventory");
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                new _28_CorrectStoredEquipmentTypes().Migrate();
                Bank.WithdrawItem(player, stored.Id);
            });
            await ctx.WaitFrameAsync();
            visor = FindHeadgear(player, "ruptorvanecr");
            ctx.AssertEqual(1, CountHeadgear(player, "ruptorvanecr"), "Reload neither loses nor duplicates the visor");
            ctx.AssertEqual(player, GetItemPossessor(visor), "The player owns the restored visor");
            ctx.AssertEqual(identity, GetObjectUUID(visor), "The native identity survives");
            ctx.AssertEqual(properties, DescribeProperties(visor), "Crafted properties survive");
            ctx.AssertEqual("visor-crafter", GetLocalString(visor, "CRAFTED_BY"), "Crafting metadata survives");
            ctx.AssertEqual("Changed before bank cycle " + cycle, GetDescription(visor), "Changed state survives each save/reload");
            ctx.AssertEqual(110, GetItemAppearance(visor, ItemAppearanceType.SimpleModel, 0), "The repaired model survives reload");
            await EquipAsync(ctx, player, visor, InventorySlot.Head);
            ctx.AssertEqual(totals, ReadEquipmentTotals(player), "Re-equipping restores the same equipment contribution");
        }
    }
}
