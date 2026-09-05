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
                foreach (var (part, target) in ArmorColorTargets())
                foreach (var (channel, layer) in channels)
                {
                    editor.OnClickColorTarget(target, channel)();
                    ctx.Assert(editor.IsCustomTintAvailable,
                        $"{part}/{channel} must retain its native picker even without a generated tint selection.");
                    var mode = ((int)part + (int)channel) % 4;
                    var expectedId = mode switch { 2 => 0, 3 => 77, _ => 35 + (int)channel };
                    AssertPickerColor(ctx, editor, TintMapPaletteColors.GetColor(layer, expectedId), $"{part}/{channel} native preview");
                }
                AssertArmorUnchanged(ctx, snapshot, civilian, "all114 part dye target selections");
            });
            ctx.SetResultDetail("All19×6 native armor targets retained a picker, including Chest70. Independent seeded raw255/raw0/explicit0/explicit77 cases displayed inherited or explicit native palette colors without armor writes. Headless VM coverage only.");
        }

        [EngineTest("Appearance editor watched color correction preserves RGB drafts until commit", Category = "AppearanceEditor", TimeoutSeconds = 30f)]
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
                    ctx.AssertEqual("0", publications.Latest(ctx, property, "Black skin draft").Value<string>(),
                        "Each watched skin RGB field must retain and publish the zero draft.");
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
                AssertPublishedColor(ctx, publications, editor, black, "Committed black skin RGB");
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
                AssertPublishedColor(ctx, publications, editor, canonical, "Post-client palette correction");

                var beforeDraft = ReadArmor(civilian);
                foreach (var (property, value) in new[]
                {
                    (nameof(editor.CustomTintRed), "2"), (nameof(editor.CustomTintRed), "230"),
                    (nameof(editor.CustomTintGreen), "35"), (nameof(editor.CustomTintBlue), "170")
                })
                {
                    publications.Clear();
                    ApplyWatchedValue(editor, property, value);
                    ctx.AssertEqual(value, publications.Latest(ctx, property, "RGB draft publication").Value<string>(),
                        "The actively edited RGB field must be published after SkipNotify ends.");
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
                canonical = TintMapPaletteColors.GetColor(TintMapLayerType.Leather1, selected);
                AssertNativeColor(ctx, outfit, (int)AppearanceArmorColor.Leather1, selected, "Committed RGB native dye");
                AssertPublishedColor(ctx, publications, editor, canonical, "Committed RGB palette correction");
                AssertInheritedAndExplicitParts(ctx, outfit);
            });
            ctx.SetResultDetail("Watched skin RGB0/0/0 preserved native color until commit, then selected black skin57 and published four zero-valued controls. Equipment picker correction and sequential RGB drafts also synchronized native dyes/controls after SkipNotify without premature writes. Incoming NuiGetBind and debounce scheduling/open-window checks are synthesized or excluded.");
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

        private static void AssertPublishedColor(EngineTestContext ctx, BindingPublications values,
            AppearanceEditorViewModel editor, TintMapColor color, string stage)
        {
            AssertPickerColor(ctx, editor, color, stage);
            var picker = values.Latest(ctx, nameof(editor.SelectedTintColor), stage);
            ctx.AssertEqual((int)color.Red, picker["r"].Value<int>(), $"{stage}: published picker red");
            ctx.AssertEqual((int)color.Green, picker["g"].Value<int>(), $"{stage}: published picker green");
            ctx.AssertEqual((int)color.Blue, picker["b"].Value<int>(), $"{stage}: published picker blue");
            foreach (var (name, expected) in new[]
            {
                (nameof(editor.CustomTintRed), color.Red), (nameof(editor.CustomTintGreen), color.Green),
                (nameof(editor.CustomTintBlue), color.Blue)
            })
                ctx.AssertEqual(expected.ToString(), values.Latest(ctx, name, stage).Value<string>(), $"{stage}: {name}");
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

        [EngineTest("Appearance editor resizing preserves bindings and native appearance", Category = "AppearanceEditor", TimeoutSeconds = 30f)]
        public static async Task ResizedPanelsPreserveBindingsAndNativeAppearance(EngineTestContext ctx)
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
                    InvokePrivate(editor, "OnEditorPartialApplied");
                    var expectedBindings = publications.SnapshotWithoutGeometry();
                    string[] originalIds = null;
                    var partialName = isArmor ? AppearanceEditorViewModel.EditorArmorPartial : AppearanceEditorViewModel.EditorMainPartial;

                    foreach (var (width, height, contentWidth, listHeight) in new[]
                    {
                        (590f, 740f, 530f, 210f), (1440f, 960f, 1380f, 368f), (590f, 520f, 530f, 210f)
                    })
                    {
                        var stage = $"{partialName} at {width}x{height}";
                        publications.Clear();
                        editor.Geometry = new GuiRectangle(0, 0, width, height);
                        // Run the actual resize-apply body. The invalid viewer cannot deliver a
                        // client Geometry event or pass the debouncer's open-window guard.
                        InvokePrivate(editor, "OnEditorPartialApplied");
                        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                        ctx.AssertEqual(partialName, typeof(AppearanceEditorViewModel).GetField("_appliedEditorPartial", flags).GetValue(editor),
                            $"{stage}: the active panel must be rebuilt");
                        ctx.AssertEqual(contentWidth, (float)typeof(AppearanceEditorViewModel).GetField("_appliedEditorContentWidth", flags).GetValue(editor),
                            $"{stage}: the actual VM apply must use the new width");
                        foreach (var (name, expected) in expectedBindings)
                            ctx.Assert(JToken.DeepEquals(expected, publications.AfterLayout(ctx, name, stage)),
                                $"{stage}: cached {name} must be republished unchanged after the resized layout.");
                        if (isArmor) AssertArmorPublications(ctx, publications, armor, stage);
                        else AssertAppearancePublications(ctx, publications, stage);

                        // Native serialization of the same production panel builder catches
                        // malformed wire JSON and boot-size controls surviving a resize.
                        var panel = JObject.Parse(JsonDump(AppearanceEditorDefinition.BuildEditorPanel(partialName, width, height).ToJson()));
                        ctx.AssertEqual((int)NuiScrollbars.Auto, panel["scrollbars"].Value<int>(), $"{stage}: actual viewport scroll policy");
                        ctx.Assert(panel["width"] == null && panel["height"] == null,
                            $"{stage}: the viewport must remain unconstrained by the content dimensions.");
                        ctx.AssertEqual(contentWidth, panel["children"][0]["width"].Value<float>(), $"{stage}: serialized inner content width");
                        var objects = new[] { panel }.Concat(panel.Descendants().OfType<JObject>()).ToArray();
                        if (!isArmor)
                        {
                            var parts = objects.Single(node => node["type"]?.Value<string>() == "list" &&
                                node["row_count"]?["bind"]?.Value<string>() == nameof(editor.PartOptions) + "_RowCount");
                            ctx.AssertEqual(listHeight, parts["height"].Value<float>(), $"{stage}: native serialized part-list height");
                        }
                        var ids = objects.Where(node => node["id"] != null).Select(node => node["id"].Value<string>()).OrderBy(id => id).ToArray();
                        ctx.Assert(ids.Length > 0 && ids.Distinct().Count() == ids.Length, $"{stage}: control IDs must be nonempty and unique.");
                        if (originalIds == null) originalIds = ids;
                        else ctx.Assert(originalIds.SequenceEqual(ids), $"{stage}: resized controls must retain the boot-registered event IDs.");

                        AssertArmorUnchanged(ctx, armor, civilian, stage);
                        ctx.Assert(creatureColors.SequenceEqual(Enum.GetValues<ColorChannel>().Select(channel => GetColor(civilian, channel))),
                            $"{stage}: all four native creature colors must remain unchanged.");
                        ctx.AssertEqual(savedItemTint, GetLocalInt(outfit, savedItemTintName), $"{stage}: pending item tint");
                        ctx.AssertEqual(savedCreatureTint, GetLocalInt(civilian, savedCreatureTintName), $"{stage}: pending creature tint");
                    }
                }
            });
            ctx.SetResultDetail("Public Bind plus six actual resize-apply calls serialized appearance/armor panels at590x740,1440x960,590x520, retained stable control IDs, and republished identical non-geometry bindings. All19 armor models,120 armor colors,4 creature colors,APC markers,TMP baselines and pending tint locals stayed unchanged. Headless test excludes client Geometry delivery, debounce timing and rendered layout.");
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
