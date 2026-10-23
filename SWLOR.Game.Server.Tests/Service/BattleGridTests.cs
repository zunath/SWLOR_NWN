using System.Linq;
using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The DM battle grid tiles 10 m placement-grid sections (20 x 20 squares at scale 1) side by side, so their
/// centers must meet edge to edge at every square size, and the grid lines must stay on square multiples.
/// </summary>
public class BattleGridTests
{
    [Test]
    public void SectionsMeetEdgeToEdgeAroundTheCenter()
    {
        var settings = new BattleGridSettings(2f, 3, 2, 0.2f);

        var centers = BattleGrid.GetSectionCenters(new Vector3(100f, 50f, 1.5f), settings);

        // 2 m squares make 40 m sections; three wide and two long centered on (100, 50).
        centers.Select(c => c.X).Distinct().Should().BeEquivalentTo(new[] { 60f, 100f, 140f });
        centers.Select(c => c.Y).Distinct().Should().BeEquivalentTo(new[] { 30f, 70f });
        centers.Should().HaveCount(6);
        centers.Should().OnlyContain(c => c.Z == 1.5f, "every section stays level with the placed spot");
    }

    [Test]
    public void GridEdgesSpanTheReportedSquareCount()
    {
        var settings = new BattleGridSettings(1.5f, 4, 1, 0f);
        var sectionSize = settings.SquareSize * BattleGrid.SquaresPerSection;

        var centers = BattleGrid.GetSectionCenters(Vector3.Zero, settings);

        var west = centers.Min(c => c.X) - sectionSize / 2f;
        var east = centers.Max(c => c.X) + sectionSize / 2f;
        (east - west).Should().BeApproximately(settings.SectionsWide * BattleGrid.SquaresPerSection * settings.SquareSize, 0.001f);
        west.Should().BeApproximately(-east, 0.001f, "the grid is centered on its placed spot");
    }

    [TestCase(77.3f, 112.9f, 2f, 78f, 112f)]
    [TestCase(77.3f, 112.9f, 0.5f, 77.5f, 113f)]
    [TestCase(-3.2f, 4.9f, 5f, -5f, 5f)]
    public void CenterSnapsToTheNearestSquareCorner(float x, float y, float squareSize, float expectedX, float expectedY)
    {
        var snapped = BattleGrid.SnapCenter(new Vector3(x, y, 2.25f), squareSize);

        snapped.X.Should().BeApproximately(expectedX, 0.001f);
        snapped.Y.Should().BeApproximately(expectedY, 0.001f);
        snapped.Z.Should().Be(2.25f, "snapping keeps the placed height");
    }

    [Test]
    public void SettingsStayInsideTheSupportedRanges()
    {
        var low = BattleGrid.Clamp(new BattleGridSettings(0f, 0, -2, -20f));
        var high = BattleGrid.Clamp(new BattleGridSettings(50f, 40, 9, 30f));

        low.Should().Be(new BattleGridSettings(BattleGrid.MinSquareSize, BattleGrid.MinSections, BattleGrid.MinSections, BattleGrid.MinHeight));
        high.Should().Be(new BattleGridSettings(BattleGrid.MaxSquareSize, BattleGrid.MaxSections, BattleGrid.MaxSections, BattleGrid.MaxHeight));
    }

    [Test]
    public void ClampRoundsAwayFloatingPointDrift()
    {
        // Repeated 0.1 m height steps drift off their decimal value; the label and stored height must not.
        var height = 0f;
        for (var step = 0; step < 7; step++)
            height += BattleGrid.HeightStep;

        var clamped = BattleGrid.Clamp(new BattleGridSettings(1.5f + 0.0001f, 2, 2, height));

        clamped.Height.Should().BeApproximately(0.7f, 0.00001f);
        clamped.SquareSize.Should().Be(1.5f);
    }

    [Test]
    public void LargestGridStaysWithinTheAnchorBudget()
    {
        BattleGrid.GetSectionCenters(Vector3.Zero, new BattleGridSettings(BattleGrid.MaxSquareSize, BattleGrid.MaxSections, BattleGrid.MaxSections, 0f))
            .Should().HaveCount(64, "each section is one invisible placeable");
    }
}
