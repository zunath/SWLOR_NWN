using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Covers how many items one power core refines (the base of three, what the Gathering rank adds, and what Bulk Refining adds) and
/// how many power cores a batch of ore takes.
/// </summary>
public class RefineryBatchTests
{
    [TestCase(0, 0, 3)]
    [TestCase(1, 0, 4)]
    [TestCase(6, 0, 9)]
    [TestCase(0, 1, 4)]
    [TestCase(1, 1, 5)]
    [TestCase(3, 2, 8)]
    [TestCase(6, 3, 12)]
    public void AnItemsPerCoreFigure_IsTheBaseOfThree_PlusTheMilestone_PlusBulkRefining(int milestone, int bulkRefining, int expected)
    {
        RefineryViewModel.ItemsPerCore(milestone, bulkRefining).Should().Be(expected);
    }

    [TestCase(-1, 0, 3)]
    [TestCase(0, -2, 3)]
    [TestCase(-5, -5, 3)]
    [TestCase(-1, 2, 5)]
    [TestCase(4, -3, 7)]
    public void ANegativeMilestoneOrBulkRefiningAddsNothing(int milestone, int bulkRefining, int expected)
    {
        RefineryViewModel.ItemsPerCore(milestone, bulkRefining).Should().Be(expected);
    }

    [TestCase(0, 3, 0)]
    [TestCase(1, 3, 1)]
    [TestCase(3, 3, 1)]
    [TestCase(4, 3, 2)]
    [TestCase(9, 9, 1)]
    [TestCase(10, 9, 2)]
    [TestCase(100, 9, 12)]
    [TestCase(100, 12, 9)]
    [TestCase(100, 4, 25)]
    [TestCase(7, 1, 7)]
    public void ABatchTakesThePowerCoresItNeeds_RoundedUp(int itemCount, int itemsPerCore, int expected)
    {
        RefineryViewModel.PowerCoresFor(itemCount, itemsPerCore).Should().Be(expected);
    }

    [TestCase(5, 0, 5)]
    [TestCase(5, -3, 5)]
    [TestCase(0, 0, 0)]
    public void ABatchOfZeroOrLessItemsPerCoreCountsAsOne(int itemCount, int itemsPerCore, int expected)
    {
        RefineryViewModel.PowerCoresFor(itemCount, itemsPerCore).Should().Be(expected);
    }

    [Test]
    public void EveryCoreHoldsAFullBatchExceptTheLast_AndMoreItemsPerCoreNeverCostMoreCores()
    {
        for (var itemsPerCore = 3; itemsPerCore <= 14; itemsPerCore++)
        {
            for (var itemCount = 1; itemCount <= 300; itemCount++)
            {
                var cores = RefineryViewModel.PowerCoresFor(itemCount, itemsPerCore);

                (cores * itemsPerCore).Should().BeGreaterThanOrEqualTo(itemCount, $"{itemCount} ore at {itemsPerCore} a core");
                ((cores - 1) * itemsPerCore).Should().BeLessThan(itemCount, $"{itemCount} ore at {itemsPerCore} a core");
                RefineryViewModel.PowerCoresFor(itemCount, itemsPerCore + 1).Should().BeLessThanOrEqualTo(cores, $"{itemCount} ore at {itemsPerCore + 1} a core");
            }
        }
    }

    [Test]
    public void TheTableAGathering45RefinerSees_IsNineAtTheMilestone_AndTwelveWithBulkRefiningThree()
    {
        var milestone = CraftRankMilestones.RefineryManagementLevelForRank(45);

        milestone.Should().Be(CraftRankMilestones.MaxRefineryManagementLevel);
        milestone.Should().Be(6);
        RefineryViewModel.ItemsPerCore(milestone, 0).Should().Be(9);
        RefineryViewModel.ItemsPerCore(milestone, ProspectingPerkRules.BulkRefiningRank3).Should().Be(12);

        // 100 ore at 9, 10, 11 and 12 a core: 12, 10, 10 and 9 power cores.
        var cores = new[]
        {
            RefineryViewModel.PowerCoresFor(100, RefineryViewModel.ItemsPerCore(milestone, 0)),
            RefineryViewModel.PowerCoresFor(100, RefineryViewModel.ItemsPerCore(milestone, ProspectingPerkRules.BulkRefiningRank1)),
            RefineryViewModel.PowerCoresFor(100, RefineryViewModel.ItemsPerCore(milestone, ProspectingPerkRules.BulkRefiningRank2)),
            RefineryViewModel.PowerCoresFor(100, RefineryViewModel.ItemsPerCore(milestone, ProspectingPerkRules.BulkRefiningRank3))
        };
        cores.Should().Equal(12, 10, 10, 9);
        RefineryViewModel.PowerCoresFor(9, RefineryViewModel.ItemsPerCore(milestone, 0)).Should().Be(1);
        RefineryViewModel.PowerCoresFor(10, RefineryViewModel.ItemsPerCore(milestone, 0)).Should().Be(2);
    }

    [Test]
    public void TheProspectorsBulkRefining_ChangesTheBatchAtTheRanksItsLevelsOpen_OnTopOfTheRankMilestone()
    {
        var perks = new GatheringPerkDefinition().BuildPerks();
        var prospector = GatheringLoadout.Find("prospector-no-style");
        var treasureSeeker = GatheringLoadout.Find("treasure-seeker-no-style");

        foreach (var rank in new[] { 0, 9, 10, 20, 29, 30, 40, 44, 45, 50 })
        {
            var milestone = CraftRankMilestones.RefineryManagementLevelForRank(rank);
            var bulk = rank >= 45 ? ProspectingPerkRules.BulkRefiningRank3
                : rank >= 30 ? ProspectingPerkRules.BulkRefiningRank2
                : rank >= 10 ? ProspectingPerkRules.BulkRefiningRank1
                : 0;

            RefineryViewModel.ItemsPerCore(milestone, prospector.RefineBatchAt(perks, rank)).Should().Be(3 + milestone + bulk, $"rank {rank}");
            RefineryViewModel.ItemsPerCore(milestone, treasureSeeker.RefineBatchAt(perks, rank)).Should().Be(3 + milestone, $"rank {rank}: only the Prospector line has Bulk Refining");
        }
    }

    [Test]
    public void ARefinerWithNoBulkRefining_NeedsTheSamePowerCoresAsBefore()
    {
        // The rank milestone alone, as the refinery computed it before Bulk Refining: ceil(items / (3 + milestone)).
        for (var milestone = 1; milestone <= CraftRankMilestones.MaxRefineryManagementLevel; milestone++)
        {
            for (var itemCount = 1; itemCount <= 200; itemCount++)
            {
                var before = (int)Math.Ceiling(itemCount / (float)(3 + milestone));

                RefineryViewModel.PowerCoresFor(itemCount, RefineryViewModel.ItemsPerCore(milestone, 0)).Should().Be(before, $"{itemCount} ore at milestone {milestone}");
            }
        }
    }
}
