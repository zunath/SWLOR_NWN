using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Crafting;

namespace SWLOR.Game.Server.Tests.Service;

public class SlicingRewardTests
{
    [Test]
    public void Catalog_HasExactlyOneHundredTwentyDirectRewardsInAgreedPools()
    {
        var entries = SlicingRewardCatalog.Entries;

        entries.Should().HaveCount(120);
        entries.Where(x => x.Source == SlicingSourceType.Lockbox).Should().HaveCount(60);
        entries.Where(x => x.Source == SlicingSourceType.Terminal).Should().HaveCount(60);

        entries.Count(x => x.Source == SlicingSourceType.Lockbox && x.Category == SlicingRewardCategory.NamedItem).Should().Be(30);
        entries.Count(x => x.Source == SlicingSourceType.Lockbox && x.Category == SlicingRewardCategory.Schematic).Should().Be(10);
        entries.Count(x => x.Source == SlicingSourceType.Lockbox && x.Category == SlicingRewardCategory.FieldNote).Should().Be(5);
        entries.Count(x => x.Source == SlicingSourceType.Lockbox && x.Category == SlicingRewardCategory.Tool).Should().Be(15);

        entries.Count(x => x.Source == SlicingSourceType.Terminal && x.Category == SlicingRewardCategory.NamedItem).Should().Be(20);
        entries.Count(x => x.Source == SlicingSourceType.Terminal && x.Category == SlicingRewardCategory.Schematic).Should().Be(15);
        entries.Count(x => x.Source == SlicingSourceType.Terminal && x.Category == SlicingRewardCategory.FieldNote).Should().Be(10);
        entries.Count(x => x.Source == SlicingSourceType.Terminal && x.Category == SlicingRewardCategory.Tool).Should().Be(15);
    }

    [Test]
    public void Catalog_HasOneExceptionalPerSourceAndTierAndUniqueValidResrefs()
    {
        var entries = SlicingRewardCatalog.Entries;
        entries.Select(x => x.Resref).Should().OnlyHaveUniqueItems();
        entries.Should().OnlyContain(x => x.Resref.Length <= 16);

        for (var tier = 1; tier <= 5; tier++)
        {
            entries.Count(x => x.Tier == tier && x.Source == SlicingSourceType.Lockbox && x.IsExceptional).Should().Be(1);
            entries.Count(x => x.Tier == tier && x.Source == SlicingSourceType.Terminal && x.IsExceptional).Should().Be(1);
        }
    }

    [TestCase(SlicingSourceType.Lockbox)]
    [TestCase(SlicingSourceType.Terminal)]
    public void ToolBonus_SplitsEvenlyAcrossOneToolPerUnlockedToolType(SlicingSourceType source)
    {
        for (var tier = 1; tier <= 5; tier++)
        {
            var pool = SlicingRewardCatalog.Get(source, tier, SlicingRewardCategory.Tool);
            pool.Should().HaveCount(tier);

            var rolled = Enumerable.Range(0, tier * 100)
                .Select(itemRoll => SlicingReward.Roll(source, tier, 0, itemRoll).Resref)
                .ToList();
            foreach (var tool in pool)
                rolled.Count(resref => resref == tool.Resref).Should().Be(100);
        }
    }

    // Design Bible Slicing "Reward Weights and Failure Curve": tiers 1-3 roll 80/12/6/2 and
    // tiers 4-5 roll 50/30/12/8 for tool, named item, schematic, and field note.
    [TestCase(1, 0, SlicingRewardCategory.Tool)]
    [TestCase(3, 7999, SlicingRewardCategory.Tool)]
    [TestCase(3, 8000, SlicingRewardCategory.NamedItem)]
    [TestCase(3, 9199, SlicingRewardCategory.NamedItem)]
    [TestCase(3, 9200, SlicingRewardCategory.Schematic)]
    [TestCase(3, 9799, SlicingRewardCategory.Schematic)]
    [TestCase(3, 9800, SlicingRewardCategory.FieldNote)]
    [TestCase(3, 9999, SlicingRewardCategory.FieldNote)]
    [TestCase(4, 4999, SlicingRewardCategory.Tool)]
    [TestCase(4, 5000, SlicingRewardCategory.NamedItem)]
    [TestCase(5, 7999, SlicingRewardCategory.NamedItem)]
    [TestCase(5, 8000, SlicingRewardCategory.Schematic)]
    [TestCase(5, 9199, SlicingRewardCategory.Schematic)]
    [TestCase(5, 9200, SlicingRewardCategory.FieldNote)]
    [TestCase(5, 9999, SlicingRewardCategory.FieldNote)]
    public void CategoryRoll_PinsAgreedWeights(int tier, int roll, SlicingRewardCategory expected)
    {
        SlicingReward.GetCategoryForRoll(tier, roll).Should().Be(expected);
    }

    [TestCase(1, 8000, 1200, 600, 200)]
    [TestCase(2, 8000, 1200, 600, 200)]
    [TestCase(3, 8000, 1200, 600, 200)]
    [TestCase(4, 5000, 3000, 1200, 800)]
    [TestCase(5, 5000, 3000, 1200, 800)]
    public void SuccessBonus_RollsEachTiersWeightsAndNeverMaterialOnly(
        int tier, int tools, int namedItems, int schematics, int fieldNotes)
    {
        var categories = Enumerable.Range(0, 10000)
            .Select(roll => SlicingReward.GetCategoryForRoll(tier, roll))
            .ToList();

        categories.Count(x => x == SlicingRewardCategory.Tool).Should().Be(tools);
        categories.Count(x => x == SlicingRewardCategory.NamedItem).Should().Be(namedItems);
        categories.Count(x => x == SlicingRewardCategory.Schematic).Should().Be(schematics);
        categories.Count(x => x == SlicingRewardCategory.FieldNote).Should().Be(fieldNotes);
        categories.Should().NotContain(SlicingRewardCategory.Common);
    }

    [TestCase(SlicingSourceType.Lockbox)]
    [TestCase(SlicingSourceType.Terminal)]
    public void SuccessRewards_AlwaysIncludeSuppliesAndOneTierAppropriateBonus(SlicingSourceType source)
    {
        for (var tier = 1; tier <= 5; tier++)
        {
            var (minimumSupplies, maximumSupplies) = tier >= 4 ? (5, 8) : (3, 5);
            foreach (var categoryRoll in new[] { 0, 4999, 5000, 7999, 8000, 9199, 9200, 9799, 9800, 9999 })
            {
                var rewards = SlicingReward.RollSuccess(source, tier, categoryRoll, 0, categoryRoll);

                rewards.Should().HaveCount(2);
                rewards.Should().OnlyContain(x => x.Source == source && x.Tier == tier);
                rewards[0].Category.Should().Be(SlicingRewardCategory.Common);
                rewards[0].Quantity.Should().BeInRange(minimumSupplies, maximumSupplies);
                rewards[1].Category.Should().NotBe(SlicingRewardCategory.Common);
                rewards[1].Quantity.Should().Be(1);
            }
        }
    }

    [TestCase(SlicingSourceType.Lockbox, 3, new[] { 3, 4, 5 })]
    [TestCase(SlicingSourceType.Terminal, 3, new[] { 3, 4, 5 })]
    [TestCase(SlicingSourceType.Lockbox, 5, new[] { 5, 6, 7, 8 })]
    [TestCase(SlicingSourceType.Terminal, 4, new[] { 5, 6, 7, 8 })]
    public void SuppliesRoll_ReachesEveryMaterialAndQuantityWithoutChangingTheBonus(
        SlicingSourceType source, int tier, int[] quantities)
    {
        var rewards = Enumerable.Range(0, 5 * quantities.Length)
            .Select(roll => SlicingReward.RollSuccess(source, tier, 0, 1, roll))
            .ToList();

        rewards.Select(x => x[0].Resref).Distinct().Should().HaveCount(5);
        foreach (var material in rewards.Select(x => x[0].Resref).Distinct())
            rewards.Where(x => x[0].Resref == material).Select(x => x[0].Quantity)
                .Should().BeEquivalentTo(quantities);

        rewards.Select(x => x[1].Resref).Should().OnlyContain(resref => resref == rewards[0][1].Resref);
        var differentBonus = SlicingReward.RollSuccess(source, tier, 0, 0, 0);
        differentBonus[0].Should().BeEquivalentTo(rewards[0][0]);
        differentBonus[1].Resref.Should().NotBe(rewards[0][1].Resref);
    }

    [Test]
    public void NamedRoll_PreservesExceptionalAndLegacyAbsoluteBands()
    {
        SlicingReward.Roll(SlicingSourceType.Lockbox, 3, 8000, 0).IsExceptional.Should().BeTrue();
        SlicingReward.Roll(SlicingSourceType.Terminal, 3, 8000, 0).IsExceptional.Should().BeTrue();
        SlicingReward.Roll(SlicingSourceType.Lockbox, 5, 5000, 0).IsExceptional.Should().BeTrue();
        SlicingReward.Roll(SlicingSourceType.Lockbox, 5, 5000, 50).Resref.Should().StartWith("espn_");

        var legacy = SlicingReward.Roll(SlicingSourceType.Lockbox, 3, 8000, 50);
        legacy.IsNewDirectReward.Should().BeFalse();
        legacy.Resref.Should().StartWith("espn_");

        var terminalNormal = SlicingReward.Roll(SlicingSourceType.Terminal, 3, 8000, 50);
        terminalNormal.IsNewDirectReward.Should().BeTrue();
        terminalNormal.IsExceptional.Should().BeFalse();
    }
}
