using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;
using SWLOR.Game.Server.Tests.Service.RecipeParameters;

namespace SWLOR.Game.Server.Tests.Service.Fabricator;

/// <summary>Every active Fabrication recipe carries Fabricator parameters that match the generator and stay within the bounds and budget.</summary>
public class FabricatorParameterCoverageTests : RecipeParameterCoverageTests
{
    protected override IRecipeParameterProfile Profile => new FabricatorParameterProfile();
}
