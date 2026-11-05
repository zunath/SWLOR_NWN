using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.PerformanceService;
using static SWLOR.Game.Server.Tests.Service.PerformanceTestSnapshots;

namespace SWLOR.Game.Server.Tests.Service;

public class PerformanceFollowTests
{
    [Test]
    public void Choose_DefaultsToTheDancerWithTheHighestScaledXpBonus()
    {
        var weak = Dancer(1, xpScaled: 8);
        var strong = Dancer(2, xpScaled: 30);
        var middle = Dancer(3, xpScaled: 15);

        PerformanceFollow.Choose(new[] { weak, strong, middle }, PerformanceKind.Dance, 0, 0).Should().BeSameAs(strong);
    }

    [Test]
    public void Choose_OnlyConsidersTheRequestedKind()
    {
        var dancer = Dancer(1, xpScaled: 8);
        var musician = Musician(2, xpScaled: 30);
        var candidates = new[] { dancer, musician };

        PerformanceFollow.Choose(candidates, PerformanceKind.Dance, 0, 0).Should().BeSameAs(dancer);
        PerformanceFollow.Choose(candidates, PerformanceKind.Music, 0, 0).Should().BeSameAs(musician);
        PerformanceFollow.Choose(new[] { dancer }, PerformanceKind.Music, 0, 0).Should().BeNull();
        PerformanceFollow.Choose(Array.Empty<PerformerSnapshot>(), PerformanceKind.Dance, 0, 0).Should().BeNull();
    }

    [Test]
    public void Choose_KeepsTheWatchersPickWhileThePerformerIsStillACandidate()
    {
        var weak = Dancer(1, xpScaled: 8);
        var strong = Dancer(2, xpScaled: 30);

        PerformanceFollow.Choose(new[] { weak, strong }, PerformanceKind.Dance, 1, 2).Should().BeSameAs(weak);
    }

    [Test]
    public void Choose_FallsBackToTheStrongestWhenThePickedPerformerIsNoLongerACandidate()
    {
        var strong = Dancer(2, xpScaled: 30);
        var weak = Dancer(3, xpScaled: 8);

        PerformanceFollow.Choose(new[] { weak, strong }, PerformanceKind.Dance, 1, 0).Should().BeSameAs(strong);
    }

    [Test]
    public void Choose_IgnoresAPickOfTheWrongKind()
    {
        var dancer = Dancer(1, xpScaled: 8);
        var musician = Musician(2, xpScaled: 30);

        // The stored dancer choice must never turn into a musician.
        PerformanceFollow.Choose(new[] { dancer, musician }, PerformanceKind.Dance, 2, 0).Should().BeSameAs(dancer);
    }

    [Test]
    public void Choose_TiesGoToTheOneAlreadyFollowed()
    {
        var first = Dancer(1, xpScaled: 15, minutesAgo: 10);
        var second = Dancer(2, xpScaled: 15, minutesAgo: 1);

        PerformanceFollow.Choose(new[] { first, second }, PerformanceKind.Dance, 0, 2).Should().BeSameAs(second);
        PerformanceFollow.Choose(new[] { first, second }, PerformanceKind.Dance, 0, 1).Should().BeSameAs(first);
    }

    [Test]
    public void Choose_TiesWithNoHistoryGoToTheEarlierStartThenTheLowerId()
    {
        var late = Dancer(1, xpScaled: 15, minutesAgo: 1);
        var early = Dancer(5, xpScaled: 15, minutesAgo: 10);
        var alsoEarly = Dancer(4, xpScaled: 15, minutesAgo: 10);

        PerformanceFollow.Choose(new[] { late, early }, PerformanceKind.Dance, 0, 0).Should().BeSameAs(early);
        PerformanceFollow.Choose(new[] { late, early, alsoEarly }, PerformanceKind.Dance, 0, 0).Should().BeSameAs(alsoEarly);
    }

    [Test]
    public void Choose_ABetterBonusBeatsTheOneAlreadyFollowed()
    {
        var followed = Dancer(1, xpScaled: 8);
        var better = Dancer(2, xpScaled: 15);

        PerformanceFollow.Choose(new[] { followed, better }, PerformanceKind.Dance, 0, 1).Should().BeSameAs(better);
    }

    [Test]
    public void TroupeCount_IncludesEveryPerformerInRangeFollowedOrNot()
    {
        var candidates = new[] { Dancer(1), Dancer(2), Musician(3), Musician(4) };

        PerformanceAudienceRules.TroupeCount(candidates).Should().Be(4);
        PerformanceMath.TroupeBonus(PerformanceAudienceRules.TroupeCount(candidates)).Should().BeApproximately(0.30, 1e-9);
        PerformanceMath.TroupeBonus(PerformanceAudienceRules.TroupeCount(new[] { Dancer(1) })).Should().Be(0.0);
    }
}
