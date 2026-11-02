using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.Game.Server.Service.ScavengeService;

namespace SWLOR.Game.Server.Tests.Service.Search;

/// <summary>
/// Covers how a find rolls the site's loot table: Treasure Hunter's rare weight, the rare entries a Dig Deeper favours, quantities and gold.
/// </summary>
public class ScavengeLootTests
{
    private static LootTable Flagged() => new()
    {
        new LootTableItem("ore", 3, 50, false),
        new LootTableItem("gem", 1, 10, false),
        new LootTableItem("ruby", 1, 1, true),
        new LootTableItem("jade", 1, 2, true)
    };

    private static LootTable Unflagged() => new()
    {
        new LootTableItem("fiberp_ruined", 3, 50, false),
        new LootTableItem("fiberp_flawed", 1, 5, false)
    };

    [Test]
    public void ADeeperFindFavoursTheRareEntriesByAStartingMultiplier()
    {
        ScavengeLoot.DeeperWeightMultiplier.Should().BeGreaterThan(1);
    }

    [TestCase(0, 1)]
    [TestCase(1, 11)]
    [TestCase(2, 21)]
    [TestCase(3, 31)]
    public void TreasureHunterAddsTenWeightPerLevelToARareFlaggedEntry(int level, int expected)
    {
        ScavengeLoot.WeightOf(new LootTableItem("ruby", 1, 1, true), level).Should().Be(expected);
    }

    [TestCase(0)]
    [TestCase(3)]
    public void TreasureHunterNeverChangesAnEntryThatIsNotFlaggedRare(int level)
    {
        ScavengeLoot.WeightOf(new LootTableItem("ore", 3, 50, false), level).Should().Be(50);
    }

    [Test]
    public void AWholeTableRollWeightsEveryEntry_WithTreasureHunterOnTheRareOnes()
    {
        ScavengeLoot.Weights(Flagged(), 2, false).Select(entry => entry.Weight).Should().Equal(50, 10, 21, 22);
    }

    [Test]
    public void ADeeperRollMultipliesTheRareEntries_AfterTreasureHunter()
    {
        var multiplier = ScavengeLoot.DeeperWeightMultiplier;

        ScavengeLoot.Weights(Flagged(), 0, true).Select(entry => entry.Weight).Should().Equal(50, 10, 1 * multiplier, 2 * multiplier);
        ScavengeLoot.Weights(Flagged(), 2, true).Select(entry => entry.Weight).Should().Equal(50, 10, 21 * multiplier, 22 * multiplier);
    }

    [Test]
    public void TheRareEntriesOfATableWithFlaggedEntriesAreTheFlaggedEntries()
    {
        ScavengeLoot.RareEntries(Flagged()).Select(item => item.Resref).Should().Equal("ruby", "jade");
    }

    [Test]
    public void TheRareEntriesOfATableWithNoFlaggedEntriesAreItsLeastLikelyEntries()
    {
        ScavengeLoot.RareEntries(Unflagged()).Select(item => item.Resref).Should().Equal("fiberp_flawed");
        ScavengeLoot.Weights(Unflagged(), 0, true).Select(entry => entry.Weight).Should().Equal(50, 5 * ScavengeLoot.DeeperWeightMultiplier);
    }

    [Test]
    public void GoldIsNeverTheRareEntryOfATableThatHasOtherItems()
    {
        var table = new LootTable
        {
            new LootTableItem("kolto", 1, 20, false),
            new LootTableItem("syringe", 1, 20, false),
            new LootTableItem(ScavengeLoot.GoldResref, 15, 5, false)
        };

        ScavengeLoot.RareEntries(table).Select(item => item.Resref).Should().Equal("kolto", "syringe");
    }

    [Test]
    public void ATableWhoseEntriesAreAllEquallyLikelyIsItsOwnRareEntries()
    {
        var table = new LootTable { new LootTableItem("herb", 1, 50, false) };

        ScavengeLoot.RareEntries(table).Select(item => item.Resref).Should().Equal("herb");
    }

    [Test]
    public void ATableOfOnlyGoldFallsBackToTheWholeTable()
    {
        var table = new LootTable { new LootTableItem(ScavengeLoot.GoldResref, 15, 50, false) };

        ScavengeLoot.RareEntries(table).Select(item => item.Resref).Should().Equal(ScavengeLoot.GoldResref);
    }

    [Test]
    public void AnEmptyTableHasNoRareEntries()
    {
        var empty = () => ScavengeLoot.RareEntries(new LootTable());

        empty.Should().Throw<ArgumentException>();
    }

    [Test]
    public void AWholeTableRollChoosesByWeight_AndTheQuantityIsOneToTheEntrysMaximum()
    {
        // Weights 50, 10, 1, 2: 0 to 49 the ore, 50 to 59 the gem, 60 the ruby, 61 and 62 the jade.
        ScavengeLoot.Roll(Flagged(), 0, 0, false, new ScriptedMinigameRandom(0, 3)).Should().Be(new ScavengeFind("ore", 3, false));
        ScavengeLoot.Roll(Flagged(), 0, 0, false, new ScriptedMinigameRandom(49, 1)).Should().Be(new ScavengeFind("ore", 1, false));
        ScavengeLoot.Roll(Flagged(), 0, 0, false, new ScriptedMinigameRandom(50, 1)).Should().Be(new ScavengeFind("gem", 1, false));
        ScavengeLoot.Roll(Flagged(), 0, 0, false, new ScriptedMinigameRandom(60, 1)).Should().Be(new ScavengeFind("ruby", 1, false));
        ScavengeLoot.Roll(Flagged(), 0, 0, false, new ScriptedMinigameRandom(62, 1)).Should().Be(new ScavengeFind("jade", 1, false));
    }

    [Test]
    public void ADeeperRollRollsTheWholeTable_WithTheRareEntriesFavoured()
    {
        // Treasure Hunter 1 makes the weights 50, 10, 11, 12; the multiplier makes the rare two 33 and 36: 129 in all. 0 to 49 is the
        // ore, 50 to 59 the gem, 60 to 92 the ruby, 93 to 128 the jade.
        var multiplier = ScavengeLoot.DeeperWeightMultiplier;
        var ruby = 60;
        var jade = 60 + 11 * multiplier;

        ScavengeLoot.Roll(Flagged(), 1, 0, true, new ScriptedMinigameRandom(0, 1)).Should().Be(new ScavengeFind("ore", 1, true));
        ScavengeLoot.Roll(Flagged(), 1, 0, true, new ScriptedMinigameRandom(ruby, 1)).Should().Be(new ScavengeFind("ruby", 1, true));
        ScavengeLoot.Roll(Flagged(), 1, 0, true, new ScriptedMinigameRandom(jade, 1)).Should().Be(new ScavengeFind("jade", 1, true));
    }

    [Test]
    public void TreasureHunterMakesARareEntryLikelierOnAWholeTableRoll()
    {
        // With level 3 the weights are 50, 10, 31, 32 (123 in all): 60 to 90 is the ruby and 91 to 122 the jade, where without it the
        // ruby was a single point of 63.
        ScavengeLoot.Roll(Flagged(), 3, 0, false, new ScriptedMinigameRandom(70, 1)).Resref.Should().Be("ruby");
        ScavengeLoot.Roll(Flagged(), 3, 0, false, new ScriptedMinigameRandom(95, 1)).Resref.Should().Be("jade");
    }

    [Test]
    public void CreditfinderRaisesGoldByTwentyPercentPerLevel_RoundingDown_AndNothingElse()
    {
        var table = new LootTable { new LootTableItem(ScavengeLoot.GoldResref, 10, 50, false) };

        ScavengeLoot.Roll(table, 0, 0, false, new ScriptedMinigameRandom(0, 7)).Quantity.Should().Be(7);
        ScavengeLoot.Roll(table, 0, 1, false, new ScriptedMinigameRandom(0, 7)).Quantity.Should().Be(8, "7 + (int)1.4");
        ScavengeLoot.Roll(table, 0, 3, false, new ScriptedMinigameRandom(0, 10)).Quantity.Should().Be(16, "10 + 6");
        ScavengeLoot.Roll(Flagged(), 0, 3, false, new ScriptedMinigameRandom(0, 3)).Quantity.Should().Be(3, "Creditfinder raises gold only");
    }

    [Test]
    public void TheShareOfRollsOnTheRareEntriesRisesWithTreasureHunter_AndAgainWithADeeperRoll()
    {
        var table = Flagged();
        var multiplier = ScavengeLoot.DeeperWeightMultiplier;

        ScavengeLoot.RareShare(table, 0, false).Should().BeApproximately(3.0 / 63, 1e-9);
        ScavengeLoot.RareShare(table, 3, false).Should().BeApproximately(63.0 / 123, 1e-9);
        ScavengeLoot.RareShare(table, 0, true).Should().BeApproximately(3.0 * multiplier / (60 + 3.0 * multiplier), 1e-9);
        ScavengeLoot.RareShare(table, 0, true).Should().BeGreaterThan(ScavengeLoot.RareShare(table, 0, false));
        ScavengeLoot.RareShare(table, 0, true).Should().BeLessThan(1.0, "a Dig Deeper favours the rare entries, it does not guarantee one");
    }
}
