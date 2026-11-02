using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;

namespace SWLOR.Game.Server.Tests.Service.RecipeParameters;

/// <summary>
/// Runs the coverage framework against the test-only profile and a registry built from its generator.
/// </summary>
public class TestForgeCoverageTests : RecipeParameterCoverageTests
{
    private readonly TestForgeProfile _profile = new();
    private IReadOnlyDictionary<RecipeType, RecipeDetail> _recipes;

    protected override IRecipeParameterProfile Profile => _profile;

    protected override IReadOnlyDictionary<RecipeType, RecipeDetail> Recipes =>
        _recipes ??= TestRecipeRegistry.Create(_profile);
}
