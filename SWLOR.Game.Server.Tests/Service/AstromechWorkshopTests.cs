using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.AppearancePreview;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.AstromechService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The Chassis Workshop's pure rules (size steps, chassis pictures, palette, message text) and the shape of its
/// window, which follows the Appearance Editor.
/// Nothing here runs the engine or the client's layout solver.
/// </summary>
public class AstromechWorkshopTests
{
    [TestCase(1.00f, 1, 1.01f)]
    [TestCase(1.00f, -1, 0.99f)]
    [TestCase(1.14f, 1, 1.15f)]
    [TestCase(1.15f, 1, 1.15f)]
    [TestCase(0.85f, -1, 0.85f)]
    [TestCase(0.86f, -1, 0.85f)]
    [TestCase(1.00f, 0, 1.00f)]
    public void StepScale_MovesOneHundredthAndStaysInRange(float current, int direction, float expected)
    {
        AstromechWorkshop.StepScale(current, direction).Should().BeApproximately(expected, 0.0001f);
    }

    [Test]
    public void StepScale_CrossesTheWholeRangeInWholeSteps()
    {
        var scale = AstromechChassisCatalog.MinimumScale;
        var steps = 0;
        while (scale < AstromechChassisCatalog.MaximumScale - 0.0001f && steps < 100)
        {
            scale = AstromechWorkshop.StepScale(scale, 1);
            steps++;
        }

        steps.Should().Be(30);
        scale.Should().BeApproximately(AstromechChassisCatalog.MaximumScale, 0.0001f);
    }

    [TestCase(0.5f, 0.85f)]
    [TestCase(2.0f, 1.15f)]
    [TestCase(1.004f, 1.00f)]
    [TestCase(1.006f, 1.01f)]
    [TestCase(0f, 1.00f)]
    [TestCase(-3f, 1.00f)]
    [TestCase(float.NaN, 1.00f)]
    public void RoundScale_SnapsToHundredthsAndClamps(float input, float expected)
    {
        AstromechWorkshop.RoundScale(input).Should().BeApproximately(expected, 0.0001f);
    }

    [TestCase(1.00f, "100%")]
    [TestCase(0.85f, "85%")]
    [TestCase(1.15f, "115%")]
    [TestCase(0.994f, "99%")]
    public void FormatScale_ShowsAWholePercent(float scale, string expected)
    {
        AstromechWorkshop.FormatScale(scale).Should().Be(expected);
    }

    [Test]
    public void GetChassisAt_MapsEveryListRowToItsChassis()
    {
        for (var index = 0; index < AstromechChassisCatalog.All.Count; index++)
            AstromechWorkshop.GetChassisAt(index).Should().BeSameAs(AstromechChassisCatalog.All[index]);
    }

    [TestCase(-1)]
    [TestCase(8)]
    [TestCase(100)]
    public void GetChassisAt_ReturnsNothingForARowThatIsNotAChassis(int index)
    {
        AstromechWorkshop.GetChassisAt(index).Should().BeNull();
    }

    [Test]
    public void TheMessagesUseAsciiPunctuationOnly()
    {
        foreach (var message in new[]
                 {
                     AstromechWorkshop.NotAstromechMessage,
                     AstromechWorkshop.InSpaceMessage,
                     AstromechWorkshop.HeadScaleMessage,
                     AstromechWorkshop.LargestMessage,
                     AstromechWorkshop.SmallestMessage
                 })
            message.Should().MatchRegex("^[\\x20-\\x7E]+$");
    }

    [TestCase(0, 1, 1)]
    [TestCase(7, 1, 0)]
    [TestCase(0, -1, 7)]
    [TestCase(3, -1, 2)]
    [TestCase(5, 0, 5)]
    public void StepChassis_GoesRoundAtEitherEnd(int index, int direction, int expected)
    {
        AstromechWorkshop.StepChassis(index, direction).Should().Be(expected);
    }

    [Test]
    public void NextView_TurnsFrontBackAngleAndFrontAgain()
    {
        AstromechWorkshop.NextView(AppearancePreviewView.Front).Should().Be(AppearancePreviewView.Back);
        AstromechWorkshop.NextView(AppearancePreviewView.Back).Should().Be(AppearancePreviewView.Angle);
        AstromechWorkshop.NextView(AppearancePreviewView.Angle).Should().Be(AppearancePreviewView.Front);
    }

    [Test]
    public void EveryChassisHasAPictureOfEachView()
    {
        // The pictures are made by tools/GenerateAppearancePreviews.py like every Appearance Editor picture.
        var pictures = Path.Combine(FindRepositoryRoot(), "SWLOR_Haks", "sw_part_previews");
        AstromechChassisCatalog.All.Should().HaveCount(ChassisWorkshopViewModel.TileCount,
            "the window binds one picture for each chassis");
        foreach (var chassis in AstromechChassisCatalog.All)
        {
            foreach (var view in Enum.GetValues<AppearancePreviewView>())
            {
                var resref = AppearancePreview.Resref(AstromechWorkshop.GetPictureKey(chassis), view);
                resref.Should().NotBeEmpty($"{chassis.Name} needs a picture name of sixteen characters or fewer");
                File.Exists(Path.Combine(pictures, resref + ".dds")).Should()
                    .BeTrue($"{resref} is the {view} picture of the {chassis.Name}");
            }
        }
    }

    [Test]
    public void ThePaletteOffersTheClothColoursForPaintAndTheMetalColoursForTrim()
    {
        AstromechWorkshop.GetPaletteSheet(AstromechPaintZone.Primary).Should().Be("gui_pal_tattoo");
        AstromechWorkshop.GetPaletteSheet(AstromechPaintZone.Secondary).Should().Be("gui_pal_tattoo");
        AstromechWorkshop.GetPaletteSheet(AstromechPaintZone.Trim).Should().Be("gui_pal_armor01");

        foreach (var zone in AstromechChassisCatalog.Zones)
        {
            var layer = AstromechChassisCatalog.GetLayer(zone);
            for (var index = 0; index < TintMapMaterialRegistry.PaletteColorCount; index++)
                AstromechWorkshop.GetPaletteColor(zone, index).Should().Be(TintMapPaletteColors.GetColor(layer, index));
        }
    }

    [Test]
    public void APaletteColourIsFoundAgain_AndAnyOtherColourIsNot()
    {
        foreach (var zone in AstromechChassisCatalog.Zones)
        {
            for (var index = 0; index < TintMapMaterialRegistry.PaletteColorCount; index++)
            {
                var color = AstromechWorkshop.GetPaletteColor(zone, index);
                var found = AstromechWorkshop.FindPaletteIndex(zone, color);
                found.Should().BeInRange(0, index, "the first cell holding a colour is the one outlined");
                AstromechWorkshop.GetPaletteColor(zone, found).Should().Be(color);
            }
        }

        AstromechWorkshop.FindPaletteIndex(AstromechPaintZone.Primary, new TintMapColor(1, 254, 3)).Should().Be(-1);
    }

    [TestCase("0", "0", "0", 0, 0, 0)]
    [TestCase("255", "128", "7", 255, 128, 7)]
    [TestCase("007", "10", "200", 7, 10, 200)]
    public void TryParseRgb_ReadsThreeWholeNumbers(string red, string green, string blue, int r, int g, int b)
    {
        AstromechWorkshop.TryParseRgb(red, green, blue, out var color).Should().BeTrue();
        color.Should().Be(new TintMapColor((byte)r, (byte)g, (byte)b));
    }

    [TestCase("", "0", "0")]
    [TestCase("12", null, "0")]
    [TestCase("256", "0", "0")]
    [TestCase("1.5", "0", "0")]
    [TestCase("-1", "0", "0")]
    [TestCase("12a", "0", "0")]
    [TestCase("1234", "0", "0")]
    [TestCase(" 12", "0", "0")]
    public void TryParseRgb_RefusesAHalfTypedColour(string red, string green, string blue)
    {
        AstromechWorkshop.TryParseRgb(red, green, blue, out _).Should().BeFalse();
    }

    [TestCase("12", "12")]
    [TestCase("007", "007")]
    [TestCase("255", "255")]
    [TestCase("256", "255")]
    [TestCase("462", "255")]
    [TestCase("1a2", "12")]
    [TestCase("-5", "5")]
    [TestCase("abc", "")]
    [TestCase("", "")]
    [TestCase(null, "")]
    public void NormalizeChannel_KeepsDigitsAndStopsAt255(string typed, string expected)
    {
        AstromechWorkshop.NormalizeChannel(typed).Should().Be(expected);
    }

    [Test]
    public void TheWorkshopWindowBuildsWithoutLayoutWarnings()
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();

        var window = new ChassisWorkshopDefinition().BuildWindow();

        window.Type.Should().Be(GuiWindowType.ChassisWorkshop);
        window.LayoutFindings.Should().BeEmpty();
        window.InitialGeometry.Height.Should().BeLessThanOrEqualTo(680f);
        window.InitialGeometry.Width.Should().BeLessThanOrEqualTo(1280f);
    }

    [Test]
    public void TheWorkshopIsTheSizeOfTheAppearanceEditor()
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();

        var workshop = new ChassisWorkshopDefinition().BuildWindow();
        var editor = new AppearanceEditorDefinition().BuildWindow();

        workshop.InitialGeometry.Width.Should().Be(editor.InitialGeometry.Width);
        workshop.InitialGeometry.Height.Should().Be(editor.InitialGeometry.Height);
        ChassisWorkshopDefinition.ChassisPanelWidth.Should().Be(AppearanceEditorDefinition.MapWidth);
    }

    [Test]
    public void TheChassisTab_ShowsEveryChassisAsAPictureThatWearsIt()
    {
        var widgets = Walk(ChassisWorkshopDefinition.BuildPanel(ChassisWorkshopViewModel.ChassisPartial)).ToList();

        for (var tile = 0; tile < AstromechChassisCatalog.All.Count; tile++)
        {
            var picture = widgets.Single(widget => widget.Id == $"cw_tile_{tile}");
            picture.Should().BeOfType<GuiButtonImage<ChassisWorkshopViewModel>>();
            Handler(picture, "click").Should().Be((nameof(ChassisWorkshopViewModel.OnClickTile), (object)tile));
            typeof(ChassisWorkshopViewModel).GetProperty($"Tile{tile}Image").Should().NotBeNull();
            typeof(ChassisWorkshopViewModel).GetProperty($"Tile{tile}Selected").Should().NotBeNull();
        }

        widgets.Should().NotContain(widget => widget.Id == $"cw_tile_{AstromechChassisCatalog.All.Count}");
        widgets.Select(widget => widget.Id).Should().Contain(new[]
        {
            "cw_preview", "cw_chassis_previous", "cw_view", "cw_chassis_next", "cw_color_palette", "cw_tint_picker",
            "cw_rgb_R", "cw_rgb_G", "cw_rgb_B"
        });
    }

    [Test]
    public void TheChassisTab_HasASwatchForEachPaintZone_AndTheWholePalette()
    {
        var widgets = Walk(ChassisWorkshopDefinition.BuildPanel(ChassisWorkshopViewModel.ChassisPartial)).ToList();

        foreach (var (name, zone) in new[]
                 {
                     ("Primary", AstromechPaintZone.Primary), ("Secondary", AstromechPaintZone.Secondary),
                     ("Trim", AstromechPaintZone.Trim)
                 })
        {
            var swatch = widgets.Single(widget => widget.Id == $"cw_swatch_{name}");
            Handler(swatch, "click").Should().Be((nameof(ChassisWorkshopViewModel.OnClickSwatch), (object)zone));
            // A right-click returns the zone to the paint its chassis comes with, like emptying a part swatch.
            Handler(swatch, "mousedown").Should().Be((nameof(ChassisWorkshopViewModel.OnMouseDownSwatch), (object)zone));
            typeof(ChassisWorkshopViewModel).GetProperty(name + "Tint").Should().NotBeNull();
            typeof(ChassisWorkshopViewModel).GetProperty(name + "Selected").Should().NotBeNull();
        }

        for (var index = 0; index < TintMapMaterialRegistry.PaletteColorCount; index++)
        {
            var cell = widgets.Single(widget => widget.Id == "cw_palette_" + index);
            Handler(cell, "click").Should().Be((nameof(ChassisWorkshopViewModel.OnClickPalette), (object)index));
        }

        var picker = widgets.Single(widget => widget.Id == "cw_tint_picker");
        picker.Events.Keys.Should().Contain(new[] { "mousedown", "mouseup" },
            "only a colour reported during a press on the picker is a choice");
    }

    [Test]
    public void TheSettingsTab_StepsTheSize()
    {
        var widgets = Walk(ChassisWorkshopDefinition.BuildPanel(ChassisWorkshopViewModel.SettingsPartial)).ToList();

        Handler(widgets.Single(widget => widget.Id == "cw_size_increase"), "click").Method
            .Should().Be(nameof(ChassisWorkshopViewModel.OnClickLarger));
        Handler(widgets.Single(widget => widget.Id == "cw_size_decrease"), "click").Method
            .Should().Be(nameof(ChassisWorkshopViewModel.OnClickSmaller));
        widgets.Should().NotContain(widget => (widget.Id ?? string.Empty).StartsWith("cw_tile_"));
    }

    [Test]
    public void TheWorkshopWindowTypeIsUnique()
    {
        var duplicates = Enum.GetValues<GuiWindowType>()
            .Where(type => (int)type == (int)GuiWindowType.ChassisWorkshop)
            .ToList();

        duplicates.Should().ContainSingle();
    }

    [Test]
    public void TheWorkshopViewModelNamesItsPartialsAndElement()
    {
        ChassisWorkshopViewModel.ChassisPartial.Should().NotBeNullOrWhiteSpace();
        ChassisWorkshopViewModel.SettingsPartial.Should().NotBeNullOrWhiteSpace();
        ChassisWorkshopViewModel.SettingsPartial.Should().NotBe(ChassisWorkshopViewModel.ChassisPartial);
        ChassisWorkshopViewModel.ContentElement.Should().NotBeNullOrWhiteSpace();
    }

    private static IEnumerable<IGuiWidget> Walk(IGuiWidget widget) =>
        new[] { widget }.Concat(widget.Elements.SelectMany(Walk));

    /// <summary>The view model method an event calls, with its first argument (the tile, zone or palette cell).</summary>
    private static (string Method, object Argument) Handler(IGuiWidget widget, string eventName)
    {
        var handler = widget.Events[eventName];
        return (handler.Method.Name, handler.Arguments.Select(argument => argument.Value).FirstOrDefault());
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
