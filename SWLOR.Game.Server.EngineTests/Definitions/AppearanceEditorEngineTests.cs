using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using ItemPlugin = SWLOR.NWN.API.NWNX.ItemPlugin;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class AppearanceEditorEngineTests
    {
        private sealed record ArmorSnapshot(uint Item, int[] Models, int[] Colors, int[] Markers, int[] Projections);

        [EngineTest("Appearance editor enables every native armor dye target and inheritance preview", Category = "AppearanceEditor", TimeoutSeconds = 30f)]
        public static async Task EveryArmorColorTargetRetainsPickerAndNativePreview(EngineTestContext ctx)
        {
            var civilian = await SpawnCivilianAsync(ctx);
            await RunAssignedAsync(ctx, civilian, () =>
            {
                var outfit = GetItemInSlot(InventorySlot.Chest, civilian);
                // Chest70 is the reported case: its available native armor dyes must not
                // depend on the currently visible mesh exposing a generated material layer.
                ItemPlugin.SetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso, 70, false);
                var channels = new[]
                {
                    (AppearanceArmorColor.Leather1, TintMapLayerType.Leather1),
                    (AppearanceArmorColor.Leather2, TintMapLayerType.Leather2),
                    (AppearanceArmorColor.Cloth1, TintMapLayerType.Cloth1),
                    (AppearanceArmorColor.Cloth2, TintMapLayerType.Cloth2),
                    (AppearanceArmorColor.Metal1, TintMapLayerType.Metal1),
                    (AppearanceArmorColor.Metal2, TintMapLayerType.Metal2)
                };
                foreach (var (channel, _) in channels)
                {
                    ItemPlugin.SetItemAppearance(outfit, ItemAppearanceType.ArmorColor, (int)channel, 35 + (int)channel, false);
                    for (var part = 0; part < (int)AppearanceArmor.Num; part++)
                    {
                        var mode = (part + (int)channel) % 4;
                        var raw = mode switch { 0 => 255, 1 or 2 => 0, _ => 77 };
                        var armorPart = (AppearanceArmor)part;
                        ItemPlugin.SetItemAppearance(outfit, ItemAppearanceType.ArmorColor,
                            ArmorColorIndexCalculator.CalculatePerPart(armorPart, channel), raw, false);
                        var marker = ArmorColorIndexCalculator.GetPerPartOverrideVariableName(armorPart, channel);
                        if (mode == 2) SetLocalInt(outfit, marker, 1);
                        else DeleteLocalInt(outfit, marker);
                    }
                }
                var snapshot = ReadArmor(civilian);
                var editor = BindWithoutClient(civilian);
                editor.OnSelectEquipment()();
                var panel = AppearanceEditorDefinition.BuildEditorPanel(AppearanceEditorViewModel.EditorArmorPartial);
                AssertGlobalSwatchImagesJson(ctx, JObject.Parse(JsonDump(panel.ToJson())), "Global native swatch images");
                var swatchJson = JObject.Parse(JsonDump(panel.ToJson())).Descendants().OfType<JObject>()
                    .Where(node => node["id"]?.Value<string>()?.StartsWith("ae_color_") == true).ToArray();
                ctx.AssertEqual(120, swatchJson.Length, "All global and part swatches have native glow bindings");
                foreach (var swatch in swatchJson)
                {
                    var expected = swatch["id"].Value<string>()["ae_color_".Length..^"Region".Length] + "Selected";
                    ctx.AssertEqual(expected, swatch["encouraged"]?["bind"]?.Value<string>(), "Glow follows this exact material target");
                }
                AssertActiveColorSwatch(ctx, editor, "GlobalLeather1Selected");
                foreach (var (channel, layer) in channels)
                {
                    var regionName = "Global" + channel + "Region";
                    var image = WidgetTree(panel).Single(widget => widget.Id == "ae_color_" + regionName);
                    ctx.Assert(image is GuiImage<AppearanceEditorViewModel>, $"{channel}: global swatch is an ordinary image.");
                    ctx.Assert(image.Events.TryGetValue("mousedown", out var mouseDown), $"{channel}: image handles mouse-down.");
                    ctx.AssertEqual(nameof(AppearanceEditorViewModel.OnMouseDownGlobalColor), mouseDown.Method.Name,
                        $"{channel}: image routes through the left-button filter");
                    ctx.AssertEqual(1, mouseDown.Arguments.Count, $"{channel}: image captures one dye channel");
                    ctx.AssertEqual(channel, (AppearanceArmorColor)mouseDown.Arguments[0].Value, $"{channel}: captured dye channel");
                    ctx.Assert(!image.Events.ContainsKey("click"), $"{channel}: image must not require a button-only click event.");
                    // The real mouse payload is client-owned. Exercise the unchanged
                    // target-selection action delegated to by the mouse-down filter.
                    editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Global, channel)();
                    AssertActiveColorSwatch(ctx, editor, "Global" + channel + "Selected");
                    AssertTintInput(ctx, editor, TintMapPaletteColors.GetColor(layer, 35 + (int)channel),
                        $"{channel}: global image selection previews its own native dye");
                    AssertPaletteRegion(ctx, (GuiRectangle)editor.GetType().GetProperty(regionName).GetValue(editor),
                        35 + (int)channel, $"{channel}: global image region follows its native dye");
                }
                foreach (var (part, target) in ArmorColorTargets())
                foreach (var (channel, layer) in channels)
                {
                    editor.OnClickColorTarget(target, channel)();
                    AssertActiveColorSwatch(ctx, editor, target.ToString() + channel + "Selected");
                    ctx.Assert(editor.IsCustomTintAvailable,
                        $"{part}/{channel} must retain its native picker even without a generated tint selection.");
                    var mode = ((int)part + (int)channel) % 4;
                    var expectedId = mode switch { 2 => 0, 3 => 77, _ => 35 + (int)channel };
                    AssertPickerColor(ctx, editor, TintMapPaletteColors.GetColor(layer, expectedId), $"{part}/{channel} native preview");
                }
                AssertArmorUnchanged(ctx, snapshot, civilian, "all114 part dye target selections");
                editor.OnSelectAppearance()();
                editor.OnSelectEquipment()();
                AssertActiveColorSwatch(ctx, editor, "GlobalLeather1Selected");
                editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Chest, AppearanceArmorColor.Cloth1)();
                InvokePrivate(editor, "ResetArmorColorToInheritance", AppearanceEditorViewModel.ColorTarget.Chest, AppearanceArmorColor.Cloth1);
                AssertActiveColorSwatch(ctx, editor, "ChestCloth1Selected");
            });
            ctx.SetResultDetail("Six global images serialized dynamic regions and channel-specific mouse-down bindings; delegated target actions selected their native dyes. All19×6 native armor targets retained a picker, including Chest70. Seeded raw255/raw0/explicit0/explicit77 cases displayed inherited or explicit colors without armor writes. Headless coverage excludes mouse payload delivery and client rendering.");
        }

        [EngineTest("Appearance palette buttons apply only their selected native color channel", Category = "AppearanceEditor", TimeoutSeconds = 30f)]
        public static async Task AppearancePaletteButtonsApplyNativeChannels(EngineTestContext ctx)
        {
            var civilian = await SpawnCivilianAsync(ctx);
            await RunAssignedAsync(ctx, civilian, () =>
            {
                var editor = BindWithoutClient(civilian);
                var panel = AppearanceEditorDefinition.BuildEditorPanel(AppearanceEditorViewModel.EditorMainPartial);
                var palette = WidgetTree(panel).Single(widget => widget.Id == "ae_color_palette");
                var buttons = WidgetTree(palette).OfType<GuiButton<AppearanceEditorViewModel>>().ToArray();
                var serialized = JObject.Parse(JsonDump(palette.ToJson()));
                AssertAppearancePaletteJson(ctx, serialized, "Appearance native button palette");
                var nativeButtons = serialized.Descendants().OfType<JObject>()
                    .Where(node => node["type"]?.Value<string>() == "button").ToArray();
                ctx.AssertEqual(176, buttons.Length, "Every appearance preset must have an actual button");
                for (var index = 0; index < buttons.Length; ++index)
                {
                    var button = buttons[index];
                    ctx.AssertEqual(button.Id, nativeButtons[index]["id"]?.Value<string>(), "Serialized native button preserves its dispatch ID");
                    ctx.Assert(button.Events.TryGetValue("click", out var click), $"Preset {index} must register a click action.");
                    ctx.AssertEqual(nameof(AppearanceEditorViewModel.OnClickColorPalette), click.Method.Name,
                        $"Preset {index} uses the existing shared palette action");
                    ctx.AssertEqual(typeof(AppearanceEditorViewModel), click.Method.DeclaringType, "Palette action belongs to the production editor");
                    ctx.AssertEqual(1, click.Arguments.Count, "Palette action captures one preset index");
                    ctx.AssertEqual(index, (int)click.Arguments[0].Value, "Each button captures its own native palette index");
                    ctx.AssertEqual(1, button.Events.Count, "Preset buttons need only their click event, without coordinate or reset handlers");
                }

                foreach (var (channel, layer, texture) in new[]
                {
                    (ColorChannel.Skin, TintMapLayerType.Skin, "gui_pal_skin"),
                    (ColorChannel.Hair, TintMapLayerType.Hair, "gui_pal_hair01"),
                    (ColorChannel.Tattoo1, TintMapLayerType.Tattoo1, "gui_pal_tattoo"),
                    (ColorChannel.Tattoo2, TintMapLayerType.Tattoo2, "gui_pal_tattoo")
                })
                {
                    // Exercise the selected-category setter; this token-zero fixture does
                    // not synthesize the client's list-array event or mouse coordinates.
                    editor.SelectedColorCategoryIndex = (int)channel;
                    InvokePrivate(editor, "LoadTintMapEditor");
                    ctx.AssertEqual(texture, editor.ColorSheetResref, $"{channel}: dynamic palette texture");
                    foreach (var paletteId in new[] { 0, 87, 175 })
                    {
                        var before = Enum.GetValues<ColorChannel>().ToDictionary(value => value, value => GetColor(civilian, value));
                        var armor = ReadArmor(civilian);
                        var click = buttons[paletteId].Events["click"];
                        // Dispatch the action captured by the real definition's button,
                        // rather than a parallel test-only palette implementation.
                        ((Action)click.Method.Invoke(editor, click.Arguments.Select(argument => argument.Value).ToArray()))();
                        foreach (var other in Enum.GetValues<ColorChannel>())
                            ctx.AssertEqual(other == channel ? paletteId : before[other], GetColor(civilian, other),
                                $"{channel}/{paletteId}: native {other} changes only when selected");
                        AssertTintInput(ctx, editor, TintMapPaletteColors.GetColor(layer, paletteId),
                            $"{channel}/{paletteId}: preset synchronizes picker and RGB fields");
                        AssertArmorUnchanged(ctx, armor, civilian, $"{channel}/{paletteId}: appearance preset retains all equipment colors and models");
                    }
                }

                var unchangedColors = Enum.GetValues<ColorChannel>().Select(channel => GetColor(civilian, channel)).ToArray();
                var unchangedArmor = ReadArmor(civilian);
                var input = new TintMapColor(editor.SelectedTintColor.R, editor.SelectedTintColor.G, editor.SelectedTintColor.B);
                using var publications = new BindingPublications(editor);
                foreach (var invalid in new[] { -1, 176 })
                {
                    publications.Clear();
                    editor.OnClickColorPalette(invalid)();
                    ctx.Assert(unchangedColors.SequenceEqual(Enum.GetValues<ColorChannel>().Select(channel => GetColor(civilian, channel))),
                        $"Invalid preset {invalid} must retain all native creature colors.");
                    AssertTintInput(ctx, editor, input, $"Invalid preset {invalid} retains RGB input");
                    AssertNoTintControlPublications(ctx, publications, $"Invalid preset {invalid} cannot echo color controls");
                    AssertArmorUnchanged(ctx, unchangedArmor, civilian, $"Invalid preset {invalid} retains equipment state");
                }
            });
            ctx.SetResultDetail("All176 native button IDs, atlas regions and captured shared-action indices validated. Twelve button actions exercised skin, hair and both tattoos at presets0,87,175 with exact native IDs, synchronized RGB and no other-channel/equipment edits; invalid−1/176 were no-ops. Headless coverage excludes client mouse delivery and rendering; fixture has no helmet or cloak.");
        }

        private static IEnumerable<IGuiWidget> WidgetTree(IGuiWidget widget)
        {
            yield return widget;
            foreach (var child in widget.Elements)
                foreach (var descendant in WidgetTree(child))
                    yield return descendant;
        }

        private static void AssertActiveColorSwatch(EngineTestContext ctx, AppearanceEditorViewModel editor, string expected)
        {
            var selectionProperties = typeof(AppearanceEditorViewModel).GetProperties()
                .Where(property => property.PropertyType == typeof(GuiRectangle) && property.Name.EndsWith("Region"))
                .Select(property => typeof(AppearanceEditorViewModel).GetProperty(property.Name[..^"Region".Length] + "Selected"))
                .ToArray();
            ctx.AssertEqual(120, selectionProperties.Length, "Every material swatch exposes a glow binding");
            var active = selectionProperties.Where(property => (bool)property.GetValue(editor)).Select(property => property.Name).ToArray();
            ctx.AssertEqual(1, active.Length, "Exactly one material target is highlighted");
            ctx.AssertEqual(expected, active.Single(), "The highlight moves to the selected target and channel");
        }

        [EngineTest("Appearance editor watched RGB input preserves drafts and requested values", Category = "AppearanceEditor", TimeoutSeconds = 30f)]
        public static async Task WatchedColorCorrectionAndDraftCommit(EngineTestContext ctx)
        {
            var civilian = await SpawnCivilianAsync(ctx);
            await RunAssignedAsync(ctx, civilian, () =>
            {
                var outfit = GetItemInSlot(InventorySlot.Chest, civilian);
                SeedInheritance(outfit);
                var editor = BindWithoutClient(civilian);
                using var publications = new BindingPublications(editor);

                ctx.Assert(editor.IsAppearanceSelected && editor.SelectedColorCategoryIndex == 0,
                    "The initial watched RGB edit must target creature skin.");
                var black = new TintMapColor(0, 0, 0);
                var blackSkinId = TintMapPaletteColors.GetClosestColorId(TintMapLayerType.Skin, black);
                var originalSkinId = GetColor(civilian, ColorChannel.Skin);
                var beforeSkinDraft = ReadArmor(civilian);
                ctx.AssertEqual(57, blackSkinId, "Native palette row for the reported black skin input");
                ctx.Assert(originalSkinId != blackSkinId, "The black skin edit must change the fixture's authored skin.");
                foreach (var property in new[]
                {
                    nameof(editor.CustomTintRed), nameof(editor.CustomTintGreen), nameof(editor.CustomTintBlue)
                })
                {
                    publications.Clear();
                    ApplyWatchedValue(editor, property, "0");
                    ctx.AssertEqual("0", editor.GetType().GetProperty(property).GetValue(editor),
                        "Each watched skin RGB field must retain the zero draft.");
                    AssertNoTintControlPublications(ctx, publications, "Valid black skin draft");
                    ctx.AssertEqual(originalSkinId, GetColor(civilian, ColorChannel.Skin),
                        "Sequential skin RGB fields must not change the native color before commit");
                    AssertArmorUnchanged(ctx, beforeSkinDraft, civilian, "uncommitted skin RGB draft");
                }
                ctx.AssertEqual("0", editor.CustomTintRed, "Black skin red draft");
                ctx.AssertEqual("0", editor.CustomTintGreen, "Black skin green draft");
                ctx.AssertEqual("0", editor.CustomTintBlue, "Black skin blue draft");
                publications.Clear();
                InvokePrivate(editor, "CommitCustomTintComponents");
                ctx.AssertEqual(blackSkinId, GetColor(civilian, ColorChannel.Skin),
                    "Committed zero RGB must select native black skin, not the old light color");
                AssertPublishedPicker(ctx, publications, editor, black, "Committed black skin RGB");
                AssertTintInput(ctx, editor, black, "Committed black skin input");
                AssertNoRgbFieldPublications(ctx, publications, "Black skin commit retains text buffers");
                AssertArmorUnchanged(ctx, beforeSkinDraft, civilian, "committed skin color leaves equipment unchanged");

                editor.OnSelectEquipment()();
                publications.Clear();

                var requested = new TintMapColor(253, 17, 91);
                var selected = TintMapPaletteColors.GetClosestColorId(TintMapLayerType.Leather1, requested);
                var canonical = TintMapPaletteColors.GetColor(TintMapLayerType.Leather1, selected);
                ctx.Assert(requested != canonical, "Picker input must require palette correction.");
                ApplyWatchedValue(editor, nameof(editor.SelectedTintColor),
                    new GuiColor(requested.Red, requested.Green, requested.Blue), () =>
                        ctx.Assert(!publications.Contains(nameof(editor.SelectedTintColor)),
                            "The production SkipNotify flag must suppress the watched picker during its setter."));
                AssertNativeColor(ctx, outfit, (int)AppearanceArmorColor.Leather1, selected, "Watched picker native dye");
                AssertTintInput(ctx, editor, requested, "Post-client requested RGB");
                AssertPublishedRgbFields(ctx, publications, editor, requested, "Picker publishes its exact RGB components");
                ctx.Assert(!publications.Contains(nameof(editor.SelectedTintColor)),
                    "The watched picker already holds the request and must not receive a redundant echo.");

                var beforeDraft = ReadArmor(civilian);
                publications.Clear();
                ApplyWatchedValue(editor, nameof(editor.CustomTintRed), "300");
                ctx.AssertEqual("255", editor.CustomTintRed, "An out-of-range component is corrected");
                ctx.AssertEqual("255", publications.Latest(ctx, nameof(editor.CustomTintRed), "Red correction").Value<string>(),
                    "Only the invalid field needs a correction publication.");
                ctx.Assert(!publications.Contains(nameof(editor.CustomTintGreen)) &&
                           !publications.Contains(nameof(editor.CustomTintBlue)) &&
                           !publications.Contains(nameof(editor.SelectedTintColor)),
                    "Correcting red must not echo the other RGB fields or picker.");
                AssertArmorUnchanged(ctx, beforeDraft, civilian, "Corrected draft before commit");
                publications.Clear();
                ApplyWatchedValue(editor, nameof(editor.CustomTintRed), string.Empty);
                ctx.AssertEqual(string.Empty, editor.CustomTintRed, "An empty component remains editable");
                InvokePrivate(editor, "CommitCustomTintComponents");
                AssertNoTintControlPublications(ctx, publications, "An empty draft cannot commit or echo controls");
                AssertArmorUnchanged(ctx, beforeDraft, civilian, "Empty RGB draft");
                foreach (var (property, value) in new[]
                {
                    (nameof(editor.CustomTintRed), "2"), (nameof(editor.CustomTintRed), "230"),
                    (nameof(editor.CustomTintGreen), "35"), (nameof(editor.CustomTintBlue), "170")
                })
                {
                    publications.Clear();
                    ApplyWatchedValue(editor, property, value);
                    ctx.AssertEqual(value, editor.GetType().GetProperty(property).GetValue(editor),
                        "The actively edited RGB field must retain its draft without a network echo.");
                    AssertNoTintControlPublications(ctx, publications, "Valid sequential RGB draft");
                    AssertArmorUnchanged(ctx, beforeDraft, civilian, "uncommitted RGB draft");
                }
                ctx.AssertEqual("230", editor.CustomTintRed, "Typing green/blue must preserve the complete red draft");
                ctx.AssertEqual("35", editor.CustomTintGreen, "Uncommitted green draft");
                ctx.AssertEqual("170", editor.CustomTintBlue, "Uncommitted blue draft");

                publications.Clear();
                // Run the same commit body as the debounce callback. The invalid viewer cannot
                // exercise its timer/window-open guard; do not pretend that a PC is connected.
                InvokePrivate(editor, "CommitCustomTintComponents");
                selected = TintMapPaletteColors.GetClosestColorId(TintMapLayerType.Leather1, new TintMapColor(230, 35, 170));
                AssertNativeColor(ctx, outfit, (int)AppearanceArmorColor.Leather1, selected, "Committed RGB native dye");
                AssertPublishedPicker(ctx, publications, editor, new TintMapColor(230, 35, 170), "Committed requested RGB");
                AssertTintInput(ctx, editor, new TintMapColor(230, 35, 170), "Committed RGB input");
                AssertNoRgbFieldPublications(ctx, publications, "RGB commit leaves unchanged text buffers alone");
                AssertInheritedAndExplicitParts(ctx, outfit);
            });
            ctx.SetResultDetail("Valid RGB drafts caused no color-control echoes;300 corrected only red to255 and empty text could not commit. Skin0/0/0 and equipment RGB commits published exact picker input while retaining text buffers and separately applying the nearest native preset. Incoming NuiGetBind and debounce scheduling/open-window checks are synthesized or excluded.");
        }

        [EngineTest("Appearance editor remembers exact RGB per target until an explicit or external color change", Category = "AppearanceEditor", TimeoutSeconds = 30f)]
        public static async Task ExactRgbInputSurvivesEditsNavigationAndResize(EngineTestContext ctx)
        {
            var civilian = await SpawnCivilianAsync(ctx);
            await RunAssignedAsync(ctx, civilian, () =>
            {
                var outfit = GetItemInSlot(InventorySlot.Chest, civilian);
                SeedInheritance(outfit);
                var editor = BindWithoutClient(civilian);
                using var publications = new BindingPublications(editor);
                editor.OnSelectEquipment()();
                ApplyWatchedValue(editor, nameof(editor.SelectedTintColor), new GuiColor(253, 17, 91));

                ApplyWatchedValue(editor, nameof(editor.CustomTintRed), "1");
                InvokePrivate(editor, "CommitCustomTintComponents");
                var requested = new TintMapColor(1, 17, 91);
                var preset = TintMapPaletteColors.GetClosestColorId(TintMapLayerType.Leather1, requested);
                AssertTintInput(ctx, editor, requested, "Red1 committed independently");
                AssertNativeColor(ctx, outfit, (int)AppearanceArmorColor.Leather1, preset, "Red1 native nearest preset");

                ApplyWatchedValue(editor, nameof(editor.CustomTintGreen), "77");
                InvokePrivate(editor, "CommitCustomTintComponents");
                requested = new TintMapColor(1, 77, 91);
                preset = TintMapPaletteColors.GetClosestColorId(TintMapLayerType.Leather1, requested);
                ctx.Assert(requested != TintMapPaletteColors.GetColor(TintMapLayerType.Leather1, preset),
                    "The fixture must distinguish entered RGB from the rendered preset.");
                AssertTintInput(ctx, editor, requested, "Green edit retains Red1 and Blue91");
                AssertNativeColor(ctx, outfit, (int)AppearanceArmorColor.Leather1, preset, "Second component native nearest preset");
                AssertInheritedAndExplicitParts(ctx, outfit);
                publications.Clear();
                ApplyWatchedValue(editor, nameof(editor.CustomTintRed), "001");
                AssertNoTintControlPublications(ctx, publications, "Leading-zero draft stays local");
                InvokePrivate(editor, "CommitCustomTintComponents");
                ctx.AssertEqual("001", editor.CustomTintRed, "Commit must not normalize valid text under the caret");
                ctx.AssertEqual("77", editor.CustomTintGreen, "Leading-zero commit retains green");
                ctx.AssertEqual("91", editor.CustomTintBlue, "Leading-zero commit retains blue");
                AssertPublishedPicker(ctx, publications, editor, requested, "Leading-zero text still has exact numeric RGB");
                AssertNoRgbFieldPublications(ctx, publications, "Leading-zero commit preserves all text buffers");

                editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.LeftFoot, AppearanceArmorColor.Leather1)();
                AssertTintInput(ctx, editor, requested, "Raw0 inherited part previews the remembered global input");
                editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.RightFoot, AppearanceArmorColor.Leather1)();
                AssertTintInput(ctx, editor, requested, "Raw255 inherited part previews the remembered global input");
                editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Global, AppearanceArmorColor.Cloth1)();
                var clothId = GetItemAppearance(outfit, ItemAppearanceType.ArmorColor, (int)AppearanceArmorColor.Cloth1);
                AssertTintInput(ctx, editor, TintMapPaletteColors.GetColor(TintMapLayerType.Cloth1, clothId), "Another semantic layer must not reuse leather input");
                editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Robe, AppearanceArmorColor.Leather1)();
                AssertTintInput(ctx, editor, TintMapPaletteColors.GetColor(TintMapLayerType.Leather1, 0), "Explicit robe palette0 must not reuse global input");
                var partRequested = new TintMapColor(2, 199, 93);
                var partPreset = TintMapPaletteColors.GetClosestColorId(TintMapLayerType.Leather1, partRequested);
                ApplyWatchedValue(editor, nameof(editor.SelectedTintColor), new GuiColor(2, 199, 93));
                AssertNativeColor(ctx, outfit, PartIndex(AppearanceArmor.Robe), partPreset, "Part input native dye");
                AssertNativeColor(ctx, outfit, (int)AppearanceArmorColor.Leather1, preset, "Part input retains global dye");
                editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Global, AppearanceArmorColor.Leather1)();
                AssertTintInput(ctx, editor, requested, "Return to remembered global input");

                var beforeResize = ReadArmor(civilian);
                editor.Geometry = new GuiRectangle(0, 0, 1440, 960);
                publications.Clear();
                InvokePrivate(editor, "OnClientPropertyUpdated", nameof(editor.Geometry));
                ctx.AssertEqual(0, publications.Count, "Geometry callback must not rebuild the layout or republish bindings");
                ctx.AssertEqual(960f, editor.Geometry.Height, "Geometry callback must not nudge the layout height");
                AssertTintInput(ctx, editor, requested, "Resize retains exact RGB input");
                AssertArmorUnchanged(ctx, beforeResize, civilian, "Resize after exact RGB input");

                editor.OnSelectAppearance()();
                var skinRequested = new TintMapColor(7, 88, 159);
                var skinPreset = TintMapPaletteColors.GetClosestColorId(TintMapLayerType.Skin, skinRequested);
                ApplyWatchedValue(editor, nameof(editor.SelectedTintColor), new GuiColor(7, 88, 159));
                ctx.AssertEqual(skinPreset, GetColor(civilian, ColorChannel.Skin), "Creature input native nearest preset");
                editor.OnSelectEquipment()();
                AssertTintInput(ctx, editor, requested, "Equipment tab restores item input");
                editor.OnSelectAppearance()();
                AssertTintInput(ctx, editor, skinRequested, "Appearance tab restores creature input");
                editor.OnSelectEquipment()();
                editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Robe, AppearanceArmorColor.Leather1)();
                AssertTintInput(ctx, editor, partRequested, "Part target restores its own input");

                // Invoke the actual action body, excluding the client's mouse-button payload.
                InvokePrivate(editor, "ResetArmorColorToInheritance",
                    AppearanceEditorViewModel.ColorTarget.Robe, AppearanceArmorColor.Leather1);
                AssertNativeColor(ctx, outfit, PartIndex(AppearanceArmor.Robe), 255, "Reset action restores native inheritance");
                ctx.AssertEqual(0, GetLocalInt(outfit, OverrideName(AppearanceArmor.Robe)), "Reset action removes the explicit marker");
                AssertTintInput(ctx, editor, requested,
                    "Selected part reset immediately previews global exact input");
                // Returning externally to the old part row must not resurrect the reset input.
                SetPartColor(outfit, AppearanceArmor.Robe, partPreset, true);
                InvokePrivate(editor, "LoadTintMapEditor");
                AssertTintInput(ctx, editor, TintMapPaletteColors.GetColor(TintMapLayerType.Leather1, partPreset), "Reset invalidates the old part input cache");
                ApplyWatchedValue(editor, nameof(editor.SelectedTintColor), new GuiColor(partRequested.Red, partRequested.Green, partRequested.Blue));

                // Choosing the very row already applied is still an explicit preset action.
                editor.OnClickColorPalette(partPreset)();
                AssertTintInput(ctx, editor, TintMapPaletteColors.GetColor(TintMapLayerType.Leather1, partPreset), "Same-row part preset clears exact input");
                editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Global, AppearanceArmorColor.Leather1)();
                AssertTintInput(ctx, editor, requested, "Part preset leaves global input intact");
                editor.OnClickColorPalette(preset)();
                var canonical = TintMapPaletteColors.GetColor(TintMapLayerType.Leather1, preset);
                AssertTintInput(ctx, editor, canonical, "Same-row global preset clears exact input");
                editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Global, AppearanceArmorColor.Cloth1)();
                editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Global, AppearanceArmorColor.Leather1)();
                AssertTintInput(ctx, editor, canonical, "Preset invalidation survives target switches");

                ApplyWatchedValue(editor, nameof(editor.SelectedTintColor), new GuiColor(requested.Red, requested.Green, requested.Blue));
                var externalPreset = (preset + 1) % TintMapMaterialRegistry.PaletteColorCount;
                ItemPlugin.SetItemAppearance(outfit, ItemAppearanceType.ArmorColor,
                    (int)AppearanceArmorColor.Leather1, externalPreset, false);
                var afterExternal = ReadArmor(civilian);
                InvokePrivate(editor, "LoadTintMapEditor");
                AssertTintInput(ctx, editor, TintMapPaletteColors.GetColor(TintMapLayerType.Leather1, externalPreset), "External native dye invalidates remembered input");
                AssertArmorUnchanged(ctx, afterExternal, civilian, "External dye preview must not reapply old input");
                ctx.AssertEqual(1, GetLocalInt(outfit, OverrideName(AppearanceArmor.Robe)), "Part edits retain explicit override marker");
                AssertNativeColor(ctx, outfit, PartIndex(AppearanceArmor.LeftFoot), 0, "Global input preserves inherited raw0");
                AssertNativeColor(ctx, outfit, PartIndex(AppearanceArmor.RightFoot), 255, "Global input preserves inherited raw255");

                editor.OnSelectAppearance()();
                var nativeSkinId = GetColor(civilian, ColorChannel.Skin);
                var nativeSkinColor = TintMapPaletteColors.GetColor(TintMapLayerType.Skin, nativeSkinId);
                var persistedSkinColor = Enumerable.Range(0, TintMapMaterialRegistry.PaletteColorCount)
                    .Select(id => TintMapPaletteColors.GetColor(TintMapLayerType.Skin, id))
                    .First(color => color != nativeSkinColor);
                var persistedSkinPreset = TintMapPaletteColors.GetClosestColorId(TintMapLayerType.Skin, persistedSkinColor);
                ctx.Assert(persistedSkinPreset != nativeSkinId, "Stored custom skin must differ from the native fallback.");
                SetLocalInt(civilian, TintMapVariable.GetCreatureColorStateName(TintMapLayerType.Skin), persistedSkinColor.ToStoredValue());
                InvokePrivate(editor, "LoadTintMapEditor");
                AssertTintInput(ctx, editor, persistedSkinColor,
                    "Stored custom skin controls follow the effective color, not the native fallback");
                ctx.AssertEqual(nativeSkinId, GetColor(civilian, ColorChannel.Skin), "Loading stored custom skin must leave its native fallback unchanged");
            });
            ctx.SetResultDetail("Requested RGB survived component commits, target/layer/tab changes and resizing. Reset action body immediately restored inherited global input and invalidated part cache; presets/external changes also invalidated remembered input. Stored custom skin controls followed its effective color over a different native fallback. Headless coverage excludes mouse/Geometry event delivery and debounce timing.");
        }

        private static void AssertTintInput(EngineTestContext ctx, AppearanceEditorViewModel editor, TintMapColor requested, string stage)
        {
            AssertPickerColor(ctx, editor, requested, stage);
            ctx.AssertEqual(requested.Red.ToString(), editor.CustomTintRed, $"{stage}: exact red field");
            ctx.AssertEqual(requested.Green.ToString(), editor.CustomTintGreen, $"{stage}: exact green field");
            ctx.AssertEqual(requested.Blue.ToString(), editor.CustomTintBlue, $"{stage}: exact blue field");
        }

        private static void AssertNoRgbFieldPublications(EngineTestContext ctx, BindingPublications values, string stage)
        {
            foreach (var name in new[] { nameof(AppearanceEditorViewModel.CustomTintRed),
                         nameof(AppearanceEditorViewModel.CustomTintGreen), nameof(AppearanceEditorViewModel.CustomTintBlue) })
                ctx.Assert(!values.Contains(name), $"{stage}: {name} must not be echoed to the client.");
        }

        private static void AssertNoTintControlPublications(EngineTestContext ctx, BindingPublications values, string stage)
        {
            AssertNoRgbFieldPublications(ctx, values, stage);
            ctx.Assert(!values.Contains(nameof(AppearanceEditorViewModel.SelectedTintColor)),
                $"{stage}: the picker must not be published before the draft commits.");
        }

        private static void ApplyWatchedValue(AppearanceEditorViewModel editor, string propertyName, object value, Action beforeCallback = null)
        {
            // Mirror only UpdatePropertyFromClient's incoming-read setup. Use its real private
            // cache/SkipNotify fields, real property setter and real completion callback.
            var baseType = typeof(AppearanceEditorViewModel).BaseType;
            var cache = (IDictionary)baseType.GetField("_propertyValues", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(editor);
            var detail = cache[propertyName];
            var valueProperty = detail.GetType().GetProperty("Value");
            var skipProperty = detail.GetType().GetProperty("SkipNotify");
            var property = editor.GetType().GetProperty(propertyName);
            var previous = property.GetValue(editor);
            valueProperty.SetValue(detail, value);
            skipProperty.SetValue(detail, true);
            try
            {
                if (!Equals(previous, value)) property.SetValue(editor, value);
            }
            finally { skipProperty.SetValue(detail, false); }
            beforeCallback?.Invoke();
            InvokePrivate(editor, "OnClientPropertyUpdated", propertyName);
        }

        private static void InvokePrivate(AppearanceEditorViewModel editor, string method, params object[] arguments)
        {
            var target = editor.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Appearance editor method {method} was not found.");
            try { target.Invoke(editor, arguments); }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            }
        }

        private static void AssertPickerColor(EngineTestContext ctx, AppearanceEditorViewModel editor, TintMapColor color, string stage)
        {
            ctx.AssertEqual(color.Red, editor.SelectedTintColor.R, $"{stage}: red");
            ctx.AssertEqual(color.Green, editor.SelectedTintColor.G, $"{stage}: green");
            ctx.AssertEqual(color.Blue, editor.SelectedTintColor.B, $"{stage}: blue");
        }

        private static void AssertPublishedRgbFields(EngineTestContext ctx, BindingPublications values,
            AppearanceEditorViewModel editor, TintMapColor color, string stage)
        {
            foreach (var (name, expected) in new[]
            {
                (nameof(editor.CustomTintRed), color.Red), (nameof(editor.CustomTintGreen), color.Green),
                (nameof(editor.CustomTintBlue), color.Blue)
            })
                ctx.AssertEqual(expected.ToString(), values.Latest(ctx, name, stage).Value<string>(), $"{stage}: {name}");
        }

        private static void AssertPublishedPicker(EngineTestContext ctx, BindingPublications values,
            AppearanceEditorViewModel editor, TintMapColor color, string stage)
        {
            AssertPickerColor(ctx, editor, color, stage);
            var picker = values.Latest(ctx, nameof(editor.SelectedTintColor), stage);
            ctx.AssertEqual((int)color.Red, picker["r"].Value<int>(), $"{stage}: published picker red");
            ctx.AssertEqual((int)color.Green, picker["g"].Value<int>(), $"{stage}: published picker green");
            ctx.AssertEqual((int)color.Blue, picker["b"].Value<int>(), $"{stage}: published picker blue");
        }

        private static IEnumerable<(AppearanceArmor Part, AppearanceEditorViewModel.ColorTarget Target)> ArmorColorTargets()
        {
            foreach (var target in Enum.GetValues<AppearanceEditorViewModel.ColorTarget>())
            {
                if (target is AppearanceEditorViewModel.ColorTarget.Invalid or AppearanceEditorViewModel.ColorTarget.Global)
                    continue;
                var part = target == AppearanceEditorViewModel.ColorTarget.Chest ? AppearanceArmor.Torso
                    : Enum.Parse<AppearanceArmor>(target.ToString());
                yield return (part, target);
            }
        }

        [EngineTest("Appearance editor republishes native binding JSON after layout and reopen", Category = "AppearanceEditor", TimeoutSeconds = 30f)]
        public static async Task HydrationPublishesBindingsAfterLayout(EngineTestContext ctx)
        {
            var civilian = await SpawnCivilianAsync(ctx);
            await RunAssignedAsync(ctx, civilian, () =>
            {
                SeedInheritance(GetItemInSlot(InventorySlot.Chest, civilian));
                var armor = ReadArmor(civilian);
                var editor = new AppearanceEditorViewModel { Geometry = new GuiRectangle(0, 0, 1200, 900) };
                using var publications = new BindingPublications(editor);
                editor.Bind(OBJECT_INVALID, 0, editor.Geometry, GuiWindowType.AppearanceEditor,
                    new AppearanceEditorPayload(civilian), OBJECT_INVALID);
                AssertAppearancePublications(ctx, publications, "initial Bind");
                AssertArmorUnchanged(ctx, armor, civilian, "initial binding publication");

                for (var visit = 0; visit < 2; visit++)
                {
                    publications.Clear();
                    editor.OnSelectEquipment()();
                    AssertArmorPublications(ctx, publications, armor, $"equipment visit {visit}");
                    AssertArmorUnchanged(ctx, armor, civilian, "armor binding publication");
                    publications.Clear();
                    editor.OnSelectAppearance()();
                    AssertAppearancePublications(ctx, publications, $"appearance visit {visit}");
                    AssertArmorUnchanged(ctx, armor, civilian, "appearance binding publication");

                    if (visit == 0)
                    {
                        publications.Clear();
                        editor.Bind(OBJECT_INVALID, 0, editor.Geometry, GuiWindowType.AppearanceEditor,
                            new AppearanceEditorPayload(civilian), OBJECT_INVALID);
                        AssertAppearancePublications(ctx, publications, "reopened Bind");
                        AssertArmorUnchanged(ctx, armor, civilian, "reopened binding publication");
                    }
                }
            });
            ctx.SetResultDetail("Actual PropertyChanged replay published native NUI JSON for appearance lists, all 19 armor combos/selections and 120 bounded sprite regions after layout, including reopen. Armor fields stayed unchanged. The invalid viewer does not test client bind storage, rendering, or the IsWindowOpen-gated deferred replay.");
        }

        [EngineTest("Appearance editor geometry notifications leave static panels and native appearance unchanged", Category = "AppearanceEditor", TimeoutSeconds = 30f)]
        public static async Task GeometryNotificationsPreserveStaticPanelsAndNativeAppearance(EngineTestContext ctx)
        {
            var civilian = await SpawnCivilianAsync(ctx);
            await RunAssignedAsync(ctx, civilian, () =>
            {
                var outfit = GetItemInSlot(InventorySlot.Chest, civilian);
                SeedInheritance(outfit);
                var savedItemTintName = TintMapVariable.GetName("pfh0_robe187", TintMapLayerType.Cloth1);
                var savedCreatureTintName = TintMapVariable.GetCreatureColorStateName(TintMapLayerType.Hair);
                var savedItemTint = new TintMapColor(255, 0, 0).ToStoredValue();
                var savedCreatureTint = new TintMapColor(20, 60, 210).ToStoredValue();
                // Leave both edits unprojected: a resize must not apply or discard them.
                SetLocalInt(outfit, savedItemTintName, savedItemTint);
                SetLocalInt(civilian, savedCreatureTintName, savedCreatureTint);
                var armor = ReadArmor(civilian);
                var creatureColors = Enum.GetValues<ColorChannel>().Select(channel => GetColor(civilian, channel)).ToArray();
                var editor = new AppearanceEditorViewModel { Geometry = new GuiRectangle(0, 0, 590, 740) };
                var converter = new GuiPropertyConverter();
                using var publications = new BindingPublications(editor);
                editor.Bind(OBJECT_INVALID, 0, editor.Geometry, GuiWindowType.AppearanceEditor,
                    new AppearanceEditorPayload(civilian), OBJECT_INVALID);

                foreach (var isArmor in new[] { false, true })
                {
                    publications.Clear();
                    if (isArmor)
                    {
                        editor.OnSelectEquipment()();
                        editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Robe, AppearanceArmorColor.Metal2)();
                    }
                    else editor.OnSelectAppearance()();
                    var expectedBindings = publications.SnapshotWithoutGeometry();
                    ctx.Assert(expectedBindings.Count > 0, "The tab must have published bindings before geometry checks.");
                    var partialName = isArmor ? AppearanceEditorViewModel.EditorArmorPartial : AppearanceEditorViewModel.EditorMainPartial;
                    var originalPanel = JObject.Parse(JsonDump(AppearanceEditorDefinition.BuildEditorPanel(partialName).ToJson()));
                    AssertStaticPanelFlexPaths(ctx, originalPanel, isArmor, partialName);

                    foreach (var (width, height) in new[]
                    {
                        (590f, 740f), (1440f, 960f), (590f, 520f)
                    })
                    {
                        var stage = $"{partialName} at {width}x{height}";
                        editor.Geometry = new GuiRectangle(0, 0, width, height);
                        publications.Clear();
                        // Exercise the actual completion callback after assigning geometry.
                        // The viewer is invalid: this is not incoming NuiGetBind transport or
                        // proof of client layout/scroll behavior at these dimensions.
                        InvokePrivate(editor, "OnClientPropertyUpdated", nameof(editor.Geometry));
                        ctx.AssertEqual(0, publications.Count, $"{stage}: geometry must not redeliver any binding");
                        ctx.AssertEqual(width, editor.Geometry.Width, $"{stage}: geometry width remains exact");
                        ctx.AssertEqual(height, editor.Geometry.Height, $"{stage}: no layout redraw height nudge");
                        foreach (var (name, expected) in expectedBindings)
                        {
                            var current = editor.GetType().GetProperty(name).GetValue(editor);
                            ctx.Assert(JToken.DeepEquals(expected, JToken.Parse(JsonDump(converter.ToJson(current)))),
                                $"{stage}: cached {name} must remain unchanged without replay.");
                        }
                        // Serialize the static production factory through the native JSON API.
                        // It must keep the identical tree and event IDs across geometry changes.
                        var panel = JObject.Parse(JsonDump(AppearanceEditorDefinition.BuildEditorPanel(partialName).ToJson()));
                        ctx.Assert(JToken.DeepEquals(originalPanel, panel), $"{stage}: the static native panel JSON and control IDs must not change.");

                        AssertArmorUnchanged(ctx, armor, civilian, stage);
                        ctx.Assert(creatureColors.SequenceEqual(Enum.GetValues<ColorChannel>().Select(channel => GetColor(civilian, channel))),
                            $"{stage}: all four native creature colors must remain unchanged.");
                        ctx.AssertEqual(savedItemTint, GetLocalInt(outfit, savedItemTintName), $"{stage}: pending item tint");
                        ctx.AssertEqual(savedCreatureTint, GetLocalInt(civilian, savedCreatureTintName), $"{stage}: pending creature tint");
                    }
                }
            });
            ctx.SetResultDetail("Six actual Geometry completion callbacks at590x740,1440x960,590x520 published no bindings or redraw nudges and retained cached inputs. Static native panel JSON/event IDs stayed identical with no fixed widths along expanding control paths. All19 armor models,120 armor colors,4 creature colors,APC markers,TMP baselines and pending tint locals stayed unchanged. Headless coverage excludes client Geometry transport and rendered layout.");
        }

        private static void AssertStaticPanelFlexPaths(EngineTestContext ctx, JObject panel, bool isArmor, string stage)
        {
            ctx.Assert(panel["width"] == null && panel["height"] == null,
                $"{stage}: the static viewport must not retain a fixed window size.");
            var objects = new[] { panel }.Concat(panel.Descendants().OfType<JObject>()).ToArray();
            var ids = objects.Where(node => node["id"] != null).Select(node => node["id"].Value<string>()).ToArray();
            ctx.Assert(ids.Length > 0 && ids.Distinct().Count() == ids.Length, $"{stage}: native control IDs must be unique.");
            var flexControls = objects.Where(node => node["type"]?.Value<string>() == "color_picker" ||
                (isArmor ? node["type"]?.Value<string>() == "combo" :
                    node["row_count"]?["bind"]?.Value<string>() == nameof(AppearanceEditorViewModel.PartOptions) + "_RowCount" ||
                    node["id"]?.Value<string>() is "ae_previous_part" or "ae_next_part")).ToArray();
            ctx.AssertEqual(isArmor ? 20 : 4, flexControls.Length, $"{stage}: all expanding picker/list/part controls are covered");
            foreach (var control in flexControls)
            {
                var path = new[] { control }.Concat(control.Ancestors().OfType<JObject>()).ToArray();
                var isPartSelector = control["type"]?.Value<string>() == "combo";
                if (isPartSelector)
                    ctx.AssertEqual(96f, control["width"]?.Value<float>(), $"{stage}: part dropdown must leave room for both arrows");
                ctx.Assert(path.Skip(isPartSelector ? 1 : 0).All(node => node["width"] == null),
                    $"{stage}: {control["type"]}/{control["id"]} must not encounter a fixed-width ancestor.");
                ctx.Assert(path.Any(node => node["type"]?.Value<string>() == "group" &&
                    node["scrollbars"]?.Value<int>() == (int)NuiScrollbars.Auto),
                    $"{stage}: expanding controls must remain inside an automatically scrolling viewport.");
            }

            if (isArmor)
            {
                AssertGlobalSwatchImagesJson(ctx, panel, stage);
                var controls = objects.Where(node => node["type"]?.Value<string>() == "combo").ToArray();
                ctx.AssertEqual(19, controls.Length, $"{stage}: all armor part selectors must be serialized");
                var columns = controls.Select(control => control.Ancestors().OfType<JObject>()
                    .First(node => node["type"]?.Value<string>() == "group")).Distinct().ToArray();
                ctx.AssertEqual(3, columns.Length, $"{stage}: armor selectors must occupy three peer groups");
                var row = columns[0].Ancestors().OfType<JObject>().First();
                ctx.AssertEqual("row", row["type"]?.Value<string>(), $"{stage}: armor groups share a row");
                ctx.AssertEqual(840f, row["height"]?.Value<float>(), $"{stage}: shared row bounds the armor stacks");
                ctx.AssertEqual(3, row["children"].OfType<JObject>().Count(node => node["type"]?.Value<string>() == "group"),
                    $"{stage}: exactly three groups must be direct row children");
                foreach (var column in columns)
                {
                    ctx.Assert(ReferenceEquals(row, column.Ancestors().OfType<JObject>().First()),
                        $"{stage}: all armor groups must be siblings in the bounded row.");
                    // The client previously showed only the left stack: even a height-only
                    // modifier disables the native peer group's equal-width eligibility.
                    ctx.Assert(column["width"] == null && column["height"] == null,
                        $"{stage}: neither dimension may be fixed on an equally sharing armor group.");
                }
                ctx.Assert(columns.Select(column => column.Descendants().OfType<JObject>()
                        .Count(node => node["type"]?.Value<string>() == "combo")).SequenceEqual(new[] { 7, 5, 7 }),
                    $"{stage}: the left, center and right armor stacks must retain all 19 controls.");
            }
            else
            {
                var palette = objects.Single(node => node["id"]?.Value<string>() == "ae_color_palette");
                AssertAppearancePaletteJson(ctx, palette, stage);
                var paletteRow = palette.Ancestors().OfType<JObject>().First();
                ctx.AssertEqual("row", paletteRow["type"]?.Value<string>(), $"{stage}: palette is in a row");
                var paletteChildren = paletteRow["children"].OfType<JObject>().ToArray();
                // A lone fixed-width palette pulled the entire group's internal content
                // toward its minimum width, even when the client drew a wide outer group.
                ctx.AssertEqual(2, paletteChildren.Length, $"{stage}: palette row includes an expansion spacer");
                ctx.Assert(ReferenceEquals(paletteChildren[0], palette) &&
                    paletteChildren[1]["type"]?.Value<string>() == "spacer" &&
                    paletteChildren[1]["width"] == null && paletteChildren[1]["height"] == null,
                    $"{stage}: a dimensionless trailing spacer must absorb spare palette-row width.");

                var navigation = objects.Where(node => node["id"]?.Value<string>() is "ae_previous_part" or "ae_next_part").ToArray();
                ctx.AssertEqual(2, navigation.Length, $"{stage}: both part navigation buttons are serialized");
                var navigationRow = navigation[0].Ancestors().OfType<JObject>().First();
                ctx.AssertEqual("row", navigationRow["type"]?.Value<string>(), $"{stage}: navigation shares a row");
                ctx.AssertEqual(40f, navigationRow["height"]?.Value<float>(), $"{stage}: navigation row owns its height");
                foreach (var button in navigation)
                {
                    ctx.Assert(ReferenceEquals(navigationRow, button.Ancestors().OfType<JObject>().First()) &&
                        button["width"] == null && button["height"] == null,
                        $"{stage}: navigation buttons must share the row without disabling equal-width sizing.");
                }
            }
        }

        private static void AssertGlobalSwatchImagesJson(EngineTestContext ctx, JObject panel, string stage)
        {
            var images = panel.Descendants().OfType<JObject>()
                .Where(node => node["id"]?.Value<string>()?.StartsWith("ae_color_Global", StringComparison.Ordinal) == true).ToArray();
            ctx.AssertEqual(6, images.Length, $"{stage}: all six global dye images must be serialized");
            foreach (var image in images)
            {
                var regionName = image["id"].Value<string>().Substring("ae_color_".Length);
                ctx.AssertEqual("image", image["type"]?.Value<string>(), $"{stage}/{regionName}: swatch fills the native image rectangle");
                ctx.Assert(image["width"] == null && image["height"] == null,
                    $"{stage}/{regionName}: neither dimension may disable equal-width sharing.");
                ctx.AssertEqual(2f, image["margin"]?.Value<float>(), $"{stage}/{regionName}: swatch margin");
                ctx.AssertEqual(1f, image["aspect"]?.Value<float>(), $"{stage}/{regionName}: artwork and native encouragement share square bounds");
                ctx.AssertEqual((int)NuiAspect.Stretch, image["image_aspect"]?.Value<int>(), $"{stage}/{regionName}: sprite fills the square independently of atlas dimensions");
                ctx.AssertEqual((int)NuiHorizontalAlign.Center, image["image_halign"]?.Value<int>(), $"{stage}/{regionName}: image stays under its centered heading");
                ctx.AssertEqual(regionName, image["image_region"]?["bind"]?.Value<string>(), $"{stage}/{regionName}: native region stays bound to the selected dye");
                ctx.AssertEqual(regionName.Contains("Metal") ? "gui_pal_armor01" : "gui_pal_tattoo",
                    image["value"]?.Value<string>(), $"{stage}/{regionName}: swatch uses its authored palette family");
                ctx.Assert(image["draw_list"] == null, $"{stage}/{regionName}: global fill must not remain a fixed draw-list inset.");
                var imageRow = image.Ancestors().OfType<JObject>().First();
                ctx.AssertEqual("row", imageRow["type"]?.Value<string>(), $"{stage}/{regionName}: image belongs to a private row");
                ctx.Assert(imageRow["height"] == null, $"{stage}/{regionName}: the square must not be forced to the outer row's height");
                ctx.AssertEqual(1, imageRow["children"].Count(), $"{stage}/{regionName}: image uses the full cell without competing spacers");
                var stack = imageRow.Ancestors().OfType<JObject>().First();
                var stackRows = stack["children"].OfType<JObject>().ToArray();
                ctx.AssertEqual(2, stackRows.Length, $"{stage}/{regionName}: cell keeps a row of vertical slack below the square");
                ctx.AssertEqual("spacer", stackRows[1]["children"]?.First?["type"]?.Value<string>(), $"{stage}/{regionName}: trailing vertical spacer");
                var cell = stack.Ancestors().OfType<JObject>().First();
                ctx.AssertEqual("group", cell["type"]?.Value<string>(), $"{stage}/{regionName}: each square has an independent cell");
                ctx.Assert(cell["width"] == null && cell["height"] == null && cell["encouraged"] == null,
                    $"{stage}/{regionName}: cell shares width and never receives the selection outline.");
                ctx.AssertEqual(0f, cell["margin"]?.Value<float>(), $"{stage}/{regionName}: cell margin");
                ctx.AssertEqual((int)NuiScrollbars.None, cell["scrollbars"]?.Value<int>(), $"{stage}/{regionName}: cell scrollbars");
            }
            var rows = images.Select(image => image.Ancestors().OfType<JObject>()
                .First(node => node["type"]?.Value<string>() == "group").Ancestors().OfType<JObject>().First()).Distinct().ToArray();
            ctx.AssertEqual(2, rows.Length, $"{stage}: global dyes occupy two shared rows");
            foreach (var row in rows)
            {
                ctx.AssertEqual("row", row["type"]?.Value<string>(), $"{stage}: global image parent is a row");
                ctx.AssertEqual(99f, row["height"]?.Value<float>(), $"{stage}: shared row bounds image height");
                ctx.AssertEqual(0f, row["margin"]?.Value<float>(), $"{stage}: global image row has no hidden margins");
                ctx.AssertEqual(3, row["children"].OfType<JObject>().Count(node => node["type"]?.Value<string>() == "group"), $"{stage}: three peer cells fill the shared row");
            }
            var column = rows[0].Ancestors().OfType<JObject>().First();
            ctx.Assert(ReferenceEquals(column, rows[1].Ancestors().OfType<JObject>().First()), $"{stage}: both image rows share their header column.");
            var header = column["children"].OfType<JObject>().First();
            ctx.AssertEqual(28f, header["height"]?.Value<float>(), $"{stage}: global label row owns its height");
            var labels = header["children"].OfType<JObject>().ToArray();
            ctx.AssertEqual(3, labels.Length, $"{stage}: one label aligns with each global dye column");
            ctx.Assert(labels.Select(label => label["value"]?.Value<string>()).SequenceEqual(new[] { "Leather", "Cloth", "Metal" }),
                $"{stage}: global dye headings keep their column order.");
            ctx.Assert(labels.All(label => label["type"]?.Value<string>() == "label" &&
                label["width"] == null && label["height"] == null && label["margin"]?.Value<float>() == 0f),
                $"{stage}: global labels remain dimensionless equal-width peers.");
        }

        private static void AssertAppearancePaletteJson(EngineTestContext ctx, JObject palette, string stage)
        {
            ctx.AssertEqual("group", palette["type"]?.Value<string>(), $"{stage}: appearance palette is a button group");
            ctx.AssertEqual(308f, palette["width"]?.Value<float>(), $"{stage}: palette grid width");
            ctx.AssertEqual(218f, palette["height"]?.Value<float>(), $"{stage}: palette has no equipment-target header space");
            var column = palette["children"].OfType<JObject>().Single();
            ctx.AssertEqual("col", column["type"]?.Value<string>(), $"{stage}: palette has one column of rows");
            var rows = column["children"].OfType<JObject>().ToArray();
            ctx.AssertEqual(11, rows.Length, $"{stage}: all176 colors fit in11 rows without a target header");
            var index = 0;
            foreach (var row in rows)
            {
                ctx.AssertEqual("row", row["type"]?.Value<string>(), $"{stage}: palette row type");
                ctx.AssertEqual(18f, row["height"]?.Value<float>(), $"{stage}: palette row height");
                ctx.AssertEqual(0f, row["margin"]?.Value<float>(), $"{stage}: palette row has no hidden margins");
                var buttons = row["children"].OfType<JObject>().ToArray();
                ctx.AssertEqual(16, buttons.Length, $"{stage}: palette row includes every native column");
                foreach (var button in buttons)
                {
                    ctx.AssertEqual("button", button["type"]?.Value<string>(), $"{stage}/{index}: preset is independently clickable");
                    ctx.AssertEqual("ae_palette_" + index, button["id"]?.Value<string>(), $"{stage}/{index}: deterministic preset event ID");
                    ctx.AssertEqual(18f, button["width"]?.Value<float>(), $"{stage}/{index}: button width");
                    ctx.AssertEqual(18f, button["height"]?.Value<float>(), $"{stage}/{index}: button height");
                    ctx.AssertEqual(0f, button["margin"]?.Value<float>(), $"{stage}/{index}: button margin");
                    var image = button.Descendants().OfType<JObject>().Single(node =>
                        node["type"]?.Type == JTokenType.Integer &&
                        node["type"].Value<int>() == (int)NuiDrawListItemType.Image);
                    ctx.AssertEqual(nameof(AppearanceEditorViewModel.ColorSheetResref), image["image"]?["bind"]?.Value<string>(),
                        $"{stage}/{index}: button draws the selected skin/hair/tattoo/equipment palette");
                    var region = image["image_region"];
                    ctx.AssertEqual((float)(index % 16 * 16 + 2), region?["x"]?.Value<float>(), $"{stage}/{index}: native atlas column");
                    ctx.AssertEqual((float)(index / 16 * 16 + 2), region?["y"]?.Value<float>(), $"{stage}/{index}: native atlas row");
                    ctx.AssertEqual(12f, region?["w"]?.Value<float>(), $"{stage}/{index}: swatch samples inside one color cell");
                    ctx.AssertEqual(12f, region?["h"]?.Value<float>(), $"{stage}/{index}: swatch avoids adjacent palette rows");
                    index++;
                }
            }
            ctx.AssertEqual(176, index, $"{stage}: no missing or extra native palette IDs");
        }

        private sealed record BindingPublication(int Sequence, JToken Value);

        private sealed class BindingPublications : IDisposable
        {
            private readonly AppearanceEditorViewModel _editor;
            private readonly GuiPropertyConverter _converter = new();
            private readonly Dictionary<string, BindingPublication> _values = new();
            private int _sequence;

            public BindingPublications(AppearanceEditorViewModel editor)
            {
                _editor = editor;
                editor.PropertyChanged += Record;
            }

            private void Record(object sender, PropertyChangedEventArgs args)
            {
                _sequence++;
                var value = _editor.GetType().GetProperty(args.PropertyName)?.GetValue(_editor);
                if (value == null)
                    return;
                // The token-zero harness cannot inspect NuiGetBind. Exercise the real native
                // JSON converter at the publication event instead, before the base token guard.
                _values[args.PropertyName] = new BindingPublication(_sequence,
                    JToken.Parse(JsonDump(_converter.ToJson(value))));
            }

            public JToken AfterLayout(EngineTestContext ctx, string property, string stage)
            {
                ctx.Assert(_values.ContainsKey(nameof(AppearanceEditorViewModel.Geometry)), $"{stage}: layout must notify geometry.");
                ctx.Assert(_values.ContainsKey(property), $"{stage}: binding {property} must be published.");
                var value = _values[property];
                // Each layout change nudges Geometry; replay starts with the cached Geometry
                // and must then re-emit the dependent controls. Early hydration alone fails.
                ctx.Assert(value.Sequence > _values[nameof(AppearanceEditorViewModel.Geometry)].Sequence,
                    $"{stage}: {property} must be republished after the final layout notification.");
                return value.Value;
            }

            public bool Contains(string property) => _values.ContainsKey(property);
            public int Count => _values.Count;

            public IReadOnlyDictionary<string, JToken> SnapshotWithoutGeometry() => _values
                .Where(pair => pair.Key != nameof(AppearanceEditorViewModel.Geometry))
                .ToDictionary(pair => pair.Key, pair => pair.Value.Value.DeepClone());

            public JToken Latest(EngineTestContext ctx, string property, string stage)
            {
                ctx.Assert(_values.ContainsKey(property), $"{stage}: binding {property} must be published.");
                return _values[property].Value;
            }

            public void Clear()
            {
                _values.Clear();
                _sequence = 0;
            }

            public void Dispose() => _editor.PropertyChanged -= Record;
        }

        private static void AssertAppearancePublications(EngineTestContext ctx, BindingPublications values, string stage)
        {
            foreach (var (optionsName, selectedName, indexName) in new[]
            {
                (nameof(AppearanceEditorViewModel.ColorCategoryOptions), nameof(AppearanceEditorViewModel.ColorCategorySelected), nameof(AppearanceEditorViewModel.SelectedColorCategoryIndex)),
                (nameof(AppearanceEditorViewModel.PartCategoryOptions), nameof(AppearanceEditorViewModel.PartCategorySelected), nameof(AppearanceEditorViewModel.SelectedPartCategoryIndex)),
                (nameof(AppearanceEditorViewModel.PartOptions), nameof(AppearanceEditorViewModel.PartSelected), nameof(AppearanceEditorViewModel.SelectedPartIndex))
            })
            {
                var options = values.AfterLayout(ctx, optionsName, stage) as JArray;
                var selected = values.AfterLayout(ctx, selectedName, stage) as JArray;
                var index = values.AfterLayout(ctx, indexName, stage).Value<int>();
                ctx.Assert(options != null && options.Count > 0, $"{stage}: {optionsName} must serialize a populated array.");
                ctx.Assert(selected != null && selected.Count == options.Count,
                    $"{stage}: {selectedName} must match the option count.");
                ctx.Assert(index >= 0 && index < options.Count, $"{stage}: {indexName} must address an available option.");
            }
            ctx.Assert(values.AfterLayout(ctx, nameof(AppearanceEditorViewModel.IsAppearanceSelected), stage).Value<bool>(),
                $"{stage}: appearance controls must be visible.");
        }

        private static void AssertArmorPublications(EngineTestContext ctx, BindingPublications values, ArmorSnapshot armor, string stage)
        {
            foreach (var (part, prefix) in new[]
            {
                (AppearanceArmor.Neck, "Neck"), (AppearanceArmor.Torso, "Chest"),
                (AppearanceArmor.Belt, "Belt"), (AppearanceArmor.Pelvis, "Pelvis"), (AppearanceArmor.Robe, "Robe"),
                (AppearanceArmor.LeftShoulder, "LeftShoulder"), (AppearanceArmor.RightShoulder, "RightShoulder"),
                (AppearanceArmor.LeftBicep, "LeftBicep"), (AppearanceArmor.RightBicep, "RightBicep"),
                (AppearanceArmor.LeftForearm, "LeftForearm"), (AppearanceArmor.RightForearm, "RightForearm"),
                (AppearanceArmor.LeftHand, "LeftHand"), (AppearanceArmor.RightHand, "RightHand"),
                (AppearanceArmor.LeftThigh, "LeftThigh"), (AppearanceArmor.RightThigh, "RightThigh"),
                (AppearanceArmor.LeftShin, "LeftShin"), (AppearanceArmor.RightShin, "RightShin"),
                (AppearanceArmor.LeftFoot, "LeftFoot"), (AppearanceArmor.RightFoot, "RightFoot")
            })
            {
                var options = values.AfterLayout(ctx, prefix + "Options", stage) as JArray;
                var selected = values.AfterLayout(ctx, prefix + "Selection", stage).Value<int>();
                ctx.Assert(options != null && options.Count > 0, $"{stage}: {prefix} combo must contain options.");
                ctx.Assert(options.All(entry => entry is JArray pair && pair.Count == 2 &&
                    pair[0].Type == JTokenType.String && pair[1].Type == JTokenType.Integer),
                    $"{stage}: {prefix} combo must use native [label, value] entries.");
                ctx.AssertEqual(armor.Models[(int)part], selected, $"{stage}: {prefix} selected model");
                ctx.Assert(options.Any(entry => entry[1].Value<int>() == selected),
                    $"{stage}: {prefix} selected model must occur in its published options.");
            }

            var regions = typeof(AppearanceEditorViewModel).GetProperties()
                .Where(property => property.PropertyType == typeof(GuiRectangle) && property.Name.EndsWith("Region"))
                .ToArray();
            ctx.AssertEqual(120, regions.Length, "All six dyes for Global and nineteen armor parts");
            foreach (var property in regions)
            {
                var rectangle = values.AfterLayout(ctx, property.Name, stage);
                var x = rectangle["x"].Value<float>();
                var y = rectangle["y"].Value<float>();
                var width = rectangle["w"].Value<float>();
                var height = rectangle["h"].Value<float>();
                ctx.Assert(width == 1f && height == 1f || width == 16f && height == 16f,
                    $"{stage}: {property.Name} must crop one color or neutral pixel, never the whole atlas.");
                ctx.Assert(x >= 0f && y >= 0f && x + width <= 256f && y + height <= 176f,
                    $"{stage}: {property.Name} must be bounded by the palette texture.");
            }
        }

        [EngineTest("Appearance editor hydration preserves native armor and tint state", Category = "AppearanceEditor", TimeoutSeconds = 30f)]
        public static async Task HydrationPreservesArmorAndPendingTint(EngineTestContext ctx)
        {
            var civilian = await SpawnCivilianAsync(ctx);
            ArmorSnapshot before = null;
            var savedTintName = TintMapVariable.GetName("pfh0_robe187", TintMapLayerType.Cloth1);
            var savedTint = new TintMapColor(255, 0, 0).ToStoredValue();
            await RunAssignedAsync(ctx, civilian, () =>
            {
                var outfit = GetItemInSlot(InventorySlot.Chest, civilian);
                SeedInheritance(outfit);
                // This persisted edit has deliberately not been projected into native colors.
                // Merely opening the editor must not apply it or create TMP baseline locals.
                SetLocalInt(outfit, savedTintName, savedTint);
                before = ReadArmor(civilian);
                var editor = BindWithoutClient(civilian);
                AssertArmorUnchanged(ctx, before, civilian, "initial Bind");
                for (var repeat = 0; repeat < 3; repeat++)
                {
                    editor.OnSelectEquipment()();
                    ctx.Assert(editor.HasItemEquipped, $"The prepared farmer outfit must be editable: {FixtureDiagnostics(civilian)}");
                    ctx.Assert(editor.IsEquipmentSelected && !editor.IsAppearanceSelected,
                        "Equipment tab selection must be mutually exclusive.");
                    ctx.AssertEqual(1, editor.EditorTabToggleValue, "Equipment toggle");
                    ctx.AssertEqual(-1, editor.SettingsTabToggleValue, "Inactive settings toggle");
                    ctx.AssertEqual("Global / Leather 1", editor.ColorTargetText, "Default armor dye target");
                    ctx.Assert(editor.IsCustomTintAvailable, "Global armor color picker must be enabled.");
                    AssertInheritedRegion(ctx, editor.LeftFootLeather1Region, "Legacy raw-zero part");
                    AssertInheritedRegion(ctx, editor.RightFootLeather1Region, "Raw-255 part");
                    AssertPaletteRegion(ctx, editor.RobeLeather1Region, 0, "Explicit palette-zero part");
                    foreach (var (channel, layer) in new[]
                    {
                        (AppearanceArmorColor.Leather1, TintMapLayerType.Leather1),
                        (AppearanceArmorColor.Leather2, TintMapLayerType.Leather2),
                        (AppearanceArmorColor.Cloth1, TintMapLayerType.Cloth1),
                        (AppearanceArmorColor.Cloth2, TintMapLayerType.Cloth2),
                        (AppearanceArmorColor.Metal1, TintMapLayerType.Metal1),
                        (AppearanceArmorColor.Metal2, TintMapLayerType.Metal2)
                    })
                    {
                        editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Global, channel)();
                        ctx.Assert(editor.IsCustomTintAvailable, $"Global {channel} picker must be enabled even without a visible material layer.");
                        var colorId = GetItemAppearance(outfit, ItemAppearanceType.ArmorColor, (int)channel);
                        var color = TintMapPaletteColors.GetColor(layer, colorId);
                        ctx.AssertEqual(color.Red, editor.SelectedTintColor.R, $"Global {channel} preview red");
                        ctx.AssertEqual(color.Green, editor.SelectedTintColor.G, $"Global {channel} preview green");
                        ctx.AssertEqual(color.Blue, editor.SelectedTintColor.B, $"Global {channel} preview blue");
                    }
                    AssertArmorUnchanged(ctx, before, civilian, "equipment hydration");
                    editor.OnSelectAppearance()();
                    ctx.Assert(editor.IsAppearanceSelected && !editor.IsEquipmentSelected,
                        "Appearance tab selection must be mutually exclusive.");
                    ctx.AssertEqual(0, editor.EditorTabToggleValue, "Appearance toggle");
                    AssertArmorUnchanged(ctx, before, civilian, "appearance hydration");
                }
                ctx.AssertEqual(savedTint, GetLocalInt(outfit, savedTintName), "Unapplied persisted material color");
            });
            await ctx.DelaySecondsAsync(3.25f);
            await RunAssignedAsync(ctx, civilian, () =>
            {
                AssertArmorUnchanged(ctx, before, civilian, "delayed hydration callbacks");
                ctx.AssertEqual(savedTint, GetLocalInt(before.Item, savedTintName), "Persisted color after delayed callbacks");
            });
            ctx.SetResultDetail("Public VM Bind and six tab transitions preserved all 19 armor models, 120 native colors, APC markers, TMP baselines, and unapplied tint state. Headless server test; client watch delivery and rendered NUI layout are not exercised.");
        }

        [EngineTest("Appearance editor global and part pickers preserve native inheritance", Category = "AppearanceEditor", TimeoutSeconds = 30f)]
        public static async Task GlobalAndPartColorActionsPreserveInheritance(EngineTestContext ctx)
        {
            var civilian = await SpawnCivilianAsync(ctx);
            await RunAssignedAsync(ctx, civilian, () =>
            {
                var outfit = GetItemInSlot(InventorySlot.Chest, civilian);
                SeedInheritance(outfit);
                var editor = BindWithoutClient(civilian);
                editor.OnSelectEquipment()();
                ctx.Assert(editor.HasItemEquipped && editor.IsCustomTintAvailable,
                    $"The global picker must be available for the fixture outfit: {FixtureDiagnostics(civilian)}");

                var requested = TintMapPaletteColors.GetColor(TintMapLayerType.Leather1, 52);
                var selected = TintMapPaletteColors.GetClosestColorId(TintMapLayerType.Leather1, requested);
                ctx.Assert(selected != 3, "The global picker edit must differ from the authored leather dye.");
                editor.SelectedTintColor = new GuiColor(requested.Red, requested.Green, requested.Blue);
                AssertNativeColor(ctx, outfit, (int)AppearanceArmorColor.Leather1, selected, "Global RGB picker");
                AssertInheritedAndExplicitParts(ctx, outfit);
                AssertPaletteRegion(ctx, editor.GlobalLeather1Region, selected, "Global swatch after RGB picker");

                editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Robe, AppearanceArmorColor.Leather1)();
                editor.OnClickColorPalette(77)();
                AssertNativeColor(ctx, outfit, PartIndex(AppearanceArmor.Robe), 77, "Per-part preset edit");
                AssertNativeColor(ctx, outfit, (int)AppearanceArmorColor.Leather1, selected, "Part edit retains global dye");
                editor.OnClickColorPalette(0)();
                AssertNativeColor(ctx, outfit, PartIndex(AppearanceArmor.Robe), 0, "Explicit per-part palette zero");
                ctx.AssertEqual(1, GetLocalInt(outfit, OverrideName(AppearanceArmor.Robe)), "Explicit-zero override marker");
                AssertPaletteRegion(ctx, editor.RobeLeather1Region, 0, "Explicit-zero swatch");

                editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Global, AppearanceArmorColor.Leather1)();
                editor.OnClickColorPalette(66)();
                AssertNativeColor(ctx, outfit, (int)AppearanceArmorColor.Leather1, 66, "Later global preset");
                AssertInheritedAndExplicitParts(ctx, outfit);
                editor.OnSelectAppearance()();
                editor.OnSelectEquipment()();
                ctx.AssertEqual("Global / Leather 1", editor.ColorTargetText, "Reopened equipment target");
                var expected = TintMapPaletteColors.GetColor(TintMapLayerType.Leather1, 66);
                ctx.AssertEqual(expected.Red, editor.SelectedTintColor.R, "Global picker red after rehydration");
                ctx.AssertEqual(expected.Green, editor.SelectedTintColor.G, "Global picker green after rehydration");
                ctx.AssertEqual(expected.Blue, editor.SelectedTintColor.B, "Global picker blue after rehydration");
                AssertInheritedRegion(ctx, editor.LeftFootLeather1Region, "Inherited raw-zero swatch after global edit");
                AssertInheritedRegion(ctx, editor.RightFootLeather1Region, "Inherited raw-255 swatch after global edit");
                AssertPaletteRegion(ctx, editor.RobeLeather1Region, 0, "Explicit zero remains distinct from inheritance");
            });
            ctx.SetResultDetail("Actual VM RGB/preset actions edited global and robe dyes independently. Native raw0/raw255 inherited parts stayed unset; APC-marked palette0 survived a later global edit and tab rehydration. No client renderer or mouse-event transport is attached.");
        }

        private static AppearanceEditorViewModel BindWithoutClient(uint target)
        {
            var geometry = new GuiRectangle(0, 0, 1200, 900);
            var editor = new AppearanceEditorViewModel { Geometry = geometry };
            // Exercise the public hydration path without creating a window or connecting a PC.
            // The invalid viewer/zero token cannot send client updates; Target is the real NPC.
            editor.Bind(OBJECT_INVALID, 0, geometry, GuiWindowType.AppearanceEditor,
                new AppearanceEditorPayload(target), OBJECT_INVALID);
            return editor;
        }

        private static void SeedInheritance(uint item)
        {
            SetPartColor(item, AppearanceArmor.LeftFoot, 0, false);
            SetPartColor(item, AppearanceArmor.RightFoot, 255, false);
            SetPartColor(item, AppearanceArmor.Robe, 0, true);
        }

        private static void SetPartColor(uint item, AppearanceArmor part, int color, bool explicitOverride)
        {
            ItemPlugin.SetItemAppearance(item, ItemAppearanceType.ArmorColor, PartIndex(part), color, false);
            if (explicitOverride)
                SetLocalInt(item, OverrideName(part), 1);
            else
                DeleteLocalInt(item, OverrideName(part));
        }

        private static int PartIndex(AppearanceArmor part) =>
            ArmorColorIndexCalculator.CalculatePerPart(part, AppearanceArmorColor.Leather1);

        private static string OverrideName(AppearanceArmor part) =>
            ArmorColorIndexCalculator.GetPerPartOverrideVariableName(part, AppearanceArmorColor.Leather1);

        private static void AssertInheritedAndExplicitParts(EngineTestContext ctx, uint outfit)
        {
            AssertNativeColor(ctx, outfit, PartIndex(AppearanceArmor.LeftFoot), 0, "Legacy inherited raw zero");
            AssertNativeColor(ctx, outfit, PartIndex(AppearanceArmor.RightFoot), 255, "Inherited raw255");
            AssertNativeColor(ctx, outfit, PartIndex(AppearanceArmor.Robe), 0, "Explicit palette zero");
            ctx.AssertEqual(0, GetLocalInt(outfit, OverrideName(AppearanceArmor.LeftFoot)), "No zero-inheritance marker");
            ctx.AssertEqual(0, GetLocalInt(outfit, OverrideName(AppearanceArmor.RightFoot)), "No 255-inheritance marker");
            ctx.AssertEqual(1, GetLocalInt(outfit, OverrideName(AppearanceArmor.Robe)), "Explicit palette-zero marker");
        }

        private static void AssertNativeColor(EngineTestContext ctx, uint item, int index, int expected, string message) =>
            ctx.AssertEqual(expected, GetItemAppearance(item, ItemAppearanceType.ArmorColor, index), message);

        private static void AssertInheritedRegion(EngineTestContext ctx, GuiRectangle region, string message)
        {
            ctx.Assert(region != null && region.Width == 1f && region.Height == 1f,
                $"{message} must show the neutral inheritance indicator, not a palette color.");
        }

        private static void AssertPaletteRegion(EngineTestContext ctx, GuiRectangle region, int color, string message)
        {
            ctx.Assert(region != null && region.Width == 16f && region.Height == 16f &&
                       region.X == color % 16 * 16 && region.Y == color / 16 * 16,
                $"{message} must select the palette cell for color {color}.");
        }

        private static ArmorSnapshot ReadArmor(uint creature)
        {
            var item = GetItemInSlot(InventorySlot.Chest, creature);
            return new ArmorSnapshot(item,
                Enumerable.Range(0, 19).Select(index => GetItemAppearance(item, ItemAppearanceType.ArmorModel, index)).ToArray(),
                Enumerable.Range(0, 120).Select(index => GetItemAppearance(item, ItemAppearanceType.ArmorColor, index)).ToArray(),
                Enumerable.Range(0, 19).SelectMany(part => Enumerable.Range(0, 6).Select(channel => GetLocalInt(item,
                    ArmorColorIndexCalculator.GetPerPartOverrideVariableName((AppearanceArmor)part, (AppearanceArmorColor)channel)))).ToArray(),
                Enumerable.Range(0, 120).SelectMany(index => new[]
                {
                    GetLocalInt(item, TintMapNativePaletteProjection.BaselineName(index)),
                    GetLocalInt(item, TintMapNativePaletteProjection.LastAppliedName(index))
                }).ToArray());
        }

        private static void AssertArmorUnchanged(EngineTestContext ctx, ArmorSnapshot expected, uint creature, string stage)
        {
            var actual = ReadArmor(creature);
            ctx.AssertEqual(expected.Item, actual.Item, $"{stage}: equipped item identity");
            ctx.Assert(expected.Models.SequenceEqual(actual.Models), $"{stage}: every native armor model must remain unchanged.");
            ctx.Assert(expected.Colors.SequenceEqual(actual.Colors), $"{stage}: every native armor color must remain unchanged.");
            ctx.Assert(expected.Markers.SequenceEqual(actual.Markers), $"{stage}: explicit override markers must remain unchanged.");
            ctx.Assert(expected.Projections.SequenceEqual(actual.Projections), $"{stage}: native projection baselines must remain unchanged.");
        }

        private static async Task<uint> SpawnCivilianAsync(EngineTestContext ctx)
        {
            var civilian = ctx.SpawnCreature("civilian");
            await ctx.WaitUntilAsync(() => GetIsObjectValid(GetItemInSlot(InventorySlot.Chest, civilian)),
                10f, "the civilian outfit to be equipped");
            await ctx.DelaySecondsAsync(0.5f);
            ctx.AssertEqual("farmer_outfit002", GetResRef(GetItemInSlot(InventorySlot.Chest, civilian)), "Fixture outfit");
            await RunAssignedAsync(ctx, civilian, () =>
            {
                // The NPC blueprint deliberately curses this outfit. Make only this spawned
                // test instance editable before taking the hydration snapshot; retain its models.
                var outfit = GetItemInSlot(InventorySlot.Chest, civilian);
                SetItemCursedFlag(outfit, false);
                SetPlotFlag(outfit, false);
                var invalidParts = UnsupportedFixtureParts(outfit);
                ctx.Assert(!GetItemCursedFlag(outfit) && !GetPlotFlag(outfit) && invalidParts.Length == 0,
                    $"Fixture must pass the native editable-item preconditions: {FixtureDiagnostics(civilian)}");
                ctx.AssertEqual(6, (int)GetAppearanceType(civilian), "Fixture must use the human armor definition");
                ctx.AssertEqual(187, GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe),
                    "Fixture preparation must retain the real dyeable robe model");
            });
            return civilian;
        }

        private static string FixtureDiagnostics(uint creature)
        {
            var item = GetItemInSlot(InventorySlot.Chest, creature);
            return $"appearance={(int)GetAppearanceType(creature)}, item={GetResRef(item)}, " +
                   $"cursed={GetItemCursedFlag(item)}, plot={GetPlotFlag(item)}, " +
                   $"unsupported parts=[{string.Join(", ", UnsupportedFixtureParts(item))}]";
        }

        private static string[] UnsupportedFixtureParts(uint item)
        {
            var definition = new GeneralArmorAppearanceDefinition();
            return Enumerable.Range(0, (int)AppearanceArmor.Num)
                .Select(index => (Part: (AppearanceArmor)index,
                    Model: GetItemAppearance(item, ItemAppearanceType.ArmorModel, index)))
                .Where(entry => !FixturePartOptions(definition, entry.Part).Contains(entry.Model))
                .Select(entry => $"{entry.Part}={entry.Model}")
                .ToArray();
        }

        private static int[] FixturePartOptions(GeneralArmorAppearanceDefinition definition, AppearanceArmor part) => part switch
        {
            AppearanceArmor.LeftFoot or AppearanceArmor.RightFoot => definition.Foot,
            AppearanceArmor.LeftShin or AppearanceArmor.RightShin => definition.Shin,
            AppearanceArmor.LeftThigh or AppearanceArmor.RightThigh => definition.Thigh,
            AppearanceArmor.LeftHand or AppearanceArmor.RightHand => definition.Hand,
            AppearanceArmor.LeftForearm or AppearanceArmor.RightForearm => definition.Forearm,
            AppearanceArmor.LeftBicep or AppearanceArmor.RightBicep => definition.Bicep,
            AppearanceArmor.LeftShoulder or AppearanceArmor.RightShoulder => definition.Shoulder,
            AppearanceArmor.Pelvis => definition.Pelvis,
            AppearanceArmor.Torso => definition.Torso,
            AppearanceArmor.Belt => definition.Belt,
            AppearanceArmor.Neck => definition.Neck,
            AppearanceArmor.Robe => definition.Robe,
            _ => Array.Empty<int>()
        };

        private static async Task RunAssignedAsync(EngineTestContext ctx, uint creature, Action action)
        {
            var completed = false;
            Exception failure = null;
            AssignCommand(creature, () =>
            {
                try { action(); }
                catch (Exception exception) { failure = exception; }
                finally { completed = true; }
            });
            await ctx.WaitUntilAsync(() => completed, 5f, "the assigned appearance editor action to complete");
            if (failure != null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
