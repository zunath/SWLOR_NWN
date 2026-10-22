using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

namespace SWLOR.Game.Server.Tests.Service.Kitchen;

/// <summary>Covers the Kitchen's value types: flavor vectors, the target zone's quality law, the cook's stats and the ingredient lines.</summary>
public class KitchenFlavorTests
{
    [Test]
    public void AFlavorVector_AddsSubtractsScalesAndClampsPerAxis()
    {
        var left = new FlavorVector(10, 20, 30);

        (left + new FlavorVector(1, 2, 3)).Should().Be(new FlavorVector(11, 22, 33));
        (left - new FlavorVector(1, 2, 3)).Should().Be(new FlavorVector(9, 18, 27));
        left.Scaled(50).Should().Be(new FlavorVector(5, 10, 15));
        new FlavorVector(13, 0, 2).Scaled(60).Should().Be(new FlavorVector(8, 0, 1), "7.8 and 1.2 round to the nearest whole number");
        new FlavorVector(13, 0, 2).Scaled(140).Should().Be(new FlavorVector(18, 0, 3));
        new FlavorVector(-5, 50, 130).Clamped(100).Should().Be(new FlavorVector(0, 50, 100));
        FlavorVector.Zero.IsZero.Should().BeTrue();
        left.IsZero.Should().BeFalse();
    }

    [Test]
    public void AFlavorVector_ReadsAndSetsEachAxis()
    {
        var vector = new FlavorVector(1, 2, 3);

        vector.Get(KitchenAxis.Savory).Should().Be(1);
        vector.Get(KitchenAxis.Sweet).Should().Be(2);
        vector.Get(KitchenAxis.Spice).Should().Be(3);
        vector.With(KitchenAxis.Sweet, 9).Should().Be(new FlavorVector(1, 9, 3));
    }

    [Test]
    public void TheDistanceBetweenTwoPoints_IsStraightLine()
    {
        new FlavorVector(0, 0, 0).DistanceTo(new FlavorVector(3, 4, 0)).Should().Be(5);
        new FlavorVector(30, 0, 16).Length.Should().BeApproximately(34, 0.01);
    }

    [Test]
    public void Quality_IsFullInsideTheInnerRadiusFallsInALineAndIsNothingAtTheOuterRadius()
    {
        var target = new KitchenTarget(new FlavorVector(50, 0, 0), 10, 30);

        target.QualityPercent(new FlavorVector(50, 0, 0)).Should().Be(100, "on the center");
        target.QualityPercent(new FlavorVector(60, 0, 0)).Should().Be(100, "exactly on the inner radius");
        target.QualityPercent(new FlavorVector(65, 0, 0)).Should().Be(75, "a quarter of the way down the slope");
        target.QualityPercent(new FlavorVector(70, 0, 0)).Should().Be(50, "halfway");
        target.QualityPercent(new FlavorVector(75, 0, 0)).Should().Be(25);
        target.QualityPercent(new FlavorVector(80, 0, 0)).Should().Be(0, "exactly on the outer radius");
        target.QualityPercent(new FlavorVector(100, 100, 100)).Should().Be(0, "far outside");
    }

    [Test]
    public void Quality_UsesDistanceInAllThreeAxes()
    {
        var target = new KitchenTarget(new FlavorVector(40, 40, 40), 5, 25);

        // 3-4-0 off on two axes is 5 away: inside the inner radius.
        target.QualityPercent(new FlavorVector(43, 44, 40)).Should().Be(100);
        // 12-9-8 off is 17 away: 8 of the 20-wide slope remain, 40%.
        target.QualityPercent(new FlavorVector(52, 49, 48)).Should().Be(40);
    }

    [Test]
    public void ATarget_ReportsItsPerfectAndToleranceRangesOnEachAxis()
    {
        var target = new KitchenTarget(new FlavorVector(30, 0, 95), 10, 30);

        target.PerfectRange(KitchenAxis.Savory).Should().Be((20, 40));
        target.ToleranceRange(KitchenAxis.Savory).Should().Be((0, 60), "the low end stops at zero");
        target.PerfectRange(KitchenAxis.Sweet).Should().Be((0, 10));
        target.ToleranceRange(KitchenAxis.Spice).Should().Be((65, 100), "the high end stops at the top of the axis");
    }

    [Test]
    public void ATarget_NeedsAnOuterRadiusLargerThanItsInnerRadius()
    {
        var invalid = () => new KitchenTarget(FlavorVector.Zero, 10, 10);

        invalid.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestCase(1, 0, 0)]
    [TestCase(10, 0, 7)]
    [TestCase(20, 40, 55)]
    [TestCase(24, 40, 58)]
    [TestCase(25, 40, 89)]
    [TestCase(50, 37, 105)]
    public void MaxCp_IsGearCpPlusThreeQuartersOfRankAndThePassiveFromRank25(int rank, int gearCp, int expected)
    {
        new KitchenStats(rank, 0, 0, gearCp).MaxCp.Should().Be(expected);
    }

    [TestCase(0, 40)]
    [TestCase(13, 29)]
    [TestCase(29, 21)]
    [TestCase(3000, 0)]
    public void ControlNarrowsThePushSpreadButOnlyHugeControlRemovesIt(int control, int spread)
    {
        new KitchenStats(20, 0, control, 0).PushSpreadPercent.Should().Be(spread);
    }

    [Test]
    public void LevelFactor_RisesWithRankAboveTheRecipeAndStaysInsideItsBounds()
    {
        var cook = new KitchenStats(20, 0, 0, 0);

        cook.LevelFactor(20).Should().Be(1.0);
        cook.LevelFactor(17).Should().BeApproximately(1.15, 1e-9);
        cook.LevelFactor(23).Should().BeApproximately(0.85, 1e-9);
        cook.LevelFactor(1).Should().Be(KitchenTuning.LevelFactorMax);
        cook.LevelFactor(60).Should().Be(KitchenTuning.LevelFactorMin);
    }

    [Test]
    public void Doneness_GrowsWithCraftsmanshipAndTheLevelFactor()
    {
        new KitchenStats(20, 0, 0, 0).Doneness(12, 20).Should().Be(12);
        var factor = 1 + KitchenTuning.CraftsmanshipDonenessPerPoint * 30;
        new KitchenStats(20, 30, 0, 0).Doneness(12, 20).Should()
            .Be((int)Math.Round(12 * factor, MidpointRounding.AwayFromZero), $"12 x {factor}");
        new KitchenStats(20, 0, 0, 0).Doneness(12, 17).Should().Be(14, "12 x 1.15 = 13.8");
    }

    [Test]
    public void AnIngredientLine_IsAddedInAtMostFourPortionsWhosePushesAddUpToTheWholeStack()
    {
        var line = new KitchenIngredientLine(
            "thune_meat", "Thune Meat", 10, new KitchenComponentFlavor(KitchenIngredientKind.Flavor, new FlavorVector(13, 0, 2)));

        line.Portions.Should().Be(4);
        line.PortionVector.Should().Be(new FlavorVector(33, 0, 5), "13 x 10 / 4 = 32.5 and 2 x 10 / 4 = 5");
        line.TotalVector.Should().Be(new FlavorVector(130, 0, 20));
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(10)]
    [TestCase(13)]
    [TestCase(16)]
    public void ThePortionsOfAStackAlwaysAddUpToTheWholeStack(int quantity)
    {
        var flavor = new KitchenComponentFlavor(KitchenIngredientKind.Flavor, new FlavorVector(11, 1, 2));
        var line = new KitchenIngredientLine("fish", "Fish", quantity, flavor);

        var portions = Enumerable.Range(0, line.Portions).Select(line.PortionVectorAt).ToList();

        portions.Aggregate(FlavorVector.Zero, (total, portion) => total + portion).Should().Be(line.TotalVector);
        portions.Select(portion => portion.Savory).Should().BeInDescendingOrder();
        portions.Max(portion => portion.Savory - portions.Min(other => other.Savory)).Should().BeLessThanOrEqualTo(1);
        Enumerable.Range(0, line.Portions).Sum(line.DilutePercentAt)
            .Should().Be(KitchenTuning.DilutePercentPerUnit * quantity);
    }

    [Test]
    public void ASixUnitStack_SplitsItsRemainderOverTheFirstPortions()
    {
        var flavor = new KitchenComponentFlavor(KitchenIngredientKind.Flavor, new FlavorVector(11, 1, 2));
        var line = new KitchenIngredientLine("bream", "Bream", 6, flavor);

        Enumerable.Range(0, 4).Select(line.PortionVectorAt).Should().Equal(
            new FlavorVector(17, 2, 3), new FlavorVector(17, 2, 3), new FlavorVector(16, 1, 3), new FlavorVector(16, 1, 3));
        line.TotalVector.Should().Be(new FlavorVector(66, 6, 12));
    }

    [Test]
    public void AnIngredientLine_OfThreeUnitsIsThreePortionsOfOneUnitEach()
    {
        var line = KitchenTestKit.Meat();

        line.Portions.Should().Be(3);
        line.PortionVector.Should().Be(new FlavorVector(13, 0, 2));
    }

    [Test]
    public void ADilutingLine_ThinsByAQuarterPerUnit()
    {
        KitchenTestKit.Water(1).DilutePercent.Should().Be(25);
        KitchenTestKit.Water(2).DilutePercent.Should().Be(25);
        KitchenTestKit.Water(6).Portions.Should().Be(4);
        KitchenTestKit.Water(6).DilutePercent.Should().Be(38, "6 units over 4 portions is 37.5 percent each");
    }

    [Test]
    public void AStock_UsesPortionsWithoutChangingTheOriginalAndComparesByValue()
    {
        var stock = new KitchenStock(new[] { 3, 2 });

        var used = stock.Use(1);

        stock.Remaining(1).Should().Be(2);
        used.Remaining(1).Should().Be(1);
        used.Should().Be(new KitchenStock(new[] { 3, 1 }));
        used.GetHashCode().Should().Be(new KitchenStock(new[] { 3, 1 }).GetHashCode());
        stock.Should().NotBe(used);
    }

    [Test]
    public void AStock_RefusesToUsePortionsThatAreGone()
    {
        var empty = new KitchenStock(new[] { 0 });

        var use = () => empty.Use(0);

        use.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void TheComponentTable_ThinsWithWaterAndPushesWithEverythingElse()
    {
        KitchenComponentTable.Get(KitchenComponentTable.Water).Kind.Should().Be(KitchenIngredientKind.Dilute);
        KitchenComponentTable.Get("mynock_meat").Kind.Should().Be(KitchenIngredientKind.Flavor);
        KitchenComponentTable.Get("MYNOCK_MEAT").Should().Be(KitchenComponentTable.Get("mynock_meat"), "resrefs are not case sensitive");
        KitchenComponentTable.Get("not_a_real_component").Should().Be(KitchenComponentTable.Unlisted);
    }
}
