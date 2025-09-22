using SWLOR.Component.Crafting.Enums;
using SWLOR.Component.Crafting.Model;

namespace SWLOR.Component.Crafting.Contracts
{
    public interface IRecipeListDefinition
    {
        public Dictionary<RecipeType, RecipeDetail> BuildRecipes();
    }
}
