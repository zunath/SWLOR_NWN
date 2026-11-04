using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition
{
    public class ForagedProvisionRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Gloam Skewer
            _builder.Create(RecipeType.GloamSkewer, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("ss_skewer")
                .Level(14)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("kinrath_meat", 3)
                .Component("herb_v", 2)
                .Component("ss_silk", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 38, targetSweet: 0, targetSpice: 27, innerRadius: 6, outerRadius: 29, freshness: 93);

            // Savory Shell Braise
            _builder.Create(RecipeType.SavoryShellBraise, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("mb_braise")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("gimp_meat", 3)
                .Component("herb_m", 2)
                .Component("mb_shell", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 39, targetSweet: 0, targetSpice: 27, innerRadius: 8, outerRadius: 27, freshness: 92);

            // Stonebarb Pot Pie
            _builder.Create(RecipeType.StonebarbPotPie, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("gs_potpie")
                .Level(26)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("cairnmog_meat", 3)
                .Component("v_flour", 2)
                .Component("gs_spine", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 40, targetSweet: 8, targetSpice: 12, innerRadius: 7, outerRadius: 29, freshness: 86);

            // Emberclaw Roast
            _builder.Create(RecipeType.EmberclawRoast, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("rk_roast")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("raivor_meat", 3)
                .Component("herb_m", 2)
                .Component("rk_claw", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 38, targetSweet: 0, targetSpice: 27, innerRadius: 4, outerRadius: 25, freshness: 88);

            // Prism Consomme
            _builder.Create(RecipeType.PrismConsomme, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("se_consomme")
                .Level(21)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("visc_urchin", 2)
                .Component("p_crystal_blue", 1)
                .Component("se_eye", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 26, targetSweet: 10, targetSpice: 13, innerRadius: 7, outerRadius: 24, freshness: 79);

            // Marshleaf Broth
            _builder.Create(RecipeType.MarshleafBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("rc_broth")
                .Level(25)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("herb_m", 3)
                .Component("distilled_water", 1)
                .Component("rc_vine", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 7, targetSweet: 6, targetSpice: 28, innerRadius: 10, outerRadius: 26, freshness: 84);

            // Bitter Fen Tea
            _builder.Create(RecipeType.BitterFenTea, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("mv_tea")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("herb_m", 3)
                .Component("distilled_water", 1)
                .Component("mv_core", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 3, targetSweet: 0, targetSpice: 31, innerRadius: 11, outerRadius: 26, freshness: 81);

            // Resonant Broth
            _builder.Create(RecipeType.ResonantBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("ae_broth")
                .Level(6)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("mynock_wing", 3)
                .Component("distilled_water", 1)
                .Component("ae_echo", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 21, targetSweet: 0, targetSpice: 10, innerRadius: 12, outerRadius: 21, freshness: 87);
        }
    }
}
