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
        var right = await PowerGlovePairEngineTests.EquipMain(ctx, wearer);
        var left = await PowerGlovePairEngineTests.WaitForPair(ctx, wearer, right);
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
                var expected = Expected("pmo0_handr201", "pmo0_handl201", appearance);
                ctx.Assert(expected > 1f, "Cathar hands enlarge every compact shell");
                var savedAppearance = ReadAppearance(right);
                for (var pass = 0; pass < 5; pass++)
                    TintMapService.ApplyCurrentColors(wearer);
                AssertPair(ctx, right, left, expected, "Repeated cosmetic refresh");
                ctx.Assert(savedAppearance.SequenceEqual(ReadAppearance(right)), "Fitting never rewrites weapon appearance IDs");
                ctx.Assert(savedAppearance.SequenceEqual(ReadAppearance(left)), "The one selected model supplies both fitted visuals");
                ctx.AssertEqual(BaseItem.PowerGlove, GetBaseItemType(right), "Fitting retains the Power Glove base item");
                AssertIndependentTransforms(ctx, right);
            }

            foreach (var appearance in new[] { 106, 103 })
            {
                ChangeGlove(wearer, right, appearance);
                AssertPair(ctx, right, left, 1f, "Invisible and Blastknuckle selections release both fits immediately");
                ctx.Assert(ReadAppearance(right).SequenceEqual(ReadAppearance(left)), "Both hands use the one selected appearance");
                AssertIndependentTransforms(ctx, right);
            }
            ChangeGlove(wearer, right, 101);

            // The normal equipment validator rejects a move into an occupied hand.
            ctx.Assert(!CreaturePlugin.RunEquip(wearer, left, InventorySlot.RightHand), "The occupied-slot equip is rejected");
            ctx.AssertEqual(right, GetItemInSlot(InventorySlot.RightHand, wearer), "Rejected equip preserves the main hand");
            ctx.AssertEqual(left, GetItemInSlot(InventorySlot.LeftHand, wearer), "Rejected equip preserves the off hand");
            AssertPair(ctx, right, left, Expected("pmo0_handr201", "pmo0_handl201", 101), "Rejected equip preserves fit");
        });

        await ctx.ExecuteInCreatureContextAsync(wearer, () => ActionUnequipItem(left));
        await PowerGlovePairEngineTests.WaitForRemovedPair(ctx, wearer, right, left);
        AssertFit(ctx, right, 1f, "Removing the cosmetic hand releases the real inventory glove's fit");
        await PowerGlovePairEngineTests.EquipExisting(ctx, wearer, right, InventorySlot.RightHand);
        left = await PowerGlovePairEngineTests.WaitForPair(ctx, wearer, right);
        AssertPair(ctx, right, left, Expected("pmo0_handr201", "pmo0_handl201", 101), "A regenerated companion fits the same real glove");
        await ctx.ExecuteInCreatureContextAsync(wearer, () => ActionUnequipItem(right));
        await PowerGlovePairEngineTests.WaitForRemovedPair(ctx, wearer, right, left);
        AssertFit(ctx, right, 1f, "Removing the main hand releases its fitted scale");
        await ctx.ExecuteInCreatureContextAsync(wearer, () =>
        {
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
            ctx.AssertEqual(wearer, GetItemPossessor(right), "The original glove remains in its owner's inventory");
            AssertFit(ctx, right, 1f, "Replacing a glove leaves no fit on the inventory item");
            SetObjectVisualTransform(sword, ObjectVisualTransform.Scale, 1.37f, nScope: FitScope);
            ctx.Assert(!CreaturePlugin.RunEquip(wearer, right, InventorySlot.LeftHand), "The real glove cannot become a separate offhand weapon");
            ctx.AssertEqual(OBJECT_INVALID, GetItemInSlot(InventorySlot.LeftHand, wearer), "Rejected glove equip leaves the offhand empty");
            ctx.AssertEqual(sword, GetItemInSlot(InventorySlot.RightHand, wearer), "Rejected glove equip preserves the other weapon's slot");
            TintMapService.ApplyCurrentColors(wearer);
            AssertFit(ctx, sword, 1.37f, "Non-Power Glove equipment transforms are untouched");
            AssertIndependentTransforms(ctx, right);
        });
        await ctx.ExecuteInCreatureContextAsync(wearer, () => ActionUnequipItem(sword));
        await ctx.WaitUntilAsync(() => GetItemInSlot(InventorySlot.RightHand, wearer) != sword, 5f, "the replacement weapon to leave the hand");
        await PowerGlovePairEngineTests.EquipExisting(ctx, wearer, right, InventorySlot.RightHand);
        left = await PowerGlovePairEngineTests.WaitForPair(ctx, wearer, right);
        AssertPair(ctx, right, left, Expected("pmo0_handr201", "pmo0_handl201", 101), "Reacquired real glove fits both hands");
        AssertFit(ctx, sword, 1.37f, "Swapping back preserves the other weapon's independent transform");
        PowerGlovePairEngineTests.AssertOwnedItems(ctx, wearer, right, 1);
        ctx.SetResultDetail("One real glove supplies both hands through native equipment events, body/armor/weapon edits, invisible reset, either-hand removal, rejected independent offhand equip, replacement and drop/acquire. Repeated refresh preserves authored appearance IDs and independent transform channels. Client pixels require separate visual verification.");
    }

    [EngineTest("One owned Power Glove retains paired fit through two save reload cycles with nested inventory", Category = "PowerGloveFit", TimeoutSeconds = 90f)]
    public static async Task SaveReloadDoesNotCompoundOrChangeOwnership(EngineTestContext ctx)
    {
        var wearer = await CreateWearer(ctx);
        var right = await PowerGlovePairEngineTests.EquipMain(ctx, wearer);
        var left = await PowerGlovePairEngineTests.WaitForPair(ctx, wearer, right);
        var bag = await CreateItem(ctx, wearer, "bag_sm", wearer);
        var stored = await CreateItem(ctx, wearer, "t_pglove", bag);
        await ctx.ExecuteInCreatureContextAsync(wearer, () =>
        {
            SetLocalString(right, RoleVariable, "right");
            SetLocalString(stored, RoleVariable, "stored");
            SetBody(wearer, AppearanceType.Cathar, 201, 201);
            ExecuteScript(ScriptName.OnAppearanceEdit, wearer);
        });

        for (var cycle = 0; cycle < 2; cycle++)
        {
            var label = $"Save/reload cycle {cycle + 1}";
            var items = GloveInventory(wearer);
            right = items.Single(item => GetLocalString(item, RoleVariable) == "right");
            stored = items.Single(item => GetLocalString(item, RoleVariable) == "stored");
            var appearance = cycle == 0 ? 104 : 105;
            var data = string.Empty;
            var ownerIdentity = string.Empty;
            var bagIdentity = string.Empty;
            var companionIdentity = string.Empty;
            Dictionary<string, (string Identity, int Quantity, int[] Appearance, string Properties)> saved = null;
            left = await PowerGlovePairEngineTests.WaitForPair(ctx, wearer, right);
            await ctx.ExecuteInCreatureContextAsync(wearer, () => ActionUnequipItem(cycle == 0 ? right : left));
            await PowerGlovePairEngineTests.WaitForRemovedPair(ctx, wearer, right, left);
            AssertFit(ctx, right, 1f, label + " normalizes the removed real glove");
            await PowerGlovePairEngineTests.EquipExisting(ctx, wearer, right, InventorySlot.RightHand);
            left = await PowerGlovePairEngineTests.WaitForPair(ctx, wearer, right);
            var previousItems = InventoryItems(wearer);
            await ctx.ExecuteInCreatureContextAsync(wearer, () =>
            {
                ctx.AssertEqual(right, GetItemInSlot(InventorySlot.RightHand, wearer), label + " begins with the original main glove");
                ctx.Assert(PowerGloveEquipment.IsLinkedPair(wearer, right, left), label + " uses a generated companion");
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Accuracy, -1, 7 + cycle), right);
                ChangeGlove(wearer, right, appearance);
                AssertPair(ctx, right, left, Expected("pmo0_handr201", "pmo0_handl201", appearance), label + " fitted equipment before saving");
                ctx.Assert(ReadAppearance(right).SequenceEqual(ReadAppearance(left)), label + " mirrors the real glove's appearance");
                // Model a stale native transform in a saved bag, without introducing a persistent marker.
                SetObjectVisualTransform(stored, ObjectVisualTransform.Scale, 1.45f, nScope: FitScope);
                saved = items.ToDictionary(item => GetLocalString(item, RoleVariable),
                    item => (GetObjectUUID(item), GetItemStackSize(item), ReadAppearance(item), PowerGlovePairEngineTests.Properties(item)));
                ctx.AssertEqual(2, saved.Count, label + " contains exactly the equipped real glove and the nested spare");
                ownerIdentity = GetObjectUUID(wearer);
                bagIdentity = GetObjectUUID(GetItemPossessor(stored, true));
                companionIdentity = GetObjectUUID(left);
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
                PowerGloveEquipment.RestorePair(wearer);
                PowerGloveFit.OnSpawn();
                TintMapService.ApplyCurrentColors(wearer);
            });
            right = GloveInventory(wearer).Single(item => GetLocalString(item, RoleVariable) == "right");
            left = await PowerGlovePairEngineTests.WaitForPair(ctx, wearer, right);
            await ctx.ExecuteInCreatureContextAsync(wearer, () =>
            {
                ctx.AssertEqual(ownerIdentity, GetObjectUUID(wearer), label + " preserves the owner identity");
                var restored = GloveInventory(wearer);
                ctx.AssertEqual(2, restored.Count, label + " retains exactly two real Power Gloves including the bag item");
                ctx.AssertEqual(1, InventoryItems(wearer).Count(PowerGloveEquipment.IsGeneratedCompanion), label + " retains exactly one temporary cosmetic hand");
                ctx.Assert(companionIdentity != GetObjectUUID(left), label + " regenerates the cosmetic hand instead of retaining a saved economy object");
                ctx.Assert(!GetIsObjectValid(GetObjectByUUID(companionIdentity)), label + " leaves no archived companion behind");
                ctx.Assert(!Item.IsEconomyRestricted(right), label + " keeps the real glove available to the economy");
                ctx.Assert(Item.IsEconomyRestricted(left), label + " excludes the cosmetic companion from the economy");
                ctx.AssertEqual(saved.Values.Sum(value => value.Quantity), restored.Sum(GetItemStackSize), label + " preserves all stack quantities");
                foreach (var item in restored)
                {
                    var role = GetLocalString(item, RoleVariable);
                    var expected = saved[role];
                    ctx.AssertEqual(expected.Identity, GetObjectUUID(item), label + " preserves each item identity");
                    ctx.AssertEqual(expected.Quantity, GetItemStackSize(item), label + " preserves each item's quantity");
                    ctx.Assert(expected.Appearance.SequenceEqual(ReadAppearance(item)), label + " preserves every weapon appearance field");
                    ctx.AssertEqual(expected.Properties, PowerGlovePairEngineTests.Properties(item), label + " preserves all real item upgrades");
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
                        ctx.AssertEqual(wearer, GetItemPossessor(item), label + " creature owns the equipped glove");
                        ctx.AssertEqual(item, GetItemInSlot(InventorySlot.RightHand, wearer), label + " retains the original main-hand assignment");
                        AssertPair(ctx, item, left, Expected("pmo0_handr201", "pmo0_handl201", appearance), label + " recomputes both fits without compounding");
                        ctx.Assert(ReadAppearance(item).SequenceEqual(ReadAppearance(left)), label + " restores matching visual choices");
                        ctx.Assert(!GetIsItemPropertyValid(GetFirstItemProperty(left)), label + " never copies upgrades to the cosmetic hand");
                    }
                }
                ctx.Log(label + " verified the two real glove identities, ownership, quantities, upgrades, appearances and one paired cosmetic fit");
            });
        }
        ctx.SetResultDetail("Two native owner/item export-import cycles follow either-hand removal, re-equip, upgrades and cosmetic edits. One equipped real glove and one nested spare retain IDs, ownership, slots, upgrades, model/color fields, counts and quantities. Each reload normalizes stored scale and supplies exactly one cosmetic companion with matching absolute fit and no item properties.");
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
