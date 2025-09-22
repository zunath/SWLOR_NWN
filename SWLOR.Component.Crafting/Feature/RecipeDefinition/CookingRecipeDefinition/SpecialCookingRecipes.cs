using SWLOR.Component.Crafting.Contracts;
using SWLOR.Component.Crafting.Enums;
using SWLOR.Component.Crafting.Model;
using SWLOR.Component.Crafting.Service;

namespace SWLOR.Component.Crafting.Feature.RecipeDefinition.CookingRecipeDefinition
{
    public class SpecialCookingRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            SpecialRecipes();

            return _builder.Build();
        }

        private void SpecialRecipes()
        {
            // Food Submission Token (Agriculture)
            _builder.Create(RecipeType.FoodSubmissionTokenAgriculture, SkillType.Agriculture)
                .Category(RecipeCategoryType.SpecialSubmissionItems)
                .Resref("food_sub_token")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 5)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .ResearchCostModifier(0.2f)
                .Component("chiro_shard", 5);
        }
    }
}
