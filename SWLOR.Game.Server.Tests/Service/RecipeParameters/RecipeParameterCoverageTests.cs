using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;

namespace SWLOR.Game.Server.Tests.Service.RecipeParameters;

/// <summary>
/// The coverage test every minigame instantiates. Derive from it, return the minigame's profile, and
/// the minigame's recipes are checked against the profile.
/// <code>
/// public class ForgeParameterCoverageTests : RecipeParameterCoverageTests
/// {
///     protected override IRecipeParameterProfile Profile => new ForgeParameterProfile();
/// }
/// </code>
/// </summary>
public abstract class RecipeParameterCoverageTests
{
    private static readonly Lazy<Dictionary<RecipeType, RecipeDetail>> _gameRecipes = new(RecipeDefinitionLoader.LoadGame);

    protected abstract IRecipeParameterProfile Profile { get; }

    /// <summary>The recipes under test; the live recipe definitions unless a test supplies its own.</summary>
    protected virtual IReadOnlyDictionary<RecipeType, RecipeDetail> Recipes => _gameRecipes.Value;

    [Test]
    public void Profile_IsWellFormed()
    {
        Assert.That(RecipeParameterAudit.ValidateProfile(Profile), Is.Empty);
    }

    [Test]
    public void Profile_CoversAtLeastOneRecipe()
    {
        var covered = Recipes.Count(pair => Profile.AppliesTo(RecipeParameterContext.From(pair.Key, pair.Value)) &&
                                            pair.Value.Skill == Profile.Skill);

        Assert.That(covered, Is.GreaterThan(0), $"{Profile.Key} matches no recipe of {Profile.Skill}");
    }

    [Test]
    public void EveryActiveRecipeOfTheSkill_HasParameters()
    {
        AssertNoProblems(RecipeParameterAudit.FindMissingParameters(Profile, Recipes),
            "every active recipe of the skill needs parameters; run the generator");
    }

    [Test]
    public void Values_StayWithinTheProfileBounds()
    {
        AssertNoProblems(RecipeParameterAudit.FindOutOfBounds(Profile, Recipes),
            "values must sit inside the bounds the profile declares");
    }

    [Test]
    public void Values_MatchTheGeneratorUnlessAnOverrideRecordsThem()
    {
        AssertNoProblems(RecipeParameterAudit.FindGeneratorDrift(Profile, Recipes),
            "values must equal the generator's output, which includes the profile's recorded overrides, or the source recipe's values for a derived recipe");
    }

    [Test]
    public void Difficulty_StaysWithinTheBudgetOfTheRecipeLevel()
    {
        AssertNoProblems(RecipeParameterAudit.FindOutOfBudget(Profile, Recipes),
            "difficulty must stay inside the profile's budget for the recipe level");
    }

    [Test]
    public void DerivedRecipes_NameCoveredSources()
    {
        AssertNoProblems(RecipeParameterAudit.FindInvalidDerivations(Profile, Recipes),
            "a derived recipe and its source must both be covered by the profile");
    }

    [Test]
    public void RecordedOverrides_AreValid()
    {
        AssertNoProblems(RecipeParameterAudit.FindInvalidOverrides(Profile, Recipes),
            "every override needs declared values inside the bounds and a recipe to apply to");
    }

    private static void AssertNoProblems(IReadOnlyList<string> problems, string because)
    {
        Assert.That(problems, Is.Empty, $"{because}. Problems:\n{string.Join("\n", problems)}");
    }
}
