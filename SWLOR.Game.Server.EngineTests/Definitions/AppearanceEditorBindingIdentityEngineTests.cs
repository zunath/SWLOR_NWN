using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.Preview;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Records every binding the appearance editor publishes (name, order and the JSON the client receives) while the
    /// editor opens and through a fixed run of colour operations, and compares the transcript with the one captured
    /// before the per-channel colour bindings stopped being declared properties.
    /// </summary>
    public static class AppearanceEditorBindingIdentityEngineTests
    {
        [EngineTest("Appearance editor publishes the same bindings and values as before the split", Category = "AppearanceEditor", TimeoutSeconds = 60f)]
        public static Task BindingTranscriptMatchesFixture(EngineTestContext ctx) =>
            RunAsync(ctx, "AppearanceEditor/BindingIdentity.txt", "/nwn/home/appearance-binding-identity.txt", ColorSteps);

        [EngineTest("Appearance editor publishes the same bindings across tab switches as before the split", Category = "AppearanceEditor", TimeoutSeconds = 60f)]
        public static Task TabSwitchTranscriptMatchesFixture(EngineTestContext ctx) =>
            RunAsync(ctx, "AppearanceEditor/BindingIdentityTabs.txt", "/nwn/home/appearance-binding-identity-tabs.txt", TabSteps);

        [EngineTest("Appearance editor publishes the same bindings through part lists, the browser and items as before the split", Category = "AppearanceEditor", TimeoutSeconds = 90f)]
        public static Task PartListTranscriptMatchesFixture(EngineTestContext ctx) =>
            RunAsync(ctx, "AppearanceEditor/BindingIdentityParts.txt", "/nwn/home/appearance-binding-identity-parts.txt", PartSteps,
                PreparePartItemsAsync, maskComboLists: true);

        [EngineTest("Appearance editor publishes the same bindings through the wardrobe as before the split", Category = "AppearanceEditor", TimeoutSeconds = 90f)]
        public static Task OutfitTranscriptMatchesFixture(EngineTestContext ctx) =>
            RunAsync(ctx, "AppearanceEditor/BindingIdentityOutfits.txt", "/nwn/home/appearance-binding-identity-outfits.txt", OutfitSteps,
                null, playerViewer: true, maskComboLists: true);

        private static async Task PreparePartItemsAsync(EngineTestContext ctx, uint civilian)
        {
            var cases = new[]
            {
                (Slot: InventorySlot.Head, Resref: "advent_helmet", Initial: 1),
                (Slot: InventorySlot.Cloak, Resref: "advent_cloak", Initial: 1),
                (Slot: InventorySlot.LeftHand, Resref: "byysk_shield002", Initial: 11),
                (Slot: InventorySlot.RightHand, Resref: "nw_wswls001", Initial: 1)
            };
            foreach (var test in cases)
            {
                var item = await ctx.EquipItemAsync(civilian, test.Resref, test.Slot);
                await AppearanceEditorEngineTests.RunAssignedAsync(ctx, civilian, () =>
                {
                    SetItemCursedFlag(item, false);
                    SetPlotFlag(item, false);
                    if (test.Slot == InventorySlot.RightHand)
                    {
                        for (var index = 0; index < 3; ++index)
                        {
                            EquippedItemAppearance.Set(item, ItemAppearanceType.WeaponModel, index, 1);
                            EquippedItemAppearance.Set(item, ItemAppearanceType.WeaponColor, index, 1);
                        }
                    }
                    else
                    {
                        EquippedItemAppearance.Set(item, ItemAppearanceType.SimpleModel, -1, test.Initial);
                    }
                });
            }
            await ctx.DelaySecondsAsync(0.5f);
        }

        private static void PartSteps(AppearanceEditorViewModel editor, Action<string, Action> step)
        {
            step("head-slot", () => editor.OnClickMapSlot(AppearanceEditorSlot.Head)());
            step("next-part", () => editor.OnClickNextPart()());
            step("previous-part", () => editor.OnClickPreviousPart()());
            step("preview-view", () => editor.OnClickPreviewView()());
            step("list-next", () => editor.OnNextPart()());
            step("list-previous", () => editor.OnPreviousPart()());
            step("torso-slot", () => editor.OnClickMapSlot(AppearanceEditorSlot.Chest)());
            step("torso-next", () => editor.OnClickNextPart()());
            step("right-hand-slot", () => editor.OnClickMapSlot(AppearanceEditorSlot.RightHand)());
            step("right-hand-previous", () => editor.OnClickPreviousPart()());
            step("body-swatch", () => editor.OnClickBodySwatch(1)());
            step("select-equipment", () => editor.OnSelectEquipment()());
            step("pelvis-slot", () => editor.OnClickMapSlot(AppearanceEditorSlot.Pelvis)());
            step("pelvis-next", () => editor.OnClickNextPart()());
            step("belt-adjust", () => editor.OnClickAdjustArmorPart(AppearanceArmor.Belt, 1)());
            step("pelvis-previous", () => editor.OnClickPreviousPart()());
            step("part-swatch", () => editor.OnClickPartSwatch(AppearanceArmorColor.Metal1)());
            step("helmet-slot", () => editor.OnClickMapSlot(AppearanceEditorSlot.Head)());
            step("helmet-next", () => editor.OnClickNextPart()());
            step("helmet-list-next", () => editor.OnNextPart()());
            step("helmet-base-swatch", () => editor.OnClickBaseSwatch(AppearanceArmorColor.Cloth2)());
            step("cloak-slot", () => editor.OnClickMapSlot(AppearanceEditorSlot.Cloak)());
            step("cloak-next", () => editor.OnClickNextPart()());
            step("main-hand", () => editor.SelectedItemTypeIndex = 3);
            step("main-hand-next", () => editor.OnClickNextPart()());
            step("main-hand-list-previous", () => editor.OnPreviousPart()());
            step("off-hand", () => editor.SelectedItemTypeIndex = 4);
            step("off-hand-next", () => editor.OnClickNextPart()());
            step("equipment-type-armor", () => editor.EquipmentTypeValue = AppearanceEditorViewModel.ArmorEquipmentValue);
            step("armor-slot", () => editor.OnClickMapSlot(AppearanceEditorSlot.Chest)());
            step("increase-height", () => editor.OnIncreaseAppearanceScale()());
            step("select-settings", () => editor.OnSelectSettings()());
            step("select-appearance", () => editor.OnSelectAppearance()());
        }

        private static void OutfitSteps(AppearanceEditorViewModel editor, Action<string, Action> step)
        {
            step("outfits-tab", () => editor.OutfitsTabToggleValue = 0);
            step("new-outfit", () => editor.OnClickNewOutfit()());
            step("store-outfit", () => editor.OnClickStoreOutfit()());
            step("new-outfit-2", () => editor.OnClickNewOutfit()());
            step("rename", () =>
            {
                editor.OutfitName = "Dusty Farmer";
                editor.OnClickRenameOutfit()();
            });
            step("wear-outfit", () => editor.OnClickWearOutfit()());
            step("quick-wear", () => editor.OnClickQuickWear()());
            step("undo-outfit", () => editor.OnClickUndoOutfit()());
            step("select-equipment", () => editor.OnSelectEquipment()());
            step("select-outfits", () => editor.OutfitsTabToggleValue = 0);
            step("select-settings", () => editor.OnSelectSettings()());
        }

        private static void TabSteps(AppearanceEditorViewModel editor, Action<string, Action> step)
        {
            step("select-slot", () => editor.OnClickMapSlot(AppearanceEditorSlot.Chest)());
            step("select-equipment", () => editor.OnSelectEquipment()());
            step("select-part", () => editor.OnClickMapSlot(AppearanceEditorSlot.Chest)());
            step("main-hand", () => editor.SelectedItemTypeIndex = 3);
            step("armor", () => editor.SelectedItemTypeIndex = 0);
            step("select-settings", () => editor.OnSelectSettings()());
            step("select-appearance", () => editor.OnSelectAppearance()());
            step("select-equipment-again", () => editor.OnSelectEquipment()());
        }

        private static void ColorSteps(AppearanceEditorViewModel editor, Action<string, Action> Step)
        {
            Step("select-equipment", () => editor.OnSelectEquipment()());
            Step("global-cloth1", () => editor.OnClickColorTarget(AppearanceColorTarget.Global, AppearanceArmorColor.Cloth1)());
            Step("palette-35", () => editor.OnClickColorPalette(35)());
            Step("chest-leather2", () => editor.OnClickColorTarget(AppearanceColorTarget.Chest, AppearanceArmorColor.Leather2)());
            Step("palette-77", () => editor.OnClickColorPalette(77)());
            Step("global-leather2", () => editor.OnClickColorTarget(AppearanceColorTarget.Global, AppearanceArmorColor.Leather2)());
            Step("custom-rgb", () => AppearanceEditorEngineTests.ApplyWatchedValue(editor, nameof(editor.SelectedTintColor), new GuiColor(17, 83, 209)));
            Step("custom-red-text", () =>
            {
                AppearanceEditorEngineTests.ApplyWatchedValue(editor, nameof(editor.CustomTintRed), "40");
                AppearanceEditorEngineTests.InvokePrivate(editor, "CommitCustomTintComponents");
            });
            Step("part-custom-rgb", () =>
            {
                editor.OnClickColorTarget(AppearanceColorTarget.LeftForearm, AppearanceArmorColor.Leather2)();
                AppearanceEditorEngineTests.ApplyWatchedValue(editor, nameof(editor.SelectedTintColor), new GuiColor(200, 17, 91));
            });
            Step("global-preset-clears-custom", () =>
            {
                editor.OnClickColorTarget(AppearanceColorTarget.Global, AppearanceArmorColor.Leather2)();
                editor.OnClickColorPalette(66)();
            });
            Step("robe-metal2", () => editor.OnClickColorTarget(AppearanceColorTarget.Robe, AppearanceArmorColor.Metal2)());
            Step("palette-12", () => editor.OnClickColorPalette(12)());
            Step("clear-color", () => editor.OnClickClearColor(AppearanceColorTarget.Robe, AppearanceArmorColor.Metal2)());
            Step("revert-chest", () => AppearanceEditorEngineTests.InvokePrivate(editor, "ResetArmorColorToInheritance",
                AppearanceColorTarget.Chest, AppearanceArmorColor.Leather2));
            Step("copy-right", () => editor.OnClickCopyToRight()());
            Step("select-appearance", () => editor.OnSelectAppearance()());
        }

        private static async Task RunAsync(EngineTestContext ctx, string Resource, string CapturePath,
            Action<AppearanceEditorViewModel, Action<string, Action>> script,
            Func<EngineTestContext, uint, Task> prepare = null, bool playerViewer = false, bool maskComboLists = false)
        {
            // The wardrobe belongs to the player editing themselves, so that run uses the headless player fixture (a native PC
            // with a registered client and a saved record); the editor is bound with it as the viewer and no window token,
            // so nothing is sent to a client.
            using var fixture = playerViewer ? await PlayerAbilityFixture.CreateAsync(ctx) : null;
            var civilian = fixture?.Creature ?? await AppearanceEditorEngineTests.SpawnCivilianAsync(ctx);
            if (fixture != null)
            {
                await AppearanceEditorEngineTests.RunAssignedAsync(ctx, civilian, () =>
                {
                    // As SpawnCivilianAsync does: only this disposable creature's outfit is made editable.
                    var outfit = GetItemInSlot(InventorySlot.Chest, civilian);
                    SetItemCursedFlag(outfit, false);
                    SetPlotFlag(outfit, false);
                });
            }
            if (prepare != null)
                await prepare(ctx, civilian);
            var transcript = new StringBuilder();
            // The unmasked transcript, written beside a mismatch or a missing fixture, for comparing two builds on one server home.
            var fullTranscript = new StringBuilder();
            await AppearanceEditorEngineTests.RunAssignedAsync(ctx, civilian, () =>
            {
                var editor = AppearanceEditorEngineTests.BindWithoutClient(civilian, playerViewer ? civilian : OBJECT_INVALID);
                var converter = new GuiPropertyConverter();
                var cache = (IDictionary)editor.GetType().BaseType!
                    .GetField("_propertyValues", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
                var phase = "init";
                var sequence = 0;

                string Json(object value) => value == null
                    ? "null"
                    : JToken.Parse(JsonDump(converter.ToJson(value))).ToString(Newtonsoft.Json.Formatting.None);

                // The armor part option lists keep only the styles whose model the server's hak set holds, so a masked
                // transcript names them without their entries (their selections and every other binding stay).
                string Line(string name, object value, bool masked) => masked && name != nameof(AppearanceEditorViewModel.QuickOutfitOptions) && value is GuiBindingList<GuiComboEntry> ? "\"<options>\"" : Json(value);

                object CachedValue(object key)
                {
                    var detail = cache[key]!;
                    return detail.GetType().GetProperty("Value")!.GetValue(detail);
                }

                void Record(object sender, PropertyChangedEventArgs args)
                {
                    var value = cache.Contains(args.PropertyName) ? CachedValue(args.PropertyName) : null;
                    sequence++;
                    transcript.Append(phase).Append(' ').Append(sequence).Append(' ').Append(args.PropertyName)
                        .Append(' ').Append(Line(args.PropertyName, value, maskComboLists)).Append('\n');
                    fullTranscript.Append(phase).Append(' ').Append(sequence).Append(' ').Append(args.PropertyName)
                        .Append(' ').Append(Json(value)).Append('\n');
                }

                // Binding already published the opening state; list it in the order the cache holds it.
                foreach (DictionaryEntry entry in cache)
                {
                    transcript.Append("init-state ").Append(entry.Key).Append(' ').Append(Line((string)entry.Key, CachedValue(entry.Key), maskComboLists)).Append('\n');
                    fullTranscript.Append("init-state ").Append(entry.Key).Append(' ').Append(Json(CachedValue(entry.Key))).Append('\n');
                }

                editor.PropertyChanged += Record;
                void Step(string name, Action action)
                {
                    phase = name;
                    sequence = 0;
                    action();
                }

                script(editor, Step);
                editor.PropertyChanged -= Record;
            });

            var actual = transcript.ToString();
            if (Directory.Exists("/nwn/home"))
                File.WriteAllText(CapturePath.Replace(".txt", "-full.txt"), fullTranscript.ToString());
            using var stream = typeof(AppearanceEditorBindingIdentityEngineTests).Assembly.GetManifestResourceStream(Resource);
            if (stream == null)
            {
                if (Directory.Exists("/nwn/home"))
                    File.WriteAllText(CapturePath, actual);
                ctx.Assert(false, $"No fixture is embedded; the transcript ({actual.Length} chars) was written to {CapturePath}.");
                return;
            }
            var expected = new StreamReader(stream, Encoding.UTF8).ReadToEnd().Replace("\r\n", "\n");
            if (expected != actual)
            {
                if (Directory.Exists("/nwn/home"))
                    File.WriteAllText(CapturePath, actual);
                var expectedLines = expected.Split('\n');
                var actualLines = actual.Split('\n');
                var index = 0;
                while (index < expectedLines.Length && index < actualLines.Length && expectedLines[index] == actualLines[index])
                    index++;
                ctx.Assert(false, $"Binding transcript differs at line {index + 1}: expected '{(index < expectedLines.Length ? expectedLines[index] : "<end>")}' " +
                                  $"but got '{(index < actualLines.Length ? actualLines[index] : "<end>")}'.");
                return;
            }
            ctx.SetResultDetail($"{actual.Split('\n').Length - 1} lines, identical to the captured transcript.");
        }
    }
}
