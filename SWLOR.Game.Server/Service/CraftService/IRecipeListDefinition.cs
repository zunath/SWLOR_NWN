using SWLOR.Game.Server.Enumeration;
using System.Collections.Generic;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Game.Server.Service.CraftService
{
    public interface IRecipeListDefinition
    {
        public Dictionary<RecipeType, RecipeDetail> BuildRecipes();
    }
}
