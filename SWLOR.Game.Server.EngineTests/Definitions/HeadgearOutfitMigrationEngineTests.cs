using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Legacy visor outfit templates remain usable through migration and two normal outfit save reloads", Category = HeadgearCategory, TimeoutSeconds = 90)]
    public static async Task LegacyVisorOutfitRemainsUsable(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        var chest = GetItemInSlot(InventorySlot.Chest, player);
        ctx.AssertEqual(BaseItem.Armor, GetBaseItemType(chest), "The wearer has ordinary body armor");
        var outfit = new PlayerOutfit { PlayerId = GetObjectUUID(player), Name = "Legacy visor outfit", Data = LegacyHeadgearFixtures.Ruptor };
        var bank = new InventoryItem { PlayerId = outfit.PlayerId, StorageId = "GLOBAL_BANK", Quantity = 1, Data = outfit.Data };
        var uuid = GetObjectUUID(chest);
        var totals = ReadEquipmentTotals(player);
        var properties = DescribeProperties(chest);
        var count = CarriedCount();
        try
        {
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                DB.Set(outfit);
                DB.Set(bank);
                new _30_ReviseLootStats().Migrate();
                var template = Deserialize(ctx, DB.Get<PlayerOutfit>(outfit.Id).Data);
                ctx.AssertEqual(BaseItem.Armor, GetBaseItemType(template), "Cosmetic legacy template retains its armor type through the merged migration");
                ctx.AssertEqual(156, GetItemAppearance(template, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso), "Cosmetic legacy template retains its historical torso");
                ctx.AssertEqual(LootStatRevisionMigration.CurrentRevision, GetLocalInt(template, LootStatRevisionMigration.RevisionVariable), "The existing loot revision still processes the outfit in the same record pass");
                Invoke("MigrationObject", "DestroyTemporaryObject", template);
                var converted = Deserialize(ctx, DB.Get<InventoryItem>(bank.Id).Data);
                ctx.AssertEqual(BaseItem.Helmet, GetBaseItemType(converted), "The same legacy payload in actual storage is converted");
                Invoke("MigrationObject", "DestroyTemporaryObject", converted);
                LoadOutfit(BindOutfit());
                ctx.AssertEqual(156, GetItemAppearance(chest, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso), "Historical torso applies through the production outfit loader");
            });
            await ctx.WaitFrameAsync();
            for (var cycle = 0; cycle < 2; cycle++)
            {
                var model = cycle + 1;
                var color = cycle + 19;
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    EquippedItemAppearance.Set(chest, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso, model);
                    EquippedItemAppearance.Set(chest, ItemAppearanceType.ArmorColor, 0, color);
                    var view = BindOutfit();
                    view.OnClickStoreOutfit()();
                    view.OnModalConfirmClick()();
                    new _30_ReviseLootStats().Migrate();
                    EquippedItemAppearance.Set(chest, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso, model + 2);
                    EquippedItemAppearance.Set(chest, ItemAppearanceType.ArmorColor, 0, color + 2);
                    LoadOutfit(BindOutfit());
                    ctx.AssertEqual(model, GetItemAppearance(chest, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso), $"Cycle {cycle}: normal outfit save and load restores the model");
                    ctx.AssertEqual(color, GetItemAppearance(chest, ItemAppearanceType.ArmorColor, 0), $"Cycle {cycle}: normal outfit save and load restores the dye");
                });
                await ctx.WaitFrameAsync();
                ctx.AssertEqual(chest, GetItemInSlot(InventorySlot.Chest, player), "Outfit loading preserves equipment occupancy");
                ctx.AssertEqual(player, GetItemPossessor(chest), "Outfit loading preserves ownership");
                ctx.AssertEqual(uuid, GetObjectUUID(chest), "Outfit loading preserves item identity");
                ctx.AssertEqual(count, CarriedCount(), "Outfit loading creates no carried items");
                ctx.AssertEqual(properties, DescribeProperties(chest), "Outfit loading preserves item properties");
                ctx.AssertEqual(totals, ReadEquipmentTotals(player), "Outfit loading leaves equipment stats unchanged");
            }
        }
        finally
        {
            DB.Delete<PlayerOutfit>(outfit.Id);
            DB.Delete<InventoryItem>(bank.Id);
        }

        OutfitViewModel BindOutfit()
        {
            var geometry = new GuiRectangle(0, 0, 800, 600);
            var view = new OutfitViewModel { Geometry = geometry };
            view.Bind(player, 0, geometry, GuiWindowType.Outfits, null, OBJECT_INVALID);
            var ids = (List<string>)typeof(OutfitViewModel).GetField("_outfitIds", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
            view.SelectedSlotIndex = ids.IndexOf(outfit.Id);
            ctx.Assert(view.SelectedSlotIndex >= 0, "The production outfit query finds the owned outfit");
            return view;
        }

        void LoadOutfit(OutfitViewModel view)
        {
            view.OnClickLoadOutfit()();
            view.OnModalConfirmClick()();
        }

        int CarriedCount()
        {
            var result = 0;
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player)) result++;
            return result;
        }
    }
}
