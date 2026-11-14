using SWLOR.Game.Server.Domain.Crafting.RecipeParameters;

namespace SWLOR.Game.Server.Tests.Service.RecipeParameters;

/// <summary>Checks every Espionage recipe against the Reaction profile.</summary>
public class ReactionParameterCoverageTests : RecipeParameterCoverageTests
{
    protected override IRecipeParameterProfile Profile => new ReactionParameterProfile();
}
