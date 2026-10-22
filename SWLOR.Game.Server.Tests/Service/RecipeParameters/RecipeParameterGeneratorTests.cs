using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service.RecipeParameters;

public class RecipeParameterGeneratorTests
{
    private static RecipeParameterContext Context(
        RecipeType type,
        int level = 10,
        RecipeCategoryType category = RecipeCategoryType.Tunic,
        int components = 2)
    {
        var parts = new Dictionary<string, int>();
        for (var index = 0; index < components; index++)
        {
            parts["comp_" + index] = 1;
        }

        return new RecipeParameterContext(type, SkillType.Smithery, category, level, true, parts);
    }

    [Test]
    public void Generate_ReturnsTheSameValuesOnEveryCall()
    {
        var profile = new TestForgeProfile();
        var context = Context(RecipeType.SpiritmasterTunic, 17, components: 3);

        var first = RecipeParameterGenerator.Generate(profile, context);
        var second = RecipeParameterGenerator.Generate(profile, context);

        Assert.That(second.Values, Is.EqualTo(first.Values));
        Assert.That(second.Parameters.Values, Is.EqualTo(first.Parameters.Values));
        Assert.That(second.Attempt, Is.EqualTo(first.Attempt));
    }

    [Test]
    public void Generate_KeepsEveryValueInsideItsBoundsOnItsGridAndInsideTheBudget()
    {
        var profile = new TestForgeProfile(SkillType.Smithery, false);
        var types = Enum.GetValues<RecipeType>().Where(type => type != RecipeType.Invalid).Take(400).ToList();

        for (var index = 0; index < types.Count; index++)
        {
            var context = Context(types[index], 1 + index % 50, components: 1 + index % 8);
            var generated = RecipeParameterGenerator.Generate(profile, context);

            foreach (var definition in profile.Definitions)
            {
                var value = generated.Values[definition.Name];
                Assert.That(definition.IsInBounds(value) && definition.IsOnGrid(value), Is.True,
                    $"{types[index]} {definition.Name}={value}");
            }

            Assert.That(profile.BudgetFor(context.Level).Contains(generated.Difficulty), Is.True, types[index].ToString());
        }
    }

    [Test]
    public void Generate_VariesRecipesWithinTheDeclaredVariationOfTheBaseValue()
    {
        var profile = new TestForgeProfile(SkillType.Smithery, false);
        var centers = new HashSet<double>();

        foreach (var type in Enum.GetValues<RecipeType>().Where(type => type != RecipeType.Invalid).Take(100))
        {
            var context = Context(type, 12);
            var baseCenter = profile.BaseValues(context)["center"];
            var center = RecipeParameterGenerator.Generate(profile, context).Values["center"];

            Assert.That(center, Is.InRange(baseCenter - 6, baseCenter + 6));
            centers.Add(center);
        }

        Assert.That(centers.Count, Is.GreaterThan(8), "recipes of one level should not all share a value");
    }

    [Test]
    public void Generate_AppliesTheCategoryThenTheRecipeOverrideAndReportsBothReasons()
    {
        var profile = new TestForgeProfile();

        var starter = RecipeParameterGenerator.Generate(
            profile, Context(RecipeType.BattlemasterBreastplate, 7, RecipeCategoryType.Breastplate));
        Assert.That(starter.Values["center"], Is.EqualTo(65));
        Assert.That(starter.Values["cooling"], Is.EqualTo(1.0));
        Assert.That(starter.Values["width"], Is.EqualTo(30));
        Assert.That(starter.OverrideReason, Is.EqualTo("Breastplates are worked hot; Starter set stays forgiving"));

        var other = RecipeParameterGenerator.Generate(
            profile, Context(RecipeType.CombatTunic, 7, RecipeCategoryType.Breastplate));
        Assert.That(other.Values["center"], Is.EqualTo(65));
        Assert.That(other.OverrideReason, Is.EqualTo("Breastplates are worked hot"));

        var plain = RecipeParameterGenerator.Generate(profile, Context(RecipeType.AdventTunic, 7));
        Assert.That(plain.OverrideReason, Is.Empty);
    }

    [Test]
    public void Generate_DoesNotChangeOtherParametersWhenOneIsOverridden()
    {
        var plain = new TestForgeProfile(SkillType.Smithery, false);
        var overridden = new TestForgeProfile();
        var context = Context(RecipeType.BattlemasterBreastplate, 7, RecipeCategoryType.Breastplate);

        var before = RecipeParameterGenerator.Generate(plain, context);
        var after = RecipeParameterGenerator.Generate(overridden, context);

        Assert.That(after.Values["style"], Is.EqualTo(before.Values["style"]));
    }

    [Test]
    public void Generate_RedrawsVariationUntilTheDifficultyFitsTheBudget()
    {
        var profile = new ConfigurableTestProfile(
            _ => ConfigurableTestProfile.FlatBase(),
            parameters => parameters.Center,
            _ => new DifficultyBudget(48, 52));

        var attempts = new HashSet<int>();
        foreach (var type in Enum.GetValues<RecipeType>().Where(type => type != RecipeType.Invalid).Take(60))
        {
            var generated = RecipeParameterGenerator.Generate(profile, Context(type));
            attempts.Add(generated.Attempt);
            Assert.That(generated.Values["center"], Is.InRange(48, 52));
        }

        Assert.That(attempts.Any(attempt => attempt > 0), Is.True, "some recipes need a second draw");
    }

    [Test]
    public void Generate_FallsBackToTheBaseValuesWhenNoDrawFits()
    {
        var profile = new ConfigurableTestProfile(
            _ => ConfigurableTestProfile.FlatBase(),
            parameters => parameters.Center,
            _ => new DifficultyBudget(50, 50));
        var center = profile.Definitions.Single(definition => definition.Name == "center");

        var type = Enum.GetValues<RecipeType>().Where(candidate => candidate != RecipeType.Invalid).First(candidate =>
            Enumerable.Range(0, RecipeParameterGenerator.MaxAttempts).All(attempt =>
                center.Vary(50, RecipeParameterSeed.Unit(profile.Key, candidate.ToString(), "center", attempt)) != 50));

        var generated = RecipeParameterGenerator.Generate(profile, Context(type));

        Assert.That(generated.Attempt, Is.EqualTo(-1));
        Assert.That(generated.Values["center"], Is.EqualTo(50));
    }

    [Test]
    public void Generate_ThrowsWhenNothingFitsTheBudget()
    {
        var profile = new ConfigurableTestProfile(
            _ => ConfigurableTestProfile.FlatBase(),
            parameters => parameters.Center,
            _ => new DifficultyBudget(1000, 1001));

        var exception = Assert.Throws<InvalidOperationException>(
            () => RecipeParameterGenerator.Generate(profile, Context(RecipeType.AdventTunic)));

        Assert.That(exception!.Message, Does.Contain("AdventTunic").And.Contain("budget"));
    }

    [Test]
    public void Generate_ThrowsWhenABaseValueIsOutsideItsBounds()
    {
        var profile = new ConfigurableTestProfile(
            _ => ConfigurableTestProfile.FlatBase(150),
            parameters => 0,
            _ => new DifficultyBudget(0, 1));

        var exception = Assert.Throws<InvalidOperationException>(
            () => RecipeParameterGenerator.Generate(profile, Context(RecipeType.AdventTunic)));

        Assert.That(exception!.Message, Does.Contain("center"));
    }

    [TestCase(2.6)]
    [TestCase(0.4)]
    public void Generate_ThrowsWhenABaseEnumValueIsNotAMember(double style)
    {
        var profile = new ConfigurableTestProfile(
            _ =>
            {
                var values = ConfigurableTestProfile.FlatBase().ToDictionary(pair => pair.Key, pair => pair.Value);
                values["style"] = style;
                return values;
            },
            parameters => 0,
            _ => new DifficultyBudget(0, 1));

        var exception = Assert.Throws<InvalidOperationException>(
            () => RecipeParameterGenerator.Generate(profile, Context(RecipeType.AdventTunic)));

        Assert.That(exception!.Message, Does.Contain("style"));
    }

    [Test]
    public void Generate_ThrowsWhenABaseValueIsMissing()
    {
        var profile = new ConfigurableTestProfile(
            _ => new Dictionary<string, double> { ["center"] = 50 },
            parameters => 0,
            _ => new DifficultyBudget(0, 1));

        Assert.Throws<InvalidOperationException>(
            () => RecipeParameterGenerator.Generate(profile, Context(RecipeType.AdventTunic)));
    }

    [Test]
    public void Generate_ThrowsWhenAnOverrideIsOutsideItsBounds()
    {
        var profile = new ConfigurableTestProfile(
            _ => ConfigurableTestProfile.FlatBase(),
            parameters => 0,
            _ => new DifficultyBudget(0, 1),
            new RecipeParameterOverrides().ForRecipe(RecipeType.AdventTunic, "too hot", ("center", 150)));

        var exception = Assert.Throws<InvalidOperationException>(
            () => RecipeParameterGenerator.Generate(profile, Context(RecipeType.AdventTunic)));

        Assert.That(exception!.Message, Does.Contain("too hot"));
    }

    [Test]
    public void Generate_ThrowsForAProfileWhoseDefinitionsDoNotMatchItsParameterType()
    {
        var profile = new ConfigurableTestProfile(
            _ => ConfigurableTestProfile.FlatBase(),
            parameters => 0,
            _ => new DifficultyBudget(0, 1),
            definitions: new[] { RecipeParameterDefinition.Integer("middle", "Middle", 0, 10) });

        Assert.Throws<InvalidOperationException>(
            () => RecipeParameterGenerator.Generate(profile, Context(RecipeType.AdventTunic)));
    }
}
