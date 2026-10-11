using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;

namespace SWLOR.Game.Server.Tests.Feature;

public class SpendingAreaExitLocationTests
{
    [TestCase(15f, 25f, 0f, 4, 4, true)]
    [TestCase(0f, 0f, 0f, 4, 4, true)]
    [TestCase(40f, 40f, 0f, 4, 4, true)]
    [TestCase(40.5f, 10f, 0f, 4, 4, false)]
    [TestCase(10f, 80f, 0f, 4, 4, false)]
    [TestCase(-1f, 10f, 0f, 4, 4, false)]
    [TestCase(10f, -0.1f, 0f, 4, 4, false)]
    [TestCase(10f, 10f, 0f, 0, 0, false)]
    [TestCase(float.NaN, 10f, 0f, 4, 4, false)]
    [TestCase(10f, float.PositiveInfinity, 0f, 4, 4, false)]
    [TestCase(10f, 10f, float.NaN, 4, 4, false)]
    public void IsPositionInsideArea_RejectsPositionsOutsideTileBounds(
        float x, float y, float z, int widthTiles, int heightTiles, bool expected)
    {
        CharacterFullRebuildViewModel.IsPositionInsideArea(x, y, z, widthTiles, heightTiles)
            .Should().Be(expected);
    }
}
