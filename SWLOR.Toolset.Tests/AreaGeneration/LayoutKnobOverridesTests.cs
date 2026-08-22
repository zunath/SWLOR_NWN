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

    [Test]
    public void ApplyTo_DisablingAccentsClearsEveryAccentTerrainPass()
    {
        var parameters = new MacroLayoutParameters
        {
            AccentTerrain = "Water",
            AccentDensity = 0.2,
            ChannelTerrain = "Water",
            AccentChannels = 2,
            PoolTerrain = "Water",
            PoolRegions = 2
        };
        var tileset = new DungeonTilesetProfile { AccentTerrain = "Water" };
        var overrides = new LayoutKnobOverrides { AccentEnabled = false };

        overrides.ApplyTo(parameters, tileset);

        parameters.AccentTerrain.Should().BeEmpty();
        parameters.AccentDensity.Should().Be(0);
        parameters.ChannelTerrain.Should().BeEmpty();
        parameters.AccentChannels.Should().Be(0);
        parameters.PoolTerrain.Should().BeEmpty();
        parameters.PoolRegions.Should().Be(0);
    }

    [Test]
    public void ApplyTo_ChannelOnlyAccentRemainsActiveWhenEnabled()
    {
        var parameters = new MacroLayoutParameters
        {
            AccentDensity = 0.2,
            AccentChannels = 1,
            PoolRegions = 1
        };
        var tileset = new DungeonTilesetProfile { ChannelTerrain = "Chasm" };
        var overrides = new LayoutKnobOverrides
        {
            AccentEnabled = true,
            AccentDensityPercent = 20
        };

        overrides.ApplyTo(parameters, tileset);

        parameters.AccentTerrain.Should().BeEmpty();
        parameters.AccentDensity.Should().Be(0);
        parameters.ChannelTerrain.Should().Be("Chasm");
        parameters.AccentChannels.Should().Be(1);
        parameters.PoolTerrain.Should().BeEmpty();
        parameters.PoolRegions.Should().Be(0);
    }
}
