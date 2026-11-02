using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;

namespace SWLOR.Game.Server.Tests.Service.RecipeParameters;

public class RecipeParameterAuditTests
{
    private static readonly TestForgeProfile _profile = new();

    private static ConfigurableTestProfile Configurable(
        RecipeParameterOverrides overrides = null,
        IReadOnlyList<RecipeParameterDefinition> definitions = null)
    {
        return new ConfigurableTestProfile(
            _ => ConfigurableTestProfile.FlatBase(),
            parameters => parameters.Center,
            _ => new DifficultyBudget(0, 100),
            overrides,
            definitions);
    }

    [Test]
    public void AGeneratedRegistry_PassesEveryCheck()
    {
        var recipes = TestRecipeRegistry.Create(_profile);

        Assert.That(RecipeParameterAudit.ValidateProfile(_profile), Is.Empty);
        Assert.That(RecipeParameterAudit.FindMissingParameters(_profile, recipes), Is.Empty);
        Assert.That(RecipeParameterAudit.FindOutOfBounds(_profile, recipes), Is.Empty);
        Assert.That(RecipeParameterAudit.FindGeneratorDrift(_profile, recipes), Is.Empty);
        Assert.That(RecipeParameterAudit.FindOutOfBudget(_profile, recipes), Is.Empty);
        Assert.That(RecipeParameterAudit.FindInvalidOverrides(_profile, recipes), Is.Empty);
    }

    [Test]
    public void FindMissingParameters_NamesRecipesWithoutParametersAndInactiveOnesThatKeepThem()
    {
        var recipes = TestRecipeRegistry.Create(_profile);
        recipes[RecipeType.AdventTunic].MinigameParameters = null;
        recipes[TestRecipeRegistry.InactiveRecipe].MinigameParameters = new TestForgeParameters(50, 20, 1.0, TestForgeStyle.Cold);

        var problems = RecipeParameterAudit.FindMissingParameters(_profile, recipes);

        Assert.That(problems, Has.Count.EqualTo(2));
        Assert.That(problems, Has.Some.Contain("AdventTunic has no TestForgeParameters"));
        Assert.That(problems, Has.Some.Contain($"{TestRecipeRegistry.InactiveRecipe} is not covered"));
    }

    [Test]
    public void FindMissingParameters_IgnoresRecipesOfOtherSkills()
    {
        var recipes = TestRecipeRegistry.Create(_profile);

        Assert.That(recipes[TestRecipeRegistry.OtherSkillRecipe].MinigameParameters, Is.Null);
        Assert.That(RecipeParameterAudit.FindMissingParameters(_profile, recipes), Is.Empty);
    }

    [Test]
    public void FindOutOfBounds_NamesTheValueAndItsBounds()
    {
        var recipes = TestRecipeRegistry.Create(_profile);
        recipes[RecipeType.AdventTunic].MinigameParameters = new TestForgeParameters(99, 20, 1.0, TestForgeStyle.Cold);

        var problems = RecipeParameterAudit.FindOutOfBounds(_profile, recipes);

        Assert.That(problems, Has.Count.EqualTo(1));
        Assert.That(problems[0], Does.Contain("AdventTunic").And.Contain("center=99").And.Contain("[20, 80]"));
    }

    [Test]
    public void FindGeneratorDrift_FlagsAHandEditedValue()
    {
        var recipes = TestRecipeRegistry.Create(_profile);
        var generated = (TestForgeParameters)recipes[RecipeType.AdventTunic].MinigameParameters;
        recipes[RecipeType.AdventTunic].MinigameParameters = new TestForgeParameters(
            generated.Center == 50 ? 51 : 50, generated.Width, generated.Cooling, generated.Style);

        var drift = RecipeParameterAudit.FindGeneratorDrift(_profile, recipes);

        Assert.That(drift, Has.Count.EqualTo(1));
        Assert.That(drift[0], Does.Contain("AdventTunic: center is").And.Contain("record an override"));
    }

    [Test]
    public void FindGeneratorDrift_AcceptsTheValuesOfARecordedOverrideAndFlagsAnyOtherValue()
    {
        var profile = Configurable(new RecipeParameterOverrides().ForRecipe(RecipeType.AdventTunic, "hand tuned", ("center", 45)));
        var recipes = TestRecipeRegistry.Create(profile);
        var tuned = (TestForgeParameters)recipes[RecipeType.AdventTunic].MinigameParameters;

        Assert.That(tuned.Center, Is.EqualTo(45));
        Assert.That(RecipeParameterAudit.FindGeneratorDrift(profile, recipes), Is.Empty);

        recipes[RecipeType.AdventTunic].MinigameParameters = new TestForgeParameters(46, tuned.Width, tuned.Cooling, tuned.Style);

        Assert.That(RecipeParameterAudit.FindGeneratorDrift(profile, recipes), Has.Count.EqualTo(1));
    }

    [Test]
    public void DerivedRecipes_MustCopyTheirSourcesParametersAndNeedNoGeneratorOutput()
    {
        var derived = new Dictionary<RecipeType, RecipeType> { [RecipeType.SpiritmasterTunic] = RecipeType.BattlemasterBreastplate };
        var profile = new TestForgeProfile(_profile.Skill, false, derived);
        var recipes = TestRecipeRegistry.Create(profile);

        Assert.That(recipes[RecipeType.SpiritmasterTunic].MinigameParameters,
            Is.SameAs(recipes[RecipeType.BattlemasterBreastplate].MinigameParameters));
        Assert.That(RecipeParameterAudit.FindGeneratorDrift(profile, recipes), Is.Empty);
        Assert.That(RecipeParameterAudit.FindMissingParameters(profile, recipes), Is.Empty);
        Assert.That(RecipeParameterAudit.FindInvalidDerivations(profile, recipes), Is.Empty);

        var source = (TestForgeParameters)recipes[RecipeType.BattlemasterBreastplate].MinigameParameters;
        recipes[RecipeType.SpiritmasterTunic].MinigameParameters =
            new TestForgeParameters(source.Center == 50 ? 51 : 50, source.Width, source.Cooling, source.Style);

        var drift = RecipeParameterAudit.FindGeneratorDrift(profile, recipes);
        Assert.That(drift, Has.Count.EqualTo(1));
        Assert.That(drift[0], Does.Contain("SpiritmasterTunic: center is").And.Contain("copy BattlemasterBreastplate"));
    }

    [Test]
    public void FindInvalidDerivations_FlagsUncoveredAndChainedSources()
    {
        var derived = new Dictionary<RecipeType, RecipeType>
        {
            [RecipeType.SpiritmasterTunic] = TestRecipeRegistry.OtherSkillRecipe,
            [TestRecipeRegistry.InactiveRecipe] = RecipeType.BattlemasterBreastplate,
            [RecipeType.AdventTunic] = RecipeType.SpiritmasterTunic,
        };
        var profile = new TestForgeProfile(_profile.Skill, false, derived);
        var recipes = TestRecipeRegistry.Create(_profile);

        var problems = RecipeParameterAudit.FindInvalidDerivations(profile, recipes);

        Assert.That(problems, Has.Some.Contain($"SpiritmasterTunic is derived from {TestRecipeRegistry.OtherSkillRecipe}, which the profile does not cover"));
        Assert.That(problems, Has.Some.Contain($"{TestRecipeRegistry.InactiveRecipe} is derived from BattlemasterBreastplate but the profile does not cover it"));
        Assert.That(problems, Has.Some.Contain("AdventTunic is derived from SpiritmasterTunic, which is itself derived"));
    }

    [Test]
    public void FindInvalidOverrides_FlagsOverridesThatOnlyReachDerivedRecipes()
    {
        var recipes = TestRecipeRegistry.Create(_profile);
        recipes[RecipeType.CombatBoots] = new RecipeDetail { Skill = _profile.Skill, Category = RecipeCategoryType.Boots, Level = 5 };
        var derived = new Dictionary<RecipeType, RecipeType>
        {
            [RecipeType.SpiritmasterTunic] = RecipeType.BattlemasterBreastplate,
            [RecipeType.CombatBoots] = RecipeType.BattlemasterBreastplate,
        };
        var overrides = new RecipeParameterOverrides()
            .ForCategory(RecipeCategoryType.Boots, "boots only", ("center", 40))
            .ForCategory(RecipeCategoryType.Breastplate, "breastplates", ("center", 40))
            .ForRecipe(RecipeType.SpiritmasterTunic, "ignored", ("center", 40));
        var profile = new ConfigurableTestProfile(
            _ => ConfigurableTestProfile.FlatBase(), parameters => 0, _ => new DifficultyBudget(0, 1), overrides, derived: derived);

        var problems = RecipeParameterAudit.FindInvalidOverrides(profile, recipes);

        Assert.That(problems, Is.EquivalentTo(new[]
        {
            "category Boots override only reaches derived recipes, which copy their source's values",
            "recipe SpiritmasterTunic is derived, so its own override has no effect; override BattlemasterBreastplate instead",
        }));
    }

    [Test]
    public void FindOutOfBudget_FlagsParametersTooHardForTheirLevel()
    {
        var recipes = TestRecipeRegistry.Create(_profile);
        recipes[RecipeType.SpiritmasterTunic].MinigameParameters = new TestForgeParameters(50, 8, 3.0, TestForgeStyle.Cold);

        var problems = RecipeParameterAudit.FindOutOfBudget(_profile, recipes);

        Assert.That(problems, Has.Count.EqualTo(1));
        Assert.That(problems[0], Does.Contain("SpiritmasterTunic").And.Contain("budget"));
    }

    [Test]
    public void FindInvalidOverrides_FlagsUndeclaredOutOfBoundsAndOrphanedOverrides()
    {
        var recipes = TestRecipeRegistry.Create(_profile);
        var overrides = new RecipeParameterOverrides()
            .ForCategory(RecipeCategoryType.Boots, "no boots here", ("center", 40))
            .ForRecipe(RecipeType.AdventTunic, "undeclared", ("heat", 5))
            .ForRecipe(RecipeType.SpiritmasterTunic, "too wide", ("width", 500))
            .ForRecipe(TestRecipeRegistry.OtherSkillRecipe, "other skill", ("width", 10))
            .ForRecipe(TestRecipeRegistry.InactiveRecipe, "inactive", ("width", 10));
        var profile = new ConfigurableTestProfile(
            _ => ConfigurableTestProfile.FlatBase(), parameters => 0, _ => new DifficultyBudget(0, 1), overrides);

        var problems = RecipeParameterAudit.FindInvalidOverrides(profile, recipes);

        Assert.That(problems, Has.Some.Contain("category Boots override matches no recipe"));
        Assert.That(problems, Has.Some.Contain("'heat' is not a declared parameter"));
        Assert.That(problems, Has.Some.Contain("width=500 is outside"));
        Assert.That(problems, Has.Some.Contain($"recipe {TestRecipeRegistry.OtherSkillRecipe} override matches no recipe"));
        Assert.That(problems, Has.Some.Contain($"recipe {TestRecipeRegistry.InactiveRecipe} override matches no recipe"));
        Assert.That(problems, Has.Count.EqualTo(5));
    }

    [Test]
    public void ValidateProfile_FlagsDuplicateNamesDuplicateHeadersAndAMismatchedConstructor()
    {
        var duplicated = Configurable(definitions: new[]
        {
            RecipeParameterDefinition.Integer("center", "Same Header", 0, 10),
            RecipeParameterDefinition.Integer("center", "Same header", 0, 10),
        });

        var problems = RecipeParameterAudit.ValidateProfile(duplicated);

        Assert.That(problems, Has.Some.Contain("parameter name 'center' is declared twice"));
        Assert.That(problems, Has.Some.Contain("Bible header 'Same Header' is used twice"));
        Assert.That(problems, Has.Some.Contain("TestForgeParameters needs a public constructor taking exactly (center, center)"));
    }

    [Test]
    public void ValidateProfile_FlagsHeadersThatDifferOnlyInWhitespace()
    {
        var definitions = new[]
        {
            RecipeParameterDefinition.Integer("center", "Configurable Heat", 0, 10),
            RecipeParameterDefinition.Integer("width", "Configurable  Heat", 0, 10),
            RecipeParameterDefinition.Decimal("cooling", "Configurable Cooling", 0.0, 10.0, 1),
            RecipeParameterDefinition.Enumeration<TestForgeStyle>("style", "Configurable Style"),
        };

        var problems = RecipeParameterAudit.ValidateProfile(Configurable(definitions: definitions));

        Assert.That(problems, Is.EqualTo(new[] { "Bible header 'Configurable Heat' is used twice" }));
    }

    [Test]
    public void ValidateProfile_FlagsABibleHeaderThatDoesNotStartWithTheProfileKey()
    {
        var definitions = new[]
        {
            RecipeParameterDefinition.Integer("center", "Skill Level", 0, 10),
            RecipeParameterDefinition.Integer("width", "Configurable Width", 0, 10),
            RecipeParameterDefinition.Decimal("cooling", "Configurable Cooling", 0.0, 10.0, 1),
            RecipeParameterDefinition.Enumeration<TestForgeStyle>("style", "Configurable Style"),
        };

        var problems = RecipeParameterAudit.ValidateProfile(Configurable(definitions: definitions));

        Assert.That(problems, Is.EqualTo(new[] { "Bible header 'Skill Level' must start with 'Configurable '" }));
    }

    [Test]
    public void ValidateProfile_FlagsAHeaderThatCollidesWithTheOverrideColumn()
    {
        var definitions = new[]
        {
            RecipeParameterDefinition.Integer("center", "Configurable Override", 0, 10),
            RecipeParameterDefinition.Integer("width", "Width", 0, 10),
            RecipeParameterDefinition.Decimal("cooling", "Cooling", 0.0, 10.0, 1),
            RecipeParameterDefinition.Enumeration<TestForgeStyle>("style", "Style"),
        };

        var problems = RecipeParameterAudit.ValidateProfile(Configurable(definitions: definitions));

        Assert.That(problems, Has.Some.Contain("Bible header 'Configurable Override' is used twice"));
    }

    [Test]
    public void ProfileKeys_MustBeUniqueWithoutRegardToCase()
    {
        ConfigurableTestProfile Keyed(string key) => new(
            _ => ConfigurableTestProfile.FlatBase(), parameters => 0, _ => new DifficultyBudget(0, 1), key: key);

        Assert.DoesNotThrow(() => RecipeParameterProfiles.EnsureUniqueKeys(new[] { Keyed("Forge"), Keyed("Circuit") }));
        var exception = Assert.Throws<InvalidOperationException>(
            () => RecipeParameterProfiles.EnsureUniqueKeys(new[] { Keyed("Forge"), Keyed("forge") }));
        Assert.That(exception!.Message, Does.Contain("'Forge'").And.Contain("without regard to case"));
    }

    [Test]
    public void GameProfiles_AreAllWellFormed()
    {
        foreach (var profile in RecipeParameterProfiles.Game)
        {
            Assert.That(RecipeParameterAudit.ValidateProfile(profile), Is.Empty, profile.Key);
        }
    }
}
