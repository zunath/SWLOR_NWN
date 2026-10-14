using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class PowerGloveAppearanceEngineTests
    {
        [EngineTest("One owned Power Glove applies every production appearance choice to both hands", Category = "PowerGloveAppearance", TimeoutSeconds = 35f)]
        public static async Task AllModelsUseProductionPicker(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx);
            player.Update(record => record.Skills[SkillType.Unarmed].Rank = 50);
            var main = await PowerGlovePairEngineTests.EquipMain(ctx, player.Creature);
            var off = await PowerGlovePairEngineTests.WaitForPair(ctx, player.Creature, main);
            var template = Gui.GetWindowTemplate(GuiWindowType.AppearanceEditor);
            var playerWindow = template.CreatePlayerWindowAction();
            var editor = (AppearanceEditorViewModel)playerWindow.ViewModel;
            editor.Geometry = new GuiRectangle(25f, 20f, 590f, 660f);
            var windows = (Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>>)typeof(Gui)
                .GetField("_playerWindows", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            ctx.Assert(!windows.ContainsKey(player.Id), "fixture GUI registration must be isolated");
            windows.Add(player.Id, new Dictionary<GuiWindowType, GuiPlayerWindow>
            {
                [GuiWindowType.AppearanceEditor] = playerWindow
            });
            var snapshots = new JArray();
            var labels = new[] { "Power Glove", "Vibroknuckler", "Blastknuckle", "Shock Glove", "Power Fist", "Bare Fists (Invisible)" };
            try
            {
                await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
                    Gui.TogglePlayerWindow(player.Creature, GuiWindowType.AppearanceEditor,
                        new AppearanceEditorPayload(player.Creature)));
                await ctx.WaitFrameAsync();
                ctx.Assert(playerWindow.WindowToken > 0, "the actual production NUI window opens");
                foreach (var (slot, itemType, item) in new[]
                {
                    (InventorySlot.RightHand, 3, main), (InventorySlot.LeftHand, 4, main)
                })
                {
                    await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
                    {
                        var weight = GetWeight(item);
                        var damage = WeaponDamage.GetEffectiveDMG(player.Creature, item);
                        var damageType = WeaponDamage.GetDamageType(item);
                        editor.OnSelectEquipment()();
                        editor.SelectedItemTypeIndex = itemType;
                        ctx.Assert(editor.HasItemEquipped, "the catalog accepts the equipped Power Glove");
                        SelectMiddleCategory();
                        var nativeCategories = JArray.Parse(JsonDump(NuiGetBind(player.Creature, playerWindow.WindowToken, nameof(editor.PartCategorySelected))));
                        ctx.Assert(new[] { false, true, false }.SequenceEqual(nativeCategories.Values<bool>()),
                            "the actual native category selection highlights Middle and clears Top and Bottom");
                        ctx.Assert(labels.SequenceEqual(editor.PartOptions), "the real picker offers all six named cosmetic choices");
                        var nativeLabels = JArray.Parse(JsonDump(NuiGetBind(player.Creature, playerWindow.WindowToken, nameof(editor.PartOptions))));
                        ctx.Assert(labels.SequenceEqual(nativeLabels.Values<string>()), "the named options reach actual native NUI bindings");
                        ctx.AssertEqual(0, editor.SelectedPartIndex, "the training glove starts at the first catalog option");
                        for (var index = 0; index < labels.Length; index++)
                        {
                            if (index > 0) editor.OnNextPart()();
                            ctx.AssertEqual(index, editor.SelectedPartIndex, "public picker navigation selects the expected model");
                            ctx.AssertEqual(index + 1, GetItemAppearance(item, ItemAppearanceType.WeaponModel, (int)AppearanceWeapon.Middle), labels[index] + " model");
                            ctx.AssertEqual(1, GetItemAppearance(item, ItemAppearanceType.WeaponColor, (int)AppearanceWeapon.Middle), labels[index] + " color family");
                            ctx.AssertEqual(main, GetItemInSlot(InventorySlot.RightHand, player.Creature), labels[index] + " preserves the one real equipped item");
                            ctx.AssertEqual(off, GetItemInSlot(InventorySlot.LeftHand, player.Creature), labels[index] + " retains its generated companion");
                            ctx.AssertEqual(index + 1, GetItemAppearance(off, ItemAppearanceType.WeaponModel, (int)AppearanceWeapon.Middle), labels[index] + " updates the other hand from the one real item");
                            ctx.AssertEqual(1, GetItemAppearance(off, ItemAppearanceType.WeaponColor, (int)AppearanceWeapon.Middle), labels[index] + " mirrors the color family");
                            ctx.AssertEqual(BaseItem.PowerGlove, GetBaseItemType(item), labels[index] + " remains the same weapon class");
                            ctx.AssertEqual(weight, GetWeight(item), labels[index] + " preserves weapon weight");
                            ctx.AssertEqual(damage, WeaponDamage.GetEffectiveDMG(player.Creature, item), labels[index] + " preserves weapon damage");
                            ctx.AssertEqual(damageType, WeaponDamage.GetDamageType(item), labels[index] + " preserves damage type");
                            foreach (var outer in new[] { AppearanceWeapon.Top, AppearanceWeapon.Bottom })
                            {
                                ctx.AssertEqual(1, GetItemAppearance(item, ItemAppearanceType.WeaponModel, (int)outer), "the present blank outer model is retained");
                                ctx.AssertEqual(1, GetItemAppearance(item, ItemAppearanceType.WeaponColor, (int)outer), "the blank outer model keeps its valid color family");
                            }
                        }
                        Capture(slot.ToString());
                        editor.OnPreviousPart()();
                        ctx.AssertEqual(5, GetItemAppearance(item, ItemAppearanceType.WeaponModel, (int)AppearanceWeapon.Middle), "the invisible option can return to a visible Power Fist through public navigation");
                        ctx.AssertEqual(5, GetItemAppearance(off, ItemAppearanceType.WeaponModel, (int)AppearanceWeapon.Middle), "returning to a visible appearance restores both hands");
                        editor.OnNextPart()();
                        PowerGlovePairEngineTests.AssertOwnedItems(ctx, player.Creature, main, 1);
                        for (var index = 0; index < labels.Length - 1; index++)
                            editor.OnPreviousPart()();
                    });
                }
                var directory = ApplicationSettings.Get().EngineTestResultsDirectory;
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "power-glove-appearance-native-states.json"), snapshots.ToString(Formatting.Indented));
                ctx.SetResultDetail("All six named choices, including Bare Fists (Invisible), applied through the production editor to one real glove and its generated companion. Original item identity, blank outer models, weight and damage are preserved. Actual native NUI window and bindings exported for isolated offline client rendering; no mouse-event transport was simulated.");
            }
            finally
            {
                Gui.ClosePlayerWindow(player.Creature, GuiWindowType.AppearanceEditor);
                await ctx.WaitFrameAsync();
                windows.Remove(player.Id);
            }

            void Capture(string name)
            {
                var binds = new JObject();
                foreach (var watched in new[] { false, true })
                {
                    for (var index = 0; ; index++)
                    {
                        var bind = NuiGetNthBind(player.Creature, playerWindow.WindowToken, watched, index);
                        if (string.IsNullOrEmpty(bind)) break;
                        binds[bind] = JToken.Parse(JsonDump(NuiGetBind(player.Creature, playerWindow.WindowToken, bind)));
                    }
                }
                var baseType = typeof(AppearanceEditorViewModel).BaseType!;
                var layouts = (Dictionary<string, string>)baseType.GetField("_groupLayouts", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
                var root = (string)baseType.GetField("_rootPartial", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
                var window = JObject.Parse(JsonDump(template.Window));
                window["root"] = JToken.Parse(GuiLayoutComposer.Compose(template.PartialViewLayouts[root], layouts));
                ctx.Assert(window.SelectTokens("$..id").Any(id => id.Value<string>() == "ae_part_select"), "the exported production root contains the actual picker list");
                snapshots.Add(new JObject
                {
                    ["name"] = name,
                    ["category_selection"] = "Production OnSelectPartCategory action with synthesized native click context; no client mouse transport simulated.",
                    ["window"] = window,
                    ["binds"] = binds
                });
            }

            void SelectMiddleCategory()
            {
                var nativeEvent = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetModule().m_cCurrentNuiEvent;
                var previous = (nativeEvent.m_player, nativeEvent.m_token, nativeEvent.m_event,
                    nativeEvent.m_element, nativeEvent.m_array_index);
                try
                {
                    // Supply only this synchronous public handler's native event context.
                    // This exercises its selection and hydration behavior, not client mouse transport.
                    nativeEvent.m_player = player.Creature;
                    nativeEvent.m_token = playerWindow.WindowToken;
                    nativeEvent.m_event = "click";
                    nativeEvent.m_element = "ae_part_category";
                    nativeEvent.m_array_index = 1;
                    ctx.AssertEqual(1, NuiGetEventArrayIndex(), "the production handler receives Middle's real array index");
                    editor.OnSelectPartCategory()();
                    ctx.AssertEqual(1, editor.SelectedPartCategoryIndex, "the public category action selects Middle");
                }
                finally
                {
                    (nativeEvent.m_player, nativeEvent.m_token, nativeEvent.m_event,
                        nativeEvent.m_element, nativeEvent.m_array_index) = previous;
                }
            }
        }
    }
}
