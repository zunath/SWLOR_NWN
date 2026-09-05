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
    public void ProductionPartialsAreFluidAndTheirFixedControlsFitInitialWindow()
    {
        _window.LayoutFindings.Should().BeEmpty();
        foreach (var name in new[] { AppearanceEditorViewModel.EditorMainPartial,
                     AppearanceEditorViewModel.EditorArmorPartial, AppearanceEditorViewModel.SettingsPartial })
        {
            var partial = _partials[name];
            Width(partial).Should().Be(0f,
                $"{name}'s viewport must fill the window instead of retaining the old 530px cap");
            NaturalWidth(partial).Should().BeLessThanOrEqualTo(_window.InitialGeometry.Width - 60f,
                $"{name}'s fixed controls must leave the initial window's border and scrollbar allowance");
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
    }

    [TestCase(AppearanceEditorViewModel.EditorMainPartial)]
    [TestCase(AppearanceEditorViewModel.EditorArmorPartial)]
    public void PickerAndRgbControlsRemainOutsideThePaletteSlotAndHaveRoom(string partialName)
    {
        var partial = _partials[partialName];
        var picker = Walk(partial).OfType<GuiColorPicker<AppearanceEditorViewModel>>().Single();
        ReadProperty<string>(picker, "SelectedColorBindName").Should().Be(nameof(AppearanceEditorViewModel.SelectedTintColor));
        Width(picker).Should().Be(0f, "the native picker must expand with its containing column");
        picker.DeclaredHeight.Should().Be(128f);
        PathTo(partial, picker).Should().OnlyContain(widget => Width(widget) == 0f,
            "a fixed-width ancestor would keep the picker narrow after the window expands");
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
            path.Take(path.Length - 1).Should().OnlyContain(widget => Width(widget) == 0f,
                "the fixed RGB fields must not pin their containing panel to a fixed width");
            NaturalWidth(row).Should().BeLessThanOrEqualTo(256f,
                "the fixed RGB inputs must fit the narrowest column that holds the palette image");
            if (row.DeclaredHeight > 0)
                row.DeclaredHeight.Should().BeGreaterThanOrEqualTo(control.DeclaredHeight);
        }
    }

    [Test]
    public void MainEditorListsFillTheirColumnWidthAndRetainBoundedHeights()
    {
        var partial = _partials[AppearanceEditorViewModel.EditorMainPartial];
        var lists = Walk(partial).OfType<GuiList<AppearanceEditorViewModel>>().ToArray();
        lists.Should().HaveCount(3);
        foreach (var list in lists)
        {
            PathTo(partial, list).Should().OnlyContain(widget => Width(widget) == 0f,
                "the category and part lists must share the increased window width");
            list.DeclaredHeight.Should().BePositive();
            PathTo(partial, list).Should().Contain(widget => widget.DeclaredHeight > 0f,
                "category and part lists must not compete with the fixed palette and picker for unbounded height");
        }
        lists.Select(list => list.DeclaredHeight).Should().BeEquivalentTo(new[] { 154f, 210f, 210f });
    }

    [Test]
    public void ArmorCombosExpandBetweenFixedArrowsAndFixedGaps()
    {
        var armor = _partials[AppearanceEditorViewModel.EditorArmorPartial];
        var combos = Walk(armor).OfType<GuiComboBox<AppearanceEditorViewModel>>().ToArray();
        combos.Should().HaveCount(19);
        foreach (var combo in combos)
        {
            var path = PathTo(armor, combo).ToArray();
            path.Should().OnlyContain(widget => Width(widget) == 0f,
                "a part dropdown must use the width left between its previous/next buttons");
            combo.DeclaredHeight.Should().Be(24f);
            combo.DeclaredMargin.Should().Be(0f);
            var row = path.Reverse().OfType<GuiRow<AppearanceEditorViewModel>>().First();
            row.Elements.Should().HaveCount(3);
            var arrows = row.Elements.OfType<GuiButton<AppearanceEditorViewModel>>().ToArray();
            arrows.Should().HaveCount(2);
            arrows.Should().OnlyContain(button => Width(button) == 24f && button.DeclaredHeight == 24f &&
                button.DeclaredMargin == 0f && button.Events.Values.Any(action =>
                    action.Method.Name == nameof(AppearanceEditorViewModel.OnClickAdjustArmorPart)));
        }

        var partsRow = Walk(armor).OfType<GuiRow<AppearanceEditorViewModel>>().Single(row =>
            row.Elements.OfType<GuiColumn<AppearanceEditorViewModel>>().Count() == 3 &&
            Walk(row).OfType<GuiComboBox<AppearanceEditorViewModel>>().Count() == 19);
        partsRow.Elements.Should().HaveCount(5);
        var gaps = partsRow.Elements.OfType<GuiSpacer<AppearanceEditorViewModel>>().ToArray();
        gaps.Should().HaveCount(2);
        gaps.Should().OnlyContain(spacer => Width(spacer) == 6f,
            "flexible gaps must not consume the same new width as the actual armor columns");
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
    public void SettingsContentCanCenterWithinTheResizedViewport()
    {
        var settings = _partials[AppearanceEditorViewModel.SettingsPartial];
        Walk(settings).Where(widget => widget is GuiColumn<AppearanceEditorViewModel> or
            GuiRow<AppearanceEditorViewModel>).Should().OnlyContain(widget => Width(widget) == 0f,
            "a fixed inner span would keep the settings controls centered inside the old narrow column");
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
    public void EditorPartialOwnsItsScrollViewportWithoutAFixedHeight(string partialName)
    {
        var partial = _partials[partialName];

        // The reported client screenshot clipped the lower armor controls despite
        // an Auto-scroll ancestor. A group does not report its contents' height
        // to that ancestor, so a None-scroll partial silently clipped its own
        // children. Check the group that actually contains the editor controls.
        ReadProperty<NuiScrollbars>(partial, "Scrollbars").Should().Be(NuiScrollbars.Auto,
            "the editor partial itself must scroll both tall content and content wider than a resized window");
        Width(partial).Should().Be(0f,
            "the scrollbar must follow the window edge instead of staying at the old panel width");
        partial.DeclaredHeight.Should().Be(0f,
            "the scroll viewport must fill the available window height, not use a fixed content height");
        partial.Elements.Should().ContainSingle().Which.Should().BeOfType<GuiColumn<AppearanceEditorViewModel>>();
        Walk(partial).OfType<GuiGroup<AppearanceEditorViewModel>>()
            .Where(group => !ReferenceEquals(group, partial) && group.Id != AppearanceEditorViewModel.ArmorColorElement)
            .Should().BeEmpty("another unsized group would conceal the editor's content extent from its scroll viewport");
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
