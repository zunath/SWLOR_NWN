using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Authoring;

namespace SWLOR.Toolset.Tests.AreaGeneration;

public sealed class LayoutKnobOverridesTests
{
    [TestCase(2, null)]
    [TestCase(1, "Road")]
    public void ApplyTo_PreservesTheTilesetCorridorWidthFloor(
        int minimumOpeningWidth,
        string? roadCrosser)
    {
        var parameters = new MacroLayoutParameters();
        var tileset = new DungeonTilesetProfile
        {
            MinimumOpeningWidth = minimumOpeningWidth,
            RoadCrosser = roadCrosser
        };
        var overrides = new LayoutKnobOverrides { CorridorWidth = 1 };

        overrides.ApplyTo(parameters, tileset);

        parameters.CorridorWidth.Should().Be(2);
    }
}
