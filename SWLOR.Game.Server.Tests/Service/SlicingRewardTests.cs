using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SlicingService;

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

    [TestCase(0, SlicingRewardCategory.Tool)]
    [TestCase(6499, SlicingRewardCategory.Tool)]
    [TestCase(6500, SlicingRewardCategory.Tool)]
    [TestCase(7999, SlicingRewardCategory.Tool)]
    [TestCase(8000, SlicingRewardCategory.NamedItem)]
    [TestCase(9199, SlicingRewardCategory.NamedItem)]
    [TestCase(9200, SlicingRewardCategory.Schematic)]
    [TestCase(9799, SlicingRewardCategory.Schematic)]
    [TestCase(9800, SlicingRewardCategory.FieldNote)]
    [TestCase(9999, SlicingRewardCategory.FieldNote)]
    public void CategoryRoll_PinsAgreedWeights(int roll, SlicingRewardCategory expected)
    {
        SlicingReward.GetCategoryForRoll(roll).Should().Be(expected);
    }

    [Test]
    public void SuccessBonus_KeepsRareRatesAndReplacesMaterialOnlyResultsWithTools()
    {
        var categories = Enumerable.Range(0, 10000)
            .Select(SlicingReward.GetCategoryForRoll)
            .ToList();

        categories.Count(x => x == SlicingRewardCategory.Tool).Should().Be(8000);
        categories.Count(x => x == SlicingRewardCategory.NamedItem).Should().Be(1200);
        categories.Count(x => x == SlicingRewardCategory.Schematic).Should().Be(600);
        categories.Count(x => x == SlicingRewardCategory.FieldNote).Should().Be(200);
        categories.Should().NotContain(SlicingRewardCategory.Common);
    }

    [TestCase(SlicingSourceType.Lockbox)]
    [TestCase(SlicingSourceType.Terminal)]
    public void SuccessRewards_AlwaysIncludeSuppliesAndOneTierAppropriateBonus(SlicingSourceType source)
    {
        for (var tier = 1; tier <= 5; tier++)
        {
            foreach (var categoryRoll in new[] { 0, 6499, 6500, 7999, 8000, 9199, 9200, 9799, 9800, 9999 })
            {
                var rewards = SlicingReward.RollSuccess(source, tier, categoryRoll, 0, categoryRoll);

                rewards.Should().HaveCount(2);
                rewards.Should().OnlyContain(x => x.Source == source && x.Tier == tier);
                rewards[0].Category.Should().Be(SlicingRewardCategory.Common);
                rewards[0].Quantity.Should().BeInRange(3, 5);
                rewards[1].Category.Should().NotBe(SlicingRewardCategory.Common);
                rewards[1].Quantity.Should().Be(1);
            }
        }
    }

    [TestCase(SlicingSourceType.Lockbox)]
    [TestCase(SlicingSourceType.Terminal)]
    public void SuppliesRoll_ReachesEveryMaterialAndQuantityWithoutChangingTheBonus(SlicingSourceType source)
    {
        var rewards = Enumerable.Range(0, 15)
            .Select(roll => SlicingReward.RollSuccess(source, 3, 8000, 50, roll))
            .ToList();

        rewards.Select(x => x[0].Resref).Distinct().Should().HaveCount(5);
        foreach (var material in rewards.Select(x => x[0].Resref).Distinct())
            rewards.Where(x => x[0].Resref == material).Select(x => x[0].Quantity)
                .Should().BeEquivalentTo(new[] { 3, 4, 5 });

        rewards.Select(x => x[1].Resref).Should().OnlyContain(resref => resref == rewards[0][1].Resref);
        var differentBonus = SlicingReward.RollSuccess(source, 3, 8000, 0, 0);
        differentBonus[0].Should().BeEquivalentTo(rewards[0][0]);
        differentBonus[1].Resref.Should().NotBe(rewards[0][1].Resref);
    }

    [Test]
    public void NamedRoll_PreservesExceptionalAndLegacyAbsoluteBands()
    {
        SlicingReward.Roll(SlicingSourceType.Lockbox, 3, 8000, 0).IsExceptional.Should().BeTrue();
        SlicingReward.Roll(SlicingSourceType.Terminal, 3, 8000, 0).IsExceptional.Should().BeTrue();

        var legacy = SlicingReward.Roll(SlicingSourceType.Lockbox, 3, 8000, 50);
        legacy.IsNewDirectReward.Should().BeFalse();
        legacy.Resref.Should().StartWith("espn_");

        var terminalNormal = SlicingReward.Roll(SlicingSourceType.Terminal, 3, 8000, 50);
        terminalNormal.IsNewDirectReward.Should().BeTrue();
        terminalNormal.IsExceptional.Should().BeFalse();
    }
}
