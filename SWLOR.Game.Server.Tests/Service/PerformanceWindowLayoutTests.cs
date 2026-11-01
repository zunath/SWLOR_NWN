using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Tests.Service;

public class PerformanceWindowLayoutTests
{
    // The smallest supported client is 1280x720 at UI scale 1.0.
    private const float ScreenWidth = 1280f;
    private const float ScreenHeight = 720f;

    private static IEnumerable<IGuiWindowDefinition> Windows()
    {
        yield return new PerformanceFootworkDefinition();
        yield return new PerformanceSessionDefinition();
        yield return new PerformanceOptionsDefinition();
        yield return new PerformanceWatchingDefinition();
    }

    [TestCaseSource(nameof(Windows))]
    public void Window_PassesTheLayoutRulesAndOpensOnA720pScreen(IGuiWindowDefinition definition)
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();

        var window = definition.BuildWindow();

        window.LayoutFindings.Should().BeEmpty();
        var geometry = window.InitialGeometry;
        (geometry.X + geometry.Width).Should().BeLessThanOrEqualTo(ScreenWidth);
        (geometry.Y + geometry.Height).Should().BeLessThanOrEqualTo(ScreenHeight);
    }
}
