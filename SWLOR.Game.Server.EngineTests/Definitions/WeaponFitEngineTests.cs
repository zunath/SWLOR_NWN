using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;
using FitVector = System.Numerics.Vector3;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class WeaponFitEngineTests
{
    [EngineTest("Native rifle hold transitions preserve primary contact and authored offsets", Category = "WeaponFit", TimeoutSeconds = 60f)]
    public static async Task NativeRifleHoldTransitions(EngineTestContext ctx)
    {
        var wearer = ctx.SpawnCreature("civilian");
        await ctx.WaitUntilAsync(() => GetIsObjectValid(GetItemInSlot(InventorySlot.Chest, wearer)),
            10f, "the rifle wearer's outfit");
        await ctx.ExecuteInCreatureContextAsync(wearer, () =>
        {
            SetCreatureAppearanceType(wearer, (AppearanceType)10006);
            SetGender(wearer, Gender.Male);
            SetPhenoType(PhenoType.Normal, wearer);
            SetCreatureBodyPart(CreaturePart.RightHand, 201, wearer);
            SetCreatureBodyPart(CreaturePart.LeftHand, 201, wearer);
            var outfit = GetItemInSlot(InventorySlot.Chest, wearer);
            EquippedItemAppearance.Set(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe, 0);
            EquippedItemAppearance.Set(outfit, ItemAppearanceType.ArmorModel, (int)CreaturePart.RightHand, 153);
            EquippedItemAppearance.Set(outfit, ItemAppearanceType.ArmorModel, (int)CreaturePart.LeftHand, 0);
            EquippedItemAppearance.Refresh(wearer, outfit);
        });
        var rifle = await ctx.EquipItemAsync(wearer, "b_rifle", InventorySlot.RightHand);
        var observedReady = false;
        await ctx.ExecuteInCreatureContextAsync(wearer, () =>
        {
            SetAppearance(rifle, 101);
            Release(rifle);
            var authored = new FitVector(.02f, -.03f, .01f);
            SetTranslation(rifle, authored);
            SetObjectVisualTransform(rifle, ObjectVisualTransform.Scale, 1.03f);
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(wearer);
            native.SetAnimation(0);
            WeaponFit.Apply(wearer);
            var idle = new FitVector(-.10091517f, .01336412f, -.12003522f);
            var ready = new FitVector(-.13350788f, -.04168537f, -.06223492f);
            AssertVector(ctx, ReadTranslation(rifle), authored + idle, "The resting grip is seated");
            // Do not call Apply after these native changes: the hook must update the fit itself.
            foreach (var animation in new[] { 1, 9, 0, 1, 0 })
            {
                native.SetAnimation(animation);
                var actualAnimation = native.m_nAnimation;
                observedReady |= actualAnimation is 1 or 9;
                AssertVector(ctx, ReadTranslation(rifle), authored + (actualAnimation is 1 or 9 ? ready : idle),
                    $"The actual native hold {actualAnimation} after request {animation} refreshes without accumulating an offset");
                ctx.AssertEqual(1.03f, GetObjectVisualTransform(rifle, ObjectVisualTransform.Scale),
                    "Native hold changes preserve the authored gun size");
            }
        });
        // Some native requests are coerced to idle outside combat. Exercise real
        // gameplay if necessary so the ready branch cannot silently go unchecked.
        if (!observedReady)
        {
            var target = ctx.SpawnCreature("civilian", 4f);
            ctx.MakeHostile(target);
            SetImmortal(target, true);
            SetCommandable(false, target);
            await ctx.ExecuteInCreatureContextAsync(wearer, () =>
            {
                SetCommandable(true, wearer);
                SetAILevel(wearer, AILevel.Medium);
                ClearAllActions(true);
                ActionAttack(target);
            });
            await ctx.WaitUntilAsync(() =>
            {
                var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(wearer);
                if (native.m_nAnimation is not (1 or 9)) return false;
                observedReady = true;
                AssertVector(ctx, ReadTranslation(rifle), new FitVector(.02f, -.03f, .01f) +
                    new FitVector(-.13350788f, -.04168537f, -.06223492f),
                    "A real native ready/firing hold seats the grip without a manual refresh");
                return true;
            }, 15f, "a real native ready or firing animation");
            await ctx.ExecuteInCreatureContextAsync(wearer, () => ClearAllActions(true));
        }
        ctx.Assert(observedReady, "At least one retained native ready/firing hold was verified");
        ctx.SetResultDetail("Observed and verified a retained native ready/firing hold, including coerced-request checks.");
    }

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
            EquippedItemAppearance.Refresh(wearer, weapon);
            TintMapService.ApplyCurrentColors(wearer);
            AssertVector(ctx, ReadTranslation(weapon), authored + fit, "Repeated refresh does not compound the correction");
            ctx.Assert(Math.Abs(.927f - GetObjectVisualTransform(weapon, ObjectVisualTransform.Scale)) < .000001f,
                "The measured scale multiplies the authored scale once");
            ctx.AssertEqual(9f, GetObjectVisualTransform(weapon, ObjectVisualTransform.RotateZ),
                "The measured rotation is added to the authored rotation once");
            ctx.AssertEqual(.017f, GetObjectVisualTransform(weapon, ObjectVisualTransform.TranslateZ,
                nScope: (ObjectVisualTransformDataScopeType)ObjectVisualTransformDataScopeItemType.ItemPart2), "Other transform scopes remain authored");

            var outfit = GetItemInSlot(InventorySlot.Chest, wearer);
            foreach (var (robe, hidden) in new[] { (3, false), (186, true), (0, false) })
            {
                EquippedItemAppearance.Set(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe, robe);
                EquippedItemAppearance.Refresh(wearer, outfit);
                TintMapService.ApplyCurrentColors(wearer);
                ctx.AssertEqual(robe, GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe),
                    "The robe fixture is available on the current body");
                AssertVector(ctx, ReadTranslation(weapon), authored + (hidden ? FitVector.Zero : fit),
                    "Robes retain visible-hand fits but never borrow a hidden hand's fit");
            }

            SetGender(wearer, Gender.Female);
            TintMapService.ApplyCurrentColors(wearer);
            AssertVector(ctx, ReadTranslation(weapon), authored + GetFit(BaseItem.Sling, 310, "pfh0_handr002"),
                "The current wearer selects the fit, not the item's previous wearer");

            SetGender(wearer, Gender.Male);
            TintMapService.ApplyCurrentColors(wearer);
            AssertVector(ctx, ReadTranslation(weapon), authored + fit, "The hand-change source carries its correction");
            SetCreatureBodyPart(CreaturePart.RightHand, 263, wearer);
            TintMapService.ApplyCurrentColors(wearer);
            AssertVector(ctx, ReadTranslation(weapon), authored + GetFit(BaseItem.Sling, 310, "pmh0_handr263"),
                "Changing the actual hand selects its independent correction");
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
        // Restore this test-only correction on every exit.
        var catalog = typeof(WeaponFit).Assembly.GetType("SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance.WeaponGripFits")!;
        var fits = (System.Collections.Generic.Dictionary<(BaseItem, int, int, int, string, string), WeaponGripFit>)catalog
            .GetField("Fits", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        var hands = new[] { "pmh0_handr002", RenderedHand("pmh0_handr002", BaseItem.Sling) }.Distinct().ToArray();
        var previous = hands.ToDictionary(hand => hand, hand =>
        {
            var present = fits.TryGetValue((BaseItem.Sling, 101, 310, 101, hand, ""), out var value);
            return (present, value);
        });
        foreach (var hand in hands)
            fits[(BaseItem.Sling, 101, 310, 101, hand, "")] = new WeaponGripFit(
                new FitVector(.01f, .02f, -.03f), new FitVector(4f, -3f, 7f), .9f);
        try { await test(); }
        finally
        {
            foreach (var hand in hands)
            {
                var key = (BaseItem.Sling, 101, 310, 101, hand, "");
                if (previous[hand].present) fits[key] = previous[hand].value;
                else fits.Remove(key);
            }
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

    private static string RenderedHand(string hand, BaseItem type) =>
        ((WeaponGripHandCatalog)typeof(WeaponGripHandRenderer).GetField("_catalog", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!).Resolve(hand, type, hand.Contains("_handr") ? "r" : "l");

    private static FitVector GetFit(BaseItem type, int middle, string hand) =>
        (FitVector)typeof(WeaponFit).Assembly.GetType("SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance.WeaponGripFits")!
            .GetMethod("GetFit", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { type, middle, 101, 101,
                RenderedHand(hand, type), RenderedHand(hand.Replace("_handr", "_handl"), type) });

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
