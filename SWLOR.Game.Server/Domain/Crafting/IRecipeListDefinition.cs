using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Crafting
{
    public interface IRecipeListDefinition
    {
        public Dictionary<RecipeType, RecipeDetail> BuildRecipes();
    }
}
