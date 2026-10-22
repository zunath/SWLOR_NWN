using FluentAssertions;
using System.Text.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Tests.Service.Reaction;

/// <summary>
/// Covers the Espionage recipes the Reaction crafts: their parameters, the unchanged ownership gates and practice XP, a full
/// attempt for every recipe, the session lifecycle around an attempt, and the bench that opens the window.
/// </summary>
public class ReactionRecipeTests
{
    private static readonly Lazy<IReadOnlyList<KeyValuePair<RecipeType, RecipeDetail>>> EspionageRecipes = new(() =>
        RecipeDefinitionLoader.LoadGame()
            .Where(pair => pair.Value.Skill == SkillType.Espionage)
            .OrderBy(pair => pair.Value.Level)
            .ThenBy(pair => pair.Key.ToString(), StringComparer.Ordinal)
            .ToList());

    private static IEnumerable<TestCaseData> EveryRecipe() =>
        EspionageRecipes.Value.Select(pair => new TestCaseData(pair.Key).SetName($"{{m}}({pair.Key})"));

    private static RecipeDetail Recipe(RecipeType type) => EspionageRecipes.Value.Single(pair => pair.Key == type).Value;

    [Test]
    public void EspionageHasFifteenRecipes_AndEachCarriesReactionParameters()
    {
        EspionageRecipes.Value.Should().HaveCount(15);
        EspionageRecipes.Value.Should().OnlyContain(pair => pair.Value.MinigameParameters is ReactionParameters);
        EspionageRecipes.Value.Count(pair => pair.Value.Category == RecipeCategoryType.Poison).Should().Be(10);
        EspionageRecipes.Value.Count(pair => pair.Value.Category == RecipeCategoryType.Tool).Should().Be(5);
    }

    [Test]
    public void PoisonsAndTrapsUseDifferentCyclePatterns()
    {
        var poisons = EspionageRecipes.Value
            .Where(pair => pair.Value.Category == RecipeCategoryType.Poison)
            .Select(pair => ((ReactionParameters)pair.Value.MinigameParameters).CyclePattern)
            .Distinct();
        var traps = EspionageRecipes.Value
            .Where(pair => pair.Value.Category == RecipeCategoryType.Tool)
            .Select(pair => ((ReactionParameters)pair.Value.MinigameParameters).CyclePattern)
            .Distinct();

        poisons.Should().NotIntersectWith(traps);
        traps.Should().ContainSingle();
    }

    [Test]
    public void TheTierSetsThePeakIntensity()
    {
        var tiers = new Dictionary<int, int>
        {
            [3] = 1, [6] = 1, [16] = 2, [18] = 2, [26] = 3, [30] = 3, [36] = 4, [45] = 4, [46] = 5, [50] = 5
        };

        foreach (var (type, detail) in EspionageRecipes.Value)
        {
            var parameters = (ReactionParameters)detail.MinigameParameters;

            ReactionParameterProfile.TierForLevel(detail.Level).Should().Be(tiers[detail.Level], type.ToString());
            parameters.PeakIntensity.Should().Be(tiers[detail.Level], type.ToString());
        }
    }

    [Test]
    public void EachTierOfEachFamilyIsHarderThanTheTierBefore()
    {
        var profile = new ReactionParameterProfile();

        foreach (var category in new[] { RecipeCategoryType.Poison, RecipeCategoryType.Tool })
        {
            var family = EspionageRecipes.Value.Where(pair => pair.Value.Category == category).ToList();
            var byTier = family.GroupBy(pair => ReactionParameterProfile.TierForLevel(pair.Value.Level))
                .OrderBy(group => group.Key)
                .Select(group => group.Max(pair => profile.BudgetFor(pair.Value.Level).Min))
                .ToList();

            byTier.Should().BeInAscendingOrder();
            byTier.Distinct().Should().HaveCount(byTier.Count);
        }
    }

    [Test]
    public void TheOwnershipGatesAreUnchanged()
    {
        RequirementTexts(RecipeType.VenomCoating1).Should().Contain("Requires Poisoncraft 1.");
        RequirementTexts(RecipeType.VenomCoating5).Should().Contain("Requires Poisoncraft 5.");
        RequirementTexts(RecipeType.WhisperthornConcentrate).Should().Contain("Requires Poisoncraft 1.");
        RequirementTexts(RecipeType.NightrootConcentrate).Should().Contain("Requires Poisoncraft 5.");
        RequirementTexts(RecipeType.TrapKit1).Should().Contain("Requires Trapcraft 1.");
        RequirementTexts(RecipeType.TrapKit4).Should().Contain("Requires Trapcraft 4.");
        RequirementTexts(RecipeType.TrapKit5).Should().Contain("Requires Master Saboteur 1.");
        Recipe(RecipeType.WhisperthornConcentrate).Requirements.Should().ContainSingle(requirement => requirement is RecipeUnlockRequirement);
    }

    private static IEnumerable<string> RequirementTexts(RecipeType type) =>
        Recipe(type).Requirements.OfType<RecipePerkRequirement>().Select(requirement => requirement.RequirementText);

    [TestCaseSource(nameof(EveryRecipe))]
    public void EspionageXpStaysTiedToEspionageRankThroughPracticeXp(RecipeType type)
    {
        var recipe = Recipe(type);

        recipe.PracticeRankLimit.Should().NotBeNull();
        foreach (var rank in new[] { 0, recipe.Level, recipe.PracticeRankLimit!.Value - 1, recipe.PracticeRankLimit.Value })
        {
            Craft.GetBaseRecipeXP(recipe, rank).Should().Be(
                Skill.GetPracticeXP(recipe.Level, rank, recipe.PracticeRankLimit.Value), $"rank {rank}");
        }

        Craft.GetBaseRecipeXP(recipe, recipe.PracticeRankLimit.Value).Should().Be(0, "a mastered tier trains nothing");
    }

    [TestCaseSource(nameof(EveryRecipe))]
    public void TheReactionPaysPracticeXpTimesItsOneNamedMultiplier(RecipeType type)
    {
        var recipe = Recipe(type);

        foreach (var rank in new[] { 0, recipe.Level, recipe.PracticeRankLimit!.Value })
        {
            CraftMinigameXp.BaseXp(recipe, rank).Should().Be(
                CraftMinigameXp.ScaleBaseXp(Craft.GetBaseRecipeXP(recipe, rank), ReactionRules.XpMultiplier), $"rank {rank}");
        }
    }

    [TestCaseSource(nameof(EveryRecipe))]
    public void EveryRecipeCanBeFinishedAtItsLevelFromEveryStartingPosition(RecipeType type)
    {
        var recipe = Recipe(type);
        var settings = ReactionSettings.ForRecipe(recipe, recipe.Level);

        for (var start = 0; start < settings.Cycle.StartPositionCount; start++)
        {
            var engine = new ReactionEngine(new SeededMinigameRandom(1), settings, start);

            var path = ReactionPlanner.FindPath(engine, minimumQuality: 90);

            path.Should().NotBeNull($"{type} can be finished at quality 90 or better from position {start}");
            path!.Count.Should().BeInRange(8, 25, $"{type} from position {start}");
        }
    }

    [TestCaseSource(nameof(EveryRecipe))]
    public void EveryRankRunsToAnEnd(RecipeType type)
    {
        var recipe = Recipe(type);

        foreach (var rank in new[] { 0, Math.Max(recipe.Level - 3, 0), recipe.Level, 50 })
        {
            var engine = new ReactionEngine(new SeededMinigameRandom(2), ReactionSettings.ForRecipe(recipe, rank));
            Play(engine);
            engine.Status.Should().NotBe(MinigameStatus.InProgress, $"rank {rank}");
        }
    }

    [Test]
    public void ARecipeAtItsMinimumRankCanStillBeFinished()
    {
        foreach (var (type, recipe) in EspionageRecipes.Value)
        {
            var settings = ReactionSettings.ForRecipe(recipe, Math.Max(recipe.Level - 3, 0));
            var engine = new ReactionEngine(new SeededMinigameRandom(1), settings, 0);

            ReactionPlanner.FindPath(engine, minimumQuality: 70).Should().NotBeNull($"{type} at its minimum rank");
        }
    }

    [Test]
    public void ASuccessfulAttemptSettlesThroughTheSessionLifecycle_WithPracticeXpAndQualityBonus()
    {
        var recipe = Recipe(RecipeType.VenomCoating2);
        const int rank = 16;
        var world = new FakeCraftSessionWorld();
        world.Recipes[RecipeType.VenomCoating2] = recipe;
        foreach (var (resref, quantity) in recipe.Components)
            world.Inventory[resref] = quantity;
        var player = new Player("player-1");
        player.Skills[SkillType.Espionage] = new PlayerSkill { Rank = rank };
        var lifecycle = new CraftSessionLifecycle(world, new ScriptedMinigameRandom());

        var start = lifecycle.Start(
            FakeCraftSessionWorld.PlayerHandle,
            player,
            new CraftSessionStartRequest(RecipeType.VenomCoating2, OBJECT_INVALID, Array.Empty<uint>()));
        start.Succeeded.Should().BeTrue(start.Message);
        player.ActiveCraftSession.Should().NotBeNull("the committed materials are saved before the attempt is played");
        var engine = new ReactionEngine(new SeededMinigameRandom(1), ReactionSettings.ForRecipe(recipe, rank), 0);
        ReactionPlanner.Play(engine, ReactionPlanner.FindPath(engine, minimumQuality: 90)!);
        engine.Status.Should().Be(MinigameStatus.Succeeded);

        var success = lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, player, start.Session, engine.Meters.Quality);

        success.Should().NotBeNull();
        success!.Outcome.QualityPercent.Should().Be(engine.Meters.Quality.Percent);
        var baseXp = CraftMinigameXp.ScaleBaseXp(
            Skill.GetPracticeXP(recipe.Level, rank, recipe.PracticeRankLimit!.Value), ReactionRules.XpMultiplier);
        success.Outcome.Xp.Should().Be(CraftOutcomeRules.CalculateXp(baseXp, 0, true, engine.Meters.Quality.Fraction));
        world.XpGiven.Should().ContainSingle().Which.Skill.Should().Be(SkillType.Espionage);
        world.Events.Should().Contain($"create:{recipe.Resref}");
        player.ActiveCraftSession.Should().BeNull();
        player.CraftedRecipes.Should().ContainKey(RecipeType.VenomCoating2);
        success.Outcome.Enhancements.Should().BeEmpty("Espionage recipes have no enhancement slots");
    }

    [Test]
    public void ABrokenPieceSettlesUnderTheSixtyFivePercentRule_WithFifteenPercentOfTheBaseXp()
    {
        var recipe = Recipe(RecipeType.TrapKit2);
        const int rank = 18;
        var world = new FakeCraftSessionWorld();
        world.Recipes[RecipeType.TrapKit2] = recipe;
        foreach (var (resref, quantity) in recipe.Components)
            world.Inventory[resref] = quantity;
        var player = new Player("player-1");
        player.Skills[SkillType.Espionage] = new PlayerSkill { Rank = rank };
        // Two component stacks: the first is lost on a roll of 65 or less, the second is kept on a roll above it.
        var lifecycle = new CraftSessionLifecycle(world, new ScriptedMinigameRandom(65, 66));
        lifecycle.Start(
            FakeCraftSessionWorld.PlayerHandle,
            player,
            new CraftSessionStartRequest(RecipeType.TrapKit2, OBJECT_INVALID, Array.Empty<uint>()))
            .Succeeded.Should().BeTrue();

        var settlement = lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, player, CraftSessionEndReason.Broken);

        var baseXp = CraftMinigameXp.BaseXp(recipe, rank);
        settlement.Settled.Should().BeTrue();
        settlement.ItemsLost.Should().Be(1);
        settlement.ItemsKept.Should().Be(1);
        settlement.Xp.Should().Be(CraftOutcomeRules.CalculateFailureXp(baseXp, 0));
        world.XpGiven.Should().ContainSingle().Which.Skill.Should().Be(SkillType.Espionage);
    }

    [Test]
    public void TheEspionageBenchOpensTheCraftingWindowForEspionage()
    {
        var path = Path.Combine(FindRepositoryRoot(), "Module", "utp", "espionage_bench.utp.json");

        using var document = JsonDocument.Parse(File.ReadAllText(path));

        document.RootElement.GetProperty("OnUsed").GetProperty("value").GetString().Should().Be("craft_on_used");
        var skills = FindLocalInts(document.RootElement, "CRAFTING_SKILL_TYPE_ID").ToList();
        skills.Should().ContainSingle().Which.Should().Be((int)SkillType.Espionage).And.Be(49);
        CraftMinigameRouting.GetWindow(SkillType.Espionage).Should().NotBe(CraftMinigameRouting.LegacyWindow);
    }

    private static IEnumerable<int> FindLocalInts(JsonElement element, string name)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("Name", out var variableName) &&
                variableName.TryGetProperty("value", out var nameValue) &&
                nameValue.ValueKind == JsonValueKind.String &&
                nameValue.GetString() == name &&
                element.TryGetProperty("Value", out var variable) &&
                variable.TryGetProperty("value", out var intValue))
            {
                yield return intValue.GetInt32();
            }

            foreach (var property in element.EnumerateObject())
            {
                foreach (var found in FindLocalInts(property.Value, name))
                    yield return found;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var found in FindLocalInts(item, name))
                    yield return found;
            }
        }
    }

    /// <summary>A careful crafter: keep Stability up, build Subtlety first, and spend Potency only when it will not finish a rough piece.</summary>
    private static void Play(ReactionEngine engine)
    {
        for (var turn = 0; turn < 120 && engine.Status == MinigameStatus.InProgress; turn++)
        {
            var action = Choose(engine);
            engine.Perform(action).Performed.Should().BeTrue($"turn {turn}: {action}");
        }
    }

    private static string Choose(ReactionEngine engine)
    {
        var meters = engine.Meters;
        var qualityDone = meters.Quality.Percent >= 90;
        var best = ReactionEngine.Measure;
        var bestScore = double.MinValue;

        if (meters.Integrity.Value < 35 && engine.Preview(ReactionEngine.Stabilize).Enabled)
            return ReactionEngine.Stabilize;

        foreach (var action in new[] { ReactionEngine.Measure, ReactionEngine.Charge, ReactionEngine.Refine })
        {
            var preview = engine.Preview(action);
            if (!preview.Enabled || preview.CanBreakItem)
                continue;

            var outcome = preview.Outcomes.Single();
            var potencyRoom = meters.Progress.Max - meters.Progress.Value;
            var qualityRoom = meters.Quality.Max - meters.Quality.Value;
            var score =
                Math.Min(outcome.Delta.Progress, potencyRoom) / (double)meters.Progress.Max * (qualityDone ? 2.0 : 0.6) +
                Math.Min(outcome.Delta.Quality, qualityRoom) / (double)meters.Quality.Max * (qualityDone ? 0.1 : 1.6) +
                outcome.Delta.Integrity / (double)meters.Integrity.Max * 0.8;
            if (preview.FinishesItem && !qualityDone)
                score -= 5;

            if (score > bestScore)
            {
                bestScore = score;
                best = action;
            }
        }

        return best;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
