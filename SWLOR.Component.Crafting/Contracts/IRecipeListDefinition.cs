using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Crafting.Contracts
{
    public interface IRecipeListDefinition
    {
        public Dictionary<RecipeType, RecipeDetail> BuildRecipes();
    }
}
