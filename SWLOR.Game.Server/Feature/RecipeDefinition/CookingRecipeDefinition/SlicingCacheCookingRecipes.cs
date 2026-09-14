using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition
{
    public class SlicingCacheCookingRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Add(RecipeType.QuietwatchJerky, "food_quietwatch", 5, "herb_v", "wild_meat");
            Add(RecipeType.DustveilTravelCakes, "food_dustveil", 15, "herb_m", "raivor_meat");
            Add(RecipeType.TombwalkerBroth, "food_tombwalk", 25, "herb_c", "byysk_meat");
            Add(RecipeType.SnowblindHuntersStew, "food_snowblind", 35, "herb_t", "sanddemon_meat");
            Add(RecipeType.NightMarchReserve, "food_nightmarch", 45, "herb_x", "wild_innards");
            return _builder.Build();
        }

        private void Add(RecipeType type, string resref, int level, string herb, string huntedIngredient)
        {
            _builder.Create(type, SkillType.Agriculture)
                .RequirementUnlocked()
                .Category(RecipeCategoryType.Food)
                .Resref(resref)
                .Level(level)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component(herb, 2)
                .Component(huntedIngredient, 2);
        }
    }
}
