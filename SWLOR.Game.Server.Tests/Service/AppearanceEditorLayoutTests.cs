using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Checks the constructed controls and their authored size budgets. The shared
/// validator catches fatal height constraints, but intentionally does not report
/// horizontal clipping or a picker placed inside a smaller palette partial.
/// These tests do not claim to run the client's layout solver.
/// </summary>
public class AppearanceEditorLayoutTests
{
    private GuiConstructedWindow _window;
    private IReadOnlyDictionary<string, IGuiWidget> _partials;
    private static readonly string[] EditorPanels =
    {
        AppearanceEditorViewModel.EditorMainPartial, AppearanceEditorViewModel.EditorArmorPartial,
        AppearanceEditorViewModel.SettingsPartial
    };

    [SetUp]
    public void BuildActualDefinitionWithoutEngineSerialization()
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();
        var definition = new AppearanceEditorDefinition();
        _window = definition.BuildWindow();

        // Validation-only windows deliberately omit serialized partials. Read
        // the exact widget tree the builder validated instead of parsing source
        // text or recreating a second version of the appearance layout.
        var builder = typeof(AppearanceEditorDefinition)
            .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(definition)!;
        var authoredWindow = (GuiWindow<AppearanceEditorViewModel>)builder.GetType()
            .GetField("_activeWindow", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(builder)!;
        _partials = authoredWindow.PartialViews;
    }

    [Test]
    public void ProductionPartialsFitInitialWindow()
    {
        _window.LayoutFindings.Should().BeEmpty();
        foreach (var name in new[] { AppearanceEditorViewModel.EditorMainPartial,
                     AppearanceEditorViewModel.EditorArmorPartial, AppearanceEditorViewModel.SettingsPartial })
        {
            var partial = _partials[name];
            Width(partial).Should().Be(0f, $"{name}'s viewport must fill the window");
            Width(partial.Elements.Single()).Should().Be(0f);
            NaturalWidth(partial).Should().BeLessThanOrEqualTo(_window.InitialGeometry.Width - 60f,
                $"{name}'s fixed controls must leave the initial window's border and scrollbar allowance");
        }
    }

    [Test]
    public void StaticPanelsRequireNoWindowGeometryAndRetainTheirBootDimensions()
    {
        var factory = typeof(AppearanceEditorDefinition).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == nameof(AppearanceEditorDefinition.BuildEditorPanel));
        factory.GetParameters().Select(parameter => parameter.ParameterType).Should().Equal(new[] { typeof(string) },
            "the same client layout must serve every window size without server-computed dimensions");
        foreach (var name in EditorPanels)
        {
            var panel = AppearanceEditorDefinition.BuildEditorPanel(name);
            Width(panel).Should().Be(0f);
            panel.DeclaredHeight.Should().Be(0f);
            panel.Elements.Should().ContainSingle();
            var content = panel.Elements.Single();
            content.Should().BeOfType<GuiColumn<AppearanceEditorViewModel>>();
            Width(content).Should().Be(0f);
            Walk(panel).Select(widget => (widget.GetType(), Width(widget), widget.DeclaredHeight))
                .Should().Equal(Walk(_partials[name]).Select(widget =>
                    (widget.GetType(), Width(widget), widget.DeclaredHeight)),
                    "reopening or switching tabs must reuse the boot layout's dimensions");
        }
    }

    [TestCase(AppearanceEditorViewModel.ArmorColorsClothLeather)]
    [TestCase(AppearanceEditorViewModel.ArmorColorsMetal)]
    public void EveryPresetHasAClickableCellInsideThePaletteSlot(string partialName)
    {
        var palette = _partials[partialName];
        var paletteButtons = Walk(palette).OfType<GuiButton<AppearanceEditorViewModel>>()
            .Where(button => button.Events.Values.Any(method =>
                method.Method.Name == nameof(AppearanceEditorViewModel.OnClickColorPalette)))
            .ToArray();
        var ids = paletteButtons.Select(button => button.Events.Values.Single(method =>
                method.Method.Name == nameof(AppearanceEditorViewModel.OnClickColorPalette)))
            .Select(method => (int)method.Arguments.Single().Value);
        ids.Should().Equal(Enumerable.Range(0, 176),
            "all presets, including the previously clipped last colors, must retain their own click targets");
        paletteButtons.Should().OnlyContain(button => Width(button) >= 16f &&
            button.DeclaredHeight >= 16f && button.DeclaredMargin == 0f);

        var slot = Walk(_partials[AppearanceEditorViewModel.EditorArmorPartial])
            .Single(widget => widget.Id == AppearanceEditorViewModel.ArmorColorElement);
        Width(slot).Should().Be(308f);
        slot.DeclaredHeight.Should().Be(246f);
        Width(palette).Should().Be(308f);
        palette.DeclaredHeight.Should().Be(246f);
        var paletteRows = Walk(palette).OfType<GuiRow<AppearanceEditorViewModel>>()
            .Where(row => row.Elements.OfType<GuiButton<AppearanceEditorViewModel>>().Any()).ToArray();
        paletteRows.Should().HaveCount(11);
        paletteRows.Should().OnlyContain(row => row.Elements.Count == 16);
        paletteRows.Max(NaturalWidth).Should().BeLessThanOrEqualTo(Width(slot) - 16f,
            "palette rows need room inside the slot instead of overflowing beside the global channels");
        NaturalHeight(palette).Should().BeLessThanOrEqualTo(slot.DeclaredHeight - 16f,
            "the slot must include all eleven swatch rows and the target label with spacing");
        Walk(palette).OfType<GuiColorPicker<AppearanceEditorViewModel>>().Should().BeEmpty(
            "the RGB picker belongs below the palette/channel row, outside this bounded partial");
    }

    [Test]
    public void PaletteAndGlobalChannelsFitTogetherInTheArmorContent()
    {
        var armor = _partials[AppearanceEditorViewModel.EditorArmorPartial];
        var slot = Walk(armor).Single(widget => widget.Id == AppearanceEditorViewModel.ArmorColorElement);
        var combinedRow = PathTo(armor, slot).Reverse().OfType<GuiRow<AppearanceEditorViewModel>>()
            .First(row => row.Elements.Count > 1);
        NaturalWidth(combinedRow).Should().BeLessThanOrEqualTo(_window.InitialGeometry.Width - 60f,
            "the old 320px palette plus 216px global channels exceeded the whole 476px window");
        combinedRow.Elements.Should().HaveCount(2);
        var channels = combinedRow.Elements.OfType<GuiGroup<AppearanceEditorViewModel>>()
            .Single(group => group.Id != AppearanceEditorViewModel.ArmorColorElement);
        ReadProperty<bool>(channels, "ShowBorder").Should().BeFalse();
        ReadProperty<NuiScrollbars>(channels, "Scrollbars").Should().Be(NuiScrollbars.None);
        PathTo(armor, channels).Should().OnlyContain(widget => Width(widget) == 0f,
            "the global channels must receive the space left beside the fixed palette");
    }

    [Test]
    public void PickerAndRgbControlsRemainOutsideThePaletteSlotAndHaveRoom(
        [Values(AppearanceEditorViewModel.EditorMainPartial, AppearanceEditorViewModel.EditorArmorPartial)] string partialName)
    {
        var partial = AppearanceEditorDefinition.BuildEditorPanel(partialName);
        var picker = Walk(partial).OfType<GuiColorPicker<AppearanceEditorViewModel>>().Single();
        ReadProperty<string>(picker, "SelectedColorBindName").Should().Be(nameof(AppearanceEditorViewModel.SelectedTintColor));
        PathTo(partial, picker).Should().OnlyContain(widget => Width(widget) == 0f,
            "the client must size the picker and all its ancestors without a server layout update");
        picker.DeclaredHeight.Should().Be(128f);
        var pickerRow = PathTo(partial, picker).Reverse().OfType<GuiRow<AppearanceEditorViewModel>>().First();
        pickerRow.Elements.Should().ContainSingle().Which.Should().BeSameAs(picker,
            "flexible side spacers must not absorb the extra width intended for the picker");
        var fields = Walk(partial).OfType<GuiTextEdit<AppearanceEditorViewModel>>()
            .Where(field => ReadProperty<string>(field, "ValueBindName") is
                nameof(AppearanceEditorViewModel.CustomTintRed) or
                nameof(AppearanceEditorViewModel.CustomTintGreen) or
                nameof(AppearanceEditorViewModel.CustomTintBlue)).ToArray();
        fields.Select(field => ReadProperty<string>(field, "ValueBindName")).Should().BeEquivalentTo(new[]
        {
            nameof(AppearanceEditorViewModel.CustomTintRed), nameof(AppearanceEditorViewModel.CustomTintGreen),
            nameof(AppearanceEditorViewModel.CustomTintBlue)
        });

        foreach (var control in fields.Cast<IGuiWidget>().Append(picker))
        {
            var path = PathTo(partial, control).ToArray();
            path.Should().NotContain(widget => widget.Id == AppearanceEditorViewModel.ArmorColorElement);
            var row = path.Reverse().OfType<GuiRow<AppearanceEditorViewModel>>().First();
            ReadProperty<string>(row, "IsVisibleBindName").Should().Be(nameof(AppearanceEditorViewModel.IsCustomTintAvailable));
            path.Take(path.Length - 1).Should().OnlyContain(widget => Width(widget) == 0f);
            if (control != picker)
            {
                Width(control).Should().Be(48f);
                NaturalWidth(row).Should().BeLessThanOrEqualTo(256f,
                    "the fixed RGB inputs must fit the narrowest column that holds the palette image");
            }
            if (row.DeclaredHeight > 0)
                row.DeclaredHeight.Should().BeGreaterThanOrEqualTo(control.DeclaredHeight);
        }

        var readout = Walk(partial).OfType<GuiLabel<AppearanceEditorViewModel>>().Single(label =>
            ReadProperty<string>(label, "TextBindName") == nameof(AppearanceEditorViewModel.ClosestTintPresetText));
        PathTo(partial, readout).Should().OnlyContain(widget => Width(widget) == 0f);
    }

    [Test]
    public void MainEditorUsesIndependentCategoryAndDetailScrollViewports()
    {
        var partial = AppearanceEditorDefinition.BuildEditorPanel(AppearanceEditorViewModel.EditorMainPartial);
        ReadProperty<NuiScrollbars>(partial, "Scrollbars").Should().Be(NuiScrollbars.None);
        var contentRow = Walk(partial).OfType<GuiRow<AppearanceEditorViewModel>>()
            .Single(row => row.Elements.OfType<GuiGroup<AppearanceEditorViewModel>>().Count() == 2);
        contentRow.Elements.Should().HaveCount(2);
        var panels = contentRow.Elements.OfType<GuiGroup<AppearanceEditorViewModel>>().ToArray();
        panels.Select(Width).Should().Equal(new[] { 200f, 0f },
            "the category rail is fixed while the detail panel consumes the remaining client width");
        panels.Should().OnlyContain(panel => panel.DeclaredHeight == 0f &&
            !ReadProperty<bool>(panel, "ShowBorder") &&
            ReadProperty<NuiScrollbars>(panel, "Scrollbars") == NuiScrollbars.Auto,
            "each side must scroll both tall content and fixed palette content wider than its viewport");
        var lists = Walk(partial).OfType<GuiList<AppearanceEditorViewModel>>().ToArray();
        lists.Should().HaveCount(3);
        foreach (var list in lists)
        {
            var binding = ReadProperty<string>(list, "RowCountBindName");
            var expectedPanel = binding == nameof(AppearanceEditorViewModel.PartOptions) + "_RowCount"
                ? panels[1] : panels[0];
            PathTo(partial, list).OfType<GuiGroup<AppearanceEditorViewModel>>().Last()
                .Should().BeSameAs(expectedPanel);
            PathTo(partial, list).Where(widget => !ReferenceEquals(widget, panels[0]))
                .Should().OnlyContain(widget => Width(widget) == 0f,
                    "lists must fill their viewport without calculated widths");
            list.DeclaredHeight.Should().Be(binding == nameof(AppearanceEditorViewModel.ColorCategoryOptions) + "_RowCount"
                ? 154f : 210f);
        }
        lists.Select(list => ReadProperty<string>(list, "RowCountBindName")).Should().BeEquivalentTo(new[]
        {
            nameof(AppearanceEditorViewModel.ColorCategoryOptions) + "_RowCount",
            nameof(AppearanceEditorViewModel.PartCategoryOptions) + "_RowCount",
            nameof(AppearanceEditorViewModel.PartOptions) + "_RowCount"
        });
    }

    [Test]
    public void ArmorCombosUseThreeEqualFlexibleGroupsWithFixedArrowsAndGaps()
    {
        var armor = AppearanceEditorDefinition.BuildEditorPanel(AppearanceEditorViewModel.EditorArmorPartial);
        ReadProperty<NuiScrollbars>(armor, "Scrollbars").Should().Be(NuiScrollbars.Auto);
        var combos = Walk(armor).OfType<GuiComboBox<AppearanceEditorViewModel>>().ToArray();
        combos.Should().HaveCount(19);
        foreach (var combo in combos)
        {
            var path = PathTo(armor, combo).ToArray();
            path.Should().OnlyContain(widget => Width(widget) == 0f,
                "all nineteen dropdowns must use client-allocated space between their fixed arrows");
            path.OfType<GuiColumn<AppearanceEditorViewModel>>().Last().DeclaredHeight.Should().Be(112f,
                "each part block must contribute a bounded height to its stack");
            combo.DeclaredHeight.Should().Be(24f);
            combo.DeclaredMargin.Should().Be(0f);
            var row = path.Reverse().OfType<GuiRow<AppearanceEditorViewModel>>().First();
            row.Elements.Should().HaveCount(3);
            var arrows = row.Elements.OfType<GuiButton<AppearanceEditorViewModel>>().ToArray();
            arrows.Should().HaveCount(2);
            arrows.Should().OnlyContain(button => Width(button) == 24f && button.DeclaredHeight == 24f &&
                button.DeclaredMargin == 0f && button.Events.Values.Any(action =>
                    action.Method.Name == nameof(AppearanceEditorViewModel.OnClickAdjustArmorPart)));
            NaturalWidth(row).Should().Be(48f,
                "only the two arrows may reserve a fixed horizontal span in this row");
        }

        var partsRow = Walk(armor).OfType<GuiRow<AppearanceEditorViewModel>>().Single(row =>
            row.Elements.OfType<GuiGroup<AppearanceEditorViewModel>>().Count() == 3 &&
            Walk(row).OfType<GuiComboBox<AppearanceEditorViewModel>>().Count() == 19);
        partsRow.DeclaredHeight.Should().Be(840f,
            "the shared row must supply the stack height without modifying its peer groups' dimensions");
        Width(partsRow).Should().Be(0f);
        partsRow.Elements.Should().HaveCount(5);
        var gaps = partsRow.Elements.OfType<GuiSpacer<AppearanceEditorViewModel>>().ToArray();
        gaps.Should().HaveCount(2);
        gaps.Should().OnlyContain(spacer => Width(spacer) == 6f,
            "flexible gaps must not consume the same new width as the actual armor columns");
        var stacks = partsRow.Elements.OfType<GuiGroup<AppearanceEditorViewModel>>().ToArray();
        stacks.Should().OnlyContain(group => Width(group) == 0f && group.DeclaredHeight == 0f &&
            !ReadProperty<bool>(group, "ShowBorder") &&
            ReadProperty<NuiScrollbars>(group, "Scrollbars") == NuiScrollbars.None,
            "either width or height on a peer group disables native equal-width sharing in both axes");
        stacks.Select(group => Walk(group).OfType<GuiComboBox<AppearanceEditorViewModel>>().Count())
            .Should().Equal(7, 5, 7);
        stacks.Should().OnlyContain(group => NaturalHeight(group) <= partsRow.DeclaredHeight,
            "every fixed part block must fit inside the height supplied by the shared row");
    }

    [Test]
    public void MainPaletteImageRetainsItsPixelBasedClickCoordinates()
    {
        var palette = Walk(_partials[AppearanceEditorViewModel.EditorMainPartial])
            .OfType<GuiImage<AppearanceEditorViewModel>>().Single(image =>
                ReadProperty<string>(image, "ResrefBindName") == nameof(AppearanceEditorViewModel.ColorSheetResref));
        Width(palette).Should().Be(256f,
            "the sixteen palette columns use fixed pixel coordinates in GetSelectedPaletteColorId");
        palette.DeclaredHeight.Should().Be(176f,
            "the eleven palette rows must keep their existing click-coordinate mapping");
    }

    [Test]
    public void FixedMainPaletteHasATrailingFlexibleSpacer()
    {
        var main = _partials[AppearanceEditorViewModel.EditorMainPartial];
        var palette = Walk(main).Single(widget => widget.Id == "ae_color_palette");
        var row = PathTo(main, palette).OfType<GuiRow<AppearanceEditorViewModel>>().Last();
        row.Elements.Should().HaveCount(2);
        row.Elements[0].Should().BeSameAs(palette);
        row.Elements[1].Should().BeOfType<GuiSpacer<AppearanceEditorViewModel>>();
        Width(row.Elements[1]).Should().Be(0f);
        row.Elements[1].DeclaredHeight.Should().Be(0f,
            "a dimension modifier would prevent the spacer absorbing the detail panel's extra width");
        Width(row).Should().Be(0f);
    }

    [Test]
    public void PreviousAndNextPartButtonsShareWidthWithoutDimensionModifiers()
    {
        var main = _partials[AppearanceEditorViewModel.EditorMainPartial];
        var previous = Walk(main).Single(widget => widget.Id == "ae_previous_part");
        var next = Walk(main).Single(widget => widget.Id == "ae_next_part");
        var row = PathTo(main, previous).OfType<GuiRow<AppearanceEditorViewModel>>().Last();
        row.Elements.Should().Equal(new[] { previous, next });
        row.DeclaredHeight.Should().Be(40f);
        Width(row).Should().Be(0f);
        row.Elements.Should().OnlyContain(button => Width(button) == 0f && button.DeclaredHeight == 0f,
            "even a height-only modifier disables the buttons' native equal-width sharing");
    }

    [Test]
    public void SettingsContentUsesClientWidthAndVerticalScrolling()
    {
        var settings = AppearanceEditorDefinition.BuildEditorPanel(AppearanceEditorViewModel.SettingsPartial);
        ReadProperty<NuiScrollbars>(settings, "Scrollbars").Should().Be(NuiScrollbars.Y);
        Walk(settings).Where(widget => widget is GuiGroup<AppearanceEditorViewModel> or GuiColumn<AppearanceEditorViewModel>)
            .Should().OnlyContain(widget => Width(widget) == 0f);
        Walk(settings).OfType<GuiRow<AppearanceEditorViewModel>>()
            .Should().OnlyContain(row => Width(row) == 0f && NaturalWidth(row) <= 256f,
                "fixed controls must leave the surrounding spacers free to follow the client width");
    }

    [Test]
    public void NavigationAndMainContentHaveNoFixedWidthAncestor()
    {
        var main = _partials["%%WINDOW_MAIN%%"];
        var toggles = Walk(main).OfType<GuiToggles<AppearanceEditorViewModel>>().ToArray();
        var primary = toggles.Single(toggle => ReadProperty<string>(toggle, "SelectedValueBindName") ==
            nameof(AppearanceEditorViewModel.EditorTabToggleValue));
        var settings = toggles.Single(toggle => ReadProperty<string>(toggle, "SelectedValueBindName") ==
            nameof(AppearanceEditorViewModel.SettingsTabToggleValue));
        PathTo(main, primary).Should().OnlyContain(widget => Width(widget) == 0f);
        Width(settings).Should().Be(150f);
        var slot = Walk(main).Single(widget => widget.Id == AppearanceEditorViewModel.MainPartialElement);
        PathTo(main, slot).Should().OnlyContain(widget => Width(widget) == 0f,
            "the active partial and its scrollbar need the full available window width");
    }

    [TestCase(AppearanceEditorViewModel.EditorMainPartial)]
    [TestCase(AppearanceEditorViewModel.EditorArmorPartial)]
    [TestCase(AppearanceEditorViewModel.SettingsPartial)]
    public void EditorViewportsHaveNoFixedRootWidthOrHeight(string partialName)
    {
        var partial = _partials[partialName];
        var expectedScrollbars = partialName switch
        {
            AppearanceEditorViewModel.EditorMainPartial => NuiScrollbars.None,
            AppearanceEditorViewModel.EditorArmorPartial => NuiScrollbars.Auto,
            _ => NuiScrollbars.Y
        };
        ReadProperty<NuiScrollbars>(partial, "Scrollbars").Should().Be(expectedScrollbars,
            "main delegates scrolling to its side-by-side viewports, and armor permits both overflow axes");
        Width(partial).Should().Be(0f,
            "the scrollbar must follow the window edge instead of staying at the old panel width");
        partial.DeclaredHeight.Should().Be(0f,
            "the scroll viewport must fill the available window height, not use a fixed content height");
        partial.Elements.Should().ContainSingle().Which.Should().BeOfType<GuiColumn<AppearanceEditorViewModel>>();
        Width(partial.Elements.Single()).Should().Be(0f);
    }

    [Test]
    public void MainContentScrollsAndUsesOnlyOneNestedArmorPaletteSlot()
    {
        var main = _partials["%%WINDOW_MAIN%%"];
        var mainSlot = Walk(main).Single(widget => widget.Id == AppearanceEditorViewModel.MainPartialElement);
        PathTo(main, mainSlot).OfType<GuiGroup<AppearanceEditorViewModel>>()
            .Should().Contain(group => ReadProperty<NuiScrollbars>(group, "Scrollbars") == NuiScrollbars.Auto,
                "the full editor must remain reachable when its window is shorter than the controls");
        Slots(main).Should().Equal(AppearanceEditorViewModel.MainPartialElement);
        Slots(_partials[AppearanceEditorViewModel.EditorMainPartial]).Should().BeEmpty();
        Slots(_partials[AppearanceEditorViewModel.SettingsPartial]).Should().BeEmpty();
        Slots(_partials[AppearanceEditorViewModel.EditorArmorPartial]).Should().Equal(AppearanceEditorViewModel.ArmorColorElement);
        Slots(_partials[AppearanceEditorViewModel.ArmorColorsClothLeather]).Should().BeEmpty();
        Slots(_partials[AppearanceEditorViewModel.ArmorColorsMetal]).Should().BeEmpty();
    }

    [Test]
    public void RuntimePanelsRetainEveryBootEventAndBinding(
        [Values(AppearanceEditorViewModel.EditorMainPartial, AppearanceEditorViewModel.EditorArmorPartial,
            AppearanceEditorViewModel.SettingsPartial)] string partialName)
    {
        var boot = _partials[partialName];
        var runtime = AppearanceEditorDefinition.BuildEditorPanel(partialName);
        foreach (var panel in new[] { boot, runtime })
        {
            var eventWidgets = Walk(panel).Where(widget => widget.Events.Count > 0).ToArray();
            eventWidgets.Should().NotBeEmpty();
            eventWidgets.Select(widget => widget.Id).Should().OnlyContain(id => !string.IsNullOrWhiteSpace(id));
            eventWidgets.Select(widget => widget.Id).Should().OnlyHaveUniqueItems(
                "an event ID must identify one control in each panel");
        }

        EventSnapshot(runtime).Should().BeEquivalentTo(EventSnapshot(boot),
            "runtime copies must route to the event handlers registered at boot, with the same typed arguments");
        BindingSnapshot(runtime).Should().BeEquivalentTo(BindingSnapshot(boot),
            "static panel copies must retain options, selected values, visibility and every draw-texture region binding");
    }

    [Test]
    public void RuntimeArmorRetainsAllPartOptionsAndAll120SwatchRegions()
    {
        var armor = AppearanceEditorDefinition.BuildEditorPanel(AppearanceEditorViewModel.EditorArmorPartial);
        var combos = Walk(armor).OfType<GuiComboBox<AppearanceEditorViewModel>>().ToArray();
        combos.Should().HaveCount(19);
        var options = combos.Select(combo => ReadProperty<string>(combo, "OptionsBindName")).ToArray();
        options.Should().OnlyHaveUniqueItems();
        options.Should().OnlyContain(name => name.EndsWith("Options", StringComparison.Ordinal));
        combos.Select(combo => ReadProperty<string>(combo, "SelectedIndexBindName")).Should().BeEquivalentTo(
            options.Select(name => name[..^"Options".Length] + "Selection"));

        var expectedRegions = typeof(AppearanceEditorViewModel).GetProperties()
            .Where(property => property.PropertyType == typeof(GuiRectangle) &&
                property.Name.EndsWith("Region", StringComparison.Ordinal))
            .Select(property => property.Name).ToArray();
        expectedRegions.Should().HaveCount(120);
        var regionBindings = BindingSnapshot(armor)
            .Where(entry => entry.Key.EndsWith(".DrawTextureRegionBindName", StringComparison.Ordinal))
            .Select(entry => entry.Value).ToArray();
        regionBindings.Should().BeEquivalentTo(expectedRegions);
    }

    private static IReadOnlyDictionary<string, string> EventSnapshot(IGuiWidget root) => Walk(root)
        .SelectMany(widget => widget.Events.Select(entry => new KeyValuePair<string, string>(
            widget.Id + "/" + entry.Key,
            entry.Value.Method.DeclaringType!.FullName + "." + entry.Value.Method + "(" +
            string.Join(",", entry.Value.Arguments.Select(argument => argument.Key.FullName + ":" +
                Convert.ToString(argument.Value, System.Globalization.CultureInfo.InvariantCulture))) + ")")))
        .ToDictionary(entry => entry.Key, entry => entry.Value);

    private static IReadOnlyDictionary<string, string> BindingSnapshot(IGuiWidget root) =>
        BoundObjects(root, "root").SelectMany(entry => Properties(entry.Value)
            .Where(property => property.PropertyType == typeof(string) &&
                property.Name.EndsWith("BindName", StringComparison.Ordinal))
            .Select(property => new KeyValuePair<string, string>(entry.Path + "." + property.Name,
                (string)property.GetValue(entry.Value))))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Value))
            .ToDictionary(entry => entry.Key, entry => entry.Value);

    private static IEnumerable<(string Path, object Value)> BoundObjects(IGuiWidget widget, string path)
    {
        yield return (path, widget);
        var drawLists = ReadProperty<System.Collections.IEnumerable>(widget, "DrawLists").Cast<object>().ToArray();
        for (var index = 0; index < drawLists.Length; index++)
        {
            var drawPath = path + "/draw/" + index;
            yield return (drawPath, drawLists[index]);
            var items = ReadProperty<System.Collections.IEnumerable>(drawLists[index], "DrawItems").Cast<object>().ToArray();
            for (var item = 0; item < items.Length; item++)
                yield return (drawPath + "/" + item, items[item]);
        }
        for (var index = 0; index < widget.Elements.Count; index++)
        foreach (var child in BoundObjects(widget.Elements[index], path + "/" + index))
            yield return child;
    }

    private static IEnumerable<PropertyInfo> Properties(object target)
    {
        for (var type = target.GetType(); type != null; type = type.BaseType)
        foreach (var property in type.GetProperties(
                     BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            yield return property;
    }

    private static IEnumerable<string> Slots(IGuiWidget widget) => Walk(widget)
        .OfType<GuiGroup<AppearanceEditorViewModel>>()
        .Where(group => group.Elements.Count == 0 && !string.IsNullOrEmpty(group.Id)).Select(group => group.Id);

    private static IEnumerable<IGuiWidget> Walk(IGuiWidget widget) =>
        new[] { widget }.Concat(widget.Elements.SelectMany(Walk));

    private static IEnumerable<IGuiWidget> PathTo(IGuiWidget root, IGuiWidget target)
    {
        if (ReferenceEquals(root, target))
            return new[] { root };
        foreach (var child in root.Elements)
        {
            var tail = PathTo(child, target).ToArray();
            if (tail.Length > 0)
                return new[] { root }.Concat(tail);
        }
        return Array.Empty<IGuiWidget>();
    }

    private static float Width(IGuiWidget widget) => ReadProperty<float>(widget, "Width");

    // Lower bounds from actual fixed dimensions; no guessed engine margins.
    private static float MinimumWidth(IGuiWidget widget) => Math.Max(Width(widget), NaturalWidth(widget));
    private static float NaturalWidth(IGuiWidget widget) => widget is GuiRow<AppearanceEditorViewModel>
        ? widget.Elements.Sum(MinimumWidth)
        : widget.Elements.Select(MinimumWidth).DefaultIfEmpty(0f).Max();
    private static float MinimumHeight(IGuiWidget widget) => Math.Max(widget.DeclaredHeight, NaturalHeight(widget));
    private static float NaturalHeight(IGuiWidget widget) => widget is GuiColumn<AppearanceEditorViewModel>
        ? widget.Elements.Sum(MinimumHeight)
        : widget.Elements.Select(MinimumHeight).DefaultIfEmpty(0f).Max();

    private static T ReadProperty<T>(object target, string name)
    {
        for (var type = target.GetType(); type != null; type = type.BaseType)
        {
            var property = type.GetProperty(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (property != null)
                return (T)property.GetValue(target)!;
        }
        throw new InvalidOperationException($"Missing widget metadata {target.GetType().Name}.{name}");
    }
}
