using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition
{
    public class FarmingSupplyRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Nutrient Solution
            _builder.Create(RecipeType.NutrientSolution, SkillType.Agriculture)
                .Category(RecipeCategoryType.FarmingSupply)
                .Resref("nutrient_sol")
                .Level(1)
                .Quantity(5)
                .Component("distilled_water", 2);

            // Growth Fertilizer
            _builder.Create(RecipeType.GrowthFertilizer, SkillType.Agriculture)
                .Category(RecipeCategoryType.FarmingSupply)
                .Resref("fert_growth")
                .Level(10)
                .Quantity(2)
                .Component("wild_innards", 2)
                .Component("distilled_water", 1);

            // Compost
            _builder.Create(RecipeType.Compost, SkillType.Agriculture)
                .Category(RecipeCategoryType.FarmingSupply)
                .Resref("compost")
                .Level(15)
                .Quantity(2)
                .Component("v_peas", 2)
                .Component("v_apple", 2);

            // Yield Fertilizer
            _builder.Create(RecipeType.YieldFertilizer, SkillType.Agriculture)
                .Category(RecipeCategoryType.FarmingSupply)
                .Resref("fert_yield")
                .Level(20)
                .Quantity(2)
                .Component("compost", 1)
                .Component("wild_innards", 2);

            // Quality Fertilizer
            _builder.Create(RecipeType.QualityFertilizer, SkillType.Agriculture)
                .Category(RecipeCategoryType.FarmingSupply)
                .Resref("fert_quality")
                .Level(30)
                .Quantity(2)
                .Component("compost", 2)
                .Component("herb_c", 1);

            // Vegetable Flour
            _builder.Create(RecipeType.VegetableFlour, SkillType.Agriculture)
                .Category(RecipeCategoryType.FarmingSupply)
                .Resref("v_flour")
                .Level(22)
                .Quantity(2)
                .Component("nysillim_grain", 2);

            // Baking Flour
            _builder.Create(RecipeType.BakingFlour, SkillType.Agriculture)
                .Category(RecipeCategoryType.FarmingSupply)
                .Resref("b_flour")
                .Level(28)
                .Quantity(2)
                .Component("nysillim_grain", 2);

            // Refined Flour
            _builder.Create(RecipeType.RefinedFlour, SkillType.Agriculture)
                .Category(RecipeCategoryType.FarmingSupply)
                .Resref("r_flour")
                .Level(34)
                .Quantity(2)
                .Component("nysillim_grain", 2);

            // Premium Flour
            _builder.Create(RecipeType.PremiumFlour, SkillType.Agriculture)
                .Category(RecipeCategoryType.FarmingSupply)
                .Resref("p_flour")
                .Level(40)
                .Component("nysillim_grain", 3);

            // Bread Flour
            _builder.Create(RecipeType.BreadFlour, SkillType.Agriculture)
                .Category(RecipeCategoryType.FarmingSupply)
                .Resref("bread_flour")
                .Level(46)
                .Component("nysillim_grain", 3);

            // Extract Jogan Seeds
            _builder.Create(RecipeType.ExtractJoganSeeds, SkillType.Agriculture)
                .Category(RecipeCategoryType.FarmingSupply)
                .Resref("seed_jogan")
                .Level(5)
                .Quantity(2)
                .Component("jogan_fruit", 2);

            // Extract Tarine Seeds
            _builder.Create(RecipeType.ExtractTarineSeeds, SkillType.Agriculture)
                .Category(RecipeCategoryType.FarmingSupply)
                .Resref("seed_tarine")
                .Level(15)
                .Quantity(2)
                .Component("tarine_leaf", 2);

            // Extract Nysillim Seeds
            _builder.Create(RecipeType.ExtractNysillimSeeds, SkillType.Agriculture)
                .Category(RecipeCategoryType.FarmingSupply)
                .Resref("seed_nysillim")
                .Level(25)
                .Quantity(2)
                .Component("nysillim_grain", 2);

            // Extract Shuura Seeds
            _builder.Create(RecipeType.ExtractShuuraSeeds, SkillType.Agriculture)
                .Category(RecipeCategoryType.FarmingSupply)
                .Resref("seed_shuura")
                .Level(35)
                .Quantity(2)
                .Component("shuura_fruit", 2);

            // Extract Silkvine Seeds
            _builder.Create(RecipeType.ExtractSilkvineSeeds, SkillType.Agriculture)
                .Category(RecipeCategoryType.FarmingSupply)
                .Resref("seed_silkvine")
                .Level(45)
                .Quantity(2)
                .Component("silkvine_fiber", 2);

            // Extract Meiloorun Seeds
            _builder.Create(RecipeType.ExtractMeiloorunSeeds, SkillType.Agriculture)
                .Category(RecipeCategoryType.FarmingSupply)
                .Resref("seed_meiloorun")
                .Level(49)
                .Quantity(2)
                .Component("meiloorun", 2);

            // Extract Firepepper Seeds
            _builder.Create(RecipeType.ExtractFirepepperSeeds, SkillType.Agriculture)
                .Category(RecipeCategoryType.FarmingSupply)
                .Resref("seed_firepepper")
                .Level(53)
                .Quantity(2)
                .Component("firepepper", 2);
        }
    }
}
