using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class PowerGloveFitEngineTests
{
    private const ObjectVisualTransformDataScopeType FitScope =
        (ObjectVisualTransformDataScopeType)ObjectVisualTransformDataScopeItemType.ItemPart2;
    private const string RoleVariable = "POWER_GLOVE_FIT_TEST_ROLE";

    [EngineTest("Power Glove fit follows equipment and appearance changes without leaking transforms", Category = "PowerGloveFit", TimeoutSeconds = 90f)]
    public static async Task EquipmentAndAppearanceLifecycle(EngineTestContext ctx)
    {
        var wearer = await CreateWearer(ctx);
        var right = await ctx.EquipItemAsync(wearer, "t_pglove", InventorySlot.RightHand);
        var left = await ctx.EquipItemAsync(wearer, "t_pglove", InventorySlot.LeftHand);
        var armor = GetItemInSlot(InventorySlot.Chest, wearer);
        await ctx.ExecuteInCreatureContextAsync(wearer, () =>
        {
            AssertFit(ctx, right, 1f, "Human right hand starts compact");
            AssertFit(ctx, left, 1f, "Human left hand starts compact");
            SetObjectVisualTransform(right, ObjectVisualTransform.Scale, 1.13f);
            SetObjectVisualTransform(right, ObjectVisualTransform.RotateZ, 17f);
            SetObjectVisualTransform(right, ObjectVisualTransform.TranslateX, .024f);
            SetObjectVisualTransform(right, ObjectVisualTransform.RotateY, 9f, nScope: FitScope);
            SetObjectVisualTransform(right, ObjectVisualTransform.TranslateZ, .007f, nScope: FitScope);

            // The body editor publishes this script after changing the natural body parts.
            SetCreatureBodyPart(CreaturePart.LeftHand, 201, wearer);
            ExecuteScript(ScriptName.OnAppearanceEdit, wearer);
            var naturalFit = Expected("pmh0_handr001", "pmh0_handl201", 101);
            ctx.Assert(naturalFit > 1f, "The asymmetric natural hand requires more clearance");
            AssertPair(ctx, right, left, naturalFit, "Natural hand edit");
            ctx.AssertEqual(201, GetCreatureBodyPart(CreaturePart.LeftHand, wearer), "Fit retains the selected natural hand");

            EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightHand, 1);
            EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftHand, 1);
            EquippedItemAppearance.Refresh(wearer, armor);
            AssertPair(ctx, right, left, 1f, "Armor overrides both natural hands");
            EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightHand, 2);
            EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftHand, 0);
            EquippedItemAppearance.Refresh(wearer, armor);
            AssertPair(ctx, right, left, Expected("pmh0_handr002", "pmh0_handl201", 101),
                "Zero armor hand falls back to the natural hand");

            SetBody(wearer, AppearanceType.Human, 1, 1);
            SetGender(wearer, Gender.Female);
            EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightHand, 109);
            EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftHand, 109);
            EquippedItemAppearance.Refresh(wearer, armor);
            AssertPair(ctx, right, left, 1f, "Human female armored hands retain the compact fit");

            SetBody(wearer, AppearanceType.Cathar, 201, 201);
            ClearArmorHands(armor);
            EquippedItemAppearance.Refresh(wearer, armor);
            foreach (var appearance in new[] { 101, 102, 104, 105 })
            {
                ChangeGlove(wearer, right, appearance);
                ChangeGlove(wearer, left, appearance);
                var expected = Expected("pmo0_handr201", "pmo0_handl201", appearance);
                ctx.Assert(expected > 1f, "Cathar hands enlarge every compact shell");
                var savedAppearance = ReadAppearance(right);
                for (var pass = 0; pass < 5; pass++)
                    TintMapService.ApplyCurrentColors(wearer);
                AssertPair(ctx, right, left, expected, "Repeated cosmetic refresh");
                ctx.Assert(savedAppearance.SequenceEqual(ReadAppearance(right)), "Fitting never rewrites weapon appearance IDs");
                ctx.AssertEqual(BaseItem.PowerGlove, GetBaseItemType(right), "Fitting retains the Power Glove base item");
                AssertIndependentTransforms(ctx, right);
            }

            foreach (var appearance in new[] { 106, 103 })
            {
                ChangeGlove(wearer, right, appearance);
                AssertFit(ctx, right, 1f, "Invisible and Blastknuckle selections release the fit immediately");
                AssertIndependentTransforms(ctx, right);
            }
            ChangeGlove(wearer, right, 101);
            ChangeGlove(wearer, left, 101);

            // The normal equipment validator rejects a move into an occupied hand.
            ctx.Assert(!CreaturePlugin.RunEquip(wearer, left, InventorySlot.RightHand), "The occupied-slot equip is rejected");
            ctx.AssertEqual(right, GetItemInSlot(InventorySlot.RightHand, wearer), "Rejected equip preserves the main hand");
            ctx.AssertEqual(left, GetItemInSlot(InventorySlot.LeftHand, wearer), "Rejected equip preserves the off hand");
            AssertPair(ctx, right, left, Expected("pmo0_handr201", "pmo0_handl201", 101), "Rejected equip preserves fit");

            Unequip(ctx, wearer, right, "The main glove unequips");
            AssertFit(ctx, right, 1f, "Unequip releases the fitted scale synchronously");
            Unequip(ctx, wearer, left, "The offhand glove unequips");
            AssertFit(ctx, left, 1f, "Offhand unequip releases the fitted scale synchronously");
            ctx.Assert(CreaturePlugin.RunEquip(wearer, left, InventorySlot.RightHand), "The old offhand moves to the main hand");
            ctx.Assert(CreaturePlugin.RunEquip(wearer, right, InventorySlot.LeftHand), "The old main hand moves to the offhand");
            ctx.AssertEqual(left, GetItemInSlot(InventorySlot.RightHand, wearer), "Swap installs the old offhand in the main slot");
            ctx.AssertEqual(right, GetItemInSlot(InventorySlot.LeftHand, wearer), "Swap installs the old main hand in the offhand slot");
            AssertPair(ctx, right, left, Expected("pmo0_handr201", "pmo0_handl201", 101), "Both swapped attachments fit equally");
        });

        await ctx.ExecuteInCreatureContextAsync(wearer, () =>
        {
            Unequip(ctx, wearer, right, "The swapped glove can be removed");
            SetDroppableFlag(right, true);
            ClearAllActions();
            ActionPutDownItem(right);
        });
        await ctx.WaitUntilAsync(() => GetItemPossessor(right) != wearer && GetArea(right) == ctx.Arena,
            10f, "the fitted glove to drop into the area");
        ctx.Track(right);
        AssertFit(ctx, right, 1f, "A dropped glove has no wearer fit");
        await ctx.ExecuteInCreatureContextAsync(wearer, () => ActionPickUpItem(right));
        await ctx.WaitUntilAsync(() => GetItemPossessor(right) == wearer, 10f, "the dropped glove to return to inventory");
        AssertFit(ctx, right, 1f, "Acquired inventory retains its authored size");

        var sword = await ctx.EquipItemAsync(wearer, "b_longsword", InventorySlot.RightHand);
        await ctx.ExecuteInCreatureContextAsync(wearer, () =>
        {
            ctx.AssertEqual(sword, GetItemInSlot(InventorySlot.RightHand, wearer), "The replacement weapon occupies the main hand");
            ctx.AssertEqual(OBJECT_INVALID, GetItemInSlot(InventorySlot.LeftHand, wearer), "The dropped glove has not re-equipped itself");
            ctx.AssertEqual(wearer, GetItemPossessor(left), "The removed main-hand glove remains in its owner's inventory");
            AssertFit(ctx, left, 1f, "Replacing a glove leaves no fit on the displaced inventory item");
            SetObjectVisualTransform(sword, ObjectVisualTransform.Scale, 1.37f, nScope: FitScope);
            ctx.Assert(CreaturePlugin.RunEquip(wearer, right, InventorySlot.LeftHand), "A glove can refit beside a different weapon");
            ctx.AssertEqual(right, GetItemInSlot(InventorySlot.LeftHand, wearer), "The reacquired glove occupies the offhand slot");
            ctx.AssertEqual(sword, GetItemInSlot(InventorySlot.RightHand, wearer), "Refitting the glove preserves the other weapon's slot");
            TintMapService.ApplyCurrentColors(wearer);
            AssertFit(ctx, sword, 1.37f, "Non-Power Glove equipment transforms are untouched");
            AssertFit(ctx, right, Expected("pmo0_handr201", "pmo0_handl201", 101), "Reacquired glove fits the current wearer");
            AssertIndependentTransforms(ctx, right);
        });
        ctx.SetResultDetail("Native equipment events, body-edit script, armor/weapon refresh, invisible/Blastknuckle reset, denied equip, hand swaps, drop/acquire and repeated refresh preserve ownership, authored appearance IDs and independent transform channels. Client pixels require separate visual verification.");
    }

    [EngineTest("Power Glove fit survives two equipment save reload cycles with nested inventory", Category = "PowerGloveFit", TimeoutSeconds = 90f)]
    public static async Task SaveReloadDoesNotCompoundOrChangeOwnership(EngineTestContext ctx)
    {
        var wearer = await CreateWearer(ctx);
        var right = await ctx.EquipItemAsync(wearer, "t_pglove", InventorySlot.RightHand);
        var left = await ctx.EquipItemAsync(wearer, "t_pglove", InventorySlot.LeftHand);
        var bag = await CreateItem(ctx, wearer, "bag_sm", wearer);
        var stored = await CreateItem(ctx, wearer, "t_pglove", bag);
        await ctx.ExecuteInCreatureContextAsync(wearer, () =>
        {
            SetLocalString(right, RoleVariable, "right");
            SetLocalString(left, RoleVariable, "left");
            SetLocalString(stored, RoleVariable, "stored");
            SetBody(wearer, AppearanceType.Cathar, 201, 201);
            ExecuteScript(ScriptName.OnAppearanceEdit, wearer);
        });

        for (var cycle = 0; cycle < 2; cycle++)
        {
            var label = $"Save/reload cycle {cycle + 1}";
            var items = GloveInventory(wearer);
            right = items.Single(item => GetLocalString(item, RoleVariable) == "right");
            left = items.Single(item => GetLocalString(item, RoleVariable) == "left");
            stored = items.Single(item => GetLocalString(item, RoleVariable) == "stored");
            var appearance = cycle == 0 ? 104 : 105;
            var data = string.Empty;
            var previousItems = InventoryItems(wearer);
            var ownerIdentity = string.Empty;
            var bagIdentity = string.Empty;
            Dictionary<string, (string Identity, int Quantity, int[] Appearance)> saved = null;
            await ctx.ExecuteInCreatureContextAsync(wearer, () =>
            {
                ctx.AssertEqual(right, GetItemInSlot(InventorySlot.RightHand, wearer), label + " begins with the original main glove");
                ctx.AssertEqual(left, GetItemInSlot(InventorySlot.LeftHand, wearer), label + " begins with the original offhand glove");
                Unequip(ctx, wearer, right, label + " removes the main glove");
                AssertFit(ctx, right, 1f, label + " normalizes the removed main glove");
                ctx.Log($"{label} after main-hand removal: {EquipmentState(wearer, right)}");
                // Empty both hands before rebuilding the pair. The native unequip may
                // change the remaining weapon's slot, and the validator rejects occupied slots.
                Unequip(ctx, wearer, left, label + " removes the other glove");
                AssertFit(ctx, left, 1f, label + " normalizes the removed offhand glove");
                ctx.AssertEqual(OBJECT_INVALID, GetItemInSlot(InventorySlot.RightHand, wearer), label + " empties the main hand");
                ctx.AssertEqual(OBJECT_INVALID, GetItemInSlot(InventorySlot.LeftHand, wearer), label + " empties the offhand");
                Equip(ctx, wearer, right, InventorySlot.RightHand, label + " restores the original main glove");
                Equip(ctx, wearer, left, InventorySlot.LeftHand, label + " restores the original offhand glove");
                ctx.AssertEqual(right, GetItemInSlot(InventorySlot.RightHand, wearer), label + " preserves the main hand after restoring the offhand");
                ChangeGlove(wearer, right, appearance);
                ChangeGlove(wearer, left, appearance);
                AssertPair(ctx, right, left, Expected("pmo0_handr201", "pmo0_handl201", appearance), label + " fitted equipment before saving");
                // Model a stale native transform in a saved bag, without introducing a persistent marker.
                SetObjectVisualTransform(stored, ObjectVisualTransform.Scale, 1.45f, nScope: FitScope);
                saved = items.ToDictionary(item => GetLocalString(item, RoleVariable),
                    item => (GetObjectUUID(item), GetItemStackSize(item), ReadAppearance(item)));
                ctx.AssertEqual(3, saved.Count, label + " contains exactly the three original glove roles");
                ownerIdentity = GetObjectUUID(wearer);
                bagIdentity = GetObjectUUID(GetItemPossessor(stored, true));
                data = ObjectPlugin.Serialize(wearer);
                // Release every saved UUID, including nested items, before the same archive reloads.
                // Destruction of a creature alone is not evidence that its children have gone.
                foreach (var item in previousItems)
                    DestroyObject(item);
                DestroyObject(wearer);
            });
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(wearer) && previousItems.All(item => !GetIsObjectValid(item)) &&
                    saved.Values.All(item => !GetIsObjectValid(GetObjectByUUID(item.Identity))),
                5f, label + " previous owner and all saved inventory to release their native identities");
            var reloaded = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(ctx.Arena, () =>
            {
                reloaded = ObjectPlugin.Deserialize(data);
                ctx.Assert(GetIsObjectValid(reloaded), label + " saved owner deserializes");
                ctx.Track(reloaded);
                PlaceReloadedWearer(ctx, reloaded);
            });
            wearer = reloaded;
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(wearer, () =>
            {
                // Use the exact spawn/login normalization path after loading the native object.
                PowerGloveFit.OnSpawn();
                TintMapService.ApplyCurrentColors(wearer);
                ctx.AssertEqual(ownerIdentity, GetObjectUUID(wearer), label + " preserves the owner identity");
                var restored = GloveInventory(wearer);
                ctx.AssertEqual(3, restored.Count, label + " retains exactly three Power Gloves including the bag item");
                ctx.AssertEqual(saved.Values.Sum(value => value.Quantity), restored.Sum(GetItemStackSize), label + " preserves all stack quantities");
                foreach (var item in restored)
                {
                    var role = GetLocalString(item, RoleVariable);
                    var expected = saved[role];
                    ctx.AssertEqual(expected.Identity, GetObjectUUID(item), label + " preserves each item identity");
                    ctx.AssertEqual(expected.Quantity, GetItemStackSize(item), label + " preserves each item's quantity");
                    ctx.Assert(expected.Appearance.SequenceEqual(ReadAppearance(item)), label + " preserves every weapon appearance field");
                    if (role == "stored")
                    {
                        var ownerBag = GetItemPossessor(item, true);
                        ctx.AssertEqual("bag_sm", GetResRef(ownerBag), label + " stored glove remains inside its bag");
                        ctx.AssertEqual(bagIdentity, GetObjectUUID(ownerBag), label + " preserves the same bag identity");
                        ctx.AssertEqual(wearer, GetItemPossessor(ownerBag), label + " creature still owns that bag");
                        AssertFit(ctx, item, 1f, label + " normalization removes stale fit from nested inventory");
                    }
                    else
                    {
                        var slot = role == "right" ? InventorySlot.RightHand : InventorySlot.LeftHand;
                        ctx.AssertEqual(wearer, GetItemPossessor(item), label + " creature owns the equipped glove");
                        ctx.AssertEqual(item, GetItemInSlot(slot, wearer), label + " retains the original hand assignment");
                        AssertFit(ctx, item, Expected("pmo0_handr201", "pmo0_handl201", appearance), label + " recomputes absolute fit without compounding");
                    }
                }
                ctx.Log(label + " verified all three glove identities, ownership, quantities, appearances and fit");
            });
        }
        ctx.SetResultDetail("Two native owner/item export-import cycles followed actual unequip, re-equip and cosmetic edits. Both equipped gloves and one nested glove retain IDs, ownership, slots, model/color fields, counts and quantities; loader normalization resets stored scale and reapplies absolute wearer fit.");
    }

    private static async Task<uint> CreateWearer(EngineTestContext ctx)
    {
        var creature = ctx.SpawnCreature("civilian");
        await ctx.WaitUntilAsync(() => GetIsObjectValid(GetItemInSlot(InventorySlot.Chest, creature)), 10f, "the fixture's outfit");
        await ctx.ExecuteInCreatureContextAsync(creature, () =>
        {
            var armor = GetItemInSlot(InventorySlot.Chest, creature);
            EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe, 0);
            ClearArmorHands(armor);
            SetBody(creature, AppearanceType.Human, 1, 1);
            EquippedItemAppearance.Refresh(creature, armor);
        });
        return creature;
    }

    private static unsafe void PlaceReloadedWearer(EngineTestContext ctx, uint creature)
    {
        ctx.Assert(!GetIsPC(creature), "The owner archive reloads with its original NPC inventory semantics");
        var native = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(creature).AsNWSCreature();
        // NWNX Deserialize calls LoadCreature/LoadObjectState, restoring the saved area ID.
        // AddToArea otherwise skips inserting the new object into the area's object list.
        native.SetArea(null);
        ObjectPlugin.AddToArea(creature, ctx.Arena, GetPositionFromLocation(ctx.GetArenaLocation()));
        ctx.AssertEqual(ctx.Arena, GetArea(creature), "The restored owner enters the test arena");
        ctx.Assert(PlayerAbilityFixture.IsListedInArena(ctx, creature), "The restored owner is in the native area object list");
    }

    private static void SetBody(uint creature, AppearanceType appearance, int right, int left)
    {
        SetCreatureAppearanceType(creature, appearance);
        SetGender(creature, Gender.Male);
        SetPhenoType(PhenoType.Normal, creature);
        SetCreatureBodyPart(CreaturePart.RightHand, right, creature);
        SetCreatureBodyPart(CreaturePart.LeftHand, left, creature);
    }

    private static void ClearArmorHands(uint armor)
    {
        EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightHand, 0);
        EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftHand, 0);
    }

    private static void ChangeGlove(uint creature, uint item, int appearance)
    {
        EquippedItemAppearance.Set(item, ItemAppearanceType.WeaponColor, (int)AppearanceWeapon.Middle, appearance / 100);
        EquippedItemAppearance.Set(item, ItemAppearanceType.WeaponModel, (int)AppearanceWeapon.Middle, appearance % 100);
        EquippedItemAppearance.Refresh(creature, item);
    }

    private static void Unequip(EngineTestContext ctx, uint creature, uint item, string label)
    {
        var quantity = GetItemStackSize(item);
        var reportedSuccess = CreaturePlugin.RunUnequip(creature, item);
        var right = GetItemInSlot(InventorySlot.RightHand, creature);
        var left = GetItemInSlot(InventorySlot.LeftHand, creature);
        var owner = GetItemPossessor(item, true);
        // Verify the native result, as the existing equipment tests do, rather than
        // assuming the wrapper's equality-to-one return mapping describes slot state.
        ctx.Assert(right != item && left != item && owner == creature,
            $"{label}: returned {reportedSuccess}, item {item:X8}, right {right:X8}, left {left:X8}, owner {owner:X8}");
        ctx.AssertEqual(quantity, GetItemStackSize(item), label + " preserves its quantity");
    }

    private static void Equip(EngineTestContext ctx, uint creature, uint item, InventorySlot slot, string label)
    {
        var before = EquipmentState(creature, item);
        var reportedSuccess = CreaturePlugin.RunEquip(creature, item, slot);
        ctx.Assert(GetItemInSlot(slot, creature) == item && GetItemPossessor(item, true) == creature,
            $"{label}: returned {reportedSuccess}; before {before}; after {EquipmentState(creature, item)}");
    }

    private static string EquipmentState(uint creature, uint item) =>
        $"item {item:X8}, right {GetItemInSlot(InventorySlot.RightHand, creature):X8}, " +
        $"left {GetItemInSlot(InventorySlot.LeftHand, creature):X8}, owner {GetItemPossessor(item, true):X8}, " +
        $"CanEquip '{SWLOR.Game.Server.Service.Item.CanEquip(creature, item)}'";

    private static int[] ReadAppearance(uint item) => new[] { ItemAppearanceType.WeaponModel, ItemAppearanceType.WeaponColor }
        .SelectMany(type => new[] { AppearanceWeapon.Bottom, AppearanceWeapon.Middle, AppearanceWeapon.Top }
            .Select(part => GetItemAppearance(item, type, (int)part))).ToArray();

    private static float Expected(string right, string left, int appearance) =>
        Math.Max(PowerGloveHandFitCatalog.GetScale(right, appearance), PowerGloveHandFitCatalog.GetScale(left, appearance));

    private static void AssertPair(EngineTestContext ctx, uint right, uint left, float expected, string label)
    {
        AssertFit(ctx, right, expected, label + " right");
        AssertFit(ctx, left, expected, label + " left");
    }

    private static void AssertFit(EngineTestContext ctx, uint item, float expected, string label) =>
        AssertTransform(ctx, item, ObjectVisualTransform.Scale, expected, FitScope, label);

    private static void AssertIndependentTransforms(EngineTestContext ctx, uint item)
    {
        AssertTransform(ctx, item, ObjectVisualTransform.Scale, 1.13f, ObjectVisualTransformDataScopeType.Base, "Base scale remains independent");
        AssertTransform(ctx, item, ObjectVisualTransform.RotateZ, 17f, ObjectVisualTransformDataScopeType.Base, "Base rotation remains independent");
        AssertTransform(ctx, item, ObjectVisualTransform.TranslateX, .024f, ObjectVisualTransformDataScopeType.Base, "Base translation remains independent");
        AssertTransform(ctx, item, ObjectVisualTransform.RotateY, 9f, FitScope, "Middle rotation remains independent");
        AssertTransform(ctx, item, ObjectVisualTransform.TranslateZ, .007f, FitScope, "Middle translation remains independent");
    }

    private static void AssertTransform(EngineTestContext ctx, uint item, ObjectVisualTransform transform, float expected,
        ObjectVisualTransformDataScopeType scope, string label)
    {
        var actual = GetObjectVisualTransform(item, transform, nScope: scope);
        ctx.Assert(Math.Abs(expected - actual) < .0001f, $"{label}: expected {expected}, actual {actual}");
    }

    private static async Task<uint> CreateItem(EngineTestContext ctx, uint creature, string resref, uint owner)
    {
        var item = OBJECT_INVALID;
        await ctx.ExecuteInCreatureContextAsync(creature, () => item = CreateItemOnObject(resref, owner));
        await ctx.WaitUntilAsync(() => GetIsObjectValid(item) && GetItemPossessor(item, true) == owner, 5f,
            $"fixture item '{resref}' to enter immediate owner {owner:X8}");
        return item;
    }

    private static List<uint> GloveInventory(uint creature) => InventoryItems(creature)
        .Where(item => GetBaseItemType(item) == BaseItem.PowerGlove).ToList();

    private static List<uint> InventoryItems(uint creature)
    {
        var items = new List<uint>();
        var visited = new HashSet<uint>();
        AddContents(creature);
        for (var slot = 0; slot < NumberOfInventorySlots; slot++)
            AddItem(GetItemInSlot((InventorySlot)slot, creature));
        return items;

        void AddContents(uint owner)
        {
            for (var item = GetFirstItemInInventory(owner); GetIsObjectValid(item); item = GetNextItemInInventory(owner))
                AddItem(item);
        }

        void AddItem(uint item)
        {
            if (!GetIsObjectValid(item) || !visited.Add(item))
                return;
            if (GetHasInventory(item))
                AddContents(item);
            items.Add(item);
        }
    }
}
