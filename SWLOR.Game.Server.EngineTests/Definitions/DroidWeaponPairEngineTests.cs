using System;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class DroidInventoryEngineTests
{
    [EngineTest("Droids wield a Power Glove pair that rebuilds its other hand through two resummons", Category = "DroidInventory", TimeoutSeconds = 120)]
    public static Task DroidPowerGlovePair(EngineTestContext ctx) => VerifyDroidWeaponPair(ctx, "b_pglove",
        BaseItem.CompanionPowerGlove, PowerGloveEquipment.IsActivePair);

    [EngineTest("Droids wield a Twin Pistols pair that rebuilds its off-hand pistol through two resummons", Category = "DroidInventory", TimeoutSeconds = 120)]
    public static Task DroidTwinPistolPair(EngineTestContext ctx) => VerifyDroidWeaponPair(ctx, "b_twinpistol",
        BaseItem.CompanionPistol, TwinPistolEquipment.IsActivePair);

    private static async Task VerifyDroidWeaponPair(EngineTestContext ctx, string resref, BaseItem companionType,
        Func<uint, bool> isActivePair)
    {
        var owner = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        var controller = await CreateController(ctx, owner);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            foreach (var stat in new[] { DroidStatSubType.Unarmed, DroidStatSubType.TwinPistol })
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DroidStat, (int)stat, 50), controller);
        });
        var weapon = OBJECT_INVALID;
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            weapon = CreateItemOnObject(resref, owner);
            ctx.Assert(GetIsObjectValid(weapon), "The paired weapon fixture exists");
        });
        await ctx.WaitFrameAsync();
        var uuid = GetObjectUUID(weapon);
        int Companions(uint droid) => Items(droid).Count(item => GetBaseItemType(item) == companionType);

        var droid = await Spawn(ctx, owner, controller);
        try
        {
            await Transfer(ctx, owner, weapon, droid);
            await Equip(ctx, droid, weapon, InventorySlot.RightHand);
            for (var cycle = 0; cycle <= 2; cycle++)
            {
                await ctx.WaitUntilAsync(() => isActivePair(droid), 5f, "the droid's weapon pair to become active");
                weapon = GetItemInSlot(InventorySlot.RightHand, droid);
                ctx.AssertEqual(uuid, GetObjectUUID(weapon), "The owned weapon keeps its identity");
                ctx.AssertEqual(companionType, GetBaseItemType(GetItemInSlot(InventorySlot.LeftHand, droid)), "The other hand is the generated companion");
                ctx.AssertEqual(1, Companions(droid), "Exactly one generated companion exists");
                var saved = Droid.LoadConstructedDroid(controller);
                ctx.Assert(saved.EquippedItems.ContainsKey(InventorySlot.RightHand), "The controller saves the owned weapon");
                ctx.Assert(!saved.EquippedItems.ContainsKey(InventorySlot.LeftHand), "The controller never saves the generated hand");
                ctx.AssertEqual(1, saved.EquippedItems.Count + saved.Inventory.Count, "Only the owned weapon is saved");
                if (cycle == 2) break;
                await Dismiss(ctx, owner, droid);
                droid = await Spawn(ctx, owner, controller);
            }

            await Unequip(ctx, droid, weapon);
            await ctx.WaitUntilAsync(() => Companions(droid) == 0, 5f, "the generated companion to be removed");
            ctx.AssertEqual(droid, GetItemPossessor(weapon), "Unequipping keeps the owned weapon in the droid's inventory");
            ctx.Assert(!isActivePair(droid), "The pair is inactive after unequipping");
        }
        finally
        {
            if (GetIsObjectValid(droid)) await Dismiss(ctx, owner, droid);
        }
    }
}
