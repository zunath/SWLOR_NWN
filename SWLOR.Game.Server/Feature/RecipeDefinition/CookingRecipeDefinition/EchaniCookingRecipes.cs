using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition
{
    public class EchaniCookingRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            // Echani Steamed Dumplings
            _builder.Create(RecipeType.EchaniSteamedDumplings, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("esh_dumplings")
                .Level(44)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("esh_wolf_meat", 3)
                .Component("esh_greens", 2)
                .Component("bread_flour", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 44, targetSweet: 16, targetSpice: 11, innerRadius: 7, outerRadius: 18, freshness: 102);

            // Echani Hand-Pulled Broth Noodles
            _builder.Create(RecipeType.EchaniBrothNoodles, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("esh_broth_noodle")
                .Level(44)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("bread_flour", 4)
                .Component("esh_greens", 3)
                .Component("ginger", 1)
                .Component("distilled_water", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 18, targetSweet: 21, targetSpice: 16, innerRadius: 6, outerRadius: 28, freshness: 104);

            // Echani Chili-Oil Noodles
            _builder.Create(RecipeType.EchaniChiliNoodles, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("esh_chili_noodle")
                .Level(46)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("bread_flour", 4)
                .Component("esh_chili_oil", 2)
                .Component("esh_scallion", 1)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 19, targetSweet: 10, targetSpice: 28, innerRadius: 7, outerRadius: 25, freshness: 101);

            // Echani Stuffed Steamed Buns
            _builder.Create(RecipeType.EchaniSteamedBuns, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("esh_steam_bun")
                .Level(44)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("esh_wolf_meat", 3)
                .Component("bread_flour", 3)
                .Component("esh_scallion", 1)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 39, targetSweet: 11, targetSpice: 8, innerRadius: 4, outerRadius: 22, freshness: 97);

            // Echani Scallion Flatbread
            _builder.Create(RecipeType.EchaniScallionFlatbread, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("esh_flatbread")
                .Level(45)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("bread_flour", 4)
                .Component("esh_scallion", 3)
                .Component("cultured_butter", 1)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 28, targetSweet: 19, targetSpice: 13, innerRadius: 4, outerRadius: 21, freshness: 102);

            // Echani Shared Hotpot Portion
            _builder.Create(RecipeType.EchaniSharedHotpot, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("esh_hotpot")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("esh_wolf_meat", 3)
                .Component("esh_greens", 2)
                .Component("esh_rice", 1)
                .Component("ginger", 1)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 37, targetSweet: 5, targetSpice: 11, innerRadius: 4, outerRadius: 18, freshness: 95);

            // Echani Crispy Claypot Rice
            _builder.Create(RecipeType.EchaniClaypotRice, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("esh_clayrice")
                .Level(46)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("esh_rice", 3)
                .Component("esh_poultry", 2)
                .Component("esh_scallion", 1)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 25, targetSweet: 16, targetSpice: 10, innerRadius: 6, outerRadius: 23, freshness: 91);

            // Echani Savory Rice Porridge
            _builder.Create(RecipeType.EchaniRicePorridge, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("esh_porridge")
                .Level(48)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("esh_rice", 3)
                .Component("esh_pickles", 2)
                .Component("esh_egg", 1)
                .Component("distilled_water", 2)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 19, targetSweet: 14, targetSpice: 1, innerRadius: 4, outerRadius: 20, freshness: 97);

            // Echani Marbled Eggs
            _builder.Create(RecipeType.EchaniMarbledEggs, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("esh_marble_egg")
                .Level(43)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("esh_egg", 3)
                .Component("herb_x", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 22, targetSweet: 4, targetSpice: 15, innerRadius: 4, outerRadius: 18, freshness: 92);

            // Echani Ginger-Steamed Fish
            _builder.Create(RecipeType.EchaniGingerFish, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("esh_ginger_fish")
                .Level(49)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("esh_fish", 3)
                .Component("ginger", 2)
                .Component("esh_scallion", 1)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 35, targetSweet: 3, targetSpice: 31, innerRadius: 4, outerRadius: 23, freshness: 92);

            // Echani Sticky Glazed Poultry
            _builder.Create(RecipeType.EchaniGlazedPoultry, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("esh_glaze_fowl")
                .Level(45)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("esh_poultry", 3)
                .Component("sugar", 2)
                .Component("ginger", 1)
                .Component("esh_chili_oil", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 34, targetSweet: 28, targetSpice: 27, innerRadius: 4, outerRadius: 22, freshness: 96);

            // Echani Sweet Seed-Paste Cakes
            _builder.Create(RecipeType.EchaniSeedPasteCakes, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("esh_seed_cake")
                .Level(46)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("esh_seeds", 3)
                .Component("bread_flour", 2)
                .Component("sugar", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 11, targetSweet: 41, targetSpice: 0, innerRadius: 4, outerRadius: 18, freshness: 97);

            return _builder.Build();
        }
    }
}
