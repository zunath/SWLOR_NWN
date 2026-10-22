using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition
{
    public class PetFoodRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Tier1();
            Tier2();
            Tier3();
            Tier4();
            Tier5();

            return _builder.Build();
        }
        private void Tier1()
        {
            // Blubbery Fish Substitute I
            _builder.Create(RecipeType.BlubberyFishSubstitute1, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_blubbfish_1")
                .Level(1)
                .Quantity(2)
                .Component("moat_carp", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 28, targetSweet: 5, targetSpice: 3, innerRadius: 11, outerRadius: 28, freshness: 77, xpPercent: 160);

            // Coarse Fish Substitute I
            _builder.Create(RecipeType.CoarseFishSubstitute1, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_coarsefish_1")
                .Level(2)
                .Quantity(2)
                .Component("lamp_marimo", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 27, targetSweet: 4, targetSpice: 11, innerRadius: 11, outerRadius: 26, freshness: 88, xpPercent: 160);

            // Cooked Fish Substitute I
            _builder.Create(RecipeType.CookedFishSubstitute1, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_cookedfish_1")
                .Level(3)
                .Quantity(2)
                .Component("moat_carp", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 32, targetSweet: 7, targetSpice: 7, innerRadius: 12, outerRadius: 29, freshness: 82, xpPercent: 160);

            // Cooked Meat Substitute I
            _builder.Create(RecipeType.CookedMeatSubstitute1, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_cookedmeat_1")
                .Level(4)
                .Quantity(3)
                .Component("warocas_meat", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 27, targetSweet: 0, targetSpice: 12, innerRadius: 10, outerRadius: 28, freshness: 74, xpPercent: 160);

            // Dry Fruit Substitute I
            _builder.Create(RecipeType.DryFruitSubstitute1, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_dryfruit_1")
                .Level(5)
                .Quantity(3)
                .Component("herb_v", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 7, targetSweet: 0, targetSpice: 33, innerRadius: 7, outerRadius: 29, freshness: 79, xpPercent: 160);

            // Fatty Fish Substitute I
            _builder.Create(RecipeType.FattyFishSubstitute1, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_fattyfish_1")
                .Level(6)
                .Quantity(2)
                .Component("phan_newt", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 33, targetSweet: 7, targetSpice: 8, innerRadius: 8, outerRadius: 30, freshness: 83, xpPercent: 160);

            // Fatty Meat Substitute I
            _builder.Create(RecipeType.FattyMeatSubstitute1, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_fattymeat_1")
                .Level(7)
                .Quantity(2)
                .Component("hamsi", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 35, targetSweet: 4, targetSpice: 8, innerRadius: 8, outerRadius: 27, freshness: 80, xpPercent: 160);

            // Juicy Fruit Substitute I
            _builder.Create(RecipeType.JuicyFruitSubstitute1, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_juicyfruit_1")
                .Level(8)
                .Quantity(3)
                .Component("v_orange", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 2, targetSweet: 28, targetSpice: 9, innerRadius: 9, outerRadius: 24, freshness: 88, xpPercent: 160);

            // Sour Fruit Substitute I
            _builder.Create(RecipeType.SourFruitSubstitute1, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_sourfruit_1")
                .Level(9)
                .Quantity(3)
                .Component("v_lemon", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 4, targetSweet: 21, targetSpice: 21, innerRadius: 8, outerRadius: 29, freshness: 90, xpPercent: 160);

            // Stringy Meat Substitute I
            _builder.Create(RecipeType.StringyMeatSubstitute1, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_stringymeat_1")
                .Level(10)
                .Quantity(3)
                .Component("mynock_meat", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 38, targetSweet: 3, targetSpice: 3, innerRadius: 8, outerRadius: 29, freshness: 79, xpPercent: 160);

            // Sweet Fruit Substitute I
            _builder.Create(RecipeType.SweetFruitSubstitute1, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_sweetfood_1")
                .Level(5)
                .Quantity(3)
                .Component("v_apple", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 5, targetSweet: 31, targetSpice: 10, innerRadius: 7, outerRadius: 23, freshness: 83, xpPercent: 160);

            // Tender Meat Substitute I
            _builder.Create(RecipeType.TenderMeatSubstitute1, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_tendermeat_1")
                .Level(8)
                .Quantity(3)
                .Component("kath_meat_1", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 33, targetSweet: 3, targetSpice: 6, innerRadius: 7, outerRadius: 22, freshness: 76, xpPercent: 160);


        }
        private void Tier2()
        {
            // Blubbery Fish Substitute II
            _builder.Create(RecipeType.BlubberyFishSubstitute2, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_blubbfish_2")
                .Level(11)
                .Quantity(2)
                .Component("sen_sardine", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 31, targetSweet: 2, targetSpice: 13, innerRadius: 7, outerRadius: 29, freshness: 80, xpPercent: 100);

            // Coarse Fish Substitute II
            _builder.Create(RecipeType.CoarseFishSubstitute2, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_coarsefish_2")
                .Level(12)
                .Quantity(2)
                .Component("rakaz_shellfish", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 34, targetSweet: 10, targetSpice: 1, innerRadius: 7, outerRadius: 23, freshness: 83, xpPercent: 100);

            // Cooked Fish Substitute II
            _builder.Create(RecipeType.CookedFishSubstitute2, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_cookedfish_2")
                .Level(13)
                .Quantity(2)
                .Component("bast_sweeper", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 33, targetSweet: 1, targetSpice: 8, innerRadius: 6, outerRadius: 25, freshness: 82, xpPercent: 100);

            // Cooked Meat Substitute II
            _builder.Create(RecipeType.CookedMeatSubstitute2, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_cookedmeat_2")
                .Level(14)
                .Quantity(3)
                .Component("raivor_meat", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 32, targetSweet: 4, targetSpice: 10, innerRadius: 7, outerRadius: 20, freshness: 75, xpPercent: 100);

            // Dry Fruit Substitute II
            _builder.Create(RecipeType.DryFruitSubstitute2, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_dryfruit_2")
                .Level(15)
                .Quantity(3)
                .Component("v_peas", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 21, targetSweet: 21, targetSpice: 3, innerRadius: 10, outerRadius: 28, freshness: 95, xpPercent: 100);

            // Fatty Fish Substitute II
            _builder.Create(RecipeType.FattyFishSubstitute2, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_fattyfish_2")
                .Level(16)
                .Quantity(2)
                .Component("greedie", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 29, targetSweet: 8, targetSpice: 5, innerRadius: 8, outerRadius: 21, freshness: 76, xpPercent: 100);

            // Fatty Meat Substitute II
            _builder.Create(RecipeType.FattyMeatSubstitute2, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_fattymeat_2")
                .Level(17)
                .Quantity(2)
                .Component("muddy_siredon", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 30, targetSweet: 2, targetSpice: 3, innerRadius: 6, outerRadius: 25, freshness: 81, xpPercent: 100);

            // Juicy Fruit Substitute II
            _builder.Create(RecipeType.JuicyFruitSubstitute2, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_juicyfruit_2")
                .Level(18)
                .Quantity(3)
                .Component("v_apple", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 0, targetSweet: 38, targetSpice: 8, innerRadius: 10, outerRadius: 20, freshness: 88, xpPercent: 100);

            // Sour Fruit Substitute II
            _builder.Create(RecipeType.SourFruitSubstitute2, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_sourfruit_2")
                .Level(19)
                .Quantity(3)
                .Component("v_peas", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 26, targetSweet: 25, targetSpice: 3, innerRadius: 10, outerRadius: 24, freshness: 94, xpPercent: 100);

            // Stringy Meat Substitute II
            _builder.Create(RecipeType.StringyMeatSubstitute2, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_stringymeat_2")
                .Level(20)
                .Quantity(3)
                .Component("cairnmog_meat", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 28, targetSweet: 0, targetSpice: 7, innerRadius: 8, outerRadius: 25, freshness: 77, xpPercent: 100);

            // Sweet Fruit Substitute II
            _builder.Create(RecipeType.SweetFruitSubstitute2, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_sweetfood_2")
                .Level(15)
                .Quantity(3)
                .Component("herb_m", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 9, targetSweet: 3, targetSpice: 36, innerRadius: 10, outerRadius: 28, freshness: 82, xpPercent: 100);

            // Tender Meat Substitute II
            _builder.Create(RecipeType.TenderMeatSubstitute2, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_tendermeat_2")
                .Level(18)
                .Quantity(3)
                .Component("klorslug_meat", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 30, targetSweet: 6, targetSpice: 0, innerRadius: 7, outerRadius: 24, freshness: 72, xpPercent: 100);


        }
        private void Tier3()
        {
            // Blubbery Fish Substitute III
            _builder.Create(RecipeType.BlubberyFishSubstitute3, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_blubbfish_3")
                .Level(21)
                .Quantity(2)
                .Component("quus", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 30, targetSweet: 4, targetSpice: 3, innerRadius: 8, outerRadius: 18, freshness: 76, xpPercent: 120);

            // Coarse Fish Substitute III
            _builder.Create(RecipeType.CoarseFishSubstitute3, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_coarsefish_3")
                .Level(22)
                .Quantity(2)
                .Component("forest_carp", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 30, targetSweet: 7, targetSpice: 7, innerRadius: 5, outerRadius: 18, freshness: 78, xpPercent: 120);

            // Cooked Fish Substitute III
            _builder.Create(RecipeType.CookedFishSubstitute3, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_cookedfish_3")
                .Level(23)
                .Quantity(2)
                .Component("cheval_salmon", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 26, targetSweet: 0, targetSpice: 1, innerRadius: 5, outerRadius: 22, freshness: 84, xpPercent: 120);

            // Cooked Meat Substitute III
            _builder.Create(RecipeType.CookedMeatSubstitute3, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_cookedmeat_3")
                .Level(24)
                .Quantity(3)
                .Component("aradile_meat", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 32, targetSweet: 0, targetSpice: 5, innerRadius: 5, outerRadius: 23, freshness: 79, xpPercent: 120);

            // Dry Fruit Substitute III
            _builder.Create(RecipeType.DryFruitSubstitute3, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_dryfruit_3")
                .Level(25)
                .Quantity(3)
                .Component("h_acorn", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 25, targetSweet: 19, targetSpice: 4, innerRadius: 5, outerRadius: 18, freshness: 93, xpPercent: 120);

            // Fatty Fish Substitute III
            _builder.Create(RecipeType.FattyFishSubstitute3, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_fattyfish_3")
                .Level(26)
                .Quantity(2)
                .Component("yorchete", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 29, targetSweet: 4, targetSpice: 10, innerRadius: 7, outerRadius: 21, freshness: 80, xpPercent: 120);

            // Fatty Meat Substitute III
            _builder.Create(RecipeType.FattyMeatSubstitute3, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_fattymeat_3")
                .Level(27)
                .Quantity(2)
                .Component("moorish_idol", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 29, targetSweet: 3, targetSpice: 4, innerRadius: 6, outerRadius: 18, freshness: 82, xpPercent: 120);

            // Juicy Fruit Substitute III
            _builder.Create(RecipeType.JuicyFruitSubstitute3, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_juicyfruit_3")
                .Level(28)
                .Quantity(3)
                .Component("s_pineapple", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 2, targetSweet: 29, targetSpice: 1, innerRadius: 5, outerRadius: 18, freshness: 81, xpPercent: 120);

            // Sour Fruit Substitute III
            _builder.Create(RecipeType.SourFruitSubstitute3, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_sourfruit_3")
                .Level(29)
                .Quantity(3)
                .Component("nash_blood", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 27, targetSweet: 8, targetSpice: 21, innerRadius: 6, outerRadius: 18, freshness: 84, xpPercent: 120);

            // Stringy Meat Substitute III
            _builder.Create(RecipeType.StringyMeatSubstitute3, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_stringymeat_3")
                .Level(30)
                .Quantity(3)
                .Component("tiger_meat", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 26, targetSweet: 0, targetSpice: 4, innerRadius: 6, outerRadius: 19, freshness: 77, xpPercent: 120);

            // Sweet Fruit Substitute III
            _builder.Create(RecipeType.SweetFruitSubstitute3, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_sweetfood_3")
                .Level(25)
                .Quantity(3)
                .Component("ginger", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 0, targetSweet: 5, targetSpice: 36, innerRadius: 8, outerRadius: 18, freshness: 80, xpPercent: 120);

            // Tender Meat Substitute III
            _builder.Create(RecipeType.TenderMeatSubstitute3, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_tendermeat_3")
                .Level(28)
                .Quantity(3)
                .Component("byysk_meat", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 35, targetSweet: 0, targetSpice: 12, innerRadius: 7, outerRadius: 22, freshness: 74, xpPercent: 120);


        }
        private void Tier4()
        {
            // Blubbery Fish Substitute IV
            _builder.Create(RecipeType.BlubberyFishSubstitute4, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_blubbfish_4")
                .Level(31)
                .Quantity(2)
                .Component("nebimonite", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 26, targetSweet: 5, targetSpice: 11, innerRadius: 4, outerRadius: 21, freshness: 78, xpPercent: 100);

            // Coarse Fish Substitute IV
            _builder.Create(RecipeType.CoarseFishSubstitute4, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_coarsefish_4")
                .Level(32)
                .Quantity(2)
                .Component("tricolored_carp", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 31, targetSweet: 9, targetSpice: 11, innerRadius: 6, outerRadius: 20, freshness: 78, xpPercent: 100);

            // Cooked Fish Substitute IV
            _builder.Create(RecipeType.CookedFishSubstitute4, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_cookedfish_4")
                .Level(33)
                .Quantity(2)
                .Component("blindfish", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 33, targetSweet: 0, targetSpice: 10, innerRadius: 5, outerRadius: 19, freshness: 83, xpPercent: 100);

            // Cooked Meat Substitute IV
            _builder.Create(RecipeType.CookedMeatSubstitute4, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_cookedmeat_4")
                .Level(34)
                .Quantity(3)
                .Component("wompratmeat", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 28, targetSweet: 4, targetSpice: 1, innerRadius: 6, outerRadius: 18, freshness: 75, xpPercent: 100);

            // Dry Fruit Substitute IV
            _builder.Create(RecipeType.DryFruitSubstitute4, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_dryfruit_4")
                .Level(35)
                .Quantity(3)
                .Component("herb_c", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 27, targetSweet: 0, targetSpice: 16, innerRadius: 8, outerRadius: 18, freshness: 80, xpPercent: 100);

            // Fatty Fish Substitute IV
            _builder.Create(RecipeType.FattyFishSubstitute4, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_fattyfish_4")
                .Level(36)
                .Quantity(2)
                .Component("bonefish", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 28, targetSweet: 6, targetSpice: 9, innerRadius: 5, outerRadius: 18, freshness: 79, xpPercent: 100);

            // Fatty Meat Substitute IV
            _builder.Create(RecipeType.FattyMeatSubstitute4, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_fattymeat_4")
                .Level(37)
                .Quantity(2)
                .Component("yayinbaligi", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 30, targetSweet: 6, targetSpice: 3, innerRadius: 4, outerRadius: 20, freshness: 77, xpPercent: 100);

            // Juicy Fruit Substitute IV
            _builder.Create(RecipeType.JuicyFruitSubstitute4, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_juicyfruit_4")
                .Level(38)
                .Quantity(3)
                .Component("tomato", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 31, targetSweet: 13, targetSpice: 12, innerRadius: 5, outerRadius: 19, freshness: 87, xpPercent: 100);

            // Sour Fruit Substitute IV
            _builder.Create(RecipeType.SourFruitSubstitute4, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_sourfruit_4")
                .Level(39)
                .Quantity(3)
                .Component("plant_butter", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 27, targetSweet: 20, targetSpice: 2, innerRadius: 6, outerRadius: 20, freshness: 87, xpPercent: 100);

            // Stringy Meat Substitute IV
            _builder.Create(RecipeType.StringyMeatSubstitute4, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_stringymeat_4")
                .Level(40)
                .Quantity(3)
                .Component("sanddemon_meat", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 38, targetSweet: 0, targetSpice: 1, innerRadius: 4, outerRadius: 20, freshness: 78, xpPercent: 100);

            // Sweet Fruit Substitute IV
            _builder.Create(RecipeType.SweetFruitSubstitute4, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_sweetfood_4")
                .Level(35)
                .Quantity(3)
                .Component("herb_t", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 13, targetSweet: 0, targetSpice: 28, innerRadius: 6, outerRadius: 18, freshness: 83, xpPercent: 100);

            // Tender Meat Substitute IV
            _builder.Create(RecipeType.TenderMeatSubstitute4, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_tendermeat_4")
                .Level(38)
                .Quantity(3)
                .Component("giant_catfish", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 31, targetSweet: 6, targetSpice: 6, innerRadius: 7, outerRadius: 19, freshness: 82, xpPercent: 100);


        }
        private void Tier5()
        {
            // Blubbery Fish Substitute V
            _builder.Create(RecipeType.BlubberyFishSubstitute5, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_blubbfish_5")
                .Level(41)
                .Quantity(2)
                .Component("dark_bass", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 35, targetSweet: 1, targetSpice: 4, innerRadius: 7, outerRadius: 18, freshness: 83, xpPercent: 100);

            // Coarse Fish Substitute V
            _builder.Create(RecipeType.CoarseFishSubstitute5, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_coarsefish_5")
                .Level(42)
                .Quantity(2)
                .Component("crystal_bass", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 26, targetSweet: 9, targetSpice: 7, innerRadius: 7, outerRadius: 18, freshness: 80, xpPercent: 100);

            // Cooked Fish Substitute V
            _builder.Create(RecipeType.CookedFishSubstitute5, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_cookedfish_5")
                .Level(43)
                .Quantity(2)
                .Component("ogre_eel", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 27, targetSweet: 1, targetSpice: 2, innerRadius: 6, outerRadius: 18, freshness: 77, xpPercent: 100);

            // Cooked Meat Substitute V
            _builder.Create(RecipeType.CookedMeatSubstitute5, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_cookedmeat_5")
                .Level(44)
                .Quantity(3)
                .Component("wild_meat", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 28, targetSweet: 0, targetSpice: 1, innerRadius: 4, outerRadius: 18, freshness: 76, xpPercent: 100);

            // Dry Fruit Substitute V
            _builder.Create(RecipeType.DryFruitSubstitute5, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_dryfruit_5")
                .Level(45)
                .Quantity(3)
                .Component("turnip", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 35, targetSweet: 15, targetSpice: 6, innerRadius: 4, outerRadius: 22, freshness: 89, xpPercent: 100);

            // Fatty Fish Substitute V
            _builder.Create(RecipeType.FattyFishSubstitute5, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_fattyfish_5")
                .Level(46)
                .Quantity(2)
                .Component("nosteau_herring", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 26, targetSweet: 10, targetSpice: 9, innerRadius: 6, outerRadius: 22, freshness: 78, xpPercent: 100);

            // Fatty Meat Substitute V
            _builder.Create(RecipeType.FattyMeatSubstitute5, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_fattymeat_5")
                .Level(47)
                .Quantity(2)
                .Component("lakerda", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 28, targetSweet: 7, targetSpice: 13, innerRadius: 4, outerRadius: 18, freshness: 77, xpPercent: 100);

            // Juicy Fruit Substitute V
            _builder.Create(RecipeType.JuicyFruitSubstitute5, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_juicyfruit_5")
                .Level(48)
                .Quantity(3)
                .Component("turnip", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 24, targetSweet: 18, targetSpice: 13, innerRadius: 5, outerRadius: 18, freshness: 95, xpPercent: 100);

            // Sour Fruit Substitute V
            _builder.Create(RecipeType.SourFruitSubstitute5, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_sourfruit_5")
                .Level(49)
                .Quantity(3)
                .Component("dried_bonito", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 35, targetSweet: 0, targetSpice: 5, innerRadius: 4, outerRadius: 21, freshness: 80, xpPercent: 100);

            // Stringy Meat Substitute V
            _builder.Create(RecipeType.StringyMeatSubstitute5, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_stringymeat_5")
                .Level(50)
                .Quantity(3)
                .Component("wild_innards", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 32, targetSweet: 4, targetSpice: 10, innerRadius: 4, outerRadius: 19, freshness: 83, xpPercent: 100);

            // Sweet Fruit Substitute V
            _builder.Create(RecipeType.SweetFruitSubstitute5, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_sweetfood_5")
                .Level(45)
                .Quantity(3)
                .Component("herb_x", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 4, targetSweet: 3, targetSpice: 26, innerRadius: 7, outerRadius: 21, freshness: 79, xpPercent: 100);

            // Tender Meat Substitute V
            _builder.Create(RecipeType.TenderMeatSubstitute5, SkillType.Agriculture)
                .Category(RecipeCategoryType.PetFood)
                .Resref("pf_tendermeat_5")
                .Level(48)
                .Quantity(3)
                .Component("shining_trout", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.PetFood, targetSavory: 35, targetSweet: 7, targetSpice: 7, innerRadius: 4, outerRadius: 19, freshness: 80, xpPercent: 100);


        }
    }
}
