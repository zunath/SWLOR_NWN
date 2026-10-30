using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.ChatCommandDefinition;
using SWLOR.Game.Server.Feature.AppearanceDefinition.AppearancePreview;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
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

        [EngineTest("Cloak RGB edits persist across editor reopening and native palette reset", Category = "CloakRGB", TimeoutSeconds = 30f)]
        public static async Task CloakRgbPersistenceAndPaletteReset(EngineTestContext ctx)
        {
            var civilian = await SpawnCivilianAsync(ctx);
            var cloak = await ctx.EquipItemAsync(civilian, "advent_cloak", InventorySlot.Cloak);
            await RunAssignedAsync(ctx, civilian, () =>
            {
                EquippedItemAppearance.Set(cloak, ItemAppearanceType.SimpleModel, -1, 175);
                EquippedItemAppearance.Refresh(civilian, cloak);
                TintMapService.ApplyCurrentColors(civilian);
                var selection = TintMapModelResolver.GetCurrentSelections(civilian)
                    .Single(value => value.Material.Resref == CloakTintRenderer.MaterialResref);
                ctx.AssertEqual("cloak_062", selection.ModelResref, "Cloak registry follows TEXTURE, independently of MODEL 4");
                var editor = BindWithoutClient(civilian);
                editor.OnSelectEquipment()();
                editor.SelectedItemTypeIndex = 2;
                editor.SelectedColorCategoryIndex = (int)AppearanceArmorColor.Cloth1;
                InvokePrivate(editor, "LoadTintMapEditor");
                ctx.Assert(editor.IsCustomTintEditable, "Cloak Cloth1 enables RGB");
                var color = new TintMapColor(17, 83, 209);
                ApplyWatchedValue(editor, nameof(editor.SelectedTintColor), new GuiColor(color.Red, color.Green, color.Blue));
                TintMapEngineTests.AssertNativeRgb(ctx, civilian, CloakTintRenderer.MaterialResref, TintMapLayerType.Cloth1, color);
                editor = BindWithoutClient(civilian);
                editor.OnSelectEquipment()();
                editor.SelectedItemTypeIndex = 2;
                editor.SelectedColorCategoryIndex = (int)AppearanceArmorColor.Cloth1;
                InvokePrivate(editor, "LoadTintMapEditor");
                AssertTintInput(ctx, editor, color, "Reopened cloak RGB");
                editor.OnClickColorPalette(77)();
                ctx.AssertEqual(0, GetLocalInt(cloak, TintMapVariable.GetItemGlobalColorStateName(TintMapLayerType.Cloth1)),
                    "Native palette selection clears the cloak RGB override");
                ctx.AssertEqual(77, GetItemAppearance(cloak, ItemAppearanceType.ArmorColor, (int)AppearanceArmorColor.Cloth1),
                    "Native cloak palette is retained for the inventory icon");
                ctx.AssertEqual(cloak, GetItemInSlot(InventorySlot.Cloak, civilian), "RGB does not replace or unequip the cloak");
            });
            await ctx.DelaySecondsAsync(1f);
            await RunAssignedAsync(ctx, civilian, () =>
                TintMapService.ApplyCurrentColors(civilian));
        }

        [EngineTest("Body customization updates exposed hands and biceps while preserving clothing", Category = "BodyCustomization", TimeoutSeconds = 30f)]
        public static async Task ExposedBodyPartsFollowEditorSelections(EngineTestContext ctx)
        {
            var civilian = await SpawnCivilianAsync(ctx);
            await RunAssignedAsync(ctx, civilian, () =>
            {
                var outfit = GetItemInSlot(InventorySlot.Chest, civilian);
                var weight = GetWeight(outfit);
                var armorClass = GetItemACValue(outfit);
                var editor = BindWithoutClient(civilian);
                foreach (var (part, category, models) in new[]
                {
                    (SWLOR.NWN.API.NWScript.Enum.Creature.CreaturePart.LeftHand, 11, new[] { 258, 259, 260, 261, 262, 263, 1 }),
                    (SWLOR.NWN.API.NWScript.Enum.Creature.CreaturePart.RightHand, 5, new[] { 258, 259, 260, 261, 262, 263, 1 }),
                    (SWLOR.NWN.API.NWScript.Enum.Creature.CreaturePart.LeftBicep, 9, new[] { 251, 1 }),
                    (SWLOR.NWN.API.NWScript.Enum.Creature.CreaturePart.RightBicep, 3, new[] { 251, 1 })
                })
                {
                    SetCreatureBodyPart(part, 1, civilian);
                    EquippedItemAppearance.Set(outfit, ItemAppearanceType.ArmorModel, (int)part, 1);
                    editor.SelectedPartCategoryIndex = category;
                    InvokePrivate(editor, "LoadBodyParts");
                    foreach (var model in models)
                    {
                        editor.SelectedPartIndex = editor.PartOptions.ToList().IndexOf($"Part #{model}");
                        ctx.Assert(editor.SelectedPartIndex >= 0, $"{part} offers {model}");
                        InvokePrivate(editor, "LoadBodyPart");
                        ctx.AssertEqual(model, GetCreatureBodyPart(part, civilian), $"{part} saves {model}");
                        ctx.AssertEqual(model, GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)part),
                            $"{part} exposed outfit follows {model}");
                    }

                    EquippedItemAppearance.Set(outfit, ItemAppearanceType.ArmorModel, (int)part, 151);
                    BodyPartAppearance.Set(part, models[0], civilian);
                    ctx.AssertEqual(151, GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)part),
                        $"{part} retains a separate glove or sleeve");
                    EquippedItemAppearance.Set(outfit, ItemAppearanceType.ArmorModel, (int)part, 0);
                    BodyPartAppearance.Set(part, 1, civilian);
                    ctx.AssertEqual(0, GetItemAppearance(outfit, ItemAppearanceType.ArmorModel, (int)part),
                        $"{part} retains native model inheritance");
                }
                ctx.AssertEqual(outfit, GetItemInSlot(InventorySlot.Chest, civilian), "Outfit remains equipped");
                ctx.AssertEqual(weight, GetWeight(outfit), "Body edits preserve outfit weight");
                ctx.AssertEqual(armorClass, GetItemACValue(outfit), "Body edits preserve outfit armor class");
            });
        }

        [EngineTest("Head scaling validates input and saves independently of body height", Category = "AppearanceEditor", TimeoutSeconds = 30f)]
        public static async Task HeadScalePersistenceAndCancellation(EngineTestContext ctx)
        {
            var civilian = await SpawnCivilianAsync(ctx);
            await RunAssignedAsync(ctx, civilian, () =>
            {
                var playerId = GetObjectUUID(civilian);
                var native = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp
                    .GetGameObject(civilian).AsNWSCreature();
                var wasPlayer = native.m_bPlayerCharacter;
                global::NWN.Native.API.CNWSPlayer client = null;
                DB.Set(new Player(playerId) { AppearanceScale = 0.92f, HeadAppearanceScale = 1.08f });
                try
                {
                    // Settings load/save and cancellation are PC-only. Attach the synthetic
                    // client before exercising any of those paths on this disposable fixture.
                    native.m_bPlayerCharacter = 1;
                    client = new global::NWN.Native.API.CNWSPlayer(0x7fff0000u);
                    client.SetGameObject(global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp
                        .GetGameObject(civilian).AsNWSObject());
                    client.m_oidPCObject = civilian;
                    global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetPlayerList().Add(client);
                    ctx.Assert(GetIsPC(civilian), "Fixture exercises the PC settings paths");

                    SetObjectVisualTransform(civilian, ObjectVisualTransform.Scale, 0.92f);
                    SetObjectVisualTransform(civilian, ObjectVisualTransform.Scale, 1.08f,
                        nScope: ObjectVisualTransformDataScopeType.CreatureHead);
                    var command = new CharacterChatCommand().BuildChatCommands()["headscale"];
                    foreach (var invalid in new[] { "NaN", "Infinity", "-Infinity", "0.5", "2", "invalid" })
                        ctx.Assert(!string.IsNullOrEmpty(command.ValidateArguments(civilian, invalid)),
                            "Reject head scale: " + invalid);
                    ctx.AssertEqual(string.Empty, command.ValidateArguments(civilian, "1.04"), "Valid head scale");
                    command.DoAction(civilian, civilian, GetLocation(civilian), "1.04");
                    AssertScale(ctx, civilian, 0.92f, 1.04f, "Chat adjustment");
                    ctx.AssertEqual(1.04f, DB.Get<Player>(playerId).HeadAppearanceScale, "Chat persists head scale");

                    var editor = BindWithoutClient(civilian);
                    editor.OnIncreaseHeadScale()();
                    AssertScale(ctx, civilian, 0.92f, 1.05f, "Head preview");
                    editor.OnClickSaveSettings()();
                    ctx.AssertEqual(0.92f, DB.Get<Player>(playerId).AppearanceScale, "Save retains body height");
                    ctx.Assert(Math.Abs(DB.Get<Player>(playerId).HeadAppearanceScale - 1.05f) < 0.0001f,
                        "Save persists head preview");

                    editor.OnDecreaseHeadScale()();
                    SetObjectVisualTransform(civilian, ObjectVisualTransform.Scale, 1.1f);
                    editor.OnCloseWindow()();
                    AssertScale(ctx, civilian, 0.92f, 1.05f, "Cancel restores both saved scales");

                    SetObjectVisualTransform(civilian, ObjectVisualTransform.Scale, 1f);
                    SetObjectVisualTransform(civilian, ObjectVisualTransform.Scale, 1f,
                        nScope: ObjectVisualTransformDataScopeType.CreatureHead);
                    foreach (var method in new[] { "ApplyHeight", "ApplyHeadScale" })
                        typeof(PlayerTemporaryEffects).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static)
                            .Invoke(null, new object[] { civilian });
                    AssertScale(ctx, civilian, 0.92f, 1.05f, "Login restores both saved scales");
                }
                finally
                {
                    if (client != null)
                    {
                        global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetPlayerList().Remove(client);
                        client.m_oidPCObject = OBJECT_INVALID;
                        client.Dispose();
                    }
                    native.m_bPlayerCharacter = wasPlayer;
                    DB.Delete<Player>(playerId);
                }
            });
        }

        [EngineTest("NPC scale edits remain on the creature without player settings", Category = "AppearanceEditor", TimeoutSeconds = 30f)]
        public static async Task NpcScaleEditsDoNotCreatePlayerSettings(EngineTestContext ctx)
        {
            var civilian = await SpawnCivilianAsync(ctx);
            await RunAssignedAsync(ctx, civilian, () =>
            {
                var id = GetObjectUUID(civilian);
                ctx.Assert(!DB.Exists<Player>(id), "NPC starts without a player record");
                SetObjectVisualTransform(civilian, ObjectVisualTransform.Scale, 1f);
                HelmetModelRenderer.SetHeadScale(civilian, 1f);
                var editor = BindWithoutClient(civilian);
                ctx.Assert(!editor.IsPlayerSettingsVisible, "NPC has no player preference controls");
                editor.OnIncreaseHeadScale()();
                editor.OnDecreaseAppearanceScale()();
                editor.OnClickSaveSettings()();
                editor.OnCloseWindow()();
                AssertScale(ctx, civilian, 0.99f, 1.01f, "NPC close retains direct scale edits");
                ctx.Assert(!DB.Exists<Player>(id), "NPC load, Save and close do not create player data");
            });
            ctx.SetResultDetail("Real NPC scale handlers and close/Save paths; no DM client is attached. DM tab access is covered by the recording settings tests.");
        }

        private static void AssertScale(EngineTestContext ctx, uint target, float body, float head, string stage)
        {
            ctx.Assert(Math.Abs(GetObjectVisualTransform(target, ObjectVisualTransform.Scale) - body) < 0.0001f,
                stage + " retains the expected body scale");
            ctx.Assert(Math.Abs(GetObjectVisualTransform(target, ObjectVisualTransform.Scale,
                nScope: ObjectVisualTransformDataScopeType.CreatureHead) - head) < 0.0001f,
                stage + " retains the expected head scale");
        }

        [EngineTest("Appearance editor ignores black picker hydration until a user gesture", Category = "AppearanceEditor", TimeoutSeconds = 30f)]
        public static async Task PickerHydrationPreservesSkin(EngineTestContext ctx)
        {
            var civilian = await SpawnCivilianAsync(ctx);
            await RunAssignedAsync(ctx, civilian, () =>
            {
                var nativeSkin = GetColor(civilian, ColorChannel.Skin);
                var stateName = TintMapVariable.GetCreatureColorStateName(TintMapLayerType.Skin);
                var savedTint = GetLocalInt(civilian, stateName);
                var editor = new AppearanceEditorViewModel { Geometry = new GuiRectangle(0, 0, 800, 900) };
                editor.Bind(OBJECT_INVALID, 0, editor.Geometry, GuiWindowType.AppearanceEditor,
                    new AppearanceEditorPayload(civilian), OBJECT_INVALID);
                ApplyWatchedValue(editor, nameof(editor.SelectedTintColor), new GuiColor(0, 0, 0), userGesture: false);
                InvokePrivate(editor, "FlushPendingPickerColor");
                editor.OnMouseDownTintPicker()();
                editor.OnMouseUpTintPicker()();
                ctx.AssertEqual(nativeSkin, GetColor(civilian, ColorChannel.Skin), "Click without a watched update retains native skin");
                ctx.AssertEqual(savedTint, GetLocalInt(civilian, stateName), "Hydration followed by an empty gesture must not create an override");
                editor.OnSelectEquipment()();
                editor.OnSelectAppearance()();
                ctx.AssertEqual(nativeSkin, GetColor(civilian, ColorChannel.Skin), "Opening and switching tabs retains native skin");
                ctx.AssertEqual(savedTint, GetLocalInt(civilian, stateName), "Black hydration must not create a custom skin tint");
                ctx.Assert(editor.IsCustomTintEditable, "Skin supports deliberate RGB edits");
                ApplyWatchedValue(editor, nameof(editor.SelectedTintColor), new GuiColor(0, 0, 0));
                editor.OnMouseUpTintPicker()();
                ctx.AssertEqual(new TintMapColor(0, 0, 0).ToStoredValue(), GetLocalInt(civilian, stateName),
                    "A deliberate black skin selection must still work");
            });
        }

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
                AssertEquipmentSwatchesJson(ctx, JObject.Parse(JsonDump(panel.ToJson())), "Equipment native swatches");
                AssertActiveColorSwatch(ctx, editor, "GlobalLeather1Selected");
                foreach (var (channel, layer) in channels)
                {
                    var regionName = "Global" + channel + "Region";
                    var swatch = WidgetTree(panel).Single(widget => widget.Id == "ae_base_swatch_" + channel);
                    ctx.Assert(swatch is GuiButton<AppearanceEditorViewModel>, $"{channel}: base swatch is a button.");
                    ctx.Assert(swatch.Events.TryGetValue("click", out var click), $"{channel}: base swatch handles click.");
                    ctx.AssertEqual(1, swatch.Events.Count, $"{channel}: base swatch needs only its click event");
                    ctx.AssertEqual(nameof(AppearanceEditorViewModel.OnClickBaseSwatch), click.Method.Name,
                        $"{channel}: base swatch routes to the base colour action");
                    ctx.AssertEqual(1, click.Arguments.Count, $"{channel}: base swatch captures one dye channel");
                    ctx.AssertEqual(channel, (AppearanceArmorColor)click.Arguments[0].Value, $"{channel}: captured dye channel");
                    // Dispatch the action captured by the real definition's button.
                    ((Action)click.Method.Invoke(editor, click.Arguments.Select(argument => argument.Value).ToArray()))();
                    AssertActiveColorSwatch(ctx, editor, "Global" + channel + "Selected");
                    AssertTintInput(ctx, editor, TintMapPaletteColors.GetColor(layer, 35 + (int)channel),
                        $"{channel}: base swatch selection previews its own native dye");
                    AssertPaletteRegion(ctx, (GuiRectangle)editor.GetType().GetProperty(regionName).GetValue(editor),
                        35 + (int)channel, $"{channel}: base swatch region follows its native dye");
                    ctx.Assert(editor.IsPaletteSelectionVisible, $"{channel}: a palette colour is outlined.");
                    AssertPaletteOutline(ctx, editor, 35 + (int)channel, $"{channel}: palette outline");
                }
                foreach (var (part, target) in ArmorColorTargets())
                {
                    // The body map selects a part; its six swatches mirror that part's colours.
                    var slot = Enum.Parse<AppearanceEditorSlot>(target.ToString());
                    editor.OnClickMapSlot(slot)();
                    ctx.Assert(editor.IsPartColorsVisible, $"{part}: part colours are shown for the selected slot.");
                    ctx.Assert((bool)editor.GetType().GetProperty($"Map{slot}Selected").GetValue(editor),
                        $"{part}: the body map highlights the selected slot.");
                    foreach (var (channel, layer) in channels)
                    {
                        var source = (GuiRectangle)editor.GetType().GetProperty($"{target}{channel}Region").GetValue(editor);
                        var mirror = (GuiRectangle)editor.GetType().GetProperty($"SelectedPart{channel}Region").GetValue(editor);
                        ctx.Assert(source.X == mirror.X && source.Y == mirror.Y && source.Width == mirror.Width &&
                            source.Height == mirror.Height, $"{part}/{channel}: the part swatch mirrors its target region.");

                        editor.OnClickColorTarget(target, channel)();
                        AssertActiveColorSwatch(ctx, editor, target.ToString() + channel + "Selected");
                        ctx.Assert((bool)editor.GetType().GetProperty($"SelectedPart{channel}Selected").GetValue(editor),
                            $"{part}/{channel}: the mirrored part swatch is highlighted.");
                        ctx.Assert(editor.IsCustomTintAvailable,
                            $"{part}/{channel} must retain its native picker even without a generated tint selection.");
                        var mode = ((int)part + (int)channel) % 4;
                        var expectedId = mode switch { 2 => 0, 3 => 77, _ => 35 + (int)channel };
                        AssertPickerColor(ctx, editor, TintMapPaletteColors.GetColor(layer, expectedId), $"{part}/{channel} native preview");
                    }
                }
                AssertArmorUnchanged(ctx, snapshot, civilian, "all114 part dye target selections");
                editor.OnSelectAppearance()();
                editor.OnSelectEquipment()();
                AssertActiveColorSwatch(ctx, editor, "GlobalLeather1Selected");
                editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Chest, AppearanceArmorColor.Cloth1)();
                InvokePrivate(editor, "ResetArmorColorToInheritance", AppearanceEditorViewModel.ColorTarget.Chest, AppearanceArmorColor.Cloth1);
                AssertActiveColorSwatch(ctx, editor, "ChestCloth1Selected");
            });
            ctx.SetResultDetail("Six base and six part swatch buttons serialized region, tint, custom, outline and code bindings; the definitions' captured base-swatch actions selected their native dyes, codes and palette outline. Selecting each of the 19 body-map slots mirrored its six target regions and codes into the part swatches, and all 19x6 native armor targets retained a picker, including Chest70. Seeded raw255/raw0/explicit0/explicit77 cases displayed inherited or explicit colors without armor writes. Headless coverage excludes mouse payload delivery and client rendering.");
        }

        [EngineTest("Appearance palette buttons apply only their selected native color channel", Category = "AppearanceEditor", TimeoutSeconds = 30f)]
        public static async Task AppearancePaletteButtonsApplyNativeChannels(EngineTestContext ctx)
        {
            var civilian = await SpawnCivilianAsync(ctx);
            await RunAssignedAsync(ctx, civilian, () =>
            {
                var editor = BindWithoutClient(civilian);
                var panel = AppearanceEditorDefinition.BuildEditorPanel(AppearanceEditorViewModel.EditorMainPartial);
                var panelJson = JObject.Parse(JsonDump(panel.ToJson()));
                foreach (var (name, texture) in new[]
                {
                    ("Skin", "gui_pal_skin"), ("Hair", "gui_pal_hair01"), ("Tattoo1", "gui_pal_tattoo"), ("Tattoo2", "gui_pal_tattoo")
                })
                {
                    AssertSwatchJson(ctx, panelJson.Descendants().OfType<JObject>()
                        .Single(node => node["id"]?.Value<string>() == "ae_body_swatch_" + name), "Body" + name, texture,
                        "Appearance body swatches");
                }
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
                    // Dispatch the body swatch action captured by the real definition; this
                    // token-zero fixture does not synthesize the client's mouse event.
                    var name = channel.ToString();
                    var swatch = WidgetTree(panel).Single(widget => widget.Id == "ae_body_swatch_" + name);
                    ctx.Assert(swatch.Events.TryGetValue("click", out var select), $"{channel}: body swatch handles click.");
                    ctx.AssertEqual(nameof(AppearanceEditorViewModel.OnClickBodySwatch), select.Method.Name,
                        $"{channel}: body swatch routes to the body colour action");
                    ctx.AssertEqual((int)channel, (int)select.Arguments.Single().Value, $"{channel}: body swatch captures its colour index");
                    ((Action)select.Method.Invoke(editor, select.Arguments.Select(argument => argument.Value).ToArray()))();
                    ctx.AssertEqual((int)channel, editor.SelectedColorCategoryIndex, $"{channel}: selected colour category");
                    ctx.AssertEqual(texture, editor.ColorSheetResref, $"{channel}: dynamic palette texture");
                    foreach (var other in new[] { "Skin", "Hair", "Tattoo1", "Tattoo2" })
                        ctx.AssertEqual(other == name, (bool)editor.GetType().GetProperty($"Body{other}Selected").GetValue(editor),
                            $"{channel}: only the selected body swatch is highlighted ({other})");
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
            ctx.SetResultDetail("All176 native button IDs, 9x7 atlas crops, letter/number headers, the bound selection outline and captured shared-action indices validated. The four body swatch buttons selected skin, hair and both tattoos with their palette sheets, highlights and codes; twelve palette actions at presets0,87,175 set exact native IDs, synchronized RGB and made no other-channel/equipment edits; invalid -1/176 were no-ops. Headless coverage excludes client mouse delivery and rendering; fixture has no helmet or cloak.");
        }

        private static IEnumerable<IGuiWidget> WidgetTree(IGuiWidget widget)
        {
            yield return widget;
            foreach (var child in widget.Elements)
                foreach (var descendant in WidgetTree(child))
                    yield return descendant;
        }

        private static PropertyInfo[] MaterialRegionProperties() => typeof(AppearanceEditorViewModel).GetProperties()
            .Where(property => property.PropertyType == typeof(GuiRectangle) && property.Name.EndsWith("Region") &&
                !property.Name.StartsWith("SelectedPart", StringComparison.Ordinal) &&
                !property.Name.StartsWith("Body", StringComparison.Ordinal))
            .ToArray();

        private static void AssertPaletteOutline(EngineTestContext ctx, AppearanceEditorViewModel editor, int colorId, string stage)
        {
            var left = colorId % 16 * AppearanceEditorViewModel.PaletteCellWidth + 1f;
            var top = colorId / 16 * AppearanceEditorViewModel.PaletteCellHeight + 1f;
            var right = left + AppearanceEditorViewModel.PaletteCellWidth - 2f;
            var bottom = top + AppearanceEditorViewModel.PaletteCellHeight - 2f;
            var expected = new[] { left, top, right, top, right, bottom, left, bottom, left, top };
            ctx.Assert(editor.PaletteSelectionPoints != null && editor.PaletteSelectionPoints.SequenceEqual(expected),
                $"{stage}: the outline surrounds palette cell {AppearancePreview.PaletteCode(colorId)}.");
        }

        private static void AssertActiveColorSwatch(EngineTestContext ctx, AppearanceEditorViewModel editor, string expected)
        {
            // The 120 Global and per-part material targets; the SelectedPart and Body swatches mirror them.
            var selectionProperties = MaterialRegionProperties()
                .Select(property => typeof(AppearanceEditorViewModel).GetProperty(property.Name[..^"Region".Length] + "Selected"))
                .ToArray();
            ctx.AssertEqual(120, selectionProperties.Length, "Every material target exposes a glow binding");
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
                ConfigureRgbArmor(outfit);
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
                ctx.AssertEqual(originalSkinId, GetColor(civilian, ColorChannel.Skin), "RGB retains the native fallback");
                ctx.AssertEqual(black.ToStoredValue(), GetLocalInt(civilian, TintMapVariable.GetCreatureColorStateName(TintMapLayerType.Skin)), "Black is persisted without quantization");
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
                ctx.AssertEqual(requested.ToStoredValue(), GetLocalInt(outfit, TintMapVariable.GetItemGlobalColorStateName(TintMapLayerType.Leather1)), "Watched picker persists exact RGB");
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
                ctx.AssertEqual(new TintMapColor(230, 35, 170).ToStoredValue(), GetLocalInt(outfit, TintMapVariable.GetItemGlobalColorStateName(TintMapLayerType.Leather1)), "Draft commit persists RGB");
                AssertArmorUnchanged(ctx, beforeDraft, civilian, "RGB commits never replace equipment or change native dyes");
                AssertPublishedPicker(ctx, publications, editor, new TintMapColor(230, 35, 170), "Committed requested RGB");
                AssertTintInput(ctx, editor, new TintMapColor(230, 35, 170), "Committed RGB input");
                AssertNoRgbFieldPublications(ctx, publications, "RGB commit leaves unchanged text buffers alone");
                AssertInheritedAndExplicitParts(ctx, outfit);
            });
            ctx.SetResultDetail("Valid RGB drafts caused no color-control echoes;300 corrected only red to255 and empty text could not commit. Skin0/0/0 and equipment RGB commits published exact picker input while retaining text buffers and persisting exact RGB without editing native dyes. Incoming NuiGetBind and debounce scheduling/open-window checks are synthesized or excluded.");
        }

        [EngineTest("Closing the editor commits only pending valid RGB text", Category = "AppearanceEditor", TimeoutSeconds = 30f)]
        public static async Task CloseCommitsRgbDraft(EngineTestContext ctx)
        {
            var civilian = await SpawnCivilianAsync(ctx);
            await RunAssignedAsync(ctx, civilian, () =>
            {
                var editor = BindWithoutClient(civilian);
                editor.OnSelectAppearance()();
                editor.CustomTintRed = "205";
                editor.CustomTintGreen = "228";
                editor.CustomTintBlue = "197";
                var stateKey = TintMapVariable.GetCreatureColorStateName(TintMapLayerType.Skin);
                editor.OnCloseWindow()();
                ctx.AssertEqual(new TintMapColor(205, 228, 197).ToStoredValue(), GetLocalInt(civilian, stateKey),
                    "Closing before the debounce applies the final complete RGB draft");
                editor.OnClickColorPalette(20)();
                editor.OnCloseWindow()();
                ctx.AssertEqual(0, GetLocalInt(civilian, stateKey),
                    "Closing a preset selection must not create a custom tint");
                editor.CustomTintRed = "";
                editor.OnCloseWindow()();
                ctx.AssertEqual(0, GetLocalInt(civilian, stateKey), "Incomplete text cannot commit on close");

                editor.CustomTintRed = "12";
                editor.CustomTintGreen = "34";
                editor.CustomTintBlue = "56";
                editor.SelectedColorCategoryIndex = 1;
                InvokePrivate(editor, "LoadTintMapEditor");
                ctx.AssertEqual(new TintMapColor(12, 34, 56).ToStoredValue(), GetLocalInt(civilian, stateKey),
                    "Selection reload commits the original skin draft after the category bind has changed");
                ctx.AssertEqual(0, GetLocalInt(civilian, TintMapVariable.GetCreatureColorStateName(TintMapLayerType.Hair)),
                    "The newly selected hair channel must not receive the old skin draft");
                editor.CustomTintRed = "120";
                editor.OnClickColorPalette(20)();
                editor.OnCloseWindow()();
                ctx.AssertEqual(0, GetLocalInt(civilian, TintMapVariable.GetCreatureColorStateName(TintMapLayerType.Hair)),
                    "A newer palette click supersedes an uncommitted RGB draft");
            });
        }

        [EngineTest("Inactive material colors survive global resets and creature materials retain authored RGB", Category = "AppearanceEditor", TimeoutSeconds = 30f)]
        public static async Task PersistedMaterialColors(EngineTestContext ctx)
        {
            var civilian = await SpawnCivilianAsync(ctx);
            await RunAssignedAsync(ctx, civilian, () =>
            {
                var outfit = GetItemInSlot(InventorySlot.Chest, civilian);
                var layer = TintMapLayerType.Cloth1;
                var key = TintMapVariable.GetName("pmh0_robe260", layer);
                var saved = new TintMapColor(100, 7, 180).ToStoredValue();
                SetLocalInt(outfit, key, saved);
                TintMapService.ResetInactiveItemCustomColor(civilian, outfit, layer, AppearanceArmor.Invalid);
                ctx.AssertEqual(saved, GetLocalInt(outfit, key),
                    "One inactive material color cannot be inferred as a legacy global set");
                var itemSelection = TintMapModelResolver.GetCurrentSelections(civilian)
                    .First(s => s.GetPaletteSource(layer) == outfit && s.Material.Layers.Contains(layer));
                var activeKey = TintMapVariable.GetName(itemSelection.Material.Resref, layer);
                SetLocalInt(outfit, activeKey, saved);
                var onePart = new[] { itemSelection, itemSelection };
                TintMapService.ResetGlobalItemCustomColor(civilian, onePart, layer);
                ctx.AssertEqual(saved, GetLocalInt(outfit, activeKey),
                    "A global preset preserves the sole independently colored active part");
                TintMapService.SetGlobalItemCustomColor(civilian, onePart, layer, new TintMapColor(1, 2, 3));
                ctx.AssertEqual(saved, GetLocalInt(outfit, activeKey),
                    "A global RGB edit preserves a sole part even when its selection is repeated");
                var selections = TintMapModelResolver.GetCurrentSelections(civilian)
                    .Where(s => s.Material.Layers.Contains(TintMapLayerType.Skin))
                    .GroupBy(s => s.Material.Resref).Select(g => g.First()).ToArray();
                ctx.Assert(selections.Length >= 2, "Fixture exposes independently named skin materials");
                TintMapService.ResetCreatureCustomColor(civilian, TintMapLayerType.Skin);
                var authored = new TintMapColor(12, 34, 56);
                var sibling = new TintMapColor(65, 43, 21);
                SetLocalInt(civilian, TintMapVariable.GetName(selections[0].Material.Resref, TintMapLayerType.Skin), authored.ToStoredValue());
                SetLocalInt(civilian, TintMapVariable.GetName(selections[1].Material.Resref, TintMapLayerType.Skin), sibling.ToStoredValue());
                TintMapService.ApplyCurrentColors(civilian);
                ctx.AssertEqual(authored, TintMapService.GetEffectiveDisplayColor(civilian, selections[0], TintMapLayerType.Skin),
                    "Authored per-material RGB participates in runtime color resolution");
                ctx.AssertEqual(sibling, TintMapService.GetEffectiveDisplayColor(civilian, selections[1], TintMapLayerType.Skin),
                    "A sibling material retains its independent color");
            });
        }

        [EngineTest("Appearance editor drag batches keep the latest RGB without picker echoes", Category = "AppearanceEditor", TimeoutSeconds = 30f)]
        public static async Task PickerDragKeepsLatestValue(EngineTestContext ctx)
        {
            var civilian = await SpawnCivilianAsync(ctx);
            await RunAssignedAsync(ctx, civilian, () =>
            {
                var item = GetItemInSlot(InventorySlot.Chest, civilian);
                SeedInheritance(item);
                ConfigureRgbArmor(item);
                var editor = new AppearanceEditorViewModel { Geometry = new GuiRectangle(0, 0, 800, 900) };
                editor.Bind(OBJECT_INVALID, 0, editor.Geometry, GuiWindowType.AppearanceEditor,
                    new AppearanceEditorPayload(civilian), OBJECT_INVALID);
                editor.OnSelectEquipment()();
                ctx.Assert(editor.IsCustomTintEditable, "The drag fixture must expose the global armor RGB channel");
                var stateName = TintMapVariable.GetItemGlobalColorStateName(TintMapLayerType.Leather1);
                var original = GetLocalInt(item, stateName);
                using var publications = new BindingPublications(editor);
                for (byte i = 0; i < 120; i++)
                    ApplyWatchedValue(editor, nameof(editor.SelectedTintColor), new GuiColor(i, 170, 43), flushPicker: false);
                ctx.AssertEqual(original, GetLocalInt(item, stateName), "Intermediate samples must not repeatedly rewrite material state");
                ctx.AssertEqual(0, publications.Count, "No intermediate picker echoes or swatch floods");
                editor.OnMouseUpTintPicker()();
                var final = new TintMapColor(119, 170, 43);
                ctx.AssertEqual(final.ToStoredValue(), GetLocalInt(item, stateName), "Release applies the last sample");
                AssertTintInput(ctx, editor, final, "Released drag");
                ctx.Assert(!publications.Contains(nameof(editor.SelectedTintColor)), "A deferred batch must not echo into the client picker");
                ctx.Assert(publications.Count <= 6, "One global color edit must not republish all 120 swatches");
                publications.Clear();
                InvokePrivate(editor, "FlushPendingPickerColor");
                ctx.AssertEqual(0, publications.Count, "An older scheduled flush cannot replay the released color");

                ApplyWatchedValue(editor, nameof(editor.SelectedTintColor), new GuiColor(240, 12, 91), flushPicker: false);
                ApplyWatchedValue(editor, nameof(editor.CustomTintRed), "1");
                InvokePrivate(editor, "FlushPendingPickerColor");
                ctx.AssertEqual(final.ToStoredValue(), GetLocalInt(item, stateName), "Typing cancels an older pending picker sample");
                InvokePrivate(editor, "CommitCustomTintComponents");
                ctx.AssertEqual(new TintMapColor(1, 170, 43).ToStoredValue(), GetLocalInt(item, stateName), "A newer typed draft wins");

                ApplyWatchedValue(editor, nameof(editor.SelectedTintColor), new GuiColor(240, 12, 91), flushPicker: false);
                editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Global, AppearanceArmorColor.Cloth1)();
                var clothName = TintMapVariable.GetItemGlobalColorStateName(TintMapLayerType.Cloth1);
                var cloth = GetLocalInt(item, clothName);
                InvokePrivate(editor, "FlushPendingPickerColor");
                ctx.AssertEqual(cloth, GetLocalInt(item, clothName), "A pending drag cannot recolor a newly selected channel");

                editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.LeftForearm, AppearanceArmorColor.Leather2)();
                var queuedPartColor = new TintMapColor(37, 121, 209);
                ApplyWatchedValue(editor, nameof(editor.SelectedTintColor),
                    new GuiColor(37, 121, 209), flushPicker: false);
                InvokePrivate(editor, "ModifyItemPart", (int)AppearanceArmor.LeftForearm, 15, -1);
                var replacement = TintMapModelResolver.GetCurrentSelections(civilian).Single(selection =>
                    selection.ArmorPart == AppearanceArmor.LeftForearm &&
                    selection.Material.Layers.Contains(TintMapLayerType.Leather2));
                ctx.AssertEqual(queuedPartColor, TintMapService.GetEffectiveDisplayColor(
                    civilian, replacement, TintMapLayerType.Leather2),
                    "A model change carries the queued picker color even without mouse-up");
                InvokePrivate(editor, "FlushPendingPickerColor");
                ctx.AssertEqual(queuedPartColor, TintMapService.GetEffectiveDisplayColor(
                    civilian, replacement, TintMapLayerType.Leather2),
                    "A late picker callback cannot revert the replacement tint");
            });
            ctx.SetResultDetail("120 samples coalesced into one exact RGB commit, at most six bindings, and no picker echo. Release/late flush, newer text input and target switches cannot replay stale colors. Incoming client events and timer advancement are synthesized.");
        }

        [EngineTest("Appearance editor persists exact RGB through native material transport and reopening", Category = "AppearanceEditor", TimeoutSeconds = 30f)]
        public static async Task ExactRgbInputSurvivesEditsNavigationAndResize(EngineTestContext ctx)
        {
            var civilian = await SpawnCivilianAsync(ctx);
            await RunAssignedAsync(ctx, civilian, () =>
            {
                var outfit = GetItemInSlot(InventorySlot.Chest, civilian);
                SeedInheritance(outfit);
                ConfigureRgbArmor(outfit);
                var before = ReadArmor(civilian);
                var editor = BindWithoutClient(civilian);
                editor.OnSelectEquipment()();
                editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Global, AppearanceArmorColor.Leather2)();
                ctx.Assert(editor.IsCustomTintEditable, "The reported ordinary armor layer supports RGB.");
                var green = new TintMapColor(205, 228, 197);
                ApplyWatchedValue(editor, nameof(editor.SelectedTintColor), new GuiColor(205, 228, 197));
                var globalName = TintMapVariable.GetItemGlobalColorStateName(TintMapLayerType.Leather2);
                ctx.AssertEqual(green.ToStoredValue(), GetLocalInt(outfit, globalName), "Global green is saved without selecting a tan preset");
                var forearm = TintMapModelResolver.GetCurrentSelections(civilian).Single(selection =>
                    selection.ArmorPart == AppearanceArmor.LeftForearm && selection.Material.Layers.Contains(TintMapLayerType.Leather2));
                AssertTintInput(ctx, editor, green, "Reported pale green");
                AssertSwatchRgb(ctx, editor.GlobalLeather2Tint, green, "Global swatch");
                ctx.Assert(editor.GlobalLeather2Custom, "Global RGB enables the preview fill over the native preset.");
                TintMapEngineTests.AssertNativeRgb(ctx, civilian, forearm.Material.Resref, TintMapLayerType.Leather2, green);
                AssertArmorUnchanged(ctx, before, civilian, "Global RGB leaves item identity, models, native dyes and markers unchanged");

                editor = BindWithoutClient(civilian);
                editor.OnSelectEquipment()();
                editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Global, AppearanceArmorColor.Leather2)();
                AssertTintInput(ctx, editor, green, "New editor loads persisted RGB without a session cache");
                editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.LeftForearm, AppearanceArmorColor.Leather2)();
                AssertTintInput(ctx, editor, green, "Inherited part previews exact global RGB");
                AssertInheritedRegion(ctx, editor.LeftForearmLeather2Region, "Inherited RGB part has an empty swatch");
                var partColor = new TintMapColor(1, 17, 91);
                ApplyWatchedValue(editor, nameof(editor.SelectedTintColor), new GuiColor(200, 17, 91));
                ApplyWatchedValue(editor, nameof(editor.CustomTintRed), "1");
                InvokePrivate(editor, "CommitCustomTintComponents");
                AssertTintInput(ctx, editor, partColor, "R1 remains one");
                AssertSwatchRgb(ctx, editor.LeftForearmLeather2Tint, partColor, "Part swatch");
                ctx.Assert(editor.LeftForearmLeather2Custom, "The explicit RGB override has a filled swatch.");
                TintMapEngineTests.AssertNativeRgb(ctx, civilian, forearm.Material.Resref, TintMapLayerType.Leather2, partColor);
                var laterGlobal = new TintMapColor(13, 149, 71);
                editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Global, AppearanceArmorColor.Leather2)();
                ApplyWatchedValue(editor, nameof(editor.SelectedTintColor), new GuiColor(13, 149, 71));
                TintMapEngineTests.AssertNativeRgb(ctx, civilian, forearm.Material.Resref, TintMapLayerType.Leather2, partColor);
                editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.LeftForearm, AppearanceArmorColor.Leather2)();
                AssertTintInput(ctx, editor, partColor, "Global change preserves explicit part RGB");
                using (var publications = new BindingPublications(editor))
                {
                    editor.Geometry = new GuiRectangle(0, 0, 1440, 960);
                    publications.Clear();
                    InvokePrivate(editor, "OnClientPropertyUpdated", nameof(editor.Geometry));
                    ctx.AssertEqual(0, publications.Count, "Resize never rebuilds or republishes bindings");
                    AssertTintInput(ctx, editor, partColor, "Resize preserves part RGB");
                }
                editor.CustomTintRed = "120";
                InvokePrivate(editor, "ResetArmorColorToInheritance",
                    AppearanceEditorViewModel.ColorTarget.LeftForearm, AppearanceArmorColor.Leather2);
                AssertTintInput(ctx, editor, laterGlobal, "Inheritance reset supersedes pending RGB text");
                TintMapEngineTests.AssertNativeRgb(ctx, civilian, forearm.Material.Resref, TintMapLayerType.Leather2, laterGlobal);
                editor.OnMouseDownTintPicker()();
                editor.SelectedTintColor = new GuiColor(120, 30, 60);
                InvokePrivate(editor, "ResetArmorColorToInheritance",
                    AppearanceEditorViewModel.ColorTarget.LeftForearm, AppearanceArmorColor.Leather2);
                InvokePrivate(editor, "FlushPendingPickerColor");
                AssertTintInput(ctx, editor, laterGlobal, "Inheritance reset supersedes pending picker input");
                TintMapEngineTests.AssertNativeRgb(ctx, civilian, forearm.Material.Resref, TintMapLayerType.Leather2, laterGlobal);
                editor.OnClickColorPalette(77)();
                AssertTintInput(ctx, editor, TintMapPaletteColors.GetColor(TintMapLayerType.Leather2, 77), "Part preset opts out of global RGB");
                ctx.Assert(!editor.LeftForearmLeather2Custom, "Preset replaces the part RGB fill.");
                editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Global, AppearanceArmorColor.Leather2)();
                AssertTintInput(ctx, editor, laterGlobal, "Part preset retains global RGB");
                editor.OnClickColorPalette(66)();
                ctx.AssertEqual(0, GetLocalInt(outfit, globalName), "Global preset clears persisted RGB");
                ctx.Assert(!editor.GlobalLeather2Custom, "Global swatch returns to preset mode.");

                editor.OnSelectAppearance()();
                var originalSkin = GetColor(civilian, ColorChannel.Skin);
                ApplyWatchedValue(editor, nameof(editor.SelectedTintColor), new GuiColor(7, 88, 159));
                var skin = new TintMapColor(7, 88, 159);
                ctx.AssertEqual(originalSkin, GetColor(civilian, ColorChannel.Skin), "Skin RGB retains native fallback");
                TintMapEngineTests.AssertNativeRgb(ctx, civilian, string.Empty, TintMapLayerType.Skin, skin);
                editor = BindWithoutClient(civilian);
                AssertTintInput(ctx, editor, skin, "New editor restores creature RGB");

                EquippedItemAppearance.Set(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe, 187);
                editor.OnSelectEquipment()();
                editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Robe, AppearanceArmorColor.Cloth1)();
                ctx.Assert(editor.IsCustomTintAvailable && editor.IsCustomTintEditable,
                    "Converted robes expose the same exact RGB editor as other armor parts.");
                var beforeRobe = ReadArmor(civilian);
                ApplyWatchedValue(editor, nameof(editor.SelectedTintColor), new GuiColor(205, 228, 197));
                AssertArmorUnchanged(ctx, beforeRobe, civilian, "Robe RGB retains all item models and native palette fields");
                var robe = TintMapModelResolver.GetCurrentSelections(civilian).Single(selection =>
                    selection.ArmorPart == AppearanceArmor.Robe && selection.Material.Layers.Contains(TintMapLayerType.Cloth1));
                TintMapEngineTests.AssertNativeRgb(ctx, civilian, robe.Material.Resref, TintMapLayerType.Cloth1,
                    new TintMapColor(205, 228, 197));
                ctx.Assert((int)GetPhenoType(civilian) >= 34, "RGB installs the robe-bearing body root.");

                editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Global, AppearanceArmorColor.Cloth1)();
                using var swatchPublications = new BindingPublications(editor);
                var purple = new TintMapColor(100, 7, 180);
                ApplyWatchedValue(editor, nameof(editor.SelectedTintColor), new GuiColor(100, 7, 180));
                AssertSwatchRgb(ctx, editor.GlobalCloth1Tint, purple, "Reported purple global Cloth1 preview");
                ctx.Assert(editor.GlobalCloth1Custom, "Custom purple replaces the gold preset preview.");
                ctx.Assert(swatchPublications.Contains(nameof(editor.GlobalCloth1Tint)) &&
                           swatchPublications.Contains(nameof(editor.GlobalCloth1Custom)),
                    "The client receives both the exact color and the enabled RGB fill.");
            });
            ctx.SetResultDetail("Reported pale green and R1 persisted exactly through native scalar material uploads, fresh editor hydration, inheritance, explicit part overrides and reset/preset actions. RGB leaves equipped item identity and native dyes unchanged. Headless tests exclude client NUI event transport and rendering.");
        }

        [EngineTest("Stock robe3 accepts global RGB and independent robe overrides", Category = "AppearanceEditor", TimeoutSeconds = 60f)]
        public static async Task StockRobeColorsAndInheritance(EngineTestContext ctx)
        {
            foreach (var gender in new[] { Gender.Male, Gender.Female })
            {
                var civilian = await SpawnCivilianAsync(ctx);
                await RunAssignedAsync(ctx, civilian, () =>
                {
                    SetGender(civilian, gender);
                    var outfit = GetItemInSlot(InventorySlot.Chest, civilian);
                    EquippedItemAppearance.Set(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe, 3);
                    var channels = new[]
                    {
                        (AppearanceArmorColor.Leather1, TintMapLayerType.Leather1),
                        (AppearanceArmorColor.Cloth1, TintMapLayerType.Cloth1),
                        (AppearanceArmorColor.Cloth2, TintMapLayerType.Cloth2),
                        (AppearanceArmorColor.Metal1, TintMapLayerType.Metal1),
                        (AppearanceArmorColor.Metal2, TintMapLayerType.Metal2)
                    };
                    foreach (var (channel, _) in channels)
                    {
                        EquippedItemAppearance.Set(outfit, ItemAppearanceType.ArmorColor,
                            ArmorColorIndexCalculator.CalculatePerPart(AppearanceArmor.Robe, channel), 255);
                        DeleteLocalInt(outfit, ArmorColorIndexCalculator.GetPerPartOverrideVariableName(AppearanceArmor.Robe, channel));
                    }
                    var before = ReadArmor(civilian);
                    var editor = BindWithoutClient(civilian);
                    editor.OnSelectEquipment()();
                    foreach (var (channel, layer) in channels)
                    {
                        var robe = TintMapModelResolver.GetCurrentSelections(civilian).Single(selection =>
                            selection.ArmorPart == AppearanceArmor.Robe && selection.Material.Layers.Contains(layer));
                        ctx.AssertEqual(gender == Gender.Male ? "pmh0_robe003" : "pfh0_robe003", robe.ModelResref,
                            "Stock robe selection resolves the correct sex and keeps its base model after phenotype changes");
                        editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Global, channel)();
                        ApplyWatchedValue(editor, nameof(editor.SelectedTintColor), new GuiColor(100, 7, 180));
                        TintMapEngineTests.AssertNativeRgb(ctx, civilian, robe.Material.Resref, layer, new TintMapColor(100, 7, 180));
                        ctx.Assert((int)GetPhenoType(civilian) >= 34, "Stock robe RGB activates the converted body root");
                        AssertArmorUnchanged(ctx, before, civilian, "Global RGB preserves equipped item and native appearance");

                        editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Robe, channel)();
                        ctx.Assert(editor.IsCustomTintEditable, $"Robe3/{channel} exposes individual RGB editing");
                        ApplyWatchedValue(editor, nameof(editor.SelectedTintColor), new GuiColor(205, 228, 197));
                        TintMapEngineTests.AssertNativeRgb(ctx, civilian, robe.Material.Resref, layer, new TintMapColor(205, 228, 197));
                        var prefix = "Robe" + channel;
                        ctx.Assert((bool)editor.GetType().GetProperty(prefix + "Custom").GetValue(editor), "Individual robe RGB has a preview");
                        AssertSwatchRgb(ctx, (GuiColor)editor.GetType().GetProperty(prefix + "Tint").GetValue(editor),
                            new TintMapColor(205, 228, 197), "Individual robe preview matches the applied value");
                        editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Global, channel)();
                        ApplyWatchedValue(editor, nameof(editor.SelectedTintColor), new GuiColor(167, 219, 3));
                        TintMapEngineTests.AssertNativeRgb(ctx, civilian, robe.Material.Resref, layer, new TintMapColor(205, 228, 197));
                        InvokePrivate(editor, "ResetArmorColorToInheritance", AppearanceEditorViewModel.ColorTarget.Robe, channel);
                        TintMapEngineTests.AssertNativeRgb(ctx, civilian, robe.Material.Resref, layer, new TintMapColor(167, 219, 3));
                        editor = BindWithoutClient(civilian);
                        editor.OnSelectEquipment()();
                        editor.OnClickColorTarget(AppearanceEditorViewModel.ColorTarget.Robe, channel)();
                        AssertTintInput(ctx, editor, new TintMapColor(167, 219, 3), "Reopening retains inherited stock robe RGB");
                    }
                });
            }
            ctx.SetResultDetail("Male and female stock robe3: all five authored dye layers receive exact native RGB. Individual robe colors have matching previews, survive subsequent global edits, and reset to inherited RGB across editor reopen. Client rendering is checked separately.");
        }

        private static void ConfigureRgbArmor(uint outfit)
        {
            EquippedItemAppearance.Set(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe, 0);
            EquippedItemAppearance.Set(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso, 98);
            EquippedItemAppearance.Set(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftForearm, 6);
            EquippedItemAppearance.Set(outfit, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightForearm, 6);
            foreach (var part in new[] { AppearanceArmor.LeftForearm, AppearanceArmor.RightForearm })
            {
                EquippedItemAppearance.Set(outfit, ItemAppearanceType.ArmorColor,
                    ArmorColorIndexCalculator.CalculatePerPart(part, AppearanceArmorColor.Leather2), 255);
                DeleteLocalInt(outfit, ArmorColorIndexCalculator.GetPerPartOverrideVariableName(part, AppearanceArmorColor.Leather2));
            }
        }

        private static void AssertSwatchRgb(EngineTestContext ctx, GuiColor actual, TintMapColor expected, string stage)
        {
            ctx.AssertEqual(expected.Red, actual.R, stage + " red");
            ctx.AssertEqual(expected.Green, actual.G, stage + " green");
            ctx.AssertEqual(expected.Blue, actual.B, stage + " blue");
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

        private static void ApplyWatchedValue(AppearanceEditorViewModel editor, string propertyName, object value, Action beforeCallback = null,
            bool flushPicker = true, bool userGesture = true)
        {
            if (userGesture && propertyName == nameof(editor.SelectedTintColor))
                editor.OnMouseDownTintPicker()();

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
            // Advance the picker batch explicitly: this headless fixture has no open
            // player window for the production timer's lifecycle guard.
            if (flushPicker && propertyName == nameof(editor.SelectedTintColor))
                InvokePrivate(editor, "FlushPendingPickerColor");
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
                var editor = new AppearanceEditorViewModel { Geometry = new GuiRectangle(0, 0, 1240, 720) };
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
                    AssertStaticPanelLayout(ctx, originalPanel, isArmor, partialName);

                    // The editor has a fixed size; a geometry report (a move, or a stale saved size)
                    // must not rebuild or republish anything.
                    foreach (var (width, height) in new[]
                    {
                        (1240f, 720f), (1600f, 900f), (900f, 600f)
                    })
                    {
                        var stage = $"{partialName} at {width}x{height}";
                        editor.Geometry = new GuiRectangle(0, 0, width, height);
                        publications.Clear();
                        // Exercise the actual completion callback after assigning geometry. The viewer
                        // is invalid: this is not incoming NuiGetBind transport or proof of client
                        // layout behavior at these dimensions.
                        InvokePrivate(editor, "OnClientPropertyUpdated", nameof(editor.Geometry));
                        ctx.AssertEqual(0, publications.Count, $"{stage}: geometry must not redeliver any binding immediately");
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
            ctx.SetResultDetail("Six actual Geometry completion callbacks at 1240x720, 1600x900 and 900x600 published no bindings and retained cached inputs. Static native panel JSON/event IDs stayed identical: fixed map and colour columns with the part picture, bound body-map slots, palette and swatches. All 19 armor models, 120 armor colors, 4 creature colors, APC markers, TMP baselines and pending tint locals stayed unchanged. Headless coverage excludes client Geometry transport and rendered layout.");
        }

        private static void AssertStaticPanelLayout(EngineTestContext ctx, JObject panel, bool isArmor, string stage)
        {
            ctx.Assert(panel["width"] == null && panel["height"] == null,
                $"{stage}: the static viewport must not retain a fixed window size.");
            var objects = new[] { panel }.Concat(panel.Descendants().OfType<JObject>()).ToArray();
            var ids = objects.Where(node => node["id"] != null).Select(node => node["id"].Value<string>()).ToArray();
            ctx.Assert(ids.Length > 0 && ids.Distinct().Count() == ids.Length, $"{stage}: native control IDs must be unique.");

            // Body map | colours, both fixed; the selected part's picture sits in the colours panel.
            var preview = objects.Single(node => node["id"]?.Value<string>() == "ae_part_preview");
            var colours = preview.Ancestors().OfType<JObject>().First(node => node["type"]?.Value<string>() == "group");
            var contentRow = colours.Ancestors().OfType<JObject>().First(node => node["type"]?.Value<string>() == "row");
            var columns = contentRow["children"].OfType<JObject>().ToArray();
            ctx.AssertEqual(2, columns.Length, $"{stage}: body map and colours share one row");
            ctx.AssertEqual(AppearanceEditorDefinition.MapWidth, columns[0]["width"]?.Value<float>(), $"{stage}: body map width");
            ctx.AssertEqual(AppearanceEditorDefinition.ColoursWidth, columns[1]["width"]?.Value<float>(), $"{stage}: colour column width");
            ctx.Assert(ReferenceEquals(columns[1], colours), $"{stage}: the part picture belongs to the colours panel.");
            ctx.Assert(!objects.Any(node => node["id"]?.Value<string>()?.StartsWith("ae_tile_", StringComparison.Ordinal) == true),
                $"{stage}: the part pictures are browsed in their own window.");

            var slots = objects.Where(node => node["id"]?.Value<string>()?.StartsWith("ae_slot_", StringComparison.Ordinal) == true).ToArray();
            ctx.AssertEqual(isArmor ? 21 : 15, slots.Length, $"{stage}: body map slot count");
            foreach (var slot in slots)
            {
                var name = slot["id"].Value<string>()["ae_slot_".Length..];
                ctx.AssertEqual($"Map{name}Image", slot["label"]?["bind"]?.Value<string>(), $"{stage}/{name}: slot picture binding");
                ctx.AssertEqual($"Map{name}Selected", slot["encouraged"]?["bind"]?.Value<string>(), $"{stage}/{name}: slot highlight");
                var outline = slot["draw_list"]?.Children<JObject>().Single();
                ctx.AssertEqual($"Map{name}Selected", outline?["enabled"]?["bind"]?.Value<string>(), $"{stage}/{name}: slot outline");
            }

            var picker = objects.Single(node => node["type"]?.Value<string>() == "color_picker");
            ctx.AssertEqual("ae_tint_picker", picker["id"]?.Value<string>(), $"{stage}: picker event ID");
            ctx.AssertEqual(nameof(AppearanceEditorViewModel.SelectedTintColor), picker["value"]?["bind"]?.Value<string>(),
                $"{stage}: picker binding");
            ctx.Assert(picker.Ancestors().OfType<JObject>().All(node => node["id"]?.Value<string>() != "ae_color_palette"),
                $"{stage}: the picker sits outside the fixed-size palette group.");
            AssertAppearancePaletteJson(ctx, objects.Single(node => node["id"]?.Value<string>() == "ae_color_palette"), stage);
            if (isArmor)
                AssertEquipmentSwatchesJson(ctx, panel, stage);
        }

        private static void AssertEquipmentSwatchesJson(EngineTestContext ctx, JObject panel, string stage)
        {
            var objects = panel.Descendants().OfType<JObject>().ToArray();
            var swatches = objects.Where(node => node["id"]?.Value<string>()?.Contains("_swatch_", StringComparison.Ordinal) == true).ToArray();
            ctx.AssertEqual(12, swatches.Length, $"{stage}: six base and six selected-part swatches");
            ctx.Assert(!objects.Any(node => node["id"]?.Value<string>()?.StartsWith("ae_color_", StringComparison.Ordinal) == true &&
                    node["id"].Value<string>() != "ae_color_palette"),
                $"{stage}: the 120 per-target swatches were replaced by the selected-part row.");
            foreach (var channel in new[]
            {
                AppearanceArmorColor.Leather1, AppearanceArmorColor.Cloth1, AppearanceArmorColor.Metal1,
                AppearanceArmorColor.Leather2, AppearanceArmorColor.Cloth2, AppearanceArmorColor.Metal2
            })
            {
                var texture = channel is AppearanceArmorColor.Metal1 or AppearanceArmorColor.Metal2 ? "gui_pal_armor01" : "gui_pal_tattoo";
                AssertSwatchJson(ctx, swatches.Single(node => node["id"].Value<string>() == "ae_base_swatch_" + channel),
                    "Global" + channel, texture, stage);
                AssertSwatchJson(ctx, swatches.Single(node => node["id"].Value<string>() == "ae_part_swatch_" + channel),
                    "SelectedPart" + channel, texture, stage);
            }
        }

        private static void AssertSwatchJson(EngineTestContext ctx, JObject swatch, string name, string texture, string stage)
        {
            ctx.AssertEqual("button", swatch["type"]?.Value<string>(), $"{stage}/{name}: swatch is a button");
            ctx.AssertEqual(name + "Selected", swatch["encouraged"]?["bind"]?.Value<string>(), $"{stage}/{name}: swatch glow");
            var drawings = swatch["draw_list"].Children<JObject>().ToArray();
            ctx.AssertEqual(3, drawings.Length, $"{stage}/{name}: palette crop, RGB fill and selection outline");

            ctx.AssertEqual((int)NuiDrawListItemType.Image, drawings[0]["type"].Value<int>(), $"{stage}/{name}: crop drawn first");
            ctx.AssertEqual(texture, drawings[0]["image"]?.Value<string>(), $"{stage}/{name}: palette sheet");
            ctx.AssertEqual(name + "Region", drawings[0]["image_region"]?["bind"]?.Value<string>(), $"{stage}/{name}: crop binding");

            var fill = drawings[1];
            ctx.AssertEqual((int)NuiDrawListItemType.PolyLine, fill["type"].Value<int>(), $"{stage}/{name}: RGB polygon");
            ctx.AssertEqual(name + "Tint", fill["color"]?["bind"]?.Value<string>(), $"{stage}/{name}: effective color binding");
            ctx.AssertEqual(name + "Custom", fill["enabled"]?["bind"]?.Value<string>(), $"{stage}/{name}: custom mode binding");
            ctx.AssertEqual(true, fill["fill"]?.Value<bool>(), $"{stage}/{name}: RGB polygon is filled");
            var points = fill["points"] as JArray;
            ctx.Assert(points != null && points.Count == 8 && points.All(point => point.Type == JTokenType.Float),
                $"{stage}/{name}: NUI requires eight flat float coordinates, not four vector objects.");

            var outline = drawings[2];
            ctx.AssertEqual((int)NuiDrawListItemType.PolyLine, outline["type"].Value<int>(), $"{stage}/{name}: outline");
            ctx.AssertEqual(name + "Selected", outline["enabled"]?["bind"]?.Value<string>(), $"{stage}/{name}: outline binding");
            ctx.AssertEqual(false, outline["fill"]?.Value<bool>(), $"{stage}/{name}: outline is not filled");
            ctx.AssertEqual(10, (outline["points"] as JArray)?.Count, $"{stage}/{name}: closed rectangle outline");

            ctx.Assert(!swatch.Parent.Children<JObject>().Any(node => (node["value"] as JObject)?["bind"]?.Value<string>() == name + "Code"),
                $"{stage}/{name}: no palette code label beside the swatch; the palette outline shows the colour.");
        }

        private static void AssertAppearancePaletteJson(EngineTestContext ctx, JObject palette, string stage)
        {
            const float cellWidth = AppearanceEditorViewModel.PaletteCellWidth;
            const float cellHeight = AppearanceEditorViewModel.PaletteCellHeight;
            ctx.AssertEqual("group", palette["type"]?.Value<string>(), $"{stage}: palette is a button group");
            ctx.AssertEqual(16 * cellWidth, palette["width"]?.Value<float>(), $"{stage}: palette grid width");
            ctx.AssertEqual(11 * cellHeight, palette["height"]?.Value<float>(), $"{stage}: palette grid height");
            ctx.AssertEqual(nameof(AppearanceEditorViewModel.IsColorPickerVisible), palette["visible"]?["bind"]?.Value<string>(),
                $"{stage}: palette visibility");

            var outline = palette["draw_list"]?.Children<JObject>().Single();
            ctx.AssertEqual((int)NuiDrawListItemType.PolyLine, outline?["type"]?.Value<int>(), $"{stage}: one selection outline");
            ctx.AssertEqual(nameof(AppearanceEditorViewModel.PaletteSelectionPoints), outline?["points"]?["bind"]?.Value<string>(),
                $"{stage}: outline points follow the selected colour");
            ctx.AssertEqual(nameof(AppearanceEditorViewModel.IsPaletteSelectionVisible), outline?["enabled"]?["bind"]?.Value<string>(),
                $"{stage}: outline hides for custom or inherited colours");

            var column = palette["children"].OfType<JObject>().Single();
            ctx.AssertEqual("col", column["type"]?.Value<string>(), $"{stage}: palette has one column of rows");
            var rows = column["children"].OfType<JObject>().ToArray();
            ctx.AssertEqual(11, rows.Length, $"{stage}: eleven colour rows with no letter or number headers");

            var buttonIndex = 0;
            for (var y = 0; y < 11; y++)
            {
                var row = rows[y];
                ctx.AssertEqual("row", row["type"]?.Value<string>(), $"{stage}: palette row type");
                ctx.AssertEqual(cellHeight, row["height"]?.Value<float>(), $"{stage}: palette row height");
                ctx.AssertEqual(0f, row["margin"]?.Value<float>(), $"{stage}: palette row has no hidden margins");
                var cells = row["children"].OfType<JObject>().ToArray();
                ctx.AssertEqual(16, cells.Length, $"{stage}: sixteen colours");
                foreach (var button in cells)
                {
                    var index = buttonIndex++;
                    ctx.AssertEqual("button", button["type"]?.Value<string>(), $"{stage}/{index}: preset is independently clickable");
                    ctx.AssertEqual("ae_palette_" + index, button["id"]?.Value<string>(), $"{stage}/{index}: deterministic preset event ID");
                    ctx.AssertEqual(cellWidth, button["width"]?.Value<float>(), $"{stage}/{index}: button width");
                    ctx.AssertEqual(cellHeight, button["height"]?.Value<float>(), $"{stage}/{index}: button height");
                    ctx.AssertEqual(0f, button["margin"]?.Value<float>(), $"{stage}/{index}: button margin");
                    var image = button.Descendants().OfType<JObject>().Single(node =>
                        node["type"]?.Type == JTokenType.Integer &&
                        node["type"].Value<int>() == (int)NuiDrawListItemType.Image);
                    ctx.AssertEqual(nameof(AppearanceEditorViewModel.ColorSheetResref), image["image"]?["bind"]?.Value<string>(),
                        $"{stage}/{index}: button draws the selected skin/hair/tattoo/equipment palette");
                    var region = image["image_region"];
                    ctx.AssertEqual((float)(index % 16 * 16 + 3), region?["x"]?.Value<float>(), $"{stage}/{index}: native atlas column");
                    ctx.AssertEqual((float)(index / 16 * 16 + 4), region?["y"]?.Value<float>(), $"{stage}/{index}: native atlas row");
                    ctx.AssertEqual(9f, region?["w"]?.Value<float>(), $"{stage}/{index}: 9x7 crop drawn at 2x keeps even pixel columns");
                    ctx.AssertEqual(7f, region?["h"]?.Value<float>(), $"{stage}/{index}: crop stays inside one color cell");
                }
            }
            ctx.AssertEqual(176, buttonIndex, $"{stage}: no missing or extra native palette IDs");
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

            var regions = MaterialRegionProperties();
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
                editor.OnClickColorPalette(selected)();
                AssertNativeColor(ctx, outfit, (int)AppearanceArmorColor.Leather1, selected, "Global preset picker");
                AssertInheritedAndExplicitParts(ctx, outfit);
                AssertPaletteRegion(ctx, editor.GlobalLeather1Region, selected, "Global swatch after preset picker");

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
            ctx.SetResultDetail("Actual VM preset actions edited global and robe dyes independently. Native raw0/raw255 inherited parts stayed unset; APC-marked palette0 survived a later global edit and tab rehydration. No client renderer or mouse-event transport is attached.");
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
