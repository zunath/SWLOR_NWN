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
    public void ProductionPartialsHaveNoLayoutWarningsAndFitInitialWindow()
    {
        _window.LayoutFindings.Should().BeEmpty();
        foreach (var name in new[] { AppearanceEditorViewModel.EditorMainPartial,
                     AppearanceEditorViewModel.EditorArmorPartial, AppearanceEditorViewModel.SettingsPartial })
        {
            var partial = _partials[name];
            Width(partial).Should().BePositive($"{name} needs an explicit content width");
            Width(partial).Should().BeLessThanOrEqualTo(_window.InitialGeometry.Width - 32f,
                "the initial window must leave room for borders and a vertical scrollbar");
            NaturalWidth(partial).Should().BeLessThanOrEqualTo(Width(partial),
                $"{name} must contain its widest authored row without clipping");
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
        NaturalWidth(combinedRow).Should().BeLessThanOrEqualTo(Width(armor) - 16f,
            "the old 320px palette plus 216px global channels exceeded the whole 476px window");
    }

    [TestCase(AppearanceEditorViewModel.EditorMainPartial)]
    [TestCase(AppearanceEditorViewModel.EditorArmorPartial)]
    public void PickerAndRgbControlsRemainOutsideThePaletteSlotAndHaveRoom(string partialName)
    {
        var partial = _partials[partialName];
        var picker = Walk(partial).OfType<GuiColorPicker<AppearanceEditorViewModel>>().Single();
        ReadProperty<string>(picker, "SelectedColorBindName").Should().Be(nameof(AppearanceEditorViewModel.SelectedTintColor));
        Width(picker).Should().BeGreaterThanOrEqualTo(250f);
        picker.DeclaredHeight.Should().BeGreaterThanOrEqualTo(128f);
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
            var width = path.Reverse().Skip(1).Select(Width).First(value => value > 0f);
            NaturalWidth(row).Should().BeLessThanOrEqualTo(width,
                "the picker and all RGB inputs must fit their actual containing column");
            if (row.DeclaredHeight > 0)
                row.DeclaredHeight.Should().BeGreaterThanOrEqualTo(control.DeclaredHeight);
        }
    }

    [Test]
    public void MainEditorListsHaveBoundedViewports()
    {
        var partial = _partials[AppearanceEditorViewModel.EditorMainPartial];
        var lists = Walk(partial).OfType<GuiList<AppearanceEditorViewModel>>().ToArray();
        lists.Should().HaveCount(3);
        foreach (var list in lists)
            PathTo(partial, list).Should().Contain(widget => widget.DeclaredHeight > 0f,
                "category and part lists must not compete with the fixed palette and picker for unbounded height");
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
