using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class WardrobeEngineTests
{
    private const string Category = "Wardrobe";
    private const string HelmetResref = "advent_helmet";

    [EngineTest("A saved outfit restores the worn helmet's model and dye with the clothes", Category = Category, TimeoutSeconds = 60)]
    public static async Task OutfitRestoresHelmetLook(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        var chest = GetItemInSlot(InventorySlot.Chest, player);
        var helmet = OBJECT_INVALID;
        var outfit = new PlayerOutfit { PlayerId = GetObjectUUID(player), Name = "Helmet outfit" };
        try
        {
            // Creating and equipping need separate script contexts.
            await ctx.ExecuteInCreatureContextAsync(player, () => helmet = CreateItemOnObject(HelmetResref, player));
            await ctx.ExecuteInCreatureContextAsync(player, () => ActionEquipItem(helmet, InventorySlot.Head));
            await ctx.WaitUntilAsync(() => GetItemInSlot(InventorySlot.Head, player) == helmet, 10f, "the helmet is worn");
            var helmetUuid = GetObjectUUID(helmet);
            var savedModel = GetItemAppearance(helmet, ItemAppearanceType.SimpleModel, -1);
            var otherModel = 45;
            ctx.Assert(savedModel != otherModel, "The test changes the helmet to a different model");

            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                EquippedItemAppearance.Set(helmet, ItemAppearanceType.ArmorColor, (int)AppearanceArmorColor.Cloth1, 21);
                EquippedItemAppearance.Set(chest, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso, 1);
                DB.Set(outfit);
                ctx.Assert(Wardrobe.Store(player, DB.Get<PlayerOutfit>(outfit.Id), out _), "The outfit saves");
                var stored = DB.Get<PlayerOutfit>(outfit.Id);
                ctx.Assert(Wardrobe.HasHelmet(stored), "The worn helmet is saved with the outfit");
                ctx.AssertEqual(savedModel, stored.HelmetId, "The saved helmet model is recorded");

                EquippedItemAppearance.Set(helmet, ItemAppearanceType.SimpleModel, -1, otherModel);
                EquippedItemAppearance.Set(helmet, ItemAppearanceType.ArmorColor, (int)AppearanceArmorColor.Cloth1, 5);
                EquippedItemAppearance.Set(chest, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso, 2);

                ctx.Assert(Wardrobe.Wear(player, stored, out var message), "The outfit is worn");
                ctx.AssertEqual(string.Empty, message, "Wearing reports no problem");
            });
            await ctx.WaitFrameAsync();

            var worn = GetItemInSlot(InventorySlot.Head, player);
            ctx.AssertEqual(helmetUuid, GetObjectUUID(worn), "The same helmet stays worn");
            ctx.AssertEqual(savedModel, GetItemAppearance(worn, ItemAppearanceType.SimpleModel, -1), "The helmet model is restored");
            ctx.AssertEqual(21, GetItemAppearance(worn, ItemAppearanceType.ArmorColor, (int)AppearanceArmorColor.Cloth1), "The helmet dye is restored");
            ctx.AssertEqual(1, GetItemAppearance(chest, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso), "The clothes are restored");

            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                // Saving with no helmet worn clears the earlier helmet look.
                ActionUnequipItem(worn);
            });
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(GetItemInSlot(InventorySlot.Head, player)), 10f, "the helmet is removed");
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                ctx.Assert(Wardrobe.Store(player, DB.Get<PlayerOutfit>(outfit.Id), out _), "The outfit saves without a helmet");
                ctx.Assert(!Wardrobe.HasHelmet(DB.Get<PlayerOutfit>(outfit.Id)), "Saving without a helmet clears the saved helmet");
            });
        }
        finally
        {
            DB.Delete<PlayerOutfit>(outfit.Id);
            if (GetIsObjectValid(helmet))
                DestroyObject(helmet);
        }
    }

    [EngineTest("Wardrobe expansions add five slots each and stop at fifty", Category = Category, TimeoutSeconds = 30)]
    public static async Task ExpansionsStopAtFifty(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            ctx.AssertEqual(Player.DefaultOutfitSlotLimit, Wardrobe.GetSlotLimit(DB.Get<Player>(GetObjectUUID(player))), "A new character starts with 25 slots");
            for (var expansion = 1; expansion <= 5; expansion++)
                ctx.AssertEqual(25 + expansion * 5, Wardrobe.ExpandSlotLimit(player), $"Expansion {expansion} adds five slots");
            ctx.AssertEqual(Wardrobe.MaxSlotLimit, Wardrobe.ExpandSlotLimit(player), "A sixth expansion stays at fifty");
            ctx.AssertEqual(Wardrobe.MaxSlotLimit, DB.Get<Player>(GetObjectUUID(player)).OutfitSlotLimit, "The limit is saved");
        });
    }
}
