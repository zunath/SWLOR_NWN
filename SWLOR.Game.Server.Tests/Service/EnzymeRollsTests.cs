using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.EnzymeCultureService;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The enzyme stat rolls. A hunted enzyme rolls from every bracket, exactly as it did before the rolls moved into
/// <see cref="EnzymeRolls"/>, and a cultured one rolls the same way from the lowest brackets only (Player City Revamp,
/// "Farming for other crafts": primary stats 0.1% to 3.0%, secondary 0.1% to 2.0%, the same weights).
/// </summary>
[NonParallelizable]
public class EnzymeRollsTests
{
    private const int Samples = 40000;

    [TearDown]
    public void RestoreTheRandomSeed()
    {
        Random.ResetSeed();
    }

    // The hunted roll is the roll it always was

    private static readonly int[] LegacyPrimaryWeights = { 70, 50, 20, 15, 10, 8, 6, 4, 2, 1 };
    private static readonly int[] LegacySecondaryWeights = { 50, 20, 15, 10, 8 };

    private static List<IncubationStatType> LegacyPrimary(EnzymeType type) => type switch
    {
        EnzymeType.Hydrolase => new List<IncubationStatType>
        {
            IncubationStatType.LearningPurity, IncubationStatType.AttackPurity, IncubationStatType.TraumaResistancePurity,
            IncubationStatType.ElectricalResistancePurity, IncubationStatType.PhysicalDefensePurity
        },
        EnzymeType.Isomerase => new List<IncubationStatType>
        {
            IncubationStatType.XPPenalty, IncubationStatType.EvasionPurity, IncubationStatType.PoisonResistancePurity,
            IncubationStatType.FireResistancePurity, IncubationStatType.MindResistancePurity
        },
        _ => new List<IncubationStatType>
        {
            IncubationStatType.MutationChance, IncubationStatType.AccuracyPurity, IncubationStatType.ForceDefensePurity,
            IncubationStatType.IceResistancePurity, IncubationStatType.MobilityResistancePurity
        },
    };

    private static List<IncubationStatType> LegacySecondary(EnzymeType type) => type switch
    {
        EnzymeType.Hydrolase => new List<IncubationStatType>
        {
            IncubationStatType.MutationChance, IncubationStatType.XPPenalty, IncubationStatType.EvasionPurity,
            IncubationStatType.PoisonResistancePurity, IncubationStatType.FireResistancePurity, IncubationStatType.MindResistancePurity,
            IncubationStatType.AccuracyPurity, IncubationStatType.ForceDefensePurity, IncubationStatType.IceResistancePurity,
            IncubationStatType.MobilityResistancePurity, IncubationStatType.DisruptionResistancePurity
        },
        EnzymeType.Isomerase => new List<IncubationStatType>
        {
            IncubationStatType.MutationChance, IncubationStatType.LearningPurity, IncubationStatType.AttackPurity,
            IncubationStatType.TraumaResistancePurity, IncubationStatType.ElectricalResistancePurity, IncubationStatType.PhysicalDefensePurity,
            IncubationStatType.AccuracyPurity, IncubationStatType.ForceDefensePurity, IncubationStatType.IceResistancePurity,
            IncubationStatType.MobilityResistancePurity, IncubationStatType.DisruptionResistancePurity
        },
        _ => new List<IncubationStatType>
        {
            IncubationStatType.XPPenalty, IncubationStatType.LearningPurity, IncubationStatType.AttackPurity,
            IncubationStatType.TraumaResistancePurity, IncubationStatType.ElectricalResistancePurity, IncubationStatType.PhysicalDefensePurity,
            IncubationStatType.EvasionPurity, IncubationStatType.PoisonResistancePurity, IncubationStatType.FireResistancePurity,
            IncubationStatType.MindResistancePurity, IncubationStatType.DisruptionResistancePurity
        },
    };

    /// <summary>The roll as <c>EnzymeLootTableDefinition</c> wrote it before this slice, kept here as the reference.</summary>
    private static List<EnzymeStatRoll> LegacyRoll(EnzymeType type)
    {
        static int Value(int index) => index switch
        {
            0 => Random.Next(1, 10),
            1 => Random.Next(11, 20),
            2 => Random.Next(21, 30),
            3 => Random.Next(31, 40),
            4 => Random.Next(41, 50),
            5 => Random.Next(51, 60),
            6 => Random.Next(61, 70),
            7 => Random.Next(71, 80),
            8 => Random.Next(81, 90),
            9 => Random.Next(91, 100),
            _ => 0
        };

        var primaryStats = LegacyPrimary(type);
        var secondaryStats = LegacySecondary(type);
        var primaryIndex = Random.GetRandomWeightedIndex(LegacyPrimaryWeights);
        var secondaryIndex = Random.GetRandomWeightedIndex(LegacySecondaryWeights);
        var primaryCount = Random.Next(0, 3);
        var secondaryCount = 1 + Random.Next(0, 3);
        var rolls = new List<EnzymeStatRoll>();

        for (var propCount = 1; propCount <= primaryCount; propCount++)
        {
            if (primaryStats.Count <= 0)
                break;

            var amount = Value(primaryIndex);
            var index = Random.Next(primaryStats.Count);
            rolls.Add(new EnzymeStatRoll(primaryStats[index], amount));
            primaryStats.RemoveAt(index);
        }

        for (var propCount = 1; propCount <= secondaryCount; propCount++)
        {
            if (secondaryStats.Count <= 0)
                break;

            var amount = Value(secondaryIndex);
            var index = Random.Next(secondaryStats.Count);
            rolls.Add(new EnzymeStatRoll(secondaryStats[index], amount));
            secondaryStats.RemoveAt(index);
        }

        return rolls;
    }

    [TestCase(EnzymeType.Hydrolase)]
    [TestCase(EnzymeType.Isomerase)]
    [TestCase(EnzymeType.Lyase)]
    public void AHuntedEnzymeRollsExactlyAsItDidBeforeTheRollsWereShared(EnzymeType type)
    {
        for (var seed = 1; seed <= 400; seed++)
        {
            Random.SetSeed(seed);
            var expected = LegacyRoll(type);
            Random.SetSeed(seed);
            var actual = EnzymeRolls.Roll(type, EnzymeRolls.HuntedMaxPrimaryBracket, EnzymeRolls.HuntedMaxSecondaryBracket);

            actual.Should().Equal(expected, $"seed {seed}");
        }
    }

    [Test]
    public void TheBracketsAndWeightsAreTheHuntedOnes()
    {
        EnzymeRolls.PrimaryWeights.Should().Equal(LegacyPrimaryWeights);
        EnzymeRolls.SecondaryWeights.Should().Equal(LegacySecondaryWeights);
        EnzymeRolls.PrimaryBrackets.Should().Be(10);
        EnzymeRolls.SecondaryBrackets.Should().Be(5);

        foreach (var type in EnzymeCultureRules.Types)
        {
            EnzymeRolls.GetPrimaryStats(type).Should().Equal(LegacyPrimary(type));
            EnzymeRolls.GetSecondaryStats(type).Should().Equal(LegacySecondary(type));
        }
    }

    [Test]
    public void ABracketRollsTenthsOfAPercentFromItsRange_TheTopOfTheRangeExclusive()
    {
        for (var bracket = 0; bracket < EnzymeRolls.PrimaryBrackets; bracket++)
        {
            EnzymeRolls.GetBracketLow(bracket).Should().Be(bracket * 10 + 1);
            EnzymeRolls.GetBracketHigh(bracket).Should().Be(bracket * 10 + 9);
        }

        Random.SetSeed(11);
        for (var sample = 0; sample < 2000; sample++)
            EnzymeRolls.RollBracketValue(2).Should().BeInRange(21, 29);
    }

    [Test]
    public void TheStatListsOfATypeNeverOverlap_SoAStatIsPrimaryOrSecondaryNeverBoth()
    {
        foreach (var type in EnzymeCultureRules.Types)
        {
            EnzymeRolls.GetPrimaryStats(type).Should().NotIntersectWith(EnzymeRolls.GetSecondaryStats(type), type.ToString());
            EnzymeRolls.GetPrimaryStats(type).Should().OnlyHaveUniqueItems().And.HaveCount(5);
            EnzymeRolls.GetSecondaryStats(type).Should().OnlyHaveUniqueItems().And.HaveCount(11);
            EnzymeRolls.GetPrimaryStats(type).Concat(EnzymeRolls.GetSecondaryStats(type))
                .Should().OnlyContain(stat => stat != IncubationStatType.Invalid && Enum.IsDefined(stat), "the incubator handles every one");
        }
    }

    // The cultured roll: the lowest brackets, no more

    [TestCase(EnzymeType.Hydrolase)]
    [TestCase(EnzymeType.Isomerase)]
    [TestCase(EnzymeType.Lyase)]
    public void ACulturedEnzymeNeverRollsAboveThreePercentPrimaryOrTwoPercentSecondary(EnzymeType type)
    {
        Random.SetSeed(2026);
        var primaryMax = 0;
        var secondaryMax = 0;
        for (var sample = 0; sample < Samples; sample++)
        {
            foreach (var roll in EnzymeRolls.Roll(type, EnzymeCultureRules.MaxPrimaryBracket, EnzymeCultureRules.MaxSecondaryBracket))
            {
                if (EnzymeRolls.GetPrimaryStats(type).Contains(roll.Stat))
                    primaryMax = Math.Max(primaryMax, roll.Amount);
                else
                    secondaryMax = Math.Max(secondaryMax, roll.Amount);
            }
        }

        primaryMax.Should().BeLessThanOrEqualTo(EnzymeCultureRules.PrimaryCeiling).And.BeGreaterThan(20, "the third bracket does come up");
        secondaryMax.Should().BeLessThanOrEqualTo(EnzymeCultureRules.SecondaryCeiling).And.BeGreaterThan(10, "the second bracket does come up");
    }

    [Test]
    public void TheCulturedLimitsAreTheThirdPrimaryBracketAndTheSecondSecondaryBracket()
    {
        EnzymeCultureRules.MaxPrimaryBracket.Should().Be(2);
        EnzymeCultureRules.MaxSecondaryBracket.Should().Be(1);
        EnzymeRolls.GetBracketHigh(EnzymeCultureRules.MaxPrimaryBracket).Should().BeLessThanOrEqualTo(EnzymeCultureRules.PrimaryCeiling);
        EnzymeRolls.GetBracketHigh(EnzymeCultureRules.MaxSecondaryBracket).Should().BeLessThanOrEqualTo(EnzymeCultureRules.SecondaryCeiling);
        EnzymeRolls.GetBracketHigh(EnzymeCultureRules.MaxPrimaryBracket + 1).Should().BeGreaterThan(EnzymeCultureRules.PrimaryCeiling, "the next bracket is the one cut off");
        EnzymeRolls.GetBracketHigh(EnzymeCultureRules.MaxSecondaryBracket + 1).Should().BeGreaterThan(EnzymeCultureRules.SecondaryCeiling);
    }

    [TestCase(EnzymeType.Hydrolase)]
    [TestCase(EnzymeType.Isomerase)]
    [TestCase(EnzymeType.Lyase)]
    public void ACulturedRollHasTheShapeOfAHuntedOne_ZeroToTwoPrimaryAndOneToThreeSecondaryOfDifferentStats(EnzymeType type)
    {
        Random.SetSeed(77);
        var primaryCounts = new HashSet<int>();
        var secondaryCounts = new HashSet<int>();
        for (var sample = 0; sample < 5000; sample++)
        {
            var rolls = EnzymeRolls.Roll(type, EnzymeCultureRules.MaxPrimaryBracket, EnzymeCultureRules.MaxSecondaryBracket);
            var primary = rolls.Where(roll => EnzymeRolls.GetPrimaryStats(type).Contains(roll.Stat)).ToList();
            var secondary = rolls.Where(roll => EnzymeRolls.GetSecondaryStats(type).Contains(roll.Stat)).ToList();

            (primary.Count + secondary.Count).Should().Be(rolls.Count, "every stat belongs to the type");
            primary.Count.Should().BeInRange(0, 2);
            secondary.Count.Should().BeInRange(1, 3);
            rolls.Select(roll => roll.Stat).Should().OnlyHaveUniqueItems();
            rolls.Should().OnlyContain(roll => roll.Amount >= 1);
            primary.Select(roll => roll.Amount / 10).Distinct().Should().HaveCountLessThanOrEqualTo(1, "one bracket per kind is rolled for the whole enzyme");
            secondary.Select(roll => roll.Amount / 10).Distinct().Should().HaveCountLessThanOrEqualTo(1);
            primaryCounts.Add(primary.Count);
            secondaryCounts.Add(secondary.Count);
        }

        primaryCounts.Should().BeEquivalentTo(new[] { 0, 1, 2 });
        secondaryCounts.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Test]
    public void TheCulturedAveragesAreThePlansElevenAndEightTenths_AgainstTwoPointOneAndOnePointSixHunted()
    {
        Random.SetSeed(5);
        var culturedPrimary = new List<int>();
        var culturedSecondary = new List<int>();
        var huntedPrimary = new List<int>();
        var huntedSecondary = new List<int>();
        for (var sample = 0; sample < Samples; sample++)
        {
            foreach (var roll in EnzymeRolls.Roll(EnzymeType.Lyase, EnzymeCultureRules.MaxPrimaryBracket, EnzymeCultureRules.MaxSecondaryBracket))
                (EnzymeRolls.GetPrimaryStats(EnzymeType.Lyase).Contains(roll.Stat) ? culturedPrimary : culturedSecondary).Add(roll.Amount);

            foreach (var roll in EnzymeRolls.Roll(EnzymeType.Lyase, EnzymeRolls.HuntedMaxPrimaryBracket, EnzymeRolls.HuntedMaxSecondaryBracket))
                (EnzymeRolls.GetPrimaryStats(EnzymeType.Lyase).Contains(roll.Stat) ? huntedPrimary : huntedSecondary).Add(roll.Amount);
        }

        culturedPrimary.Average().Should().BeApproximately(11.4, 0.5, "1.1% on average, in tenths of a percent");
        culturedSecondary.Average().Should().BeApproximately(7.9, 0.4, "0.8% on average");
        huntedPrimary.Average().Should().BeGreaterThan(culturedPrimary.Average() + 5, "hunting stays the source of the best rolls");
        huntedSecondary.Average().Should().BeGreaterThan(culturedSecondary.Average() + 3);
    }

    [Test]
    public void AQuarterOfHuntedEnzymesRollPrimaryStatsAboveThreePercent_SoHuntingStaysTheSourceOfTheBest()
    {
        var above = EnzymeRolls.PrimaryWeights.Skip(EnzymeCultureRules.MaxPrimaryBracket + 1).Sum() / (double)EnzymeRolls.PrimaryWeights.Sum();

        above.Should().BeApproximately(0.25, 0.01, "the plan: a quarter of hunted enzymes roll primary stats above 3%");

        Random.SetSeed(9);
        var bracketHits = new int[EnzymeRolls.PrimaryBrackets];
        for (var sample = 0; sample < Samples; sample++)
            bracketHits[Random.GetRandomWeightedIndex(EnzymeRolls.PrimaryWeights.ToArray())]++;

        bracketHits.Skip(3).Sum().Should().BeInRange((int)(Samples * 0.23), (int)(Samples * 0.27));
    }

    [Test]
    public void ACulturedEnzymeKeepsTheWeightsOfItsBrackets()
    {
        var weights = EnzymeRolls.PrimaryWeights.Take(3).ToList();
        weights.Should().Equal(70, 50, 20);
        EnzymeRolls.SecondaryWeights.Take(2).Should().Equal(50, 20);

        Random.SetSeed(31);
        var hits = new int[3];
        var secondaryHits = new int[2];
        for (var sample = 0; sample < Samples; sample++)
        {
            hits[Random.GetRandomWeightedIndex(weights.ToArray())]++;
            secondaryHits[Random.GetRandomWeightedIndex(EnzymeRolls.SecondaryWeights.Take(2).ToArray())]++;
        }

        hits[0].Should().BeInRange((int)(Samples * 0.48), (int)(Samples * 0.52), "70 of 140");
        hits[1].Should().BeInRange((int)(Samples * 0.34), (int)(Samples * 0.38), "50 of 140");
        hits[2].Should().BeInRange((int)(Samples * 0.125), (int)(Samples * 0.16), "20 of 140");
        secondaryHits[0].Should().BeInRange((int)(Samples * 0.69), (int)(Samples * 0.74), "50 of 70");
    }

    [Test]
    public void AnUnknownTypeIsRefused()
    {
        var roll = () => EnzymeRolls.Roll(EnzymeType.Invalid, 2, 1);

        roll.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestCase(-5, 1)]
    [TestCase(0, 1)]
    [TestCase(9, 10)]
    [TestCase(99, 10)]
    public void ABracketLimitOutsideTheRangeIsClamped(int limit, int expectedBrackets)
    {
        // A limit below the first bracket still rolls the first; one above the last rolls them all.
        Random.SetSeed(3);
        var seen = new HashSet<int>();
        for (var sample = 0; sample < 3000; sample++)
        {
            foreach (var roll in EnzymeRolls.Roll(EnzymeType.Lyase, limit, 0))
            {
                if (EnzymeRolls.GetPrimaryStats(EnzymeType.Lyase).Contains(roll.Stat))
                    seen.Add(roll.Amount / 10);
            }
        }

        seen.Count.Should().BeLessThanOrEqualTo(expectedBrackets);
        seen.Should().OnlyContain(bracket => bracket >= 0 && bracket < expectedBrackets);
    }
}
