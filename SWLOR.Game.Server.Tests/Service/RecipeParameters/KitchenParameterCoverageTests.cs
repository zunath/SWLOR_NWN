using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Domain.Crafting.RecipeParameters;

namespace SWLOR.Game.Server.Tests.Service.RecipeParameters;

/// <summary>Every active Agriculture recipe carries the Kitchen parameters the generator gives it, inside the profile's bounds and difficulty budget.</summary>
public class KitchenParameterCoverageTests : RecipeParameterCoverageTests
{
    protected override IRecipeParameterProfile Profile => new KitchenParameterProfile();
}
