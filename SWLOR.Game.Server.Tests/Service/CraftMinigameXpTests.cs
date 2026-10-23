using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>Covers the multiplier a minigame puts on its crafts' base XP, which keeps its XP per hour at the old window's.</summary>
public class CraftMinigameXpTests
{
    [Test]
    public void OnlyAMinigameThatNeedsOneHasAMultiplier_AndOnlyForTheRecipeLevelsThatNeedIt()
    {
        foreach (var minigame in Enum.GetValues<CraftMinigameType>().Where(minigame =>
                     minigame is not (CraftMinigameType.Reaction or CraftMinigameType.Fabricator or CraftMinigameType.Circuit or CraftMinigameType.Line
                         or CraftMinigameType.Search or CraftMinigameType.Kitchen)))
        {
            foreach (var level in new[] { 1, 10, 11, 50 })
                CraftMinigameXp.MultiplierFor(minigame, level).Should().Be(1.0, $"{minigame} level {level}");
        }

        CraftMinigameXp.MultiplierFor(CraftMinigameType.Reaction, 1).Should().BeGreaterThan(1.0);
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Reaction, ReactionRules.XpMultiplierLastLevel).Should().BeGreaterThan(1.0);
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Reaction, ReactionRules.XpMultiplierLastLevel + 1).Should().Be(1.0);
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Reaction, 50).Should().Be(1.0);

        foreach (var level in new[] { 1, 10, 11, 50 })
            CraftMinigameXp.MultiplierFor(CraftMinigameType.Circuit, level).Should().Be(CircuitRules.XpMultiplierFor(level), $"Circuit level {level}");

        foreach (var level in new[] { 1, 10, 11, 20, 30, 31, 41, 50 })
            CraftMinigameXp.MultiplierFor(CraftMinigameType.Kitchen, level).Should().Be(KitchenTuning.XpMultiplierForLevel(level), $"Kitchen level {level}");

        // The Line is not a recipe minigame: its level is the fish's, and every level band of fish has a multiplier.
        foreach (var level in new[] { 1, 10, 11, 50, 52 })
            CraftMinigameXp.MultiplierFor(CraftMinigameType.Line, level).Should().BeGreaterThan(1.0, $"Line fish level {level}");

        CraftMinigameXp.MultiplierFor(CraftMinigameType.Line, 0).Should().Be(LineRules.XpMultiplierLevels0To10);
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Line, 10).Should().Be(LineRules.XpMultiplierLevels0To10);
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Line, 11).Should().Be(LineRules.XpMultiplierLevels11To20);
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Line, 21).Should().Be(LineRules.XpMultiplierLevels21To30);
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Line, 31).Should().Be(LineRules.XpMultiplierLevels31To40);
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Line, 41).Should().Be(LineRules.XpMultiplierLevels41Up);
        CraftMinigameXp.MultiplierFor(CraftMinigameType.Line, 52).Should().Be(LineRules.XpMultiplierLevels41Up);

        // The Search board is not a recipe minigame either: its level is the scavenge site's, 1 to 5 (a level 0 site takes level 1's). It pays today's XP per attempt
        // unless CraftSim shows the XP rate needs more, so its multipliers are at least 1.
        foreach (var level in new[] { 0, 1, 2, 3, 4, 5 })
            CraftMinigameXp.MultiplierFor(CraftMinigameType.Search, level).Should().Be(SearchRules.XpMultiplierForLevel(level)).And.BeGreaterThanOrEqualTo(1.0, $"Search site level {level}");
    }

    [TestCase(10, 2.5, 25)]
    [TestCase(5, 1.5, 8)]
    [TestCase(150, 1.0, 150)]
    [TestCase(0, 3.0, 0)]
    public void ScaleBaseXpRoundsToTheNearestWholeXp(int baseXp, double multiplier, int expected)
    {
        CraftMinigameXp.ScaleBaseXp(baseXp, multiplier).Should().Be(expected);
    }

    [Test]
    public void ARecipeWhoseSkillHasNoMinigameKeepsTodaysXp()
    {
        var recipe = new RecipeDetail { Skill = SkillType.Armor, Level = 20 };

        CraftMinigameRouting.GetRoute(SkillType.Armor).IsLegacy.Should().BeTrue();
        CraftMinigameXp.BaseXp(recipe, 15).Should().Be(Craft.GetBaseRecipeXP(recipe, 15));
    }

    private static readonly SkillType[] PacedSkills = { SkillType.Smithery, SkillType.Engineering, SkillType.Fabrication, SkillType.Agriculture };

    [SetUp]
    public void LoadTheXpChart() => Skill.CacheXPChartData();

    [Test]
    public void TheCraftingPaceMultiplierIsOneNamedConstantOfOnePointOhFive()
    {
        CraftMinigameXp.CraftingPaceMultiplier.Should().Be(1.05);
    }

    [Test]
    public void OnlyTheFourCraftingSkillsGetTheCraftingPaceMultiplier()
    {
        foreach (var skill in Enum.GetValues<SkillType>())
        {
            var expected = PacedSkills.Contains(skill) ? CraftMinigameXp.CraftingPaceMultiplier : 1.0;
            CraftMinigameXp.PaceMultiplierFor(skill).Should().Be(expected, skill.ToString());
            CraftMinigameXp.AppliesCraftingPace(skill).Should().Be(PacedSkills.Contains(skill), skill.ToString());
        }

        // Gathering, Espionage and every combat skill and Piloting are among the skills that keep today's XP.
        foreach (var skill in new[] { SkillType.Gathering, SkillType.Espionage, SkillType.Armor, SkillType.Piloting })
            CraftMinigameXp.PaceMultiplierFor(skill).Should().Be(1.0, skill.ToString());
    }

    [TestCase(SkillType.Smithery)]
    [TestCase(SkillType.Engineering)]
    [TestCase(SkillType.Fabrication)]
    [TestCase(SkillType.Agriculture)]
    public void ACraftInAPacedSkillPaysTheGatedXpTimesThePaceMultiplier(SkillType skill)
    {
        var recipe = new RecipeDetail { Skill = skill, Level = 20 };
        var route = CraftMinigameRouting.GetRoute(skill);
        var baseXp = Craft.GetBaseRecipeXP(recipe, 20);
        var gated = route.Minigame is { } minigame ? CraftMinigameXp.ScaleBaseXp(baseXp, CraftMinigameXp.MultiplierFor(minigame, 20)) : baseXp;

        CraftMinigameXp.BaseXp(recipe, 20).Should().Be(CraftMinigameXp.ScaleBaseXp(gated, 1.05));
        CraftMinigameXp.BaseXp(recipe, 20).Should().BeGreaterThan(gated, "600 XP at level is more than 600 after the multiplier");
    }

    [TestCase(SkillType.Gathering)]
    [TestCase(SkillType.Armor)]
    [TestCase(SkillType.Piloting)]
    public void ARecipeOfAnyOtherSkillKeepsTodaysXp(SkillType skill)
    {
        var recipe = new RecipeDetail { Skill = skill, Level = 20 };

        CraftMinigameXp.BaseXp(recipe, 20).Should().Be(Craft.GetBaseRecipeXP(recipe, 20));
    }

    [Test]
    public void EspionageCraftingXpKeepsItsPracticeXpAndItsReactionBand_WithNoPaceMultiplier()
    {
        var recipe = new RecipeDetail { Skill = SkillType.Espionage, Level = 10, PracticeRankLimit = 20 };
        var reaction = CraftMinigameXp.MultiplierFor(CraftMinigameType.Reaction, 10);

        foreach (var rank in new[] { 0, 10, 15, 19, 20, 30 })
        {
            var practice = Skill.GetPracticeXP(10, rank, 20);
            Craft.GetBaseRecipeXP(recipe, rank).Should().Be(practice, $"rank {rank}");
            CraftMinigameXp.BaseXp(recipe, rank).Should().Be(CraftMinigameXp.ScaleBaseXp(practice, reaction), $"rank {rank}: the Reaction band, nothing more");
        }
    }

    [Test]
    public void ThePaceMultiplierRoundsToTheNearestWholeXpAndNeverLowersAnXp()
    {
        foreach (var baseXp in new[] { 0, 76, 150, 300, 450, 600, 676, 750, 900, 976, 1050, 1200 })
        {
            var paced = CraftMinigameXp.ScaleBaseXp(baseXp, CraftMinigameXp.CraftingPaceMultiplier);
            paced.Should().BeGreaterThanOrEqualTo(baseXp);
            paced.Should().Be((int)Math.Round(baseXp * 1.05, MidpointRounding.AwayFromZero));
        }

        CraftMinigameXp.ScaleBaseXp(600, 1.05).Should().Be(630);
        CraftMinigameXp.ScaleBaseXp(1200, 1.05).Should().Be(1260);
        CraftMinigameXp.ScaleBaseXp(0, 1.05).Should().Be(0);
    }

    [Test]
    public void OnlyCraftingCodeReadsTheCraftsBaseXp_SoCombatAndPilotingXpCannotPickUpThePaceMultiplier()
    {
        var serverRoot = Path.Combine(FindRepositoryRoot(), "SWLOR.Game.Server");
        var files = Directory.EnumerateFiles(serverRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToList();

        string[] Mentioning(params string[] needles) => files
            .Where(path => needles.Any(needle => File.ReadAllText(path).Contains(needle, StringComparison.Ordinal)))
            .Select(path => Path.GetFileName(path))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        // BaseXp is the only member that applies the pace; the fishing fight scales its own XP with the Line's band alone.
        Mentioning("CraftMinigameXp.BaseXp(").Should().Equal("CraftSessionLifecycle.cs", "CraftViewModel.cs");
        Mentioning("CraftingPaceMultiplier", "PaceMultiplierFor", "AppliesCraftingPace").Should().Equal("CraftMinigameXp.cs");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
