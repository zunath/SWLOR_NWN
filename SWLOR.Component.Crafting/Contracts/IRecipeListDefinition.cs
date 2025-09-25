using SWLOR.Shared.Domain.Crafting.Enums;
using SWLOR.Shared.Domain.Crafting.ValueObjects;

namespace SWLOR.Component.Crafting.Contracts
{
    public interface IRecipeListDefinition
    {
        public Dictionary<RecipeType, RecipeDetail> BuildRecipes();
    }
}
