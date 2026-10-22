using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Tests.Service.Circuit.CircuitTestData;

namespace SWLOR.Game.Server.Tests.Service.Circuit;

/// <summary>Covers the Circuit window's definition and route and the text its buttons show.</summary>
public class CircuitWindowTests
{
    [Test]
    public void Engineering_IsRoutedToTheCircuitWindow_AndNoOtherSkillIs()
    {
        var route = CraftMinigameRouting.GetRoute(SkillType.Engineering);

        route.Window.Should().Be(GuiWindowType.CraftCircuit);
        route.Minigame.Should().Be(CraftMinigameType.Circuit);
        route.IsLegacy.Should().BeFalse();
        CraftMinigameRouting.GetRoute(SkillType.Smithery).Window.Should().NotBe(GuiWindowType.CraftCircuit);
        Enum.GetValues<GuiWindowType>().Count(window => window == GuiWindowType.CraftCircuit).Should().Be(1);
    }

    [Test]
    public void TheWindowBuildsWithoutLayoutFindings()
    {
        using var validationBuild = GuiLayoutValidator.BeginValidationOnlyBuild();

        var window = new CircuitCraftDefinition().BuildWindow();

        window.Type.Should().Be(GuiWindowType.CraftCircuit);
        window.LayoutFindings.Should().BeEmpty();
    }

    [Test]
    public void AButtonShowsItsCpCost_AndItsRowShowsTheResultThenTheOverloadRisk()
    {
        var engine = Engine(Setup(), 6);

        var tune = engine.Preview(CircuitActionIds.Tune);

        CircuitActionText.ButtonText(tune).Should().Be("Tune (3 CP)");
        CircuitActionText.ButtonText(engine.Preview(CircuitActionIds.Wire)).Should().Be("Wire");
        CircuitActionText.ResultText(tune).Should().Be("Calibration +129, Load +1, Integrity -3, CP -3");
        CircuitActionText.RiskText(tune).Should().Be("Overload 8%: Load to 0, Integrity -24.");
        CircuitActionText.RowText(tune).Should().Be(
            "Calibration +129, Load +1, Integrity -3, CP -3\nOverload 8%: Load to 0, Integrity -24.");
    }

    [Test]
    public void ASafeActionSaysItCannotOverload()
    {
        var engine = Engine(Setup(), 9);

        var discharge = engine.Preview(CircuitActionIds.DischargeAssembly);

        CircuitActionText.RiskText(discharge).Should().Be(CircuitActionText.NoRisk);
        CircuitActionText.ResultText(discharge).Should().Be("Assembly +43, Load -9, Integrity -4");
        CircuitActionText.RowText(engine.Preview(CircuitActionIds.Ground)).Should().Contain("Load -3");
    }

    [Test]
    public void TheFinishingWarningLeadsTheResult_AndAnOverloadThatBreaksThePieceIsCalledOut()
    {
        var finishing = Engine(Setup(baseProgress: 10, craftsmanship: 1000), 6).Preview(CircuitActionIds.Wire);
        CircuitActionText.ResultText(finishing).Should().StartWith(CircuitActionText.FinishesItem);
        CircuitActionText.RiskText(finishing).Should().EndWith(CircuitActionText.FinishesItem);

        var fragile = Engine(Setup(integrity: 4), 6).Preview(CircuitActionIds.Wire);
        CircuitActionText.ResultText(fragile).Should().NotContain("Finishes");
        CircuitActionText.RiskText(fragile).Should().EndWith(CircuitActionText.BreaksPiece);
    }

    [Test]
    public void ADisabledActionShowsWhyInPlaceOfTheResult_AndTheTooltipCarriesEverything()
    {
        var engine = Engine(Setup(rank: 5, level: 5), 0);

        var patch = engine.Preview(CircuitActionIds.Patch);

        CircuitActionText.ResultText(patch).Should().Be("Unlocks at Engineering rank 10.");
        CircuitActionText.RiskText(patch).Should().BeEmpty();

        var tooltip = CircuitActionText.Tooltip(Engine(Setup(), 8).Preview(CircuitActionIds.Wire));
        tooltip.Should().StartWith("Builds Assembly and adds 1 Load.");
        tooltip.Should().Contain("Overload 24%");
    }

    [Test]
    public void EveryTextTheWindowShowsIsAsciiPunctuation()
    {
        var engine = Engine(Setup(), 9);
        var texts = engine.PreviewAll().SelectMany(preview => new[]
        {
            CircuitActionText.ButtonText(preview), CircuitActionText.RowText(preview), CircuitActionText.Tooltip(preview),
            preview.Action.Description
        });

        texts.Should().OnlyContain(text => text.All(character => character < 128));
    }
}
