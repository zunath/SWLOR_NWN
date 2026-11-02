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
