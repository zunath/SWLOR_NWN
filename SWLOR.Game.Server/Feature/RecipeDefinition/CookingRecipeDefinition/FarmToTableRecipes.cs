using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition
{
    public class FarmToTableRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Jogan Tart
            _builder.Create(RecipeType.JoganTart, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("jogan_tart")
                .Level(5)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("jogan_fruit", 2)
                .Component("v_flour", 1)
                .Component("sugar", 1);

            // Sweetcane Glaze Cake
            _builder.Create(RecipeType.SweetcaneGlazeCake, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("cane_cake")
                .Level(9)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("sugar", 4)
                .Component("v_flour", 2)
                .Component("jogan_fruit", 1);

            // Tarine Herb Broth
            _builder.Create(RecipeType.TarineHerbBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("tarine_broth")
                .Level(14)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("tarine_leaf", 1)
                .Component("herb_m", 1)
                .Component("distilled_water", 1);

            // Nysillim Porridge
            _builder.Create(RecipeType.NysillimPorridge, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("nys_porridge")
                .Level(24)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("nysillim_grain", 2)
                .Component("sugar", 1)
                .Component("distilled_water", 1);

            // Shuura Glazed Roast
            _builder.Create(RecipeType.ShuuraGlazedRoast, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("shuura_roast")
                .Level(33)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("shuura_fruit", 2)
                .Component("wild_meat", 2)
                .Component("herb_t", 1);

            // Shuura Chutney
            _builder.Create(RecipeType.ShuuraChutney, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("shuura_chutney")
                .Level(36)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("shuura_fruit", 2)
                .Component("ginger", 1)
                .Component("sugar", 2);

            // Meiloorun Sorbet
            _builder.Create(RecipeType.MeiloorunSorbet, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("meil_sorbet")
                .Level(47)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("meiloorun", 2)
                .Component("sugar", 2)
                .Component("distilled_water", 1);

            // Meiloorun Feast
            _builder.Create(RecipeType.MeiloorunFeast, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("meil_feast")
                .Level(49)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("meiloorun", 2)
                .Component("dark_bass", 2)
                .Component("herb_x", 1);

            // Firepepper Noodle Soup
            _builder.Create(RecipeType.FirepepperNoodleSoup, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("pepper_noodles")
                .Level(52)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("firepepper", 1)
                .Component("r_flour", 2)
                .Component("herb_x", 1)
                .Component("distilled_water", 1);

            // Firepepper Ambrosia
            _builder.Create(RecipeType.FirepepperAmbrosia, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("pepper_ambrosia")
                .Level(53)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("prs_firepepper", 1)
                .Component("meiloorun", 1)
                .Component("sugar", 2);

            // Tarine Tea
            _builder.Create(RecipeType.TarineTea, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("tarine_tea")
                .Level(12)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("tarine_leaf", 2)
                .Component("distilled_water", 1);

            // Citrus Tarine Tea
            _builder.Create(RecipeType.CitrusTarineTea, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("citrus_tea")
                .Level(22)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("tarine_leaf", 2)
                .Component("v_lemon", 1)
                .Component("sugar", 1);

            // Masters Tarine Tea
            _builder.Create(RecipeType.MastersTarineTea, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("masters_tea")
                .Level(42)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("prs_tarine", 1)
                .Component("tarine_leaf", 2)
                .Component("sugar", 1);

            // Premium Grain Feed
            _builder.Create(RecipeType.PremiumGrainFeed, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_nys_feed")
                .Level(30)
                .Quantity(2)
                .Component("nysillim_grain", 3)
                .Component("distilled_water", 1);

            // Universal Treat
            _builder.Create(RecipeType.UniversalTreat, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_uni_treat")
                .Level(45)
                .Quantity(2)
                .Component("prs_nysillim", 1)
                .Component("nysillim_grain", 2)
                .Component("sugar", 2);
        }
    }
}
