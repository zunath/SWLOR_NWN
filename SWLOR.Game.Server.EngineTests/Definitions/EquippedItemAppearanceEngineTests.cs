using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using ItemPlugin = SWLOR.NWN.API.NWNX.ItemPlugin;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class EquippedItemAppearanceEngineTests
    {
        private const string ItemMarker = "ENGINE_TEST_COSMETIC_ITEM";
        private const string QueuedAbilityMarker = "ACTIVE_ABILITY_ID";
        private static EventObservation _observation;

        // These are real production-dispatch observers, loaded only with the engine-test
        // assembly. They never signal an event or skip an equip on behalf of a test.
        [NWNEventHandler(ScriptName.OnValidateItemEquipBefore)]
        public static void ObserveValidateBefore() => Record(ScriptName.OnValidateItemEquipBefore, OBJECT_SELF);
        [NWNEventHandler(ScriptName.OnValidateItemEquipAfter)]
        public static void ObserveValidateAfter() => Record(ScriptName.OnValidateItemEquipAfter, OBJECT_SELF);
        [NWNEventHandler(ScriptName.OnItemEquipValidateBefore)]
        public static void ObserveEquipBefore() => Record(ScriptName.OnItemEquipValidateBefore, OBJECT_SELF);
        [NWNEventHandler(ScriptName.OnItemEquipValidateAfter)]
        public static void ObserveEquipAfter() => Record(ScriptName.OnItemEquipValidateAfter, OBJECT_SELF);
        [NWNEventHandler(ScriptName.OnItemUnequipBefore)]
        public static void ObserveUnequipBefore() => Record(ScriptName.OnItemUnequipBefore, OBJECT_SELF);
        [NWNEventHandler(ScriptName.OnItemUnequipAfter)]
        public static void ObserveUnequipAfter() => Record(ScriptName.OnItemUnequipAfter, OBJECT_SELF);
        [NWNEventHandler(ScriptName.OnSWLORItemEquipValidBefore)]
        public static void ObserveValidatedEquip() => Record(ScriptName.OnSWLORItemEquipValidBefore, OBJECT_SELF);
        [NWNEventHandler(ScriptName.OnModuleEquip)]
        public static void ObserveModuleEquip() => Record(ScriptName.OnModuleEquip, GetPCItemLastEquippedBy());
        [NWNEventHandler(ScriptName.OnModuleUnequip)]
        public static void ObserveModuleUnequip() => Record(ScriptName.OnModuleUnequip, GetPCItemLastUnequippedBy());

        [EngineTest("Equipped armor cosmetic edits preserve gameplay and emit no equipment events", Category = "AppearanceEditor", TimeoutSeconds = 60f)]
        public static async Task ArmorEditsNeverReequip(EngineTestContext ctx)
        {
            var creature = await SpawnCivilianAsync(ctx);
            var item = GetItemInSlot(InventorySlot.Chest, creature);
            using var observation = new EventObservation(creature);
            var genuineEvents = await VerifyGenuineEquipmentEventsAsync(ctx, creature, item, InventorySlot.Chest, observation);
            uint template = OBJECT_INVALID;
            await AssignedAsync(ctx, creature, () =>
            {
                template = CopyItem(item, creature, true);
                ctx.Assert(GetIsObjectValid(template), "The outfit template must be a separate inventory item.");
                // A template is deliberately mechanically different. Only its cosmetic
                // fields may reach the equipped item through ApplyOutfit.
                ItemPlugin.SetWeight(template, 941);
                SetLocalInt(template, ItemMarker, 999);
                AddItemProperty(DurationType.Permanent, ItemPropertyACBonus(5), template);
                for (var part = 0; part < (int)AppearanceArmor.Num; ++part)
                    EquippedItemAppearance.Set(template, ItemAppearanceType.ArmorModel, part,
                        part == (int)AppearanceArmor.Robe ? 0 : 1);
                for (var index = 0; index < 120; ++index)
                    EquippedItemAppearance.Set(template, ItemAppearanceType.ArmorColor, index, 20 + index % 100);
                for (var part = 0; part < (int)AppearanceArmor.Num; ++part)
                for (var channel = 0; channel < 6; ++channel)
                    SetLocalInt(template, ArmorColorIndexCalculator.GetPerPartOverrideVariableName(
                        (AppearanceArmor)part, (AppearanceArmorColor)channel), 1);
                SetLocalInt(template, TintMapVariable.GetName("engine_cosmetic", TintMapLayerType.Cloth1), 18);
                SeedGameplaySentinels(ctx, creature, item);
            });
            await ctx.DelaySecondsAsync(0.5f);
            var before = Snapshot(creature, item, InventorySlot.Chest);
            var templateBefore = ItemPlugin.GetEntireItemAppearance(template);
            observation.Reset();

            await AssignedAsync(ctx, creature, () =>
            {
                var editor = BindEditor(creature);
                var torso = GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso) == 2 ? 1 : 2;
                InvokeModify(editor, (int)AppearanceArmor.Torso, torso);
                ctx.AssertEqual(torso, GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso),
                    "The production editor changes the existing item's torso");
            });
            await AssertSettledAsync(ctx, before, observation, "editor torso change");

            await AssignedAsync(ctx, creature, () =>
            {
                EquippedItemAppearance.Set(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftFoot, 2);
                EquippedItemAppearance.Set(item, ItemAppearanceType.ArmorColor,
                    ArmorColorIndexCalculator.CalculatePerPart(AppearanceArmor.LeftFoot, AppearanceArmorColor.Cloth1), 77);
                SetLocalInt(item, ArmorColorIndexCalculator.GetPerPartOverrideVariableName(
                    AppearanceArmor.LeftFoot, AppearanceArmorColor.Cloth1), 1);
                BindEditor(creature).OnClickCopyToRight()();
                AssertMirrored(ctx, item, true, "copy left to right");
            });
            await AssertSettledAsync(ctx, before, observation, "copy left to right");

            await AssignedAsync(ctx, creature, () =>
            {
                EquippedItemAppearance.Set(item, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightFoot, 3);
                EquippedItemAppearance.Set(item, ItemAppearanceType.ArmorColor,
                    ArmorColorIndexCalculator.CalculatePerPart(AppearanceArmor.RightFoot, AppearanceArmorColor.Cloth1), 66);
                BindEditor(creature).OnClickCopyToLeft()();
                AssertMirrored(ctx, item, false, "copy right to left");
            });
            await AssertSettledAsync(ctx, before, observation, "copy right to left");

            await AssignedAsync(ctx, creature, () =>
            {
                EquippedItemAppearance.ApplyOutfit(creature, item, template);
                for (var part = 0; part < (int)AppearanceArmor.Num; ++part)
                    ctx.AssertEqual(GetItemAppearance(template, ItemAppearanceType.ArmorModel, part),
                        GetItemAppearance(item, ItemAppearanceType.ArmorModel, part), $"Outfit model {part}");
                for (var index = 0; index < 120; ++index)
                    ctx.AssertEqual(GetItemAppearance(template, ItemAppearanceType.ArmorColor, index),
                        GetItemAppearance(item, ItemAppearanceType.ArmorColor, index), $"Outfit dye {index}");
                for (var part = 0; part < (int)AppearanceArmor.Num; ++part)
                for (var channel = 0; channel < 6; ++channel)
                    ctx.AssertEqual(1, GetLocalInt(item, ArmorColorIndexCalculator.GetPerPartOverrideVariableName(
                        (AppearanceArmor)part, (AppearanceArmorColor)channel)), $"Outfit override marker {part}/{channel}");
                ctx.AssertEqual(18, GetLocalInt(item, TintMapVariable.GetName("engine_cosmetic", TintMapLayerType.Cloth1)),
                    "Outfit copies stored material tint state");
                ctx.AssertEqual(templateBefore, ItemPlugin.GetEntireItemAppearance(template), "The template remains unchanged");
            });
            await AssertSettledAsync(ctx, before, observation, "outfit application");
            ctx.SetResultDetail("Native torso edits, both seven-part side copies, and all19 outfit models/120 dyes retained the same equipped item, item properties/locals, armor class/weight, NPC resources, skin properties, and queued-ability marker. Zero observed equipment events after settling. Genuine lifecycle control: " + genuineEvents + ". NPC fixture does not prove PC-only module event delivery or client visual rendering.");
        }

        [EngineTest("Equipped weapon model and color edits preserve gameplay and emit no equipment events", Category = "AppearanceEditor", TimeoutSeconds = 45f)]
        public static async Task WeaponEditsNeverReequip(EngineTestContext ctx)
        {
            var creature = await SpawnCivilianAsync(ctx);
            var weapon = await ctx.EquipItemAsync(creature, "nw_wswls001", InventorySlot.RightHand);
            await ctx.DelaySecondsAsync(0.5f);
            using var observation = new EventObservation(creature);
            var genuineEvents = await VerifyGenuineEquipmentEventsAsync(ctx, creature, weapon, InventorySlot.RightHand, observation);
            await AssignedAsync(ctx, creature, () =>
            {
                // Use a registered model/color combination on all three parts, so the
                // editor's public-appearance eligibility check is part of the exercise.
                for (var index = 0; index < 3; ++index)
                {
                    EquippedItemAppearance.Set(weapon, ItemAppearanceType.WeaponModel, index, 1);
                    EquippedItemAppearance.Set(weapon, ItemAppearanceType.WeaponColor, index, 1);
                }
                SeedGameplaySentinels(ctx, creature, weapon);
            });
            var before = Snapshot(creature, weapon, InventorySlot.RightHand);
            observation.Reset();
            await AssignedAsync(ctx, creature, () =>
            {
                var editor = BindEditor(creature);
                editor.SelectedItemTypeIndex = 3;
                ctx.Assert(editor.HasItemEquipped, "The native fixture weapon passes the editor's appearance eligibility checks.");
                for (var index = 0; index < 3; ++index)
                {
                    InvokeModify(editor, index, 202, 2);
                    ctx.AssertEqual(2, GetItemAppearance(weapon, ItemAppearanceType.WeaponModel, index), $"Weapon model {index}");
                    ctx.AssertEqual(2, GetItemAppearance(weapon, ItemAppearanceType.WeaponColor, index), $"Weapon color {index}");
                }
                EquippedItemAppearance.Set(weapon, ItemAppearanceType.WeaponModel, (int)AppearanceWeapon.Top, 3);
                EquippedItemAppearance.Set(weapon, ItemAppearanceType.WeaponColor, (int)AppearanceWeapon.Top, 3);
                EquippedItemAppearance.Refresh(creature, weapon);
                ctx.AssertEqual(3, GetItemAppearance(weapon, ItemAppearanceType.WeaponModel, (int)AppearanceWeapon.Top),
                    "Direct batch refresh keeps the edited weapon model");
                ctx.AssertEqual(3, GetItemAppearance(weapon, ItemAppearanceType.WeaponColor, (int)AppearanceWeapon.Top),
                    "Direct batch refresh keeps the edited weapon color");
            });
            await AssertSettledAsync(ctx, before, observation, "three weapon parts, colors, and explicit visual refresh");
            ctx.SetResultDetail("Production editor changed all three weapon model/color pairs and directly refreshed a later batch on the same equipped weapon. Native item/gameplay snapshots and queued-ability marker stayed identical, with zero observed equipment events. Genuine lifecycle control: " + genuineEvents + ". NPC fixture excludes PC database and client visual rendering.");
        }

        [EngineTest("Helmet cloak and off-hand shield cosmetic edits preserve every equipped item", Category = "AppearanceEditor", TimeoutSeconds = 60f)]
        public static async Task SimpleModelSlotsNeverReequip(EngineTestContext ctx)
        {
            var creature = await SpawnCivilianAsync(ctx);
            var cases = new[]
            {
                (Slot: InventorySlot.Head, Page: 1, Resref: "advent_helmet", Type: BaseItem.Helmet, Initial: 1, Edited: 2, Batch: 3),
                (Slot: InventorySlot.Cloak, Page: 2, Resref: "advent_cloak", Type: BaseItem.Cloak, Initial: 1, Edited: 2, Batch: 3),
                (Slot: InventorySlot.LeftHand, Page: 4, Resref: "byysk_shield002", Type: BaseItem.LargeShield, Initial: 11, Edited: 12, Batch: 13)
            };
            var items = new Dictionary<InventorySlot, uint>();
            foreach (var test in cases)
            {
                var item = await ctx.EquipItemAsync(creature, test.Resref, test.Slot);
                items.Add(test.Slot, item);
                ctx.AssertEqual(test.Type, GetBaseItemType(item), $"{test.Slot}: fixture uses a registered base item");
                await AssignedAsync(ctx, creature, () =>
                {
                    SetItemCursedFlag(item, false);
                    SetPlotFlag(item, false);
                    EquippedItemAppearance.Set(item, ItemAppearanceType.SimpleModel, -1, test.Initial);
                });
            }
            await ctx.DelaySecondsAsync(0.5f);
            using var observation = new EventObservation(creature);
            var genuineEvents = new List<string>();
            foreach (var test in cases)
                genuineEvents.Add(test.Slot + ": " + await VerifyGenuineEquipmentEventsAsync(
                    ctx, creature, items[test.Slot], test.Slot, observation));
            await AssignedAsync(ctx, creature, () =>
            {
                foreach (var item in items.Values)
                    SeedGameplaySentinels(ctx, creature, item);
            });
            await ctx.DelaySecondsAsync(0.5f);
            var before = cases.Select(test => Snapshot(creature, items[test.Slot], test.Slot)).ToArray();
            var chestBefore = Snapshot(creature, GetItemInSlot(InventorySlot.Chest, creature), InventorySlot.Chest);
            observation.Reset();

            foreach (var test in cases)
            {
                var item = items[test.Slot];
                await AssignedAsync(ctx, creature, () =>
                {
                    var editor = BindEditor(creature);
                    editor.SelectedItemTypeIndex = test.Page;
                    ctx.Assert(editor.HasItemEquipped, $"{test.Slot}: fixture passes editor eligibility.");
                    // Helmet/cloak dispatch uses the ignored armor-part index; the
                    // registered simple shield uses the production simple-weapon path.
                    InvokeModify(editor, test.Slot == InventorySlot.LeftHand ? (int)ItemAppearanceType.SimpleModel : -1, test.Edited);
                    ctx.AssertEqual(test.Edited, GetItemAppearance(item, ItemAppearanceType.SimpleModel, -1),
                        $"{test.Slot}: the actual editor changes the simple model");
                    EquippedItemAppearance.Set(item, ItemAppearanceType.SimpleModel, -1, test.Batch);
                    if (test.Slot is InventorySlot.Head or InventorySlot.Cloak)
                        EquippedItemAppearance.Set(item, ItemAppearanceType.ArmorColor, (int)AppearanceArmorColor.Cloth1, 77);
                    EquippedItemAppearance.Refresh(creature, item);
                    ctx.AssertEqual(test.Batch, GetItemAppearance(item, ItemAppearanceType.SimpleModel, -1),
                        $"{test.Slot}: direct batched visual refresh retains the simple model");
                    if (test.Slot is InventorySlot.Head or InventorySlot.Cloak)
                        ctx.AssertEqual(77, GetItemAppearance(item, ItemAppearanceType.ArmorColor, (int)AppearanceArmorColor.Cloth1),
                            $"{test.Slot}: direct color write persists on the same item");
                });
                foreach (var snapshot in before)
                    await AssertSettledAsync(ctx, snapshot, observation, $"{test.Slot} edit retains {snapshot.Slot}");
                await AssertSettledAsync(ctx, chestBefore, observation, $"{test.Slot} edit retains the equipped armor");
            }
            ctx.SetResultDetail("Native editor and Set/Refresh paths changed registered helmet, cloak, and left-hand simple-shield models; helmet/cloak dyes also changed. All three equipped objects and the chest retained their identity, IPs/locals, AC/weight, resource and queued-state snapshots, with zero lifecycle callbacks. Genuine controls: " + string.Join("; ", genuineEvents) + ". NPC fixture excludes PC-only module delivery and client rendering.");
        }

        private static async Task<string> VerifyGenuineEquipmentEventsAsync(EngineTestContext ctx, uint creature, uint item,
            InventorySlot slot, EventObservation observation)
        {
            observation.Reset();
            await AssignedAsync(ctx, creature, () =>
            {
                ClearAllActions();
                ActionUnequipItem(item);
            });
            await ctx.WaitUntilAsync(() => GetItemInSlot(slot, creature) != item, 10f, "a genuine unequip to empty the slot");
            await ctx.DelaySecondsAsync(0.2f);
            await AssignedAsync(ctx, creature, () =>
            {
                SetLocalString(creature, QueuedAbilityMarker, "genuine-equip-must-clear");
                ClearAllActions();
                ActionEquipItem(item, slot);
            });
            await ctx.WaitUntilAsync(() => GetItemInSlot(slot, creature) == item, 10f, "a genuine equip to restore the same item");
            await ctx.DelaySecondsAsync(0.5f);
            foreach (var script in new[] { ScriptName.OnItemEquipValidateBefore, ScriptName.OnItemEquipValidateAfter,
                         ScriptName.OnItemUnequipBefore, ScriptName.OnItemUnequipAfter, ScriptName.OnSWLORItemEquipValidBefore })
                ctx.AssertEqual(1, observation.Counts[script], $"Genuine lifecycle must dispatch {script} exactly once");
            ctx.AssertEqual(string.Empty, GetLocalString(creature, QueuedAbilityMarker),
                "The real equip still clears queued ability state through the production handler");
            return string.Join(", ", observation.Counts.Select(pair => $"{pair.Key}={pair.Value}"));
        }

        private static void SeedGameplaySentinels(EngineTestContext ctx, uint creature, uint item)
        {
            SetLocalInt(item, ItemMarker, 731);
            SetLocalString(item, ItemMarker, "retain item-local state");
            SetLocalObject(item, ItemMarker, creature);
            AddItemProperty(DurationType.Permanent, TagItemProperty(ItemPropertyACBonus(2), ItemMarker), item);
            ctx.SuppressNPCNaturalRegen(creature);
            ctx.SetNPCResources(creature, 60, 70);
            SetCurrentHitPoints(creature, Math.Max(1, GetMaxHitPoints(creature) - 1));
            SetLocalString(creature, QueuedAbilityMarker, "cosmetic-edit-must-retain");
            SetLocalInt(creature, "ACTIVE_ABILITY_EFFECTIVE_PERK_LEVEL", 2);
        }

        private sealed record GameplaySnapshot(uint Creature, uint Item, InventorySlot Slot, string UUID,
            int Weight, int ArmorClass, int BaseArmorClass, int CreatureArmorClass, int[] Resources,
            string[] Properties, string[] SkinProperties, int StackSize, bool Identified, int LocalInt,
            string LocalString, uint LocalObject, string QueuedAbility, int QueuedLevel);

        private static GameplaySnapshot Snapshot(uint creature, uint item, InventorySlot slot) => new(
            creature, item, slot, GetObjectUUID(item), GetWeight(item), GetItemACValue(item), ItemPlugin.GetBaseArmorClass(item),
            GetAC(creature), new[] { GetCurrentHitPoints(creature), GetMaxHitPoints(creature), Stat.GetCurrentFP(creature),
                Stat.GetMaxFP(creature), Stat.GetCurrentStamina(creature), Stat.GetMaxStamina(creature) },
            Properties(item), Properties(GetItemInSlot(InventorySlot.CreatureArmor, creature)), GetItemStackSize(item),
            GetIdentified(item), GetLocalInt(item, ItemMarker), GetLocalString(item, ItemMarker), GetLocalObject(item, ItemMarker),
            GetLocalString(creature, QueuedAbilityMarker), GetLocalInt(creature, "ACTIVE_ABILITY_EFFECTIVE_PERK_LEVEL"));

        private static async Task AssertSettledAsync(EngineTestContext ctx, GameplaySnapshot before,
            EventObservation observation, string stage)
        {
            // The previous replacement implementation queued destruction/equip and tint
            // carry. Observe beyond a frame so deferred lifecycle work cannot evade this test.
            await ctx.DelaySecondsAsync(0.75f);
            ctx.Assert(GetIsObjectValid(before.Item), $"{stage}: the original item remains valid.");
            ctx.AssertEqual(before.Item, GetItemInSlot(before.Slot, before.Creature), $"{stage}: same item in the same slot");
            ctx.AssertEqual(before.Creature, GetItemPossessor(before.Item), $"{stage}: same possessor");
            var after = Snapshot(before.Creature, before.Item, before.Slot);
            ctx.Assert(before.Resources.SequenceEqual(after.Resources), $"{stage}: current and maximum HP/FP/Stamina unchanged.");
            ctx.Assert(before.Properties.SequenceEqual(after.Properties), $"{stage}: item properties unchanged.");
            ctx.Assert(before.SkinProperties.SequenceEqual(after.SkinProperties), $"{stage}: native stat-skin properties unchanged.");
            // Records compare arrays by identity, so compare the remaining scalar state
            // after substituting the already-checked snapshot arrays.
            ctx.AssertEqual(before, after with { Resources = before.Resources, Properties = before.Properties,
                SkinProperties = before.SkinProperties }, $"{stage}: identity, AC, weight, inventory, locals, and queued state");
            foreach (var pair in observation.Counts)
                ctx.AssertEqual(0, pair.Value, $"{stage}: cosmetic edits must not dispatch {pair.Key}");
        }

        private static string[] Properties(uint item)
        {
            var result = new List<string>();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                result.Add($"{GetItemPropertyType(ip)}:{GetItemPropertySubType(ip)}:{GetItemPropertyCostTable(ip)}:" +
                    $"{GetItemPropertyCostTableValue(ip)}:{GetItemPropertyParam1(ip)}:{GetItemPropertyParam1Value(ip)}:" +
                    $"{GetItemPropertyDurationType(ip)}:{GetItemPropertyTag(ip)}");
            return result.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        private static void AssertMirrored(EngineTestContext ctx, uint item, bool copyToRight, string stage)
        {
            foreach (var (left, right) in new[] { (AppearanceArmor.LeftShoulder, AppearanceArmor.RightShoulder),
                         (AppearanceArmor.LeftBicep, AppearanceArmor.RightBicep), (AppearanceArmor.LeftForearm, AppearanceArmor.RightForearm),
                         (AppearanceArmor.LeftHand, AppearanceArmor.RightHand), (AppearanceArmor.LeftThigh, AppearanceArmor.RightThigh),
                         (AppearanceArmor.LeftShin, AppearanceArmor.RightShin), (AppearanceArmor.LeftFoot, AppearanceArmor.RightFoot) })
            {
                ctx.AssertEqual(GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)left),
                    GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)right), $"{stage}: {left}/{right} models");
                for (var channel = 0; channel < 6; ++channel)
                {
                    var source = copyToRight ? left : right;
                    var destination = copyToRight ? right : left;
                    var color = GetItemAppearance(item, ItemAppearanceType.ArmorColor,
                        ArmorColorIndexCalculator.CalculatePerPart(source, (AppearanceArmorColor)channel));
                    var isExplicit = ArmorColorIndexCalculator.ShouldUsePerPartColor(color, GetLocalInt(item,
                        ArmorColorIndexCalculator.GetPerPartOverrideVariableName(source, (AppearanceArmorColor)channel)) > 0);
                    ctx.AssertEqual(isExplicit ? color : 255, GetItemAppearance(item, ItemAppearanceType.ArmorColor,
                            ArmorColorIndexCalculator.CalculatePerPart(destination, (AppearanceArmorColor)channel)),
                        $"{stage}: {left}/{right} dye {channel} preserves explicit color or inheritance");
                    ctx.AssertEqual(isExplicit ? 1 : 0, GetLocalInt(item,
                        ArmorColorIndexCalculator.GetPerPartOverrideVariableName(destination, (AppearanceArmorColor)channel)),
                        $"{stage}: destination override marker {channel}");
                }
            }
        }

        private static async Task<uint> SpawnCivilianAsync(EngineTestContext ctx)
        {
            var creature = ctx.SpawnCreature("civilian");
            await ctx.WaitUntilAsync(() => GetIsObjectValid(GetItemInSlot(InventorySlot.Chest, creature)), 10f, "the civilian outfit equip");
            await ctx.DelaySecondsAsync(0.5f);
            await AssignedAsync(ctx, creature, () =>
            {
                var item = GetItemInSlot(InventorySlot.Chest, creature);
                SetItemCursedFlag(item, false);
                SetPlotFlag(item, false);
            });
            return creature;
        }

        private static AppearanceEditorViewModel BindEditor(uint creature)
        {
            var geometry = new GuiRectangle(0, 0, 1200, 900);
            var editor = new AppearanceEditorViewModel { Geometry = geometry };
            editor.Bind(OBJECT_INVALID, 0, geometry, GuiWindowType.AppearanceEditor, new AppearanceEditorPayload(creature), OBJECT_INVALID);
            editor.OnSelectEquipment()();
            return editor;
        }

        private static void InvokeModify(AppearanceEditorViewModel editor, int index, int model, int color = -1) =>
            typeof(AppearanceEditorViewModel).GetMethod("ModifyItemPart", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(editor, new object[] { index, model, color });

        private static async Task AssignedAsync(EngineTestContext ctx, uint creature, Action action)
        {
            var completed = false;
            Exception failure = null;
            AssignCommand(creature, () =>
            {
                try { action(); }
                catch (Exception exception) { failure = exception; }
                finally { completed = true; }
            });
            await ctx.WaitUntilAsync(() => completed, 5f, "the assigned equipped-item action");
            if (failure != null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }

        private static void Record(string script, uint creature)
        {
            if (_observation?.Creature == creature)
                ++_observation.Counts[script];
        }

        private sealed class EventObservation : IDisposable
        {
            public uint Creature { get; }
            public Dictionary<string, int> Counts { get; } = new[] { ScriptName.OnValidateItemEquipBefore,
                ScriptName.OnValidateItemEquipAfter, ScriptName.OnItemEquipValidateBefore, ScriptName.OnItemEquipValidateAfter,
                ScriptName.OnItemUnequipBefore, ScriptName.OnItemUnequipAfter, ScriptName.OnSWLORItemEquipValidBefore,
                ScriptName.OnModuleEquip, ScriptName.OnModuleUnequip }.ToDictionary(script => script, _ => 0);

            public EventObservation(uint creature)
            {
                if (_observation != null) throw new InvalidOperationException("Equipment observation is already active.");
                Creature = creature;
                _observation = this;
            }

            public void Reset()
            {
                foreach (var script in Counts.Keys.ToArray()) Counts[script] = 0;
            }

            public void Dispose() => _observation = null;
        }
    }
}
