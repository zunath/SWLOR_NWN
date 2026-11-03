using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Tests.Service;

public class CraftRankMilestonesTests
{
    // The skill ranks the retired perks required for each of their ranks, which are the rank milestones now.
    private static readonly int[] TierPerkRequirements = { 0, 10, 20, 30, 40 };
    private static readonly int[] RefineryManagementRequirements = { 0, 10, 20, 30, 40, 45 };
    private static readonly int[] ResearchRequirements = { 10, 20, 30, 40, 50 };
    private static readonly int[] ResearchMilestoneRequirements = { 25, 50 };

    [TestCase(-1, 1)]
    [TestCase(0, 1)]
    [TestCase(9, 1)]
    [TestCase(10, 2)]
    [TestCase(19, 2)]
    [TestCase(20, 3)]
    [TestCase(29, 3)]
    [TestCase(30, 4)]
    [TestCase(39, 4)]
    [TestCase(40, 5)]
    [TestCase(49, 5)]
    [TestCase(50, 5)]
    [TestCase(400, 5)]
    public void TierForRank_UnlocksOneTierEveryTenRanksFromRankZero(int rank, int expectedTier)
    {
        CraftRankMilestones.TierForRank(rank).Should().Be(expectedTier);
    }

    [Test]
    public void TierForRank_GrantsEachTierAtTheRankItsPerkRequired()
    {
        for (var tier = 1; tier <= TierPerkRequirements.Length; tier++)
        {
            var requiredRank = TierPerkRequirements[tier - 1];
            CraftRankMilestones.TierForRank(requiredRank).Should().Be(tier);
            CraftRankMilestones.RequiredRankForTier(tier).Should().Be(requiredRank);
            if (requiredRank > 0)
                CraftRankMilestones.TierForRank(requiredRank - 1).Should().Be(tier - 1);
        }
    }

    [TestCase(0, 0)]
    [TestCase(1, 0)]
    [TestCase(2, 10)]
    [TestCase(3, 20)]
    [TestCase(4, 30)]
    [TestCase(5, 40)]
    public void RequiredRankForTier_NamesTheLowestRankOfTheTier(int tier, int expectedRank)
    {
        CraftRankMilestones.RequiredRankForTier(tier).Should().Be(expectedRank);
    }

    [TestCase(-1, 1)]
    [TestCase(0, 1)]
    [TestCase(9, 1)]
    [TestCase(10, 2)]
    [TestCase(19, 2)]
    [TestCase(20, 3)]
    [TestCase(29, 3)]
    [TestCase(30, 4)]
    [TestCase(39, 4)]
    [TestCase(40, 5)]
    [TestCase(44, 5)]
    [TestCase(45, 6)]
    [TestCase(50, 6)]
    public void RefineryManagementLevelForRank_AddsAnItemAtEachMilestone(int gatheringRank, int expectedLevel)
    {
        CraftRankMilestones.RefineryManagementLevelForRank(gatheringRank).Should().Be(expectedLevel);
    }

    [Test]
    public void RefineryManagementLevelForRank_GrantsEachLevelAtTheRankItsPerkRequired()
    {
        for (var level = 1; level <= RefineryManagementRequirements.Length; level++)
        {
            var requiredRank = RefineryManagementRequirements[level - 1];
            CraftRankMilestones.RefineryManagementLevelForRank(requiredRank).Should().Be(level);
            if (requiredRank > 0)
                CraftRankMilestones.RefineryManagementLevelForRank(requiredRank - 1).Should().Be(level - 1);
        }

        CraftRankMilestones.RefineryManagementLevelForRank(50).Should().Be(CraftRankMilestones.MaxRefineryManagementLevel);
        (3 + CraftRankMilestones.RefineryManagementLevelForRank(45)).Should().Be(9, "a power core refines 9 items at Gathering 45");
    }

    [TestCase(-1, 0)]
    [TestCase(0, 0)]
    [TestCase(9, 0)]
    [TestCase(10, 1)]
    [TestCase(19, 1)]
    [TestCase(20, 2)]
    [TestCase(29, 2)]
    [TestCase(30, 3)]
    [TestCase(39, 3)]
    [TestCase(40, 4)]
    [TestCase(49, 4)]
    [TestCase(50, 5)]
    [TestCase(400, 5)]
    public void ResearchTierForRank_StartsAtFabricationTenAndStopsAtFive(int fabricationRank, int expectedTier)
    {
        CraftRankMilestones.ResearchTierForRank(fabricationRank).Should().Be(expectedTier);
    }

    [Test]
    public void ResearchTierForRank_GrantsEachTierAtTheRankItsPerkRequired()
    {
        for (var tier = 1; tier <= ResearchRequirements.Length; tier++)
        {
            var requiredRank = ResearchRequirements[tier - 1];
            CraftRankMilestones.ResearchTierForRank(requiredRank).Should().Be(tier);
            CraftRankMilestones.ResearchTierForRank(requiredRank - 1).Should().Be(tier - 1);
            CraftRankMilestones.RequiredFabricationRankForResearchTier(tier).Should().Be(requiredRank);
        }
    }

    [TestCase(0, 10)]
    [TestCase(1, 10)]
    [TestCase(2, 20)]
    [TestCase(5, 50)]
    public void RequiredFabricationRankForResearchTier_IsTenPerTier(int tier, int expectedRank)
    {
        CraftRankMilestones.RequiredFabricationRankForResearchTier(tier).Should().Be(expectedRank);
    }

    [TestCase(0, 1, 0)]
    [TestCase(24, 1, 0)]
    [TestCase(25, 2, 1)]
    [TestCase(49, 2, 1)]
    [TestCase(50, 3, 2)]
    [TestCase(400, 3, 2)]
    public void ResearchMilestones_AddAJobAndALicensedRunAtFabricationTwentyFiveAndFifty(
        int fabricationRank,
        int expectedConcurrentJobs,
        int expectedLicensedRunBonus)
    {
        CraftRankMilestones.MaxConcurrentResearchJobsForRank(fabricationRank).Should().Be(expectedConcurrentJobs);
        CraftRankMilestones.BlueprintLicensedRunBonusForRank(fabricationRank).Should().Be(expectedLicensedRunBonus);
    }

    [Test]
    public void ResearchMilestones_MatchTheRanksTheRetiredPerksRequired()
    {
        for (var level = 1; level <= ResearchMilestoneRequirements.Length; level++)
        {
            var requiredRank = ResearchMilestoneRequirements[level - 1];
            CraftRankMilestones.BlueprintLicensedRunBonusForRank(requiredRank).Should().Be(level);
            CraftRankMilestones.BlueprintLicensedRunBonusForRank(requiredRank - 1).Should().Be(level - 1);
            CraftRankMilestones.MaxConcurrentResearchJobsForRank(requiredRank).Should().Be(level + 1);
            CraftRankMilestones.MaxConcurrentResearchJobsForRank(requiredRank - 1).Should().Be(level);
        }
    }
}
