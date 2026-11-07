using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;
using FitVector = System.Numerics.Vector3;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class WeaponFitEngineTests
{
    [EngineTest("A delayed old-wearer refresh cannot strip the new wearer's shared fit", Category = "WeaponFit", TimeoutSeconds = 60f)]
    public static Task DelayedTransferRefresh(EngineTestContext ctx) => WithFixtureCorrection(() => DelayedTransferRefreshCore(ctx));

    private static async Task DelayedTransferRefreshCore(EngineTestContext ctx)
    {
        var previous = ctx.SpawnCreature("civilian");
        var current = ctx.SpawnCreature("civilian");
        await ctx.WaitUntilAsync(() => GetIsObjectValid(GetItemInSlot(InventorySlot.Chest, previous)) &&
            GetIsObjectValid(GetItemInSlot(InventorySlot.Chest, current)), 10f, "both fixture outfits");
        foreach (var wearer in new[] { previous, current })
            await ctx.ExecuteInCreatureContextAsync(wearer, () =>
            {
                SetCreatureAppearanceType(wearer, AppearanceType.Human);
                SetGender(wearer, Gender.Male);
                SetPhenoType(PhenoType.Normal, wearer);
                SetCreatureBodyPart(CreaturePart.RightHand, 2, wearer);
                var armor = GetItemInSlot(InventorySlot.Chest, wearer);
                EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe, 0);
                EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)CreaturePart.RightHand, 0);
                EquippedItemAppearance.Refresh(wearer, armor);
            });
        var weapon = await ctx.EquipItemAsync(previous, "b_pistol", InventorySlot.RightHand);
        await ctx.ExecuteInCreatureContextAsync(previous, () =>
        {
            SetAppearance(weapon, 310);
            WeaponFit.Apply(previous);
            CreaturePlugin.RunUnequip(previous, weapon);
            ctx.Assert(GetItemInSlot(InventorySlot.RightHand, previous) != weapon, "The previous wearer releases the pistol");
            ClearAllActions();
            ActionGiveItem(weapon, current);
        });
        await ctx.WaitUntilAsync(() => GetItemPossessor(weapon) == current, 10f, "the pistol transfer");
        await ctx.ExecuteInCreatureContextAsync(current, () =>
        {
            CreaturePlugin.RunEquip(current, weapon, InventorySlot.RightHand);
            ctx.AssertEqual(weapon, GetItemInSlot(InventorySlot.RightHand, current), "The new wearer equips the pistol");
            WeaponFit.Apply(current);
        });
        var fitted = ReadTranslation(weapon);
        ctx.Assert(fitted != FitVector.Zero, "The new wearer has the fixture correction");
        await ctx.ExecuteInCreatureContextAsync(previous, () =>
        {
            SetLocalObject(previous, "WEAPON_GRIP_FIT_RIGHT", weapon);
            WeaponFit.Apply(previous);
            AssertVector(ctx, ReadTranslation(weapon), fitted, "Stale tracking follows the current equipped owner");
        });
    }

    [EngineTest("Shared weapon fits preserve authored transforms across refresh, appearance changes and copies", Category = "WeaponFit", TimeoutSeconds = 60f)]
    public static Task SharedFitLifecycle(EngineTestContext ctx) => WithFixtureCorrection(() => SharedFitLifecycleCore(ctx));

    private static async Task SharedFitLifecycleCore(EngineTestContext ctx)
    {
        var wearer = ctx.SpawnCreature("civilian");
        await ctx.WaitUntilAsync(() => GetIsObjectValid(GetItemInSlot(InventorySlot.Chest, wearer)), 10f, "the fixture outfit");
        await ctx.ExecuteInCreatureContextAsync(wearer, () =>
        {
            SetCreatureAppearanceType(wearer, AppearanceType.Human);
            SetGender(wearer, Gender.Male);
            SetPhenoType(PhenoType.Normal, wearer);
            SetCreatureBodyPart(CreaturePart.RightHand, 2, wearer);
            var armor = GetItemInSlot(InventorySlot.Chest, wearer);
            EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe, 0);
            EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)CreaturePart.RightHand, 0);
            EquippedItemAppearance.Refresh(wearer, armor);
        });
        var weapon = await ctx.EquipItemAsync(wearer, "b_pistol", InventorySlot.RightHand);
        var authored = new FitVector(.013f, -.021f, .007f);
        string archive = null;
        string identity = null;
        await ctx.ExecuteInCreatureContextAsync(wearer, () =>
        {
            Release(weapon);
            SetTranslation(weapon, authored);
            SetObjectVisualTransform(weapon, ObjectVisualTransform.Scale, 1.03f);
            SetObjectVisualTransform(weapon, ObjectVisualTransform.RotateZ, 2f);
            SetObjectVisualTransform(weapon, ObjectVisualTransform.TranslateZ, .017f,
                nScope: (ObjectVisualTransformDataScopeType)ObjectVisualTransformDataScopeItemType.ItemPart2);
            SetAppearance(weapon, 310);
            var fit = GetFit(BaseItem.Sling, 310, "pmh0_handr002");
            ctx.Assert(fit != FitVector.Zero, "The fixture exercises a nonzero test correction");
            for (var i = 0; i < 5; i++) TintMapService.ApplyCurrentColors(wearer);
            AssertVector(ctx, ReadTranslation(weapon), authored + fit, "Repeated refresh does not compound the correction");
            ctx.AssertEqual(1.03f, GetObjectVisualTransform(weapon, ObjectVisualTransform.Scale), "Scale remains authored");
            ctx.AssertEqual(2f, GetObjectVisualTransform(weapon, ObjectVisualTransform.RotateZ), "Rotation remains authored");
            ctx.AssertEqual(.017f, GetObjectVisualTransform(weapon, ObjectVisualTransform.TranslateZ,
                nScope: (ObjectVisualTransformDataScopeType)ObjectVisualTransformDataScopeItemType.ItemPart2), "Other transform scopes remain authored");

            SetGender(wearer, Gender.Female);
            TintMapService.ApplyCurrentColors(wearer);
            AssertVector(ctx, ReadTranslation(weapon), authored + GetFit(BaseItem.Sling, 310, "pfh0_handr002"),
                "The current wearer selects the fit, not the item's previous wearer");

            SetGender(wearer, Gender.Male);
            TintMapService.ApplyCurrentColors(wearer);
            AssertVector(ctx, ReadTranslation(weapon), authored + fit, "The hand-change source carries its correction");
            SetCreatureBodyPart(CreaturePart.RightHand, 263, wearer);
            TintMapService.ApplyCurrentColors(wearer);
            AssertVector(ctx, ReadTranslation(weapon), authored, "An unmeasured hand removes only the correction");
            SetCreatureBodyPart(CreaturePart.RightHand, 2, wearer);
            TintMapService.ApplyCurrentColors(wearer);
            SetAppearance(weapon, 999);
            TintMapService.ApplyCurrentColors(wearer);
            AssertVector(ctx, ReadTranslation(weapon), authored, "An unmeasured weapon removes only the correction");
            SetAppearance(weapon, 310);
            SetGender(wearer, Gender.Male);
            TintMapService.ApplyCurrentColors(wearer);
            AssertVector(ctx, ReadTranslation(weapon), authored + fit, "The copy source carries its measured correction");

            var copy = CopyItem(weapon, wearer, true);
            ctx.Track(copy);
            ctx.Assert(GetIsObjectValid(copy), "The fitted item can be copied");
            var copyBefore = ReadTranslation(copy);
            AssertVector(ctx, copyBefore, FitVector.Zero, "Native CopyItem resets visual transforms before fit normalization");
            Release(copy);
            AssertVector(ctx, ReadTranslation(copy), copyBefore, "Copied locals cannot subtract the source item's fit");

            CreaturePlugin.RunUnequip(wearer, weapon);
            ctx.Assert(GetItemInSlot(InventorySlot.RightHand, wearer) != weapon, "The fitted item can be unequipped");
            WeaponFit.RefreshInventory(wearer);
            AssertVector(ctx, ReadTranslation(weapon), authored, "Unequipping preserves authored translation");
            ctx.AssertEqual("", GetLocalString(weapon, "WEAPON_GRIP_FIT_ITEM"), "Released items carry no applied correction");
            CreaturePlugin.RunEquip(wearer, weapon, InventorySlot.RightHand);
            ctx.AssertEqual(weapon, GetItemInSlot(InventorySlot.RightHand, wearer), "The item can be re-equipped");
            WeaponFit.Apply(wearer);
            AssertVector(ctx, ReadTranslation(weapon), authored + fit, "Re-equipping recomputes the current fit");
            ctx.Assert(GetLocalFloat(weapon, "WEAPON_GRIP_FIT_X") != 0f, "The saved item carries an applied correction");
            identity = GetObjectUUID(weapon);
            archive = ObjectPlugin.Serialize(weapon);
            DestroyObject(weapon);
        });
        await ctx.WaitUntilAsync(() => !GetIsObjectValid(weapon) && !GetIsObjectValid(GetObjectByUUID(identity)),
            5f, "the saved item's identity to be released");
        await ctx.ExecuteInCreatureContextAsync(wearer, () =>
        {
            var restored = ObjectPlugin.Deserialize(archive);
            ctx.Track(restored);
            ctx.Assert(GetIsObjectValid(restored), "The fitted item reloads from its native archive");
            ctx.AssertEqual(identity, GetObjectUUID(restored), "The native item identity survives reload");
            Release(restored);
            AssertVector(ctx, ReadTranslation(restored), authored, "A reload removes only the saved applied correction");
            ctx.AssertEqual(1.03f, GetObjectVisualTransform(restored, ObjectVisualTransform.Scale), "Reload retains authored scale");
            ctx.AssertEqual(2f, GetObjectVisualTransform(restored, ObjectVisualTransform.RotateZ), "Reload retains authored rotation");
        });
    }

    private static async Task WithFixtureCorrection(Func<Task> test)
    {
        // Exercise a nonzero delta without shipping an unverified fit. The test assembly is
        // loaded only by the isolated engine-test runner, and every exit restores the catalog.
        var catalog = typeof(WeaponFit).Assembly.GetType("SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance.WeaponGripFits")!;
        var fits = (System.Collections.Generic.Dictionary<(BaseItem, int, int, int, string, string), FitVector>)catalog
            .GetField("Fits", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        var key = (BaseItem.Sling, 101, 310, 101, "pmh0_handr002", "");
        var hadEntry = fits.TryGetValue(key, out var previous);
        fits[key] = new FitVector(.01f, .02f, -.03f);
        try { await test(); }
        finally
        {
            if (hadEntry) fits[key] = previous;
            else fits.Remove(key);
        }
    }

    private static void SetAppearance(uint item, int middle)
    {
        foreach (var part in new[] { AppearanceWeapon.Bottom, AppearanceWeapon.Middle, AppearanceWeapon.Top })
        {
            EquippedItemAppearance.Set(item, ItemAppearanceType.WeaponModel, (int)part, part == AppearanceWeapon.Middle ? middle % 100 : 1);
            EquippedItemAppearance.Set(item, ItemAppearanceType.WeaponColor, (int)part, part == AppearanceWeapon.Middle ? middle / 100 : 1);
        }
    }

    private static FitVector GetFit(BaseItem type, int middle, string hand) =>
        (FitVector)typeof(WeaponFit).Assembly.GetType("SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance.WeaponGripFits")!
            .GetMethod("GetFit", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { type, middle, 101, 101, hand, hand.Replace("_handr", "_handl") });

    private static void Release(uint item) => typeof(WeaponFit).GetMethod("Release", BindingFlags.NonPublic | BindingFlags.Static)!
        .Invoke(null, new object[] { item });

    private static FitVector ReadTranslation(uint item) => new(
        GetObjectVisualTransform(item, ObjectVisualTransform.TranslateX),
        GetObjectVisualTransform(item, ObjectVisualTransform.TranslateY),
        GetObjectVisualTransform(item, ObjectVisualTransform.TranslateZ));

    private static void SetTranslation(uint item, FitVector translation)
    {
        SetObjectVisualTransform(item, ObjectVisualTransform.TranslateX, translation.X);
        SetObjectVisualTransform(item, ObjectVisualTransform.TranslateY, translation.Y);
        SetObjectVisualTransform(item, ObjectVisualTransform.TranslateZ, translation.Z);
    }

    private static void AssertVector(EngineTestContext ctx, FitVector actual, FitVector expected, string label) =>
        ctx.Assert(FitVector.Distance(actual, expected) < .000001f, $"{label}: expected {expected}, observed {actual}");
}
