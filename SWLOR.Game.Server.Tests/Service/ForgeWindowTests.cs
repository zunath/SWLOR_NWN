using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Forge;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>Covers the Plasma Forge window's layout and the words it shows for the engine's previews.</summary>
public class ForgeWindowTests
{
    private static ForgeSetup Setup(int startingThermal = 50, int cooling = 0, int shapeShare = 50, int enhancementShapePercent = 0) =>
        new(new ForgeParameters(50, 30, cooling, startingThermal, shapeShare), 20, 20, 0, 0, 0, enhancementShapePercent);

    private static ForgeEngine Engine(ForgeSetup setup = null) =>
        new(setup ?? Setup(), new ScriptedMinigameRandom(Enumerable.Repeat(100, 50).ToArray()));

    [Test]
    public void TheWindowBuildsWithoutLayoutFindings()
    {
        using var validationBuild = GuiLayoutValidator.BeginValidationOnlyBuild();

        var window = new ForgeDefinition().BuildWindow();

        window.Type.Should().Be(GuiWindowType.Forge);
        window.LayoutFindings.Should().BeEmpty();
    }

    [Test]
    public void ThePreviewTextShowsEveryChangeAndTheFinishesTheItemWarning()
    {
        var engine = Engine(Setup(startingThermal: 65));
        for (var press = 0; press < 4; press++)
            engine.Perform(ForgeActionIds.HydroPress);
        engine.Meters.Progress.Value.Should().Be(816);

        var preview = engine.Preview(ForgeActionIds.HydroPress);
        var text = ForgePreviewText.Describe(preview, engine.Setup);

        preview.FinishesItem.Should().BeTrue();
        text.Should().Be("Shape +18.4%, Integrity -5. FINISHES THE ITEM");
    }

    [Test]
    public void ThePreviewTextShowsThermalCostsAndCooling()
    {
        var engine = Engine(Setup(startingThermal: 40, cooling: 5));

        ForgePreviewText.Describe(engine.Preview(ForgeActionIds.RampPlasma), engine.Setup)
            .Should().Be("Integrity -2, CP -5, Thermal 40 -> 60");
        ForgePreviewText.Describe(engine.Preview(ForgeActionIds.VibroHone), engine.Setup)
            .Should().Be("Finish +14.0%, Integrity -4, Thermal 40 -> 35");
    }

    [Test]
    public void ThePreviewTextWarnsWhenAnActionMayBreakThePiece()
    {
        var engine = Engine(Setup(startingThermal: 100, shapeShare: 60, enhancementShapePercent: 100));
        while (engine.Meters.Integrity.Value > 22)
            engine.Perform(ForgeActionIds.HydroPress);

        var preview = engine.Preview(ForgeActionIds.HydroPress);

        preview.CanBreakItem.Should().BeTrue();
        ForgePreviewText.Describe(preview, engine.Setup).Should().EndWith(ForgePreviewText.BreaksItem);
    }

    [TestCase(0, 80, "No enhancement slots.")]
    [TestCase(1, 49, "Enhancements: Slot 1 not met (50%)")]
    [TestCase(1, 50, "Enhancements: Slot 1 met (50%)")]
    [TestCase(2, 84, "Enhancements: Slot 1 met (50%), Slot 2 not met (85%)")]
    [TestCase(3, 85, "Enhancements: Slot 1 met (50%), Slot 2 met (85%), Slot 3 met (85%)")]
    public void TheThresholdLineShowsWhichSlotsTheFinishSoFarMeets(int slots, int qualityPercent, string expected)
    {
        ForgePreviewText.Thresholds(slots, qualityPercent).Should().Be(expected);
    }

    [Test]
    public void ADisabledActionShowsWhyInPlaceOfAResult()
    {
        var engine = Engine();

        ForgePreviewText.Describe(engine.Preview(ForgeActionIds.CryoQuench), engine.Setup)
            .Should().Be("The piece must be 80% shaped first.");
        ForgePreviewText.Describe(engine.Preview(ForgeActionIds.Rebond), engine.Setup)
            .Should().Be("Integrity is already full.");
    }

    [Test]
    public void TheButtonAndTooltipCarryTheCostsAndTheActionsDescription()
    {
        var engine = Engine();
        var ramp = engine.Preview(ForgeActionIds.RampPlasma);
        var press = engine.Preview(ForgeActionIds.HydroPress);

        ForgePreviewText.ButtonText(ramp).Should().Be("Ramp Plasma [5 CP]");
        ForgePreviewText.ButtonText(press).Should().Be("Hydro-Press");
        ForgePreviewText.Tooltip(ramp).Should().Contain("Raises Thermal").And.Contain("5 CP").And.Contain("2 Integrity");
        ForgePreviewText.Tooltip(press).Should().Contain("5 Integrity").And.NotContain("CP");
    }

    [Test]
    public void EveryWordTheWindowShowsIsPlainAscii()
    {
        var engine = Engine();
        var strings = engine.PreviewAll()
            .SelectMany(preview => new[]
            {
                ForgePreviewText.Describe(preview, engine.Setup),
                ForgePreviewText.Tooltip(preview),
                ForgePreviewText.ButtonText(preview),
                preview.Action.Description
            })
            .Append(ForgePreviewText.FinishesItem)
            .Append(ForgePreviewText.BreaksItem);

        foreach (var text in strings)
            text.Should().MatchRegex("^[\\x20-\\x7E]*$");
    }
}
