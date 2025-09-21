using SWLOR.Game.Server.Enumeration;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService
{
    public interface IRecipeListDefinition
    {
        public Dictionary<RecipeType, RecipeDetail> BuildRecipes();
    }
}
