using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.AppearanceDefinition;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class HelmetModelRendererEngineTests
    {
        [EngineTest("Helmet model scrolling through 64 preserves every playable species and gender", Category = "HelmetModelRenderer", TimeoutSeconds = 180f)]
        public static async Task ScrollThroughNativeHelmet(EngineTestContext ctx)
        {
            var creature = ctx.SpawnCreature("civilian");
            await ctx.DelaySecondsAsync(1f);
            var helmet = await ctx.EquipItemAsync(creature, "advent_helmet", InventorySlot.Head);
            var catalog = (IDictionary)typeof(AppearanceEditorViewModel)
                .GetField("_armorAppearances", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            var failures = new List<string>();
            var combinations = 0;
            foreach (var race in RacialAppearanceRegistry.GetAppearanceTypes()
                         .Where(race => ((BaseArmorAppearanceDefinition)catalog[race]).Helmet.Contains(64)))
            foreach (var gender in new[] { Gender.Female, Gender.Male })
            {
                combinations++;
                try { await VerifyCombination(ctx, creature, helmet, race, gender); }
                catch (EngineTestAssertionException exception) { failures.Add($"{race}/{gender}: {exception.Message}"); }
            }
            ctx.Assert(combinations > 0, "The editor must offer helmet64 for playable appearances.");
            ctx.Assert(failures.Count == 0, string.Join(Environment.NewLine, failures));
            ctx.SetResultDetail($"{combinations} species/gender combinations offering helmet64: two passes through models 63/64/65/114, hide/show, native head refresh and settled updates. Same item, ownership and canonical head. Server state only; client rendering requires separate evidence.");
        }

        private static async Task VerifyCombination(EngineTestContext ctx, uint creature, uint helmet, AppearanceType race, Gender gender)
        {
            RacialAppearanceRegistry.TryGet(race, out var definition);
            await ctx.ExecuteInCreatureContextAsync(creature, () =>
            {
                ArmorDisplay.Apply(creature, false, true);
                SetCreatureAppearanceType(creature, race);
                SetGender(creature, gender);
                SetCreatureBodyPart(CreaturePart.Head,
                    (gender == Gender.Female ? definition.FemaleHeads : definition.MaleHeads).First(), creature);
                ArmorDisplay.Apply(creature, true, true);
            });
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(creature);
            var originalHead = native.m_pStats.m_nHeadVariation;
            var geometry = new GuiRectangle(0, 0, 1200, 900);
            var editor = new AppearanceEditorViewModel { Geometry = geometry };
            editor.Bind(OBJECT_INVALID, 0, geometry, GuiWindowType.AppearanceEditor,
                new AppearanceEditorPayload(creature), OBJECT_INVALID);
            editor.OnSelectEquipment()();
            editor.SelectedItemTypeIndex = 1;
            var modify = typeof(AppearanceEditorViewModel).GetMethod("ModifyItemPart", BindingFlags.Instance | BindingFlags.NonPublic);

            for (var pass = 0; pass < 2; pass++)
            foreach (var model in new[] { 63, 64, 65, 114, 64, 63 })
            {
                await ctx.ExecuteInCreatureContextAsync(creature, () =>
                {
                    // Exercise a real native head refresh before leaving the projected path.
                    if (pass == 1 && model == 64)
                        SetCreatureBodyPart(CreaturePart.Head, originalHead, creature);
                    modify.Invoke(editor, new object[] { (int)AppearanceArmor.Invalid, model, -1 });
                    AssertShown();
                    ArmorDisplay.Apply(creature, false, true);
                    ctx.AssertEqual(originalHead, native.m_cAppearance.m_nHeadVariation, $"{model}: hiding restores the head");
                    ctx.AssertEqual(OBJECT_INVALID, native.m_cAppearance.m_oidHeadItem, $"{model}: hiding removes the attachment");
                    ctx.Assert(!HelmetModelRenderer.IsShownByOwner(helmet), $"{model}: owner hide choice is retained");
                    ArmorDisplay.Apply(creature, true, true);
                    AssertShown();
                    native.UpdateAppearanceForEquippedItems();
                    AssertShown();
                });
                await ctx.DelaySecondsAsync(0.1f);
                AssertShown();

                void AssertShown()
                {
                    var bodyRace = Get2DAString("appearance", "RACE", (int)race);
                    var projected = model != 64 && bodyRace.Length == 1 &&
                        Get2DAString("appearance", "MODELTYPE", (int)race).StartsWith("P", StringComparison.OrdinalIgnoreCase) &&
                        ModelResource.Exists(HelmetModelRenderer.GetHeadModel(gender == Gender.Female ? 'f' : 'm',
                            bodyRace, (ushort)(1000 + model)));
                    ctx.AssertEqual(projected ? (ushort)(1000 + model) : originalHead,
                        native.m_cAppearance.m_nHeadVariation, $"Pass {pass}, model {model}: visible head");
                    ctx.AssertEqual(projected ? OBJECT_INVALID : helmet, native.m_cAppearance.m_oidHeadItem,
                        $"Pass {pass}, model {model}: native helmet attachment");
                    ctx.AssertEqual(projected ? 1 : 0, GetHiddenWhenEquipped(helmet), $"{model}: native visibility");
                    ctx.Assert(HelmetModelRenderer.IsShownByOwner(helmet), $"{model}: owner still shows the helmet");
                    ctx.AssertEqual(originalHead, native.m_pStats.m_nHeadVariation, $"{model}: canonical head is unchanged");
                    ctx.AssertEqual(helmet, GetItemInSlot(InventorySlot.Head, creature), $"{model}: the same helmet stays equipped");
                    ctx.AssertEqual(creature, GetItemPossessor(helmet), $"{model}: ownership is unchanged");
                    ctx.AssertEqual(model, GetItemAppearance(helmet, ItemAppearanceType.SimpleModel, 0), $"{model}: chosen model is retained");
                }
            }
        }
    }
}
