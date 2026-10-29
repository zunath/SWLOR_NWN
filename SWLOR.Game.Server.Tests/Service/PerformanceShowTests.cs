using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.PerformanceService;
using static SWLOR.Game.Server.Tests.Service.PerformanceTestSnapshots;

namespace SWLOR.Game.Server.Tests.Service;

public class PerformanceShowTests
{
    [Test]
    public void Prepare_MusiciansInEachOthersRangeFormABandThatPlaysTheEarliestStartersSong()
    {
        var leader = Musician(1, minutesAgo: 10, song: 100);
        var second = Musician(2, minutesAgo: 5, song: 200, x: 5f);
        var third = Musician(3, minutesAgo: 1, song: 300, x: 10f);

        PerformanceShow.Prepare(new[] { second, third, leader });

        leader.BandTrackId.Should().Be(100);
        second.BandTrackId.Should().Be(100);
        third.BandTrackId.Should().Be(100);
    }

    [Test]
    public void Prepare_MusiciansLinkedThroughAThirdAreOneBand()
    {
        var left = Musician(1, minutesAgo: 10, song: 100, x: 0f);
        var middle = Musician(2, minutesAgo: 5, song: 200, x: 15f);
        var right = Musician(3, minutesAgo: 1, song: 300, x: 30f);

        PerformanceShow.Prepare(new[] { left, middle, right });

        right.BandTrackId.Should().Be(100);
    }

    [Test]
    public void Prepare_AMusicianOutOfRangeKeepsTheirOwnSong()
    {
        var band = Musician(1, minutesAgo: 10, song: 100, x: 0f);
        var alone = Musician(2, minutesAgo: 20, song: 200, x: 60f);

        PerformanceShow.Prepare(new[] { band, alone });

        band.BandTrackId.Should().Be(100);
        alone.BandTrackId.Should().Be(200);
    }

    [Test]
    public void Prepare_TheLeaderIsWhoeverStartedFirstEvenWithAHigherId()
    {
        var lowId = Musician(1, minutesAgo: 1, song: 100);
        var highId = Musician(9, minutesAgo: 30, song: 900, x: 3f);

        PerformanceShow.Prepare(new[] { lowId, highId });

        lowId.BandTrackId.Should().Be(900);
    }

    [Test]
    public void Prepare_DancersHaveNoBandSong()
    {
        var dancer = Dancer(1);

        PerformanceShow.Prepare(new[] { dancer, Musician(2) });

        dancer.BandTrackId.Should().BeNull();
    }

    [Test]
    public void Prepare_ADancersPartnerIsTheStrongestOtherDancerInMutualRange()
    {
        var dancer = Dancer(1, xpScaled: 8);
        var weak = Dancer(2, xpScaled: 8, x: 5f);
        var strong = Dancer(3, xpScaled: 22, x: 8f);
        var farAway = Dancer(4, xpScaled: 30, x: 50f);

        PerformanceShow.Prepare(new[] { dancer, weak, strong, farAway });

        dancer.Partner.Should().BeSameAs(strong);
        farAway.Partner.Should().BeNull();
    }

    [Test]
    public void Prepare_AMusiciansPartnerIsAnotherMemberOfTheirBand()
    {
        var musician = Musician(1, xpScaled: 8);
        var bandmate = Musician(2, xpScaled: 15, x: 5f);
        var otherBand = Musician(3, xpScaled: 30, x: 80f);

        PerformanceShow.Prepare(new[] { musician, bandmate, otherBand });

        musician.Partner.Should().BeSameAs(bandmate);
        otherBand.Partner.Should().BeNull();
    }

    [Test]
    public void Prepare_HarmonyNeedsADancerWithinTheMusiciansRange()
    {
        var withDancer = Musician(1, x: 0f);
        var withoutDancer = Musician(2, x: 100f);
        var dancer = Dancer(3, x: 10f);

        PerformanceShow.Prepare(new[] { withDancer, withoutDancer, dancer });

        withDancer.HarmonyActive.Should().BeTrue();
        withoutDancer.HarmonyActive.Should().BeFalse();
        dancer.HarmonyActive.Should().BeFalse();
    }

    [Test]
    public void Prepare_ResetsWhatAnEarlierTickLeftOnTheSnapshots()
    {
        var musician = Musician(1);
        var dancer = Dancer(2, x: 5f);
        PerformanceShow.Prepare(new[] { musician, dancer });
        musician.HarmonyActive.Should().BeTrue();

        PerformanceShow.Prepare(new[] { musician });

        musician.HarmonyActive.Should().BeFalse();
        musician.Partner.Should().BeNull();
    }
}
