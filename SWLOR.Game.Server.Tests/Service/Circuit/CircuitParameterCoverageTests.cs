using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;
using SWLOR.Game.Server.Tests.Service.RecipeParameters;

namespace SWLOR.Game.Server.Tests.Service.Circuit;

/// <summary>Every active Engineering recipe carries Circuit parameters that match the generator and stay within the bounds and budget.</summary>
public class CircuitParameterCoverageTests : RecipeParameterCoverageTests
{
    protected override IRecipeParameterProfile Profile => new CircuitParameterProfile();
}
