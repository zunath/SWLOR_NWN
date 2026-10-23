using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Crafting revamp workstream 11b: the Smithery, Engineering, Fabrication and Agriculture guilds give 50% of the XP chart's pay for the task's
/// level against the player's rank, per item handed in. The Hunter's Guild gives none.
/// </summary>
public class GuildTaskXpTests
{
    private static readonly GuildType[] CraftingGuilds =
    {
        GuildType.SmitheryGuild, GuildType.EngineeringGuild, GuildType.FabricationGuild, GuildType.AgricultureGuild
    };

    [OneTimeSetUp]
    public void LoadTheXpChart() => Skill.CacheXPChartData();

    [Test]
    public void TheAwardIsFiftyPercentOfTheCharts()
    {
        GuildTaskXp.XpPercent.Should().Be(50);
    }

    [TestCase(GuildType.SmitheryGuild, SkillType.Smithery)]
    [TestCase(GuildType.EngineeringGuild, SkillType.Engineering)]
    [TestCase(GuildType.FabricationGuild, SkillType.Fabrication)]
    [TestCase(GuildType.AgricultureGuild, SkillType.Agriculture)]
    public void EachCraftingGuildGivesXpInItsOwnSkill(GuildType guild, SkillType skill)
    {
        GuildTaskXp.SkillOf(guild).Should().Be(skill);
        GuildTaskXp.GivesXp(guild).Should().BeTrue();
    }

    [TestCase(GuildType.HuntersGuild)]
    [TestCase(GuildType.Invalid)]
    public void TheHuntersGuildAndAnInvalidGuildGiveNoXp(GuildType guild)
    {
        GuildTaskXp.SkillOf(guild).Should().Be(SkillType.Invalid);
        GuildTaskXp.GivesXp(guild).Should().BeFalse();
        GuildTaskXp.TaskLevel(guild, "anything", true, (_, _) => 10, _ => 10).Should().BeNull("nothing resolves a level for a guild that gives no XP");
    }

    [Test]
    public void OnlyTheFourCraftingGuildsGiveXp()
    {
        Enum.GetValues<GuildType>().Where(GuildTaskXp.GivesXp).Should().BeEquivalentTo(CraftingGuilds);
    }

    // task level, skill rank, items, expected XP: half the chart's XP for level - rank, per item, rounded down.
    [TestCase(10, 10, 1, 300)]
    [TestCase(10, 10, 4, 1200)]
    [TestCase(11, 10, 1, 338)]
    [TestCase(16, 10, 1, 600)]
    [TestCase(40, 10, 1, 600)]
    [TestCase(1, 0, 1, 338)]
    [TestCase(10, 11, 1, 225)]
    [TestCase(10, 12, 1, 150)]
    [TestCase(10, 13, 1, 75)]
    [TestCase(10, 14, 1, 38)]
    [TestCase(10, 15, 1, 0)]
    [TestCase(10, 30, 4, 0)]
    [TestCase(10, 10, 0, 0)]
    public void TheAwardIsHalfTheChartsXpForTheLevelAgainstTheRankPerItem(int taskLevel, int rank, int quantity, int expected)
    {
        GuildTaskXp.Amount(taskLevel, rank, quantity).Should().Be(expected);
    }

    [Test]
    public void AHandInPaysNothingFromFiveLevelsBelowTheRank_TheSameCutoffAsCraftingAndQuickCraft()
    {
        foreach (var level in new[] { 1, 10, 25, 49 })
        {
            GuildTaskXp.Amount(level, level + 4, 1).Should().BeGreaterThan(0, $"level {level} at rank {level + 4}");
            GuildTaskXp.Amount(level, level + 5, 1).Should().Be(0, $"level {level} at rank {level + 5}");

            // Crafting the recipe pays the chart's XP until the same rank, and quick craft opens at it.
            Skill.GetDeltaXP(level - (level + 4)).Should().BeGreaterThan(0);
            Skill.GetDeltaXP(level - (level + 5)).Should().Be(0);
            QuickCraftRules.GetRequiredRank(new RecipeDetail { Level = level }).Should().Be(level + 5);
        }
    }

    [Test]
    public void TheAwardNeverExceedsHalfTheChartsTopPayPerItem()
    {
        for (var level = 0; level <= 53; level++)
        for (var rank = 0; rank <= 50; rank++)
            GuildTaskXp.Amount(level, rank, 1).Should().BeLessThanOrEqualTo(600);
    }

    [Test]
    public void ACraftedTaskUsesTheRecipesLevel_AndACatchTaskUsesTheFishesLevel()
    {
        int? Recipe(SkillType skill, string resref) => skill == SkillType.Agriculture && resref == "roast_carp" ? 12 : null;
        int? Fish(string resref) => resref == "moat_carp" ? 0 : null;

        GuildTaskXp.TaskLevel(GuildType.AgricultureGuild, "roast_carp", true, Recipe, Fish).Should().Be(12);
        GuildTaskXp.TaskLevel(GuildType.AgricultureGuild, "moat_carp", false, Recipe, Fish).Should().Be(0);
        GuildTaskXp.TaskLevel(GuildType.AgricultureGuild, "roast_carp", false, Recipe, Fish).Should().Be(12, "a non-fish good a recipe makes uses the recipe");
        GuildTaskXp.TaskLevel(GuildType.AgricultureGuild, "moat_carp", true, Recipe, Fish).Should().BeNull("a crafted task never reads a fish");
        GuildTaskXp.TaskLevel(GuildType.AgricultureGuild, "unknown_item", true, Recipe, Fish).Should().BeNull();
        GuildTaskXp.TaskLevel(GuildType.AgricultureGuild, "", true, Recipe, Fish).Should().BeNull();
    }

    [Test]
    public void ARecipeIsLookedUpInTheGuildsOwnSkill()
    {
        var asked = new List<SkillType>();
        int? Recipe(SkillType skill, string resref)
        {
            asked.Add(skill);
            return 5;
        }

        foreach (var guild in CraftingGuilds)
            GuildTaskXp.TaskLevel(guild, "item", true, Recipe, _ => null);

        asked.Should().Equal(SkillType.Smithery, SkillType.Engineering, SkillType.Fabrication, SkillType.Agriculture);
    }

    [Test]
    public void TheFishLevelsComeFromTheFishEnum()
    {
        GuildTaskXp.FishLevel("moat_carp").Should().Be(0);
        GuildTaskXp.FishLevel("Moat_Carp").Should().Be(0, "resrefs compare without case");
        GuildTaskXp.FishLevel("frigorifish").Should().Be(50);
        GuildTaskXp.FishLevel("not_a_fish").Should().BeNull();
    }
}
