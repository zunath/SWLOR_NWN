using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Crafting revamp workstream 11 changes how fast the four crafting skills level and leaves Espionage alone. Infiltration, slicing, poison and
/// trap crafting and the practice XP of Espionage recipes share <c>Skill.GetDeltaXP</c> and <c>Craft.GetBaseRecipeXP</c> with the crafting skills, so
/// their XP is pinned here: a change to the shared XP code that moves any of these numbers fails this class.
/// </summary>
public class EspionageXpRegressionTests
{
    // Espionage ranks the slicing grid is read at.
    private static readonly int[] Ranks = { 0, 10, 15, 20, 22, 28, 30, 40, 42, 45, 48, 50 };

    [OneTimeSetUp]
    public void LoadTheXpChart() => Skill.CacheXPChartData();

    // npcLevel, espionageRank, undetected, detected (detection pays 15% of the undetected XP, rounded down).
    [TestCase(1, 0, 676, 101)]
    [TestCase(1, 10, 0, 0)]
    [TestCase(1, 50, 0, 0)]
    [TestCase(10, 0, 1200, 180)]
    [TestCase(10, 10, 600, 90)]
    [TestCase(10, 25, 0, 0)]
    [TestCase(25, 0, 1200, 180)]
    [TestCase(25, 10, 1200, 180)]
    [TestCase(25, 25, 600, 90)]
    [TestCase(25, 45, 0, 0)]
    [TestCase(40, 25, 1200, 180)]
    [TestCase(40, 45, 0, 0)]
    [TestCase(50, 0, 1200, 180)]
    [TestCase(50, 25, 1200, 180)]
    [TestCase(50, 45, 1050, 157)]
    [TestCase(50, 50, 600, 90)]
    public void InfiltrationXp_IsUnchanged(int npcLevel, int rank, int undetected, int detected)
    {
        EspionageInfiltration.CalculateXp(npcLevel, rank, false).Should().Be(undetected);
        EspionageInfiltration.CalculateXp(npcLevel, rank, true).Should().Be(detected);
    }

    // Tiers 1-5, each read at Ranks.
    [TestCase(PerkType.Poisoncraft, 1, new[] { 600, 150, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 })]
    [TestCase(PerkType.Poisoncraft, 2, new[] { 0, 0, 600, 150, 150, 0, 0, 0, 0, 0, 0, 0 })]
    [TestCase(PerkType.Poisoncraft, 3, new[] { 0, 0, 0, 0, 0, 600, 300, 0, 0, 0, 0, 0 })]
    [TestCase(PerkType.Poisoncraft, 4, new[] { 0, 0, 0, 0, 0, 0, 0, 600, 300, 150, 0, 0 })]
    [TestCase(PerkType.Poisoncraft, 5, new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 600, 0 })]
    [TestCase(PerkType.Trapcraft, 1, new[] { 600, 150, 150, 0, 0, 0, 0, 0, 0, 0, 0, 0 })]
    [TestCase(PerkType.Trapcraft, 2, new[] { 0, 0, 0, 300, 150, 150, 0, 0, 0, 0, 0, 0 })]
    [TestCase(PerkType.Trapcraft, 3, new[] { 0, 0, 0, 0, 0, 0, 600, 150, 150, 0, 0, 0 })]
    [TestCase(PerkType.Trapcraft, 4, new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 600, 150, 0 })]
    [TestCase(PerkType.Trapcraft, 5, new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 })]
    [TestCase(PerkType.Slicing, 1, new[] { 600, 150, 150, 150, 0, 0, 0, 0, 0, 0, 0, 0 })]
    [TestCase(PerkType.Slicing, 2, new[] { 0, 0, 0, 0, 600, 150, 0, 0, 0, 0, 0, 0 })]
    [TestCase(PerkType.Slicing, 3, new[] { 0, 0, 0, 0, 0, 0, 600, 150, 0, 0, 0, 0 })]
    [TestCase(PerkType.Slicing, 4, new[] { 0, 0, 0, 0, 0, 0, 0, 0, 600, 150, 0, 0 })]
    [TestCase(PerkType.Slicing, 5, new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 600, 0 })]
    public void EspionageTierXp_IsUnchanged(PerkType profession, int tier, int[] expected)
    {
        Ranks.Select(rank => Skill.CalculateEspionageXP(profession, tier, rank)).Should().Equal(expected);
    }

    // Craft.GetBaseRecipeXP sends practice-limited recipes (Espionage's) through GetPracticeXP: the XP chart with a 150 floor until the limit.
    [TestCase(10, 0, 20, 1200)]
    [TestCase(10, 15, 20, 150)]
    [TestCase(10, 20, 20, 0)]
    [TestCase(30, 25, 35, 1050)]
    [TestCase(30, 40, 35, 0)]
    public void PracticeXp_IsUnchanged(int activityLevel, int rank, int rankLimit, int expected)
    {
        Skill.GetPracticeXP(activityLevel, rank, rankLimit).Should().Be(expected);
    }

    [Test]
    public void TheXpChartTheyShareIsUnchanged()
    {
        Enumerable.Range(-6, 15).Select(Skill.GetDeltaXP)
            .Should().Equal(0, 0, 76, 150, 300, 450, 600, 676, 750, 900, 976, 1050, 1200, 1200, 1200);
        Enumerable.Range(0, 50).Sum(Skill.GetRequiredXP).Should().Be(411_125);
    }
}
