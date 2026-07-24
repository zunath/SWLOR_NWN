using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SlicingService;

namespace SWLOR.Game.Server.Tests.Service;

public class SlicingRewardTests
{
    [Test]
    public void Catalog_HasExactlyOneHundredNewDirectRewardsInAgreedPools()
    {
        var entries = SlicingRewardCatalog.Entries;

        entries.Should().HaveCount(100);
        entries.Where(x => x.Source == SlicingSourceType.Lockbox).Should().HaveCount(50);
        entries.Where(x => x.Source == SlicingSourceType.Terminal).Should().HaveCount(50);

        entries.Count(x => x.Source == SlicingSourceType.Lockbox && x.Category == SlicingRewardCategory.NamedItem).Should().Be(30);
        entries.Count(x => x.Source == SlicingSourceType.Lockbox && x.Category == SlicingRewardCategory.Schematic).Should().Be(10);
        entries.Count(x => x.Source == SlicingSourceType.Lockbox && x.Category == SlicingRewardCategory.FieldNote).Should().Be(5);
        entries.Count(x => x.Source == SlicingSourceType.Lockbox && x.Category == SlicingRewardCategory.Tool).Should().Be(5);

        entries.Count(x => x.Source == SlicingSourceType.Terminal && x.Category == SlicingRewardCategory.NamedItem).Should().Be(20);
        entries.Count(x => x.Source == SlicingSourceType.Terminal && x.Category == SlicingRewardCategory.Schematic).Should().Be(15);
        entries.Count(x => x.Source == SlicingSourceType.Terminal && x.Category == SlicingRewardCategory.FieldNote).Should().Be(10);
        entries.Count(x => x.Source == SlicingSourceType.Terminal && x.Category == SlicingRewardCategory.Tool).Should().Be(5);
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

    [TestCase(0, SlicingRewardCategory.Common)]
    [TestCase(6499, SlicingRewardCategory.Common)]
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
