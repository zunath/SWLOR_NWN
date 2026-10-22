using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service.Kitchen;

/// <summary>
/// Covers what the recipe definitions give the Kitchen: a flavor row for every component, a dish for every recipe, and targets
/// a cook can actually reach.
/// </summary>
public class KitchenRecipeParameterTests
{
    private static readonly Lazy<Dictionary<RecipeType, RecipeDetail>> Recipes = new(RecipeDefinitionLoader.LoadGame);

    private static IEnumerable<KeyValuePair<RecipeType, RecipeDetail>> CookingRecipes() =>
        Recipes.Value
            .Where(pair => pair.Value.Skill == SkillType.Agriculture && pair.Value.IsActive)
            .OrderBy(pair => pair.Key.ToString(), StringComparer.Ordinal);

    private static KitchenParameters ParametersOf(RecipeDetail recipe) => (KitchenParameters)recipe.MinigameParameters;

    [Test]
    public void EveryComponentOfACookingRecipe_HasARowInTheComponentTable()
    {
        var missing = CookingRecipes()
            .SelectMany(pair => pair.Value.Components.Keys.Select(resref => (Recipe: pair.Key, Resref: resref)))
            .Where(use => !KitchenComponentTable.Contains(use.Resref))
            .Select(use => $"{use.Recipe}: {use.Resref}")
            .ToList();

        missing.Should().BeEmpty("every ingredient needs a flavor vector in KitchenComponentTable");
    }

    [Test]
    public void TheComponentTable_HoldsNoRowThatNoCookingRecipeUses()
    {
        var used = CookingRecipes().SelectMany(pair => pair.Value.Components.Keys).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unused = KitchenComponentTable.Resrefs.Where(resref => !used.Contains(resref)).ToList();

        unused.Should().BeEmpty("a row for a component no recipe commits is stale");
    }

    [Test]
    public void EveryFlavorRow_PushesSomethingGentleAndWaterIsTheOnlyThinner()
    {
        foreach (var resref in KitchenComponentTable.Resrefs)
        {
            var flavor = KitchenComponentTable.Get(resref);
            if (flavor.Kind == KitchenIngredientKind.Dilute)
            {
                resref.Should().Be(KitchenComponentTable.Water);
                flavor.Vector.IsZero.Should().BeTrue();
                continue;
            }

            flavor.Vector.Length.Should().BeInRange(5, 20, resref);
            new[] { flavor.Vector.Savory, flavor.Vector.Sweet, flavor.Vector.Spice }
                .Should().OnlyContain(axis => axis >= 0 && axis <= 15, resref);
        }
    }

    [Test]
    public void ThereAreFourDishesAndEveryCategoryOfRecipeMapsToOne()
    {
        var recipes = CookingRecipes().ToList();

        recipes.Where(pair => pair.Value.Category == RecipeCategoryType.PetFood)
            .Should().OnlyContain(pair => ParametersOf(pair.Value).Dish == KitchenDish.PetFood);
        recipes.Where(pair => pair.Value.Category == RecipeCategoryType.CookingEnhancement)
            .Should().OnlyContain(pair => ParametersOf(pair.Value).Dish == KitchenDish.Soup);
        recipes.Select(pair => ParametersOf(pair.Value).Dish).Distinct()
            .Should().BeEquivalentTo(Enum.GetValues<KitchenDish>());
    }

    [TestCase(RecipeType.MynockBroth, KitchenDish.Soup)]
    [TestCase(RecipeType.PebbleSoup, KitchenDish.Soup)]
    [TestCase(RecipeType.OrangeCurry, KitchenDish.Soup)]
    [TestCase(RecipeType.SoyRamen, KitchenDish.Soup)]
    [TestCase(RecipeType.MynockMeatBall, KitchenDish.Roast)]
    [TestCase(RecipeType.RoastCarp, KitchenDish.Roast)]
    [TestCase(RecipeType.UrchinSushi, KitchenDish.Roast)]
    [TestCase(RecipeType.SugarCookies, KitchenDish.Sweet)]
    [TestCase(RecipeType.OrangeJuice, KitchenDish.Sweet)]
    [TestCase(RecipeType.AppleAuLait, KitchenDish.Sweet)]
    [TestCase(RecipeType.DantooineCarrotCake, KitchenDish.Sweet)]
    [TestCase(RecipeType.DustveilTravelCakes, KitchenDish.Roast)]
    [TestCase(RecipeType.CookingEnhancementHP1, KitchenDish.Soup)]
    [TestCase(RecipeType.BlubberyFishSubstitute1, KitchenDish.PetFood)]
    public void TheDishFollowsTheRecipesCategoryAndName(RecipeType type, KitchenDish dish)
    {
        ParametersOf(Recipes.Value[type]).Dish.Should().Be(dish);
    }

    [Test]
    public void TheCommittedIngredientsOfEveryRecipe_AddUpToTheTargetsDirectionAndMostOfItsSize()
    {
        var problems = new List<string>();
        foreach (var (type, recipe) in CookingRecipes())
        {
            var parameters = ParametersOf(recipe);
            var center = new FlavorVector(parameters.TargetSavory, parameters.TargetSweet, parameters.TargetSpice);
            var committed = KitchenParameterProfile.CommittedFlavor(recipe.Components).Clamped(KitchenTuning.AxisMax);

            // The pantry makes up a shortfall a pinch at a time, so the committed ingredients should get within 3 pinches of
            // the center once the pot is built (a recipe may also overshoot and thin it with water).
            var shortfall = Math.Max(0, center.DistanceTo(committed) - parameters.OuterRadius);
            if (shortfall > KitchenTuning.PantryPush * 6)
                problems.Add($"{type}: the committed ingredients end {committed.DistanceTo(center):F0} from the center");
        }

        problems.Should().BeEmpty();
    }

    [Test]
    public void EveryRecipe_StartsWithAnEmptyPotThatEarnsNoQuality()
    {
        var problems = new List<string>();
        foreach (var (type, recipe) in CookingRecipes())
        {
            var kitchen = KitchenRecipe.From(ParametersOf(recipe), recipe.Level, recipe.Components);
            if (kitchen.Target.QualityPercent(FlavorVector.Zero) != 0)
                problems.Add($"{type}: an empty pot scores {kitchen.Target.QualityPercent(FlavorVector.Zero)}%");
        }

        problems.Should().BeEmpty("a cook who adds nothing must not earn quality");
    }

    [Test]
    public void EveryRecipe_HasEnoughFreshnessForAnUnhurriedCookToFinishIt()
    {
        var problems = new List<string>();
        foreach (var (type, recipe) in CookingRecipes())
        {
            var parameters = ParametersOf(recipe);
            var center = new FlavorVector(parameters.TargetSavory, parameters.TargetSweet, parameters.TargetSpice);
            var committed = KitchenParameterProfile.CommittedFlavor(recipe.Components);
            var pantryTurns = (int)Math.Ceiling(Math.Max(0, center.Length - committed.Length) / KitchenTuning.PantryPush);
            var simmerTurns = (int)Math.Ceiling(KitchenTuning.DonenessMax / (double)KitchenTuning.SimmerMediumDoneness);
            var turns = KitchenParameterProfile.Portions(recipe.Components) + pantryTurns + simmerTurns;

            if (turns * KitchenTuning.FreshnessDecayPerTurn > parameters.Freshness)
                problems.Add($"{type}: {turns} turns cost {turns * KitchenTuning.FreshnessDecayPerTurn} Freshness of {parameters.Freshness}");
        }

        problems.Should().BeEmpty();
    }

    [Test]
    public void APlainCookWithMidTierGearCanFinishEveryRecipeAtTheirOwnLevel()
    {
        var problems = new List<string>();
        var runs = 0;
        var finished = 0;
        var qualities = new List<int>();
        foreach (var (type, recipe) in CookingRecipes())
        {
            var kitchen = KitchenRecipe.From(ParametersOf(recipe), recipe.Level, recipe.Components);
            var recipeFinished = 0;
            for (var seed = 1; seed <= 5; seed++)
            {
                var stats = new KitchenStats(Math.Min(recipe.Level, 50), 12, 13, 20);
                var engine = new KitchenEngine(new SeededMinigameRandom(seed * 7919 + recipe.Level), kitchen, stats);
                KitchenSimpleCook.Cook(engine);
                runs++;
                if (engine.Status != MinigameStatus.Succeeded)
                    continue;

                finished++;
                recipeFinished++;
                qualities.Add(engine.Meters.QualityPercent);
            }

            if (recipeFinished == 0)
                problems.Add($"{type} was never finished in 5 tries");
        }

        problems.Should().BeEmpty();
        (finished / (double)runs).Should().BeGreaterThan(0.9, "a plain cook should usually get a dish done");
        qualities.Average().Should().BeGreaterThan(65, "and usually get it somewhere near the target");
    }

    [Test]
    public void TheKitchenBuildsFromEveryCookingRecipe()
    {
        foreach (var (type, recipe) in CookingRecipes())
        {
            var kitchen = KitchenRecipe.From(ParametersOf(recipe), recipe.Level, recipe.Components);

            kitchen.Lines.Should().HaveCount(recipe.Components.Count, type.ToString());
            kitchen.Lines.Should().OnlyContain(line => line.Portions >= 1 && line.Portions <= KitchenTuning.MaxPortionsPerLine);
            kitchen.Freshness.Should().Be(ParametersOf(recipe).Freshness);
            new KitchenEngine(new FixedRolls(100), kitchen, new KitchenStats(recipe.Level, 0, 0, 0))
                .Actions.Should().NotBeEmpty();
        }
    }

    [Test]
    public void TheProfileKeepsTheBibleHeadersTheParityTestsExpect()
    {
        var profile = new KitchenParameterProfile();

        RecipeParameterBibleColumns.Headers(profile).Should().Equal(
            "Kitchen Dish",
            "Kitchen Target Savory",
            "Kitchen Target Sweet",
            "Kitchen Target Spice",
            "Kitchen Inner Radius",
            "Kitchen Outer Radius",
            "Kitchen Freshness",
            "Kitchen Override");
    }
}
