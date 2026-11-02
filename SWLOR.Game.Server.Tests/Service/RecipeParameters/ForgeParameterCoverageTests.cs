using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;

namespace SWLOR.Game.Server.Tests.Service.RecipeParameters;

/// <summary>Checks every Smithery recipe against the Plasma Forge profile.</summary>
public class ForgeParameterCoverageTests : RecipeParameterCoverageTests
{
    protected override IRecipeParameterProfile Profile => new ForgeParameterProfile();
}
