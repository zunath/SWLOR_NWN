using System;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using FitVector = System.Numerics.Vector3;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class WeaponHandFitEngineTests
{
    [EngineTest("A held pistol takes its wearer's hand move and keeps it when a former wearer refreshes", Category = "WeaponHandFit", TimeoutSeconds = 60f)]
    public static async Task DisplacedPistolKeepsItsNewWearersMove(EngineTestContext ctx)
    {
        var droid = ctx.SpawnCreature("civilian");
        var cathar = ctx.SpawnCreature("civilian", 2f);
        await ctx.ExecuteInCreatureContextAsync(droid, () => SetBody(droid, AppearanceType.Droid, 1));
        await ctx.ExecuteInCreatureContextAsync(cathar, () => SetBody(cathar, AppearanceType.Cathar, 201));

        var pistol = await ctx.EquipItemAsync(droid, "b_pistol", InventorySlot.RightHand);
        await ctx.ExecuteInCreatureContextAsync(droid, () => WeaponHandFit.Apply(droid));
        var droidMove = WeaponHandFitCatalog.Get("pmz0_handr001");
        ctx.Assert(droidMove != FitVector.Zero, "The droid frame has a recorded move");
        AssertMove(ctx, pistol, droidMove, "The droid's held pistol takes the droid frame's move");

        // A transform something else set on the item survives the fit and its removal.
        await ctx.ExecuteInCreatureContextAsync(droid, () =>
        {
            SetObjectVisualTransform(pistol, ObjectVisualTransform.TranslateX, ReadMove(pistol).X + .1f);
            WeaponHandFit.Apply(droid);
        });
        AssertMove(ctx, pistol, droidMove + new FitVector(.1f, 0f, 0f), "Refreshing keeps a transform the fit does not own");

        await ctx.ExecuteInCreatureContextAsync(droid, () =>
        {
            ClearAllActions();
            ActionUnequipItem(pistol);
            ActionGiveItem(pistol, cathar);
        });
        await ctx.WaitUntilAsync(() => GetItemPossessor(pistol) == cathar, 10f, "the pistol to change hands");
        AssertMove(ctx, pistol, new FitVector(.1f, 0f, 0f), "A pistol that left the hand keeps only the transform it does not own");

        await ctx.ExecuteInCreatureContextAsync(cathar, () => ActionEquipItem(pistol, InventorySlot.RightHand));
        await ctx.WaitUntilAsync(() => GetItemInSlot(InventorySlot.RightHand, cathar) == pistol, 10f, "the new wearer to hold the pistol");
        var catharMove = WeaponHandFitCatalog.Get("pmo0_handr201");
        await ctx.ExecuteInCreatureContextAsync(cathar, () => WeaponHandFit.Apply(cathar));
        AssertMove(ctx, pistol, catharMove + new FitVector(.1f, 0f, 0f), "The new wearer's pistol takes the Cathar hand's move");

        // The former wearer still remembers the pistol it held; its next refresh must not reset the new wearer's fit.
        await ctx.ExecuteInCreatureContextAsync(droid, () => WeaponHandFit.Apply(droid));
        AssertMove(ctx, pistol, catharMove + new FitVector(.1f, 0f, 0f), "The former wearer's refresh leaves the new wearer's move in place");
    }

    [EngineTest("A held rifle takes the rifle move for its wearer's hand and loses it when unequipped", Category = "WeaponHandFit", TimeoutSeconds = 60f)]
    public static async Task HeldRifleTakesTheRifleMove(EngineTestContext ctx)
    {
        var droid = ctx.SpawnCreature("civilian");
        await ctx.ExecuteInCreatureContextAsync(droid, () => SetBody(droid, AppearanceType.Droid, 1));

        var rifle = await ctx.EquipItemAsync(droid, "b_rifle", InventorySlot.RightHand);
        await ctx.ExecuteInCreatureContextAsync(droid, () => WeaponHandFit.Apply(droid));
        var pistolMove = WeaponHandFitCatalog.Get("pmz0_handr001");
        var rifleMove = WeaponHandFit.RifleMove(pistolMove, "pmz0_handr001");
        ctx.Assert(rifleMove != FitVector.Zero && rifleMove != pistolMove, "The droid frame's rifle move differs from its pistol move");
        AssertMove(ctx, rifle, rifleMove, "The droid's held rifle takes the rifle move");

        await ctx.ExecuteInCreatureContextAsync(droid, () =>
        {
            ClearAllActions();
            ActionUnequipItem(rifle);
        });
        await ctx.WaitUntilAsync(() => GetItemInSlot(InventorySlot.RightHand, droid) != rifle, 10f, "the rifle to leave the hand");
        await ctx.ExecuteInCreatureContextAsync(droid, () => WeaponHandFit.Apply(droid));
        AssertMove(ctx, rifle, FitVector.Zero, "A rifle that left the hand carries no move");
    }

    private static void SetBody(uint creature, AppearanceType appearance, int hand)
    {
        SetCreatureAppearanceType(creature, appearance);
        SetGender(creature, Gender.Male);
        SetPhenoType(PhenoType.Normal, creature);
        SetCreatureBodyPart(CreaturePart.RightHand, hand, creature);
        SetCreatureBodyPart(CreaturePart.LeftHand, hand, creature);
        var armor = GetItemInSlot(InventorySlot.Chest, creature);
        if (GetIsObjectValid(armor))
        {
            // The fixture's outfit must not replace the natural hands under test.
            EquippedItemAppearance.Set(armor, SWLOR.NWN.API.NWScript.Enum.Item.ItemAppearanceType.ArmorModel,
                (int)SWLOR.NWN.API.NWScript.Enum.Item.AppearanceArmor.RightHand, 0);
            EquippedItemAppearance.Set(armor, SWLOR.NWN.API.NWScript.Enum.Item.ItemAppearanceType.ArmorModel,
                (int)SWLOR.NWN.API.NWScript.Enum.Item.AppearanceArmor.LeftHand, 0);
            EquippedItemAppearance.Refresh(creature, armor);
        }
    }

    private static FitVector ReadMove(uint item) => new(
        GetObjectVisualTransform(item, ObjectVisualTransform.TranslateX),
        GetObjectVisualTransform(item, ObjectVisualTransform.TranslateY),
        GetObjectVisualTransform(item, ObjectVisualTransform.TranslateZ));

    private static void AssertMove(EngineTestContext ctx, uint item, FitVector expected, string label)
    {
        var actual = ReadMove(item);
        ctx.Assert(Math.Abs(actual.X - expected.X) < .0001f && Math.Abs(actual.Y - expected.Y) < .0001f &&
                   Math.Abs(actual.Z - expected.Z) < .0001f, $"{label}: expected {expected}, was {actual}");
    }
}
