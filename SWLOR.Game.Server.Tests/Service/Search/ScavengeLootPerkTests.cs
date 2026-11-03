using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.Game.Server.Service.ScavengeService;

namespace SWLOR.Game.Server.Tests.Service.Search;

/// <summary>
/// Covers the loot weights the Treasure Seeker perks change: the multiplier an ordinary find puts on a table's rare entries
/// (<c>findWeightBonus</c>) and the bonus on a Dig Deeper's (<c>deeperWeightBonus</c>). With no bonus a roll is exactly what it was.
/// </summary>
public class ScavengeLootPerkTests
{
    /// <summary>Ore 50, gem 10, and the flagged ruby 1 and jade 2: the rare entries are the last two.</summary>
    private static LootTable Flagged() => new()
    {
        new LootTableItem("ore", 3, 50, false),
        new LootTableItem("gem", 1, 10, false),
        new LootTableItem("ruby", 1, 1, true),
        new LootTableItem("jade", 1, 2, true)
    };

    /// <summary>No entry is flagged, so the least likely entry (the first) is the rare one.</summary>
    private static LootTable RareFirst() => new()
    {
        new LootTableItem("fiberp_flawed", 1, 5, false),
        new LootTableItem("fiberp_ruined", 3, 50, false)
    };

    private static List<int> WeightsOf(LootTable table, int treasureHunter, bool deeper, int deeperBonus = 0, int findBonus = 0) =>
        ScavengeLoot.Weights(table, treasureHunter, deeper, deeperBonus, findBonus).Select(entry => entry.Weight).ToList();

    /// <summary>The entry a roll of <paramref name="roll"/> lands on in a table with these weights, walking them in order.</summary>
    private static string Landing(LootTable table, IReadOnlyList<int> weights, int roll)
    {
        var cumulative = 0;
        for (var index = 0; index < weights.Count; index++)
        {
            cumulative += weights[index];
            if (roll < cumulative)
                return table[index].Resref;
        }

        return table[^1].Resref;
    }

    [Test]
    public void WithNoBonus_AnOrdinaryFindsWeightsAreExactlyWhatTheyWere()
    {
        WeightsOf(Flagged(), 0, false).Should().Equal(50, 10, 1, 2);
        WeightsOf(Flagged(), 2, false).Should().Equal(50, 10, 21, 22);
        WeightsOf(Flagged(), 2, false, 0, 0).Should().Equal(50, 10, 21, 22);
        WeightsOf(RareFirst(), 0, false).Should().Equal(5, 50);
    }

    [Test]
    public void WithNoBonus_ADigDeeperFindsWeightsAreExactlyWhatTheyWere()
    {
        var multiplier = ScavengeLoot.DeeperWeightMultiplier;

        WeightsOf(Flagged(), 0, true).Should().Equal(50, 10, 1 * multiplier, 2 * multiplier);
        WeightsOf(Flagged(), 2, true, 0, 0).Should().Equal(50, 10, 21 * multiplier, 22 * multiplier);
        WeightsOf(RareFirst(), 0, true).Should().Equal(5 * multiplier, 50);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(5)]
    public void AFindBonusMultipliesTheRareEntriesOfAnOrdinaryFindByOnePlusTheBonus_AndNothingElse(int bonus)
    {
        var multiplier = 1 + bonus;

        WeightsOf(Flagged(), 0, false, 0, bonus).Should().Equal(50, 10, 1 * multiplier, 2 * multiplier);
        // Treasure Hunter's weight comes first, then the multiplier.
        WeightsOf(Flagged(), 2, false, 0, bonus).Should().Equal(50, 10, 21 * multiplier, 22 * multiplier);
        WeightsOf(RareFirst(), 0, false, 0, bonus).Should().Equal(5 * multiplier, 50);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(8)]
    public void ADeeperBonusAddsToTheDigMultiplierOnTheRareEntries_AndNothingElse(int bonus)
    {
        var multiplier = ScavengeLoot.DeeperWeightMultiplier + bonus;

        WeightsOf(Flagged(), 0, true, bonus).Should().Equal(50, 10, 1 * multiplier, 2 * multiplier);
        WeightsOf(Flagged(), 2, true, bonus).Should().Equal(50, 10, 21 * multiplier, 22 * multiplier);
        WeightsOf(RareFirst(), 0, true, bonus).Should().Equal(5 * multiplier, 50);
    }

    [Test]
    public void ADigIgnoresTheFindBonus_AndAnOrdinaryFindIgnoresTheDeeperBonus()
    {
        WeightsOf(Flagged(), 0, true, 0, 6).Should().Equal(WeightsOf(Flagged(), 0, true), "the find bonus is for ordinary finds");
        WeightsOf(Flagged(), 0, false, 6, 0).Should().Equal(WeightsOf(Flagged(), 0, false), "the dig bonus is for digs");
        WeightsOf(Flagged(), 0, true, 2, 6).Should().Equal(WeightsOf(Flagged(), 0, true, 2, 0));
    }

    [Test]
    public void ANegativeFindBonusNeverLowersARareEntry()
    {
        WeightsOf(Flagged(), 0, false, 0, -2).Should().Equal(WeightsOf(Flagged(), 0, false));
    }

    [Test]
    public void ATableWithNoFlaggedEntries_BoostsItsLowestWeightNonGoldEntries()
    {
        var table = new LootTable
        {
            new LootTableItem("kolto", 1, 20, false),
            new LootTableItem("syringe", 1, 20, false),
            new LootTableItem(ScavengeLoot.GoldResref, 15, 5, false)
        };

        // Equally likely entries are all rare, and gold is never one while the table has other items.
        ScavengeLoot.RareEntries(table).Select(item => item.Resref).Should().Equal("kolto", "syringe");
        // Treasure Hunter changes only flagged entries, and gold is never boosted.
        WeightsOf(table, 3, false, 0, 1).Should().Equal(40, 40, 5);

        var uneven = new LootTable
        {
            new LootTableItem("fiberp_ruined", 3, 50, false),
            new LootTableItem("fiberp_flawed", 1, 5, false),
            new LootTableItem("fiberp_pristine", 1, 5, false),
            new LootTableItem(ScavengeLoot.GoldResref, 15, 1, false)
        };
        ScavengeLoot.RareEntries(uneven).Select(item => item.Resref).Should().Equal("fiberp_flawed", "fiberp_pristine");
        WeightsOf(uneven, 0, false, 0, 2).Should().Equal(50, 15, 15, 1);
        WeightsOf(uneven, 0, true, 1).Should().Equal(50, 5 * (ScavengeLoot.DeeperWeightMultiplier + 1), 5 * (ScavengeLoot.DeeperWeightMultiplier + 1), 1);
    }

    [Test]
    public void ATableOfOneEntryBoostsThatEntry_WhichChangesNoChoice()
    {
        var table = new LootTable { new LootTableItem("herb", 1, 50, false) };

        WeightsOf(table, 0, false, 0, 3).Should().Equal(200);
        ScavengeLoot.RareShare(table, 0, false, 0, 3).Should().BeApproximately(1.0, 1e-9);
        ScavengeLoot.Roll(table, 0, 0, false, new ScriptedMinigameRandom(199, 1), 0, 3).Resref.Should().Be("herb");
    }

    [Test]
    public void TheRareShareOfAnOrdinaryFindRisesWithTheFindBonus_AndWithNoBonusIsWhatItWas()
    {
        var table = Flagged();

        ScavengeLoot.RareShare(table, 0, false, 0, 0).Should().BeApproximately(3.0 / 63, 1e-9);
        ScavengeLoot.RareShare(table, 0, false, 0, 0).Should().Be(ScavengeLoot.RareShare(table, 0, false));
        ScavengeLoot.RareShare(table, 0, false, 0, 1).Should().BeApproximately(6.0 / 66, 1e-9, "rare entries 2 and 4 of 66");
        ScavengeLoot.RareShare(table, 0, false, 0, 3).Should().BeApproximately(12.0 / 72, 1e-9, "rare entries 4 and 8 of 72");
        ScavengeLoot.RareShare(table, 3, false, 0, 1).Should().BeApproximately(126.0 / 186, 1e-9, "Treasure Hunter 3 makes them 31 and 32, doubled");

        var previous = 0.0;
        for (var bonus = 0; bonus <= 6; bonus++)
        {
            var share = ScavengeLoot.RareShare(table, 0, false, 0, bonus);

            share.Should().BeGreaterThan(previous, $"find bonus {bonus}");
            share.Should().BeLessThan(1.0);
            previous = share;
        }
    }

    [Test]
    public void TheRareShareOfADigRisesWithTheDeeperBonus()
    {
        var table = Flagged();
        var multiplier = ScavengeLoot.DeeperWeightMultiplier;

        ScavengeLoot.RareShare(table, 0, true, 0, 0).Should().BeApproximately(3.0 * multiplier / (60 + 3.0 * multiplier), 1e-9);
        ScavengeLoot.RareShare(table, 0, true, 2, 0).Should().BeApproximately(3.0 * (multiplier + 2) / (60 + 3.0 * (multiplier + 2)), 1e-9);
        ScavengeLoot.RareShare(table, 0, true, 8, 0).Should().BeGreaterThan(ScavengeLoot.RareShare(table, 0, true, 0, 0));
        ScavengeLoot.RareShare(table, 0, true, 8, 0).Should().BeLessThan(1.0, "a Jackpot dig favours the rare entries, it does not guarantee one");
        ScavengeLoot.RareShare(table, 0, true, 0, 6).Should().BeApproximately(ScavengeLoot.RareShare(table, 0, true), 1e-9, "a dig ignores the find bonus");
    }

    [Test]
    public void AFindBonusMovesARollOntoARareEntry_ThatLandedOnAnotherWithout()
    {
        // Plain weights 50, 10, 1, 2: 0 to 49 ore, 50 to 59 gem, 60 ruby, 61 and 62 jade.
        // With a find bonus of 1 they are 50, 10, 2, 4: 60 and 61 ruby, 62 to 65 jade.
        ScavengeLoot.Roll(Flagged(), 0, 0, false, new ScriptedMinigameRandom(61, 1)).Should().Be(new ScavengeFind("jade", 1, false));
        ScavengeLoot.Roll(Flagged(), 0, 0, false, new ScriptedMinigameRandom(61, 1), 0, 1).Should().Be(new ScavengeFind("ruby", 1, false));
        ScavengeLoot.Roll(Flagged(), 0, 0, false, new ScriptedMinigameRandom(65, 1), 0, 1).Should().Be(new ScavengeFind("jade", 1, false));
        ScavengeLoot.Roll(Flagged(), 0, 0, false, new ScriptedMinigameRandom(59, 1), 0, 1).Should().Be(new ScavengeFind("gem", 1, false), "the common entries keep their range");
        ScavengeLoot.Roll(Flagged(), 0, 0, false, new ScriptedMinigameRandom(0, 3), 0, 1).Should().Be(new ScavengeFind("ore", 3, false));
    }

    [Test]
    public void ADeeperBonusMovesARollOntoARareEntry_ThatLandedOnAnotherWithout()
    {
        // With the dig multiplier m the weights are 50, 10, m, 2m: the ruby takes 60 to 59 + m and the jade the 2m points after it. A
        // deeper bonus of 2 makes the multiplier m + 2, so the first jade point of the plain dig (60 + m) is a ruby point.
        var multiplier = ScavengeLoot.DeeperWeightMultiplier;
        var firstJade = 60 + multiplier;
        var lastRuby = 59 + multiplier;
        var lastJadeBoosted = 59 + 3 * (multiplier + 2);

        ScavengeLoot.Roll(Flagged(), 0, 0, true, new ScriptedMinigameRandom(lastRuby, 1)).Should().Be(new ScavengeFind("ruby", 1, true));
        ScavengeLoot.Roll(Flagged(), 0, 0, true, new ScriptedMinigameRandom(firstJade, 1)).Should().Be(new ScavengeFind("jade", 1, true));
        ScavengeLoot.Roll(Flagged(), 0, 0, true, new ScriptedMinigameRandom(firstJade, 1), 2).Should().Be(new ScavengeFind("ruby", 1, true));
        ScavengeLoot.Roll(Flagged(), 0, 0, true, new ScriptedMinigameRandom(firstJade + 2, 1), 2).Should().Be(new ScavengeFind("jade", 1, true));
        ScavengeLoot.Roll(Flagged(), 0, 0, true, new ScriptedMinigameRandom(lastJadeBoosted, 1), 2).Should().Be(new ScavengeFind("jade", 1, true));
        ScavengeLoot.Roll(Flagged(), 0, 0, true, new ScriptedMinigameRandom(lastRuby, 1), 0, 6).Should().Be(new ScavengeFind("ruby", 1, true), "a dig ignores the find bonus");
        ScavengeLoot.Roll(Flagged(), 0, 0, true, new ScriptedMinigameRandom(firstJade, 1), 0, 6).Should().Be(new ScavengeFind("jade", 1, true));
    }

    [Test]
    public void ATableWithNoFlaggedEntriesFavoursItsLeastLikelyEntryOnAFindBonus()
    {
        // The flawed fiber weighs 5 of 55; a find bonus of 2 makes it 15 of 65: 0 to 14 flawed.
        ScavengeLoot.Roll(RareFirst(), 0, 0, false, new ScriptedMinigameRandom(10, 1)).Resref.Should().Be("fiberp_ruined");
        ScavengeLoot.Roll(RareFirst(), 0, 0, false, new ScriptedMinigameRandom(10, 1), 0, 2).Resref.Should().Be("fiberp_flawed");
        ScavengeLoot.Roll(RareFirst(), 0, 0, false, new ScriptedMinigameRandom(15, 1), 0, 2).Resref.Should().Be("fiberp_ruined");
    }

    [Test]
    public void WithNoBonus_EveryRollLandsWhereItAlwaysDid_OnAnOrdinaryFindAndOnADig()
    {
        // Treasure Hunter 2 makes the weights 50, 10, 21, 22; a dig multiplies the last two.
        var table = Flagged();
        var multiplier = ScavengeLoot.DeeperWeightMultiplier;
        var ordinary = new[] { 50, 10, 21, 22 };
        var dig = new[] { 50, 10, 21 * multiplier, 22 * multiplier };

        for (var roll = 0; roll < ordinary.Sum(); roll++)
        {
            ScavengeLoot.Roll(table, 2, 0, false, new ScriptedMinigameRandom(roll, 1), 0, 0).Resref.Should().Be(Landing(table, ordinary, roll), $"ordinary roll {roll}");
        }

        for (var roll = 0; roll < dig.Sum(); roll++)
        {
            ScavengeLoot.Roll(table, 2, 0, true, new ScriptedMinigameRandom(roll, 1), 0, 0).Resref.Should().Be(Landing(table, dig, roll), $"dig roll {roll}");
        }
    }

    [Test]
    public void WithABonus_EveryRollLandsWhereTheBoostedWeightsPutIt()
    {
        var table = Flagged();
        var ordinary = new[] { 50, 10, 21 * 3, 22 * 3 };
        var dig = new[] { 50, 10, 21 * (ScavengeLoot.DeeperWeightMultiplier + 4), 22 * (ScavengeLoot.DeeperWeightMultiplier + 4) };

        for (var roll = 0; roll < ordinary.Sum(); roll++)
        {
            ScavengeLoot.Roll(table, 2, 0, false, new ScriptedMinigameRandom(roll, 1), 0, 2).Resref.Should().Be(Landing(table, ordinary, roll), $"ordinary roll {roll}");
        }

        for (var roll = 0; roll < dig.Sum(); roll++)
        {
            ScavengeLoot.Roll(table, 2, 0, true, new ScriptedMinigameRandom(roll, 1), 4, 0).Resref.Should().Be(Landing(table, dig, roll), $"dig roll {roll}");
        }
    }

    [Test]
    public void ABonusChangesNeitherTheQuantityNorTheGoldBonus()
    {
        var table = new LootTable
        {
            new LootTableItem(ScavengeLoot.GoldResref, 10, 50, false),
            new LootTableItem("ruby", 2, 1, true)
        };

        ScavengeLoot.Roll(table, 0, 2, false, new ScriptedMinigameRandom(0, 10), 0, 4).Should().Be(new ScavengeFind(ScavengeLoot.GoldResref, 14, false), "10 gold and 2 x 20% more");
        ScavengeLoot.Roll(table, 0, 0, false, new ScriptedMinigameRandom(52, 2), 0, 4).Should().Be(new ScavengeFind("ruby", 2, false));
        ScavengeLoot.Roll(table, 0, 0, true, new ScriptedMinigameRandom(52, 1), 5).Should().Be(new ScavengeFind("ruby", 1, true));
    }

    [Test]
    public void ARollNeedsASourceOfChance_AndAnEmptyTableHasNoRareEntries()
    {
        var noRandom = () => ScavengeLoot.Roll(Flagged(), 0, 0, false, null, 0, 1);
        var emptyShare = () => ScavengeLoot.RareShare(new LootTable(), 0, false, 0, 1);

        noRandom.Should().Throw<ArgumentNullException>();
        emptyShare.Should().Throw<ArgumentException>();
    }

    [Test]
    public void ARollOverManyDraws_FindsTheRareEntriesAsOftenAsTheirShare_WithAndWithoutTheBonus()
    {
        // A seeded source makes the run repeatable; the tolerance is several standard deviations of 20,000 draws.
        const int draws = 20000;
        var table = Flagged();

        foreach (var (bonus, deeper) in new[] { (0, false), (1, false), (3, false), (2, true) })
        {
            var random = new SeededMinigameRandom(20261023);
            var rare = 0;
            for (var draw = 0; draw < draws; draw++)
            {
                var find = deeper
                    ? ScavengeLoot.Roll(table, 0, 0, true, random, bonus, 0)
                    : ScavengeLoot.Roll(table, 0, 0, false, random, 0, bonus);
                if (find.Resref is "ruby" or "jade")
                    rare++;
            }

            var expected = deeper
                ? ScavengeLoot.RareShare(table, 0, true, bonus, 0)
                : ScavengeLoot.RareShare(table, 0, false, 0, bonus);
            ((double)rare / draws).Should().BeApproximately(expected, 0.015, $"bonus {bonus}, deeper {deeper}");
        }
    }
}
