using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Research;

namespace SWLOR.Game.Server.Tests.Service.Research;

/// <summary>Covers how evidence becomes a winner and a size inside today's d10, and how licensed runs stay out of it.</summary>
public class ResearchOutcomeRulesTests
{
    private static Dictionary<ResearchCandidate, int> Evidence(int credit, int time) => new()
    {
        [ResearchCandidate.CreditReduction] = credit,
        [ResearchCandidate.TimeReduction] = time
    };

    [TestCase(0, 0)]
    [TestCase(1, 1)]
    [TestCase(2, 3)]
    [TestCase(3, 5)]
    [TestCase(4, 6)]
    [TestCase(5, 8)]
    [TestCase(6, 10)]
    [TestCase(9, 10)]
    public void AReductionRisesWithEvidenceFromTheDiceMinimumToTheDiceMaximum(int evidence, int reduction)
    {
        ResearchOutcomeRules.Magnitude(ResearchCandidate.CreditReduction, evidence).Should().Be(reduction);
        ResearchOutcomeRules.Magnitude(ResearchCandidate.TimeReduction, evidence).Should().Be(reduction);
    }

    [Test]
    public void NoReductionEverExceedsTodaysDice()
    {
        for (var evidence = 0; evidence <= 20; evidence++)
        {
            ResearchOutcomeRules.Magnitude(ResearchCandidate.CreditReduction, evidence).Should().BeLessThanOrEqualTo(10);
            ResearchOutcomeRules.Magnitude(ResearchCandidate.TimeReduction, evidence).Should().BeLessThanOrEqualTo(10);
        }
    }

    [Test]
    public void FixedGrantsAndLicensedRunsHaveNoEvidenceBasedSize()
    {
        ResearchOutcomeRules.Magnitude(ResearchCandidate.FixedGrant, 5).Should().Be(0);
        ResearchOutcomeRules.Magnitude(ResearchCandidate.LicensedRuns, 5).Should().Be(0, "licensed runs are rolled when the experiment opens");
    }

    [Test]
    public void AReductionNeverFallsAsEvidenceRises()
    {
        foreach (var candidate in new[] { ResearchCandidate.CreditReduction, ResearchCandidate.TimeReduction })
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
        var (winner, magnitude) = ResearchOutcomeRules.Resolve(1, 0, new Dictionary<ResearchCandidate, int>(), new ScriptedMinigameRandom());

        winner.Should().Be(ResearchCandidate.FixedGrant);
        magnitude.Should().Be(0);
    }

    [TestCase(3)]
    [TestCase(5)]
    [TestCase(8)]
    public void ARandomLevelThatRolledLicensedRunsHasItsOwnGrantAsWinnerWhateverTheEvidence(int level)
    {
        var (winner, magnitude) = ResearchOutcomeRules.Resolve(
            level, 2, new Dictionary<ResearchCandidate, int> { [ResearchCandidate.FixedGrant] = 5 }, new ScriptedMinigameRandom());

        winner.Should().Be(ResearchCandidate.FixedGrant);
        magnitude.Should().Be(0, "the runs were rolled at the start");
    }

    [Test]
    public void TheBetterEvidencedReductionWinsWithoutARoll()
    {
        var random = new ScriptedMinigameRandom();

        ResearchOutcomeRules.Resolve(3, 0, Evidence(4, 2), random).Should().Be((ResearchCandidate.CreditReduction, 6));
        ResearchOutcomeRules.Resolve(5, 0, Evidence(1, 3), random).Should().Be((ResearchCandidate.TimeReduction, 5));
        ResearchOutcomeRules.Resolve(8, 0, Evidence(0, 6), random).Should().Be((ResearchCandidate.TimeReduction, 10));
    }

    [Test]
    public void NoEvidenceOnEitherReductionMeansNoWinner()
    {
        var (winner, magnitude) = ResearchOutcomeRules.Resolve(3, 0, Evidence(0, 0), new ScriptedMinigameRandom());

        winner.Should().Be(ResearchCandidate.None);
        magnitude.Should().Be(0);
    }

    [TestCase(0, ResearchCandidate.CreditReduction)]
    [TestCase(399, ResearchCandidate.CreditReduction)]
    [TestCase(400, ResearchCandidate.TimeReduction)]
    [TestCase(799, ResearchCandidate.TimeReduction)]
    public void ATieGoesByTodaysOddsBetweenTheTwoReductions(int roll, ResearchCandidate expected)
    {
        var (winner, magnitude) = ResearchOutcomeRules.Resolve(3, 0, Evidence(2, 2), new ScriptedMinigameRandom(roll));

        winner.Should().Be(expected);
        magnitude.Should().Be(ResearchOutcomeRules.Magnitude(expected, 2));
    }

    [Test]
    public void TheLeadersAreTheCandidatesSharingTheMostEvidence()
    {
        ResearchOutcomeRules.Leaders(3, 0, Evidence(2, 2)).Should().Equal(ResearchCandidate.CreditReduction, ResearchCandidate.TimeReduction);
        ResearchOutcomeRules.Leaders(3, 0, Evidence(0, 0)).Should().BeEmpty();
        ResearchOutcomeRules.Leaders(3, 0, Evidence(0, 4)).Should().Equal(ResearchCandidate.TimeReduction);
    }

    [Test]
    public void TheTieBreakIsFairOverManyRolls()
    {
        var random = new SeededMinigameRandom(3);
        var credit = 0;
        const int Draws = 40_000;
        for (var draw = 0; draw < Draws; draw++)
        {
            if (ResearchOutcomeRules.Resolve(3, 0, Evidence(2, 2), random).Winner == ResearchCandidate.CreditReduction)
                credit++;
        }

        (credit / (double)Draws).Should().BeApproximately(0.5, 0.015);
    }
}
