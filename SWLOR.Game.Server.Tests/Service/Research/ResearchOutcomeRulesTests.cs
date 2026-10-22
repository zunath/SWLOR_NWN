using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Research;

namespace SWLOR.Game.Server.Tests.Service.Research;

/// <summary>Covers how evidence becomes a winner and a magnitude inside today's dice range.</summary>
public class ResearchOutcomeRulesTests
{
    private static Dictionary<ResearchCandidate, int> Evidence(int runs, int credit, int time) => new()
    {
        [ResearchCandidate.LicensedRuns] = runs,
        [ResearchCandidate.CreditReduction] = credit,
        [ResearchCandidate.TimeReduction] = time
    };

    [TestCase(0, 0, 0)]
    [TestCase(1, 1, 1)]
    [TestCase(2, 1, 3)]
    [TestCase(3, 2, 5)]
    [TestCase(4, 2, 6)]
    [TestCase(5, 3, 8)]
    [TestCase(6, 3, 10)]
    [TestCase(9, 3, 10)]
    public void AMagnitudeRisesWithEvidenceFromTheDiceMinimumToTheDiceMaximum(int evidence, int runs, int reduction)
    {
        ResearchOutcomeRules.Magnitude(ResearchCandidate.LicensedRuns, evidence).Should().Be(runs);
        ResearchOutcomeRules.Magnitude(ResearchCandidate.CreditReduction, evidence).Should().Be(reduction);
        ResearchOutcomeRules.Magnitude(ResearchCandidate.TimeReduction, evidence).Should().Be(reduction);
    }

    [Test]
    public void NoMagnitudeEverExceedsTodaysDice()
    {
        for (var evidence = 0; evidence <= 20; evidence++)
        {
            ResearchOutcomeRules.Magnitude(ResearchCandidate.LicensedRuns, evidence).Should().BeLessThanOrEqualTo(3);
            ResearchOutcomeRules.Magnitude(ResearchCandidate.CreditReduction, evidence).Should().BeLessThanOrEqualTo(10);
            ResearchOutcomeRules.Magnitude(ResearchCandidate.TimeReduction, evidence).Should().BeLessThanOrEqualTo(10);
        }

        ResearchOutcomeRules.Magnitude(ResearchCandidate.FixedGrant, 5).Should().Be(0, "a fixed grant has no size");
    }

    [Test]
    public void AMagnitudeNeverFallsAsEvidenceRises()
    {
        foreach (var candidate in new[] { ResearchCandidate.LicensedRuns, ResearchCandidate.CreditReduction, ResearchCandidate.TimeReduction })
        {
            var last = 0;
            for (var evidence = 0; evidence <= 8; evidence++)
            {
                var magnitude = ResearchOutcomeRules.Magnitude(candidate, evidence);
                magnitude.Should().BeGreaterThanOrEqualTo(last);
                last = magnitude;
            }
        }
    }

    [Test]
    public void AFixedLevelsWinnerIsItsOwnGrantWithNoSize()
    {
        var (winner, magnitude) = ResearchOutcomeRules.Resolve(1, new Dictionary<ResearchCandidate, int>(), new ScriptedMinigameRandom());

        winner.Should().Be(ResearchCandidate.FixedGrant);
        magnitude.Should().Be(0);
    }

    [Test]
    public void TheBestEvidencedCandidateWinsWithoutARoll()
    {
        var random = new ScriptedMinigameRandom();

        ResearchOutcomeRules.Resolve(3, Evidence(1, 4, 2), random).Should().Be((ResearchCandidate.CreditReduction, 6));
        ResearchOutcomeRules.Resolve(5, Evidence(3, 1, 0), random).Should().Be((ResearchCandidate.LicensedRuns, 2));
        ResearchOutcomeRules.Resolve(8, Evidence(0, 0, 6), random).Should().Be((ResearchCandidate.TimeReduction, 10));
    }

    [Test]
    public void NoEvidenceOnAnyCandidateMeansNoWinner()
    {
        var (winner, magnitude) = ResearchOutcomeRules.Resolve(3, Evidence(0, 0, 0), new ScriptedMinigameRandom());

        winner.Should().Be(ResearchCandidate.None);
        magnitude.Should().Be(0);
    }

    [TestCase(0, ResearchCandidate.LicensedRuns)]
    [TestCase(299, ResearchCandidate.LicensedRuns)]
    [TestCase(300, ResearchCandidate.CreditReduction)]
    [TestCase(699, ResearchCandidate.CreditReduction)]
    public void ATieBetweenAllThreeGoesByTodaysOdds(int roll, ResearchCandidate expected)
    {
        var (winner, magnitude) = ResearchOutcomeRules.Resolve(3, Evidence(2, 2, 2), new ScriptedMinigameRandom(roll));

        winner.Should().Be(expected);
        magnitude.Should().Be(ResearchOutcomeRules.Magnitude(expected, 2));
    }

    [TestCase(0, ResearchCandidate.CreditReduction)]
    [TestCase(399, ResearchCandidate.CreditReduction)]
    [TestCase(400, ResearchCandidate.TimeReduction)]
    public void ATieBetweenTwoGoesByTheirShareOfTodaysOdds(int roll, ResearchCandidate expected)
    {
        var (winner, _) = ResearchOutcomeRules.Resolve(5, Evidence(1, 3, 3), new ScriptedMinigameRandom(roll));

        winner.Should().Be(expected);
    }

    [Test]
    public void TheLeadersAreTheCandidatesSharingTheMostEvidence()
    {
        ResearchOutcomeRules.Leaders(3, Evidence(2, 2, 1)).Should().Equal(ResearchCandidate.LicensedRuns, ResearchCandidate.CreditReduction);
        ResearchOutcomeRules.Leaders(3, Evidence(0, 0, 0)).Should().BeEmpty();
        ResearchOutcomeRules.Leaders(3, Evidence(0, 0, 4)).Should().Equal(ResearchCandidate.TimeReduction);
    }

    [Test]
    public void TheTieBreakIsFairOverManyRollsAtTheOddsOfToday()
    {
        var random = new SeededMinigameRandom(3);
        var credit = 0;
        const int Draws = 40_000;
        for (var draw = 0; draw < Draws; draw++)
        {
            if (ResearchOutcomeRules.Resolve(3, Evidence(0, 2, 2), random).Winner == ResearchCandidate.CreditReduction)
                credit++;
        }

        (credit / (double)Draws).Should().BeApproximately(0.5, 0.015);
    }
}
