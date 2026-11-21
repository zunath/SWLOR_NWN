using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>The City Affairs window, and the two City Hall windows that open it, build with no layout warnings.</summary>
public class CityAffairsWindowTests
{
    [Test]
    public void TheCityAffairsWindowBuildsWithoutLayoutWarnings()
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();

        var window = new CityAffairsDefinition().BuildWindow();

        window.Type.Should().Be(GuiWindowType.CityAffairs);
        window.LayoutFindings.Should().BeEmpty();
    }

    [Test]
    public void TheCityAffairsWindowOpensWithinA1280x720ClientWithRoomForEveryTab()
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();

        var geometry = new CityAffairsDefinition().BuildWindow().InitialGeometry;

        geometry.Height.Should().BeLessThanOrEqualTo(680f, "a window taller than the client opens with its title bar off the screen");
        geometry.Width.Should().BeLessThanOrEqualTo(1280f);
        // The client draws each of the four tabs about 148 pixels wide whatever width the tab row asks for, and the content
        // column loses about 64 pixels to its margin and scrollbar: a narrower window clips the Bounties tab and scrolls the bar.
        geometry.Width.Should().BeGreaterThanOrEqualTo(4 * 148f + 64f);
    }

    [Test]
    public void ManageCityBuildsWithoutLayoutWarnings()
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();

        new ManageCityDefinition().BuildWindow().LayoutFindings.Should().BeEmpty();
    }

    [Test]
    public void ManageCitizenshipBuildsWithoutLayoutWarnings()
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();

        new ManageCitizenshipDefinition().BuildWindow().LayoutFindings.Should().BeEmpty();
    }
}
