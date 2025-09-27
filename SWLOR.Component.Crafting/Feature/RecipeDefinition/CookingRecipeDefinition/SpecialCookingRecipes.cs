using SWLOR.Component.Crafting.Contracts;

using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Crafting.Enums;
using SWLOR.Shared.Domain.Crafting.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Crafting.Feature.RecipeDefinition.CookingRecipeDefinition
{
    public class SpecialCookingRecipes : IRecipeListDefinition
    {
                private readonly IRecipeBuilder _builder;

        public SpecialCookingRecipes(IRecipeBuilder builder)
        {
            _builder = builder;
        }

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
