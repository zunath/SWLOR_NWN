using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
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
                StoredItemSweepRunner.Run();
                var template = Deserialize(ctx, DB.Get<PlayerOutfit>(outfit.Id).Data);
                ctx.AssertEqual(BaseItem.Armor, GetBaseItemType(template), "Cosmetic legacy template retains its armor type through the merged migration");
                ctx.AssertEqual(156, GetItemAppearance(template, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso), "Cosmetic legacy template retains its historical torso");
                ctx.AssertEqual(LootStatRevisionMigration.CurrentRevision, GetLocalInt(template, LootStatRevisionMigration.RevisionVariable), "The existing loot revision still processes the outfit in the same record pass");
                Invoke("MigrationObject", "DestroyTemporaryObject", template);
                var converted = Deserialize(ctx, DB.Get<InventoryItem>(bank.Id).Data);
                ctx.AssertEqual(BaseItem.Helmet, GetBaseItemType(converted), "The same legacy payload in actual storage is converted");
                Invoke("MigrationObject", "DestroyTemporaryObject", converted);
                LoadOutfit();
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
                    ctx.Assert(Wardrobe.Store(player, DB.Get<PlayerOutfit>(outfit.Id), out _), "The production outfit store saves the worn clothes");
                    StoredItemSweepRunner.Run();
                    EquippedItemAppearance.Set(chest, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso, model + 2);
                    EquippedItemAppearance.Set(chest, ItemAppearanceType.ArmorColor, 0, color + 2);
                    LoadOutfit();
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

        void LoadOutfit()
        {
            var owned = Wardrobe.GetOutfits(GetObjectUUID(player)).Find(saved => saved.Id == outfit.Id);
            ctx.Assert(owned != null, "The production outfit query finds the owned outfit");
            ctx.Assert(Wardrobe.Wear(player, owned, out _), "The production outfit loader wears the outfit");
        }

        int CarriedCount()
        {
            var result = 0;
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player)) result++;
            return result;
        }
    }
}
