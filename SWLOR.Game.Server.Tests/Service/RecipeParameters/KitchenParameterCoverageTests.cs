using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;

namespace SWLOR.Game.Server.Tests.Service.RecipeParameters;

/// <summary>Every active Agriculture recipe carries the Kitchen parameters the generator gives it, inside the profile's bounds and difficulty budget.</summary>
public class KitchenParameterCoverageTests : RecipeParameterCoverageTests
{
    protected override IRecipeParameterProfile Profile => new KitchenParameterProfile();
}
