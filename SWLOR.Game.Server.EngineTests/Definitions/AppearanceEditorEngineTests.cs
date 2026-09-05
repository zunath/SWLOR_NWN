using System.Linq;
using System.Threading.Tasks;
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
    public static class AppearanceEditorEngineTests
    {
        private sealed record ArmorSnapshot(uint Item, int[] Models, int[] Colors, int[] Markers, int[] Projections);

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
