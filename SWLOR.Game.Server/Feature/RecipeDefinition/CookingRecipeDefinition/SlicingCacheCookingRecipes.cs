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
            Add(RecipeType.QuietwatchJerky, "food_quietwatch", 5, "herb_v", "wild_meat", parameters: new KitchenParameters(dish: KitchenDish.Roast, targetSavory: 24, targetSweet: 5, targetSpice: 13, innerRadius: 8, outerRadius: 26, freshness: 72));
            Add(RecipeType.DustveilTravelCakes, "food_dustveil", 15, "herb_m", "raivor_meat", parameters: new KitchenParameters(dish: KitchenDish.Roast, targetSavory: 23, targetSweet: 0, targetSpice: 20, innerRadius: 8, outerRadius: 24, freshness: 69));
            Add(RecipeType.TombwalkerBroth, "food_tombwalk", 25, "herb_c", "byysk_meat", parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 35, targetSweet: 0, targetSpice: 17, innerRadius: 10, outerRadius: 27, freshness: 69));
            Add(RecipeType.SnowblindHuntersStew, "food_snowblind", 35, "herb_t", "sanddemon_meat", parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 24, targetSweet: 4, targetSpice: 24, innerRadius: 9, outerRadius: 29, freshness: 74));
            Add(RecipeType.NightMarchReserve, "food_nightmarch", 45, "herb_x", "wild_innards", parameters: new KitchenParameters(dish: KitchenDish.Roast, targetSavory: 19, targetSweet: 0, targetSpice: 29, innerRadius: 6, outerRadius: 20, freshness: 72));
            return _builder.Build();
        }

        private void Add(RecipeType type, string resref, int level, string herb, string huntedIngredient, RecipeMinigameParameters parameters = null)
        {
            _builder.Create(type, SkillType.Agriculture)
                .RequirementUnlocked()
                .Category(RecipeCategoryType.Food)
                .Resref(resref)
                .Level(level)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component(herb, 2)
                .Component(huntedIngredient, 2)
                .MinigameParameters(parameters);
        }
    }
}
