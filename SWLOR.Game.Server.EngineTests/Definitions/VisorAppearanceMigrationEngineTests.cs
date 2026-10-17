using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Combined player item migration repairs worn and nested visor models and artwork before two bank reloads", Category = HeadgearCategory, TimeoutSeconds = 90)]
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
        var identity = GetObjectUUID(visor);
        var properties = DescribeProperties(visor);
        var totals = ReadEquipmentTotals(player);
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            ctx.AssertEqual(223, GetItemAppearance(visor, ItemAppearanceType.SimpleModel, 0), "Fixture reproduces the shipped reserved helmet model");
            new _19_UpdateItemIcons().Migrate(player);
            ctx.AssertEqual(88, GetItemAppearance(material, ItemAppearanceType.SimpleModel, 0), "The earlier checkpoint does not perform another artwork traversal");
            new _22_ReviseLootStats().Migrate(player);
            ctx.AssertEqual(110, GetItemAppearance(visor, ItemAppearanceType.SimpleModel, 0), "The combined player migration repairs the worn helmet");
            ctx.AssertEqual(110, GetItemAppearance(nested, ItemAppearanceType.SimpleModel, 0), "The same traversal repairs nested headgear");
            ctx.AssertEqual(33, GetItemAppearance(custom, ItemAppearanceType.SimpleModel, 0), "Valid customized visor models survive");
            ctx.AssertEqual(ItemIconMigration.GetUpdatedModel("aen_hp1", (int)GetBaseItemType(material), 88),
                GetItemAppearance(material, ItemAppearanceType.SimpleModel, 0), "The same traversal updates other item artwork");
            ctx.AssertEqual(visor, GetItemInSlot(InventorySlot.Head, player), "Appearance repair keeps the same item equipped");
            ctx.AssertEqual(totals, ReadEquipmentTotals(player), "Appearance-only repair leaves equipment totals unchanged");
            ctx.Assert(!LootStatRevisionMigration.MigratePlayerItems(player), "A combined migration retry is stable");
        });
        for (var cycle = 1; cycle <= 2; cycle++)
        {
            await UnequipAsync(ctx, player, visor);
            InventoryItem stored = null;
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                SetDescription(visor, "Changed before bank cycle " + cycle);
                stored = Bank.DepositItem(player, visor);
                ctx.AssertEqual(fixture.Id, stored.PlayerId, "Bank ownership is retained");
                ctx.AssertEqual(1, stored.Quantity, "Exactly one visor is saved");
            });
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(visor), 5f, "the saved visor to leave live inventory");
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                new _27_UpdateStoredItems().Migrate();
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
