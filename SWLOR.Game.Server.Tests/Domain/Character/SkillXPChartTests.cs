using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>The skill XP chart and the Espionage practice ranks, read without a container.</summary>
[TestFixture]
public class SkillXPChartTests
{
    [Test]
    public void TheChart_CoversRanksZeroToOneHundred()
    {
        SkillXPChart.GetRequiredXP(0).Should().Be(550);
        SkillXPChart.GetRequiredXP(100).Should().Be(400000);
        FluentActions.Invoking(() => SkillXPChart.GetRequiredXP(101)).Should().Throw<Exception>().WithMessage("Level 101*");
        FluentActions.Invoking(() => SkillXPChart.GetRequiredXP(-1)).Should().Throw<Exception>();
    }

    [Test]
    public void TotalXp_IsTheRunningSumOfTheRequirements()
    {
        var sum = 0;
        for (var level = 0; level <= 100; level++)
        {
            sum += SkillXPChart.GetRequiredXP(level);
            SkillXPChart.GetTotalRequiredXP(level).Should().Be(sum);
        }

        FluentActions.Invoking(() => SkillXPChart.GetTotalRequiredXP(101)).Should().Throw<Exception>().WithMessage("Level 101*");
    }

    [TestCase(0, 0, 0)]
    [TestCase(550, 0, 550)]
    [TestCase(551, 1, 1)]
    [TestCase(1375, 1, 825)]
    public void GetLevelByTotalXP_FindsTheLevelAndTheRemainder(int total, int level, int remainder)
    {
        SkillXPChart.GetLevelByTotalXP(total).Should().Be((level, remainder));
    }

    [TestCase(-1, 450)]
    [TestCase(-4, 76)]
    [TestCase(-5, 0)]
    [TestCase(-30, 0)]
    [TestCase(0, 450)]
    [TestCase(30, 450)]
    public void GetDeltaXP_ReadsTheDifferenceTable_AndCapsAtTheHighestDelta(int delta, int expected)
    {
        SkillXPChart.GetDeltaXP(delta).Should().Be(expected);
    }

    [Test]
    public void GetPracticeXP_HasAFloorUntilTheLimit()
    {
        SkillXPChart.GetPracticeXP(10, 25, 30).Should().Be(150, "a trivial activity still pays the floor");
        SkillXPChart.GetPracticeXP(10, 11, 20).Should().Be(450);
        SkillXPChart.GetPracticeXP(10, 20, 20).Should().Be(0);
    }

    [TestCase(PerkType.Poisoncraft, 1, 0)]
    [TestCase(PerkType.Poisoncraft, 5, 48)]
    [TestCase(PerkType.Trapcraft, 2, 18)]
    [TestCase(PerkType.Slicing, 4, 42)]
    public void GetEspionageRequiredRank_ListsTheTierUnlocks(PerkType profession, int tier, int expected)
    {
        SkillXPChart.GetEspionageRequiredRank(profession, tier).Should().Be(expected);
    }

    [Test]
    public void GetEspionageRequiredRank_RejectsUnknownProfessionsAndTiers()
    {
        FluentActions.Invoking(() => SkillXPChart.GetEspionageRequiredRank(PerkType.Invalid, 1))
            .Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => SkillXPChart.GetEspionageRequiredRank(PerkType.Slicing, 0))
            .Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => SkillXPChart.GetEspionageRequiredRank(PerkType.Slicing, 6))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void GetEspionagePracticeRankLimit_IsTheNextUnlockOrTheSkillCap()
    {
        SkillXPChart.GetEspionagePracticeRankLimit(PerkType.Trapcraft, 1).Should().Be(18);
        SkillXPChart.GetEspionagePracticeRankLimit(PerkType.Trapcraft, 5).Should().Be(50);
    }

    [Test]
    public void CalculateEspionageXP_IsZeroBelowTheTier_AndPracticeXPAbove()
    {
        SkillXPChart.CalculateEspionageXP(PerkType.Poisoncraft, 2, 10).Should().Be(0);
        SkillXPChart.CalculateEspionageXP(PerkType.Poisoncraft, 2, 15).Should().Be(450);
        SkillXPChart.CalculateEspionageXP(PerkType.Poisoncraft, 2, 28).Should().Be(0);
    }
}
