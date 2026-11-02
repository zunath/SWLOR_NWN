using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.EnzymeCultureService;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// A cultured enzyme is an ordinary enzyme to the incubator, so Breeders' Charter (a tenth of what a stage's enzymes add to a purity
/// stat, rounded down to 0.1%) works on it the way it does on a hunted one. The plan's figure: with the Charter, an average cultured
/// stat gains 0.1% per stage.
/// </summary>
[NonParallelizable]
public class EnzymeBreedersCharterTests
{
    private const int MaxStageIncrease = 100;

    [TearDown]
    public void RestoreTheRandomSeed()
    {
        Random.ResetSeed();
    }

    private static (double Primary, double Secondary) CulturedAverages(EnzymeType type)
    {
        Random.SetSeed(404);
        var primary = new List<int>();
        var secondary = new List<int>();
        for (var sample = 0; sample < 30000; sample++)
        {
            foreach (var roll in EnzymeRolls.Roll(type, EnzymeCultureRules.MaxPrimaryBracket, EnzymeCultureRules.MaxSecondaryBracket))
                (EnzymeRolls.GetPrimaryStats(type).Contains(roll.Stat) ? primary : secondary).Add(roll.Amount);
        }

        return (primary.Average(), secondary.Average());
    }

    [TestCase(EnzymeType.Hydrolase)]
    [TestCase(EnzymeType.Isomerase)]
    [TestCase(EnzymeType.Lyase)]
    public void AnAverageCulturedPrimaryStatGainsOneTenthOfAPercentPerStage(EnzymeType type)
    {
        var (primary, _) = CulturedAverages(type);

        primary.Should().BeApproximately(11.4, 0.5, "about 1.1% on a stat");
        EdictBonusRules.GetBreedersCharterBonus((int)Math.Round(primary), MaxStageIncrease).Should().Be(1, "a tenth of 1.1% is 0.11%, rounded down to 0.1%");
    }

    [Test]
    public void AHuntedEnzymeGainsMoreFromTheSameCharter()
    {
        EdictBonusRules.GetBreedersCharterBonus(21, MaxStageIncrease).Should().Be(2, "the 2.1% a hunted primary stat averages");
        EdictBonusRules.GetBreedersCharterBonus(11, MaxStageIncrease).Should().Be(1);
    }

    [Test]
    public void ASecondaryStatAtItsAverageGainsNothing_TheCharterRoundsDownToTenthsOfAPercent()
    {
        var (_, secondary) = CulturedAverages(EnzymeType.Lyase);

        secondary.Should().BeApproximately(7.9, 0.4);
        EdictBonusRules.GetBreedersCharterBonus((int)Math.Round(secondary), MaxStageIncrease).Should().Be(0, "a tenth of 0.8% rounds down to nothing");
    }

    [Test]
    public void TheBestCulturedStatsStillGainOnlyAFewTenths()
    {
        var topPrimary = EnzymeRolls.GetBracketHigh(EnzymeCultureRules.MaxPrimaryBracket);

        EdictBonusRules.GetBreedersCharterBonus(topPrimary, MaxStageIncrease).Should().Be(2);
        EdictBonusRules.GetBreedersCharterBonus(3 * topPrimary, MaxStageIncrease).Should().Be(8, "three of the best enzymes on one stat, 8.7%");
        EdictBonusRules.GetBreedersCharterBonus(0, MaxStageIncrease).Should().Be(0);
    }

    [Test]
    public void EveryStatACulturedEnzymeRollsIsOneTheIncubatorReads()
    {
        // The incubator's stat switch throws for anything that is not a real incubation stat, and a cultured enzyme rolls from the
        // same lists a hunted one does.
        foreach (var type in EnzymeCultureRules.Types)
        {
            Random.SetSeed(808);
            for (var sample = 0; sample < 2000; sample++)
            {
                foreach (var roll in EnzymeRolls.Roll(type, EnzymeCultureRules.MaxPrimaryBracket, EnzymeCultureRules.MaxSecondaryBracket))
                {
                    roll.Stat.Should().NotBe(IncubationStatType.Invalid);
                    Enum.IsDefined(roll.Stat).Should().BeTrue();
                    roll.Amount.Should().BeInRange(1, MaxStageIncrease);
                }
            }
        }
    }
}
