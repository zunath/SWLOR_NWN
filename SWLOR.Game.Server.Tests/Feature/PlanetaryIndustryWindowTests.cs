using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>The Holdings window (the Planetary Depot row) and the Trade Exchange window (the port line) build with no layout warnings.</summary>
public class PlanetaryIndustryWindowTests
{
    [Test]
    public void ThePlanetaryHoldingsWindowBuildsWithoutLayoutWarnings()
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();

        var window = new PlanetaryHoldingsDefinition().BuildWindow();

        window.Type.Should().Be(GuiWindowType.PlanetaryHoldings);
        window.LayoutFindings.Should().BeEmpty();
    }

    [Test]
    public void TheTradeExchangeWindowBuildsWithoutLayoutWarnings()
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();

        var window = new TradeExchangeDefinition().BuildWindow();

        window.Type.Should().Be(GuiWindowType.TradeExchange);
        window.LayoutFindings.Should().BeEmpty();
    }
}
