using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition
{
    public class CookingRecipes: IRecipeListDefinition
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
            _builder.Create(RecipeType.MynockMeatBall, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("mynock_mball")
                .Level(1)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("mynock_meat", 3)
                .Component("mynock_wing", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 52, targetSweet: 1, targetSpice: 14, innerRadius: 11, outerRadius: 31, freshness: 76, xpPercent: 160);

            _builder.Create(RecipeType.WarocasPotPie, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("waro_potpie")
                .Level(1)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("warocas_meat", 3)
                .Component("waro_leg", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 51, targetSweet: 5, targetSpice: 8, innerRadius: 12, outerRadius: 36, freshness: 83, xpPercent: 160);

            // Roast Carp
            _builder.Create(RecipeType.RoastCarp, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("roast_carp")
                .Level(1)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("moat_carp", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 30, targetSweet: 2, targetSpice: 6, innerRadius: 8, outerRadius: 24, freshness: 73, xpPercent: 160);

            _builder.Create(RecipeType.SugarCookies, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("sugar_cookies")
                .Level(2)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("sugar", 4)
                .Component("v_flour", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 1, targetSweet: 52, targetSpice: 0, innerRadius: 8, outerRadius: 33, freshness: 88, xpPercent: 160);

            // Marimo Stew
            _builder.Create(RecipeType.MarimoStew, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("marimo_stew")
                .Level(2)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("lamp_marimo", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 29, targetSweet: 0, targetSpice: 0, innerRadius: 12, outerRadius: 18, freshness: 67, xpPercent: 160);

            _builder.Create(RecipeType.OrangeJuice, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("orange_juice")
                .Level(3)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("v_orange", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 0, targetSweet: 29, targetSpice: 0, innerRadius: 10, outerRadius: 22, freshness: 69, xpPercent: 160);

            _builder.Create(RecipeType.PebbleSoup, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("pebble_soup")
                .Level(3)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("v_pebble", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 21, targetSweet: 0, targetSpice: 4, innerRadius: 12, outerRadius: 20, freshness: 75, xpPercent: 160);

            _builder.Create(RecipeType.MynockBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("mynock_broth")
                .Level(3)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("mynock_wing", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 28, targetSweet: 0, targetSpice: 6, innerRadius: 14, outerRadius: 25, freshness: 68, xpPercent: 160);

            // Urchin Sushi
            _builder.Create(RecipeType.UrchinSushi, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("urchin_sushi")
                .Level(3)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("visc_urchin", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 25, targetSweet: 11, targetSpice: 4, innerRadius: 9, outerRadius: 26, freshness: 68, xpPercent: 160);

            _builder.Create(RecipeType.Noodles, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("noodles")
                .Level(4)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("v_flour", 4)
                .Component("butter_1", 1)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 15, targetSweet: 23, targetSpice: 0, innerRadius: 11, outerRadius: 26, freshness: 78, xpPercent: 160);

            _builder.Create(RecipeType.KathSandwich, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("kath_sandwich")
                .Level(4)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("kath_meat_1", 3)
                .Component("v_flour", 2)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 38, targetSweet: 7, targetSpice: 10, innerRadius: 11, outerRadius: 35, freshness: 72, xpPercent: 160);

            // Blackened Newt
            _builder.Create(RecipeType.BlackenedNewt, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("blackened_newt")
                .Level(4)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("phan_newt", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 30, targetSweet: 5, targetSpice: 1, innerRadius: 9, outerRadius: 24, freshness: 74, xpPercent: 160);

            _builder.Create(RecipeType.KinrathMeatBall, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("kinrath_mball")
                .Level(5)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("kinrath_meat", 3)
                .Component("kinrath_limb", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 50, targetSweet: 0, targetSpice: 16, innerRadius: 11, outerRadius: 29, freshness: 77, xpPercent: 160);

            // Cooked Jellyfish
            _builder.Create(RecipeType.CookedJellyfish, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("cooked_jellyfish")
                .Level(5)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("cobalt_jellyfish", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 29, targetSweet: 0, targetSpice: 2, innerRadius: 8, outerRadius: 24, freshness: 71, xpPercent: 160);

            _builder.Create(RecipeType.LemonCookies, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("lemon_cookies")
                .Level(6)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("sugar", 2)
                .Component("v_flour", 1)
                .Component("v_lemon", 3)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 0, targetSweet: 40, targetSpice: 14, innerRadius: 6, outerRadius: 28, freshness: 77, xpPercent: 160);

            _builder.Create(RecipeType.ViscaranHerbSoup, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("v_herb_soup")
                .Level(6)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("herb_v", 3)
                .Component("distilled_water", 1)
                .Component("sugar", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 6, targetSweet: 16, targetSpice: 23, innerRadius: 9, outerRadius: 25, freshness: 76, xpPercent: 160);

            _builder.Create(RecipeType.OrangeCurry, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("orange_curry")
                .Level(6)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("v_orange", 4)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 0, targetSweet: 30, targetSpice: 1, innerRadius: 13, outerRadius: 24, freshness: 75, xpPercent: 160);

            // Denzi Treat
            _builder.Create(RecipeType.DenziTreat, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("denzi_treat")
                .Level(6)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("denizanasi", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 30, targetSweet: 6, targetSpice: 11, innerRadius: 10, outerRadius: 21, freshness: 75, xpPercent: 160);

            _builder.Create(RecipeType.OrangeAuLait, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("o_aulait")
                .Level(7)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("v_orange", 3)
                .Component("distilled_water", 2)
                .Component("herb_v", 3)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 9, targetSweet: 27, targetSpice: 33, innerRadius: 9, outerRadius: 32, freshness: 90, xpPercent: 160);

            // Peeled Crayfish
            _builder.Create(RecipeType.PeeledCrayfish, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("peeled_crayfish")
                .Level(7)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("crayfish", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 30, targetSweet: 4, targetSpice: 5, innerRadius: 11, outerRadius: 22, freshness: 67, xpPercent: 160);

            _builder.Create(RecipeType.KathBloodBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("k_blood_broth")
                .Level(8)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("kath_blood", 3)
                .Component("distilled_water", 1)
                .Component("herb_v", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 19, targetSweet: 5, targetSpice: 35, innerRadius: 13, outerRadius: 33, freshness: 76, xpPercent: 160);

            // Peeled Lobster
            _builder.Create(RecipeType.PeeledLobster, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("peeled_lobster")
                .Level(8)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("cala_lobster", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 28, targetSweet: 7, targetSpice: 5, innerRadius: 9, outerRadius: 22, freshness: 73, xpPercent: 160);

            _builder.Create(RecipeType.GimpassaSandwich, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("g_sandwich")
                .Level(9)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("gimp_meat", 3)
                .Component("distilled_water", 1)
                .Component("herb_v", 3)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 42, targetSweet: 6, targetSpice: 33, innerRadius: 10, outerRadius: 33, freshness: 83, xpPercent: 160);


            // Cooked Bibikibo
            _builder.Create(RecipeType.CookedBibikibo, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("cooked_bibikibo")
                .Level(9)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("bibikibo", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 20, targetSweet: 0, targetSpice: 0, innerRadius: 11, outerRadius: 19, freshness: 70, xpPercent: 160);

            _builder.Create(RecipeType.GimpassaStew, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("g_stew")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("gimp_blood", 2)
                .Component("gimp_meat", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 34, targetSweet: 0, targetSpice: 17, innerRadius: 12, outerRadius: 34, freshness: 69, xpPercent: 160);

            // Sliced Sardine
            _builder.Create(RecipeType.SlicedSardine, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("sliced_sardine")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("dath_sardine", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 30, targetSweet: 0, targetSpice: 4, innerRadius: 9, outerRadius: 19, freshness: 73, xpPercent: 160);

            // Cooking Enhancement - Duration I
            _builder.Create(RecipeType.CookingEnhancementDuration1, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_dur1")
                .Level(1)
                .Quantity(1)
                .Component("mynock_meat", 2)
                .Component("mynock_wing", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 27, targetSweet: 0, targetSpice: 9, innerRadius: 11, outerRadius: 27, freshness: 66, xpPercent: 160);

            // Cooking Enhancement - FP I
            _builder.Create(RecipeType.CookingEnhancementFP1, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_fp1")
                .Level(3)
                .Quantity(1)
                .Component("mynock_wing", 2)
                .Component("mynock_meat", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 26, targetSweet: 4, targetSpice: 1, innerRadius: 11, outerRadius: 20, freshness: 64, xpPercent: 160);

            // Cooking Enhancement - FP Regen I
            _builder.Create(RecipeType.CookingEnhancementFPRegen1, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_fpr1")
                .Level(8)
                .Quantity(1)
                .Component("kath_blood", 4)
                .Component("herb_v", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 38, targetSweet: 8, targetSpice: 28, innerRadius: 9, outerRadius: 40, freshness: 76, xpPercent: 160);

            // Cooking Enhancement - HP I
            _builder.Create(RecipeType.CookingEnhancementHP1, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_hp1")
                .Level(4)
                .Quantity(1)
                .Component("warocas_meat", 3)
                .Component("v_pebble", 3)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 50, targetSweet: 0, targetSpice: 10, innerRadius: 12, outerRadius: 39, freshness: 80, xpPercent: 160);

            // Cooking Enhancement - HP Regen I
            _builder.Create(RecipeType.CookingEnhancementHPRegen1, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_hpr1")
                .Level(6)
                .Quantity(1)
                .Component("kinrath_meat", 3)
                .Component("herb_v", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 36, targetSweet: 0, targetSpice: 18, innerRadius: 12, outerRadius: 37, freshness: 78, xpPercent: 160);

            // Cooking Enhancement - Combat Readiness I
            _builder.Create(RecipeType.CookingEnhancementCombatReadiness1, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_recast1")
                .Level(8)
                .Quantity(1)
                .Component("gimp_meat", 3)
                .Component("waro_leg", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 38, targetSweet: 0, targetSpice: 10, innerRadius: 12, outerRadius: 32, freshness: 78, xpPercent: 160);

            // Cooking Enhancement - Rest Regen I
            _builder.Create(RecipeType.CookingEnhancementRestRegen1, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_rest1")
                .Level(2)
                .Quantity(1)
                .Component("herb_v", 2)
                .Component("mynock_wing", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 16, targetSweet: 4, targetSpice: 24, innerRadius: 12, outerRadius: 27, freshness: 68, xpPercent: 160);

            // Cooking Enhancement - STM I
            _builder.Create(RecipeType.CookingEnhancementSTM1, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_stm1")
                .Level(4)
                .Quantity(1)
                .Component("kinrath_limb", 3)
                .Component("gimp_meat", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 44, targetSweet: 0, targetSpice: 8, innerRadius: 10, outerRadius: 41, freshness: 72, xpPercent: 160);

            // Cooking Enhancement - STM Regen I
            _builder.Create(RecipeType.CookingEnhancementSTMRegen1, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_stmr1")
                .Level(7)
                .Quantity(1)
                .Component("v_orange", 4)
                .Component("herb_v", 3)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 2, targetSweet: 34, targetSpice: 34, innerRadius: 11, outerRadius: 34, freshness: 88, xpPercent: 160);

            // Cooking Enhancement - XP Bonus I
            _builder.Create(RecipeType.CookingEnhancementXPBonus1, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_xp1")
                .Level(6)
                .Quantity(1)
                .Component("gimp_blood", 3)
                .Component("kinrath_limb", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 40, targetSweet: 0, targetSpice: 15, innerRadius: 10, outerRadius: 34, freshness: 73, xpPercent: 160);

            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceFire1, "cen_res_fir1", 5, "gimp_meat", 3, "waro_leg", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 40, targetSweet: 0, targetSpice: 9, innerRadius: 13, outerRadius: 39, freshness: 78, xpPercent: 160));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistancePoison1, "cen_res_psn1", 6, "kath_blood", 3, "herb_v", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 21, targetSweet: 0, targetSpice: 32, innerRadius: 11, outerRadius: 33, freshness: 74, xpPercent: 160));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceElectrical1, "cen_res_elec1", 7, "gimp_blood", 3, "kinrath_limb", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 34, targetSweet: 8, targetSpice: 23, innerRadius: 9, outerRadius: 34, freshness: 77, xpPercent: 160));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceIce1, "cen_res_ice1", 8, "v_orange", 3, "herb_v", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 0, targetSweet: 25, targetSpice: 15, innerRadius: 12, outerRadius: 26, freshness: 77, xpPercent: 160));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceMind1, "cen_res_mnd1", 5, "mynock_wing", 3, "mynock_meat", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 43, targetSweet: 5, targetSpice: 16, innerRadius: 12, outerRadius: 37, freshness: 73, xpPercent: 160));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceMobility1, "cen_res_mob1", 6, "kinrath_limb", 3, "gimp_meat", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 44, targetSweet: 0, targetSpice: 14, innerRadius: 11, outerRadius: 35, freshness: 72, xpPercent: 160));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceTrauma1, "cen_res_tra1", 7, "warocas_meat", 3, "v_pebble", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 43, targetSweet: 0, targetSpice: 9, innerRadius: 12, outerRadius: 33, freshness: 76, xpPercent: 160));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceDisruption1, "cen_res_dis1", 8, "kinrath_meat", 3, "herb_v", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 35, targetSweet: 0, targetSpice: 19, innerRadius: 10, outerRadius: 34, freshness: 79, xpPercent: 160));
        }

        private void Tier2()
        {
            _builder.Create(RecipeType.RaivorMeatBall, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("raivor_mball")
                .Level(11)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("raivor_meat", 3)
                .Component("raivor_scale", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 44, targetSweet: 3, targetSpice: 7, innerRadius: 10, outerRadius: 29, freshness: 80, xpPercent: 100);

            _builder.Create(RecipeType.CairnmogPotPie, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("cairn_potpie")
                .Level(11)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("cairnmog_meat", 3)
                .Component("cairnmog_tooth", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 41, targetSweet: 0, targetSpice: 11, innerRadius: 7, outerRadius: 29, freshness: 76, xpPercent: 100);

            // Fish Broth
            _builder.Create(RecipeType.FishBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("fish_broth")
                .Level(11)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("hamsi", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 29, targetSweet: 0, targetSpice: 0, innerRadius: 10, outerRadius: 24, freshness: 75, xpPercent: 100);

            _builder.Create(RecipeType.ChocolateCookies, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("choco_cookies")
                .Level(12)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("bubble_choc", 4)
                .Component("b_flour", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 8, targetSweet: 51, targetSpice: 5, innerRadius: 6, outerRadius: 28, freshness: 92, xpPercent: 100);

            _builder.Create(RecipeType.KlorslugSurprise, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("slug_surprise")
                .Level(12)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("klorslug_meat", 3)
                .Component("klorslug_tail", 2)
                .Component("klorslug_innards", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 55, targetSweet: 1, targetSpice: 8, innerRadius: 9, outerRadius: 32, freshness: 80, xpPercent: 100);

            // Cooked Sardine
            _builder.Create(RecipeType.CookedSardine, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("cooked_sardine")
                .Level(12)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("sen_sardine", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 23, targetSweet: 0, targetSpice: 10, innerRadius: 11, outerRadius: 25, freshness: 68, xpPercent: 100);

            _builder.Create(RecipeType.AppleJuice, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("apple_juice")
                .Level(13)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("v_apple", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 6, targetSweet: 27, targetSpice: 0, innerRadius: 5, outerRadius: 18, freshness: 68, xpPercent: 100);

            // Ra'Kaznar Special
            _builder.Create(RecipeType.RaKaznarSpecial, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("rakaz_special")
                .Level(13)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("rakaz_shellfish", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 28, targetSweet: 0, targetSpice: 7, innerRadius: 8, outerRadius: 19, freshness: 71, xpPercent: 100);

            _builder.Create(RecipeType.PeaSoup, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("pea_soup")
                .Level(13)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("v_peas", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 19, targetSweet: 15, targetSpice: 0, innerRadius: 9, outerRadius: 20, freshness: 78, xpPercent: 100);

            _builder.Create(RecipeType.RaivorBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("raivor_broth")
                .Level(13)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("raivor_scale", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 22, targetSweet: 6, targetSpice: 11, innerRadius: 11, outerRadius: 24, freshness: 78, xpPercent: 100);

            _builder.Create(RecipeType.SobaNoodles, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("soba_noodles")
                .Level(14)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("b_flour", 4)
                .Component("sweet_butter", 1)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 10, targetSweet: 22, targetSpice: 2, innerRadius: 9, outerRadius: 22, freshness: 78, xpPercent: 100);

            _builder.Create(RecipeType.CairnmogSandwich, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("cairn_sandwich")
                .Level(14)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("cairnmog_meat", 3)
                .Component("b_flour", 2)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 36, targetSweet: 9, targetSpice: 1, innerRadius: 7, outerRadius: 30, freshness: 73, xpPercent: 100);

            // Maringna
            _builder.Create(RecipeType.Maringna, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("maringna")
                .Level(14)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("bast_sweeper", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 27, targetSweet: 0, targetSpice: 6, innerRadius: 10, outerRadius: 21, freshness: 74, xpPercent: 100);

            _builder.Create(RecipeType.NashtahMeatBall, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("nash_mball")
                .Level(15)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("nashtah_meat", 3)
                .Component("nashtah_foot", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 46, targetSweet: 6, targetSpice: 7, innerRadius: 9, outerRadius: 32, freshness: 77, xpPercent: 100);

            // Cooked Mackerel
            _builder.Create(RecipeType.CookedMackerel, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("cooked_mackerel")
                .Level(15)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("mackerel", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 29, targetSweet: 5, targetSpice: 11, innerRadius: 10, outerRadius: 25, freshness: 75, xpPercent: 100);

            _builder.Create(RecipeType.MysteryCookies, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("mystery_cookies")
                .Level(16)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("sugar", 2)
                .Component("b_flour", 1)
                .Component("passion_fruit", 3)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 6, targetSweet: 54, targetSpice: 13, innerRadius: 5, outerRadius: 27, freshness: 84, xpPercent: 100);

            _builder.Create(RecipeType.MandoHerbSoup, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("mando_herbsoup")
                .Level(16)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("herb_m", 3)
                .Component("distilled_water", 1)
                .Component("sugar", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 0, targetSweet: 21, targetSpice: 26, innerRadius: 9, outerRadius: 26, freshness: 79, xpPercent: 100);

            _builder.Create(RecipeType.GreenCurry, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("green_curry")
                .Level(16)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("passion_fruit", 4)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 4, targetSweet: 33, targetSpice: 7, innerRadius: 9, outerRadius: 27, freshness: 80, xpPercent: 100);

            // Greedie Stew
            _builder.Create(RecipeType.GreedieStew, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("greedie_stew")
                .Level(16)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("greedie", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 27, targetSweet: 0, targetSpice: 10, innerRadius: 8, outerRadius: 23, freshness: 70, xpPercent: 100);

            _builder.Create(RecipeType.AppleAuLait, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("apple_aulait")
                .Level(17)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("v_apple", 3)
                .Component("distilled_water", 2)
                .Component("herb_m", 3)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 3, targetSweet: 18, targetSpice: 28, innerRadius: 7, outerRadius: 24, freshness: 90, xpPercent: 100);

            // Blackened Frog
            _builder.Create(RecipeType.BlackenedFrog, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("blackened_frog")
                .Level(17)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("copper_frog", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 26, targetSweet: 0, targetSpice: 13, innerRadius: 7, outerRadius: 19, freshness: 68, xpPercent: 100);

            _builder.Create(RecipeType.RaivorBloodBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("raiv_bloodbroth")
                .Level(18)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("raivor_blood", 3)
                .Component("distilled_water", 1)
                .Component("herb_m", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 24, targetSweet: 10, targetSpice: 25, innerRadius: 11, outerRadius: 34, freshness: 76, xpPercent: 100);

            // Brain Stew
            _builder.Create(RecipeType.BrainStew, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("brain_stew")
                .Level(18)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("yellow_globe", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 29, targetSweet: 3, targetSpice: 5, innerRadius: 9, outerRadius: 19, freshness: 73, xpPercent: 100);

            _builder.Create(RecipeType.NashtahSandwich, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("nash_sandwich")
                .Level(19)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("nashtah_meat", 3)
                .Component("distilled_water", 1)
                .Component("herb_m", 3)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 41, targetSweet: 0, targetSpice: 32, innerRadius: 10, outerRadius: 28, freshness: 80, xpPercent: 100);

            // Cooked Siredon
            _builder.Create(RecipeType.CookedSiredon, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("cooked_siredon")
                .Level(19)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("muddy_siredon", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 23, targetSweet: 0, targetSpice: 3, innerRadius: 7, outerRadius: 19, freshness: 74, xpPercent: 100);

            _builder.Create(RecipeType.NashtahStew, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("nash_stew")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("nash_blood", 2)
                .Component("nashtah_meat", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 37, targetSweet: 0, targetSpice: 12, innerRadius: 12, outerRadius: 34, freshness: 70, xpPercent: 100);

            // Cooked Istavrit
            _builder.Create(RecipeType.CookedIstavrit, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("cooked_istavrit")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("istavrit", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 21, targetSweet: 8, targetSpice: 4, innerRadius: 10, outerRadius: 22, freshness: 69, xpPercent: 100);

            // Cooking Enhancement - Duration II
            _builder.Create(RecipeType.CookingEnhancementDuration2, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_dur2")
                .Level(11)
                .Quantity(1)
                .Component("raivor_meat", 2)
                .Component("raivor_scale", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 31, targetSweet: 0, targetSpice: 6, innerRadius: 9, outerRadius: 20, freshness: 67, xpPercent: 100);

            // Cooking Enhancement - FP II
            _builder.Create(RecipeType.CookingEnhancementFP2, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_fp2")
                .Level(13)
                .Quantity(1)
                .Component("raivor_scale", 2)
                .Component("raivor_meat", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 19, targetSweet: 0, targetSpice: 11, innerRadius: 11, outerRadius: 21, freshness: 69, xpPercent: 100);

            // Cooking Enhancement - FP Regen II
            _builder.Create(RecipeType.CookingEnhancementFPRegen2, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_fpr2")
                .Level(18)
                .Quantity(1)
                .Component("cairnmog_meat", 4)
                .Component("herb_m", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 48, targetSweet: 4, targetSpice: 16, innerRadius: 9, outerRadius: 34, freshness: 77, xpPercent: 100);

            // Cooking Enhancement - HP II
            _builder.Create(RecipeType.CookingEnhancementHP2, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_hp2")
                .Level(14)
                .Quantity(1)
                .Component("cairnmog_meat", 3)
                .Component("passion_fruit", 3)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 34, targetSweet: 19, targetSpice: 9, innerRadius: 9, outerRadius: 35, freshness: 84, xpPercent: 100);

            // Cooking Enhancement - HP Regen II
            _builder.Create(RecipeType.CookingEnhancementHPRegen2, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_hpr2")
                .Level(16)
                .Quantity(1)
                .Component("raivor_blood", 3)
                .Component("herb_m", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 23, targetSweet: 10, targetSpice: 24, innerRadius: 12, outerRadius: 32, freshness: 80, xpPercent: 100);

            // Cooking Enhancement - Combat Readiness II
            _builder.Create(RecipeType.CookingEnhancementCombatReadiness2, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_recast2")
                .Level(18)
                .Quantity(1)
                .Component("nashtah_meat", 3)
                .Component("nashtah_foot", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 48, targetSweet: 0, targetSpice: 11, innerRadius: 12, outerRadius: 33, freshness: 80, xpPercent: 100);

            // Cooking Enhancement - Rest Regen II
            _builder.Create(RecipeType.CookingEnhancementRestRegen2, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_rest2")
                .Level(12)
                .Quantity(1)
                .Component("herb_v", 2)
                .Component("nashtah_meat", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 26, targetSweet: 1, targetSpice: 13, innerRadius: 12, outerRadius: 25, freshness: 73, xpPercent: 100);

            // Cooking Enhancement - STM II
            _builder.Create(RecipeType.CookingEnhancementSTM2, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_stm2")
                .Level(14)
                .Quantity(1)
                .Component("nashtah_foot", 3)
                .Component("v_apple", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 20, targetSweet: 9, targetSpice: 3, innerRadius: 10, outerRadius: 22, freshness: 79, xpPercent: 100);

            // Cooking Enhancement - STM Regen II
            _builder.Create(RecipeType.CookingEnhancementSTMRegen2, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_stmr2")
                .Level(17)
                .Quantity(1)
                .Component("passion_fruit", 4)
                .Component("herb_m", 3)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 9, targetSweet: 31, targetSpice: 28, innerRadius: 12, outerRadius: 29, freshness: 85, xpPercent: 100);

            // Cooking Enhancement - XP Bonus II
            _builder.Create(RecipeType.CookingEnhancementXPBonus2, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_xp2")
                .Level(16)
                .Quantity(1)
                .Component("nashtah_meat", 3)
                .Component("raivor_scale", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 36, targetSweet: 3, targetSpice: 8, innerRadius: 11, outerRadius: 35, freshness: 75, xpPercent: 100);

            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceFire2, "cen_res_fir2", 15, "nashtah_meat", 3, "nashtah_foot", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 45, targetSweet: 5, targetSpice: 15, innerRadius: 9, outerRadius: 35, freshness: 76, xpPercent: 100));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistancePoison2, "cen_res_psn2", 16, "raivor_blood", 3, "herb_m", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 17, targetSweet: 5, targetSpice: 31, innerRadius: 9, outerRadius: 31, freshness: 73, xpPercent: 100));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceElectrical2, "cen_res_elec2", 17, "raivor_scale", 3, "raivor_meat", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 32, targetSweet: 0, targetSpice: 7, innerRadius: 9, outerRadius: 32, freshness: 75, xpPercent: 100));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceIce2, "cen_res_ice2", 18, "passion_fruit", 3, "herb_m", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 7, targetSweet: 30, targetSpice: 26, innerRadius: 11, outerRadius: 28, freshness: 74, xpPercent: 100));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceMind2, "cen_res_mnd2", 15, "raivor_meat", 3, "raivor_scale", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 35, targetSweet: 4, targetSpice: 10, innerRadius: 11, outerRadius: 33, freshness: 77, xpPercent: 100));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceMobility2, "cen_res_mob2", 16, "nashtah_foot", 3, "v_apple", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 18, targetSweet: 18, targetSpice: 4, innerRadius: 8, outerRadius: 24, freshness: 80, xpPercent: 100));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceTrauma2, "cen_res_tra2", 17, "cairnmog_meat", 3, "passion_fruit", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 28, targetSweet: 12, targetSpice: 7, innerRadius: 9, outerRadius: 28, freshness: 78, xpPercent: 100));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceDisruption2, "cen_res_dis2", 18, "cairnmog_meat", 3, "herb_m", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 31, targetSweet: 2, targetSpice: 17, innerRadius: 8, outerRadius: 34, freshness: 79, xpPercent: 100));
        }

        private void Tier3()
        {
            _builder.Create(RecipeType.AradileMeatBall, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("aradile_mball")
                .Level(21)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("aradile_meat", 3)
                .Component("aradile_tail", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 49, targetSweet: 0, targetSpice: 11, innerRadius: 7, outerRadius: 27, freshness: 76, xpPercent: 120);

            _builder.Create(RecipeType.TigerPotPie, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("tiger_potpie")
                .Level(21)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("tiger_meat", 3)
                .Component("q_tiger_paw", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 44, targetSweet: 2, targetSpice: 10, innerRadius: 6, outerRadius: 25, freshness: 79, xpPercent: 120);

            // Cooked Salpa
            _builder.Create(RecipeType.CookedSalpa, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("cooked_salpa")
                .Level(21)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("trans_salpa", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 25, targetSweet: 4, targetSpice: 8, innerRadius: 8, outerRadius: 21, freshness: 71, xpPercent: 120);

            _builder.Create(RecipeType.AcornCookies, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("acorn_cookies")
                .Level(22)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("h_acorn", 4)
                .Component("p_flour", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 21, targetSweet: 28, targetSpice: 0, innerRadius: 4, outerRadius: 21, freshness: 85, xpPercent: 120);

            // Herb Quus
            _builder.Create(RecipeType.HerbQuus, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("herb_quus")
                .Level(22)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("quus", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 32, targetSweet: 0, targetSpice: 8, innerRadius: 8, outerRadius: 23, freshness: 70, xpPercent: 120);

            _builder.Create(RecipeType.PineappleJuice, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("pine_juice")
                .Level(23)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("s_pineapple", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 0, targetSweet: 30, targetSpice: 7, innerRadius: 5, outerRadius: 21, freshness: 75, xpPercent: 120);


            _builder.Create(RecipeType.VegetableSoup, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("veg_soup")
                .Level(23)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("veggie_clump", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 22, targetSweet: 8, targetSpice: 7, innerRadius: 8, outerRadius: 19, freshness: 78, xpPercent: 120);

            _builder.Create(RecipeType.AradileBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("ara_broth")
                .Level(23)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("aradile_innards", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 27, targetSweet: 0, targetSpice: 9, innerRadius: 7, outerRadius: 20, freshness: 73, xpPercent: 120);

            // Carp Sushi
            _builder.Create(RecipeType.CarpSushi, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("carp_sushi")
                .Level(23)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("forest_carp", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 25, targetSweet: 0, targetSpice: 0, innerRadius: 5, outerRadius: 23, freshness: 68, xpPercent: 120);

            _builder.Create(RecipeType.RamenNoodles, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("ramen_noodles")
                .Level(24)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("p_flour", 4)
                .Component("c_butter", 1)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 17, targetSweet: 24, targetSpice: 2, innerRadius: 10, outerRadius: 18, freshness: 83, xpPercent: 120);

            _builder.Create(RecipeType.AradileSandwich, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("ara_sandwich")
                .Level(24)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("aradile_meat", 3)
                .Component("p_flour", 2)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 38, targetSweet: 4, targetSpice: 8, innerRadius: 6, outerRadius: 25, freshness: 72, xpPercent: 120);

            // Goldfish Bowl
            _builder.Create(RecipeType.GoldfishBowl, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("goldfish_bowl")
                .Level(24)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("tiny_goldfish", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 25, targetSweet: 7, targetSpice: 8, innerRadius: 7, outerRadius: 21, freshness: 67, xpPercent: 120);

            _builder.Create(RecipeType.ByyskMeatBall, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("byysk_mball")
                .Level(25)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("byysk_meat", 3)
                .Component("byysk_tail", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 50, targetSweet: 3, targetSpice: 9, innerRadius: 7, outerRadius: 24, freshness: 82, xpPercent: 120);

            // Blackened Hoptoad
            _builder.Create(RecipeType.BlackenedHoptoad, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("b_hoptoad")
                .Level(25)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("hoptoad", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 29, targetSweet: 1, targetSpice: 3, innerRadius: 9, outerRadius: 27, freshness: 75, xpPercent: 120);

            _builder.Create(RecipeType.CinnaCookies, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("cinna_cookies")
                .Level(26)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("sugar", 2)
                .Component("p_flour", 1)
                .Component("h_acorn", 3)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 11, targetSweet: 31, targetSpice: 0, innerRadius: 7, outerRadius: 18, freshness: 82, xpPercent: 120);

            _builder.Create(RecipeType.MonCalaHerbSoup, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("moncal_hsoup")
                .Level(26)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("herb_c", 3)
                .Component("distilled_water", 1)
                .Component("sugar", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 21, targetSweet: 25, targetSpice: 9, innerRadius: 7, outerRadius: 22, freshness: 81, xpPercent: 120);

            _builder.Create(RecipeType.RedCurry, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("red_curry")
                .Level(26)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("h_acorn", 2)
                .Component("distilled_water", 1)
                .Component("veggie_clump", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 24, targetSweet: 7, targetSpice: 0, innerRadius: 10, outerRadius: 20, freshness: 82, xpPercent: 120);

            // Smoked Salmon
            _builder.Create(RecipeType.SmokedSalmon, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("smoked_salmon")
                .Level(26)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("cheval_salmon", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 22, targetSweet: 5, targetSpice: 4, innerRadius: 8, outerRadius: 21, freshness: 71, xpPercent: 120);

            _builder.Create(RecipeType.PineappleAuLait, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("pine_aulait")
                .Level(27)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("s_pineapple", 3)
                .Component("distilled_water", 2)
                .Component("herb_c", 3)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 14, targetSweet: 28, targetSpice: 22, innerRadius: 7, outerRadius: 22, freshness: 90, xpPercent: 120);

            // Deepwater Broth
            _builder.Create(RecipeType.DeepwaterBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("deep_broth")
                .Level(27)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("yorchete", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 22, targetSweet: 0, targetSpice: 10, innerRadius: 7, outerRadius: 18, freshness: 69, xpPercent: 120);

            _builder.Create(RecipeType.AmphiHydrusBloodBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("amphi_bbroth")
                .Level(28)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("amphi_blood", 3)
                .Component("distilled_water", 1)
                .Component("herb_c", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 27, targetSweet: 0, targetSpice: 26, innerRadius: 7, outerRadius: 31, freshness: 82, xpPercent: 120);

            // White Peeled Lobster
            _builder.Create(RecipeType.WhitePeeledLobster, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("white_p_lobster")
                .Level(28)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("white_lobster", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 31, targetSweet: 0, targetSpice: 11, innerRadius: 7, outerRadius: 23, freshness: 75, xpPercent: 120);

            _builder.Create(RecipeType.SnakeSandwich, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("snake_sandwich")
                .Level(29)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("viper_meat", 3)
                .Component("distilled_water", 1)
                .Component("herb_c", 3)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 42, targetSweet: 0, targetSpice: 21, innerRadius: 5, outerRadius: 25, freshness: 80, xpPercent: 120);

            // Fat Greedie Stew
            _builder.Create(RecipeType.FatGreedieStew, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("fat_greedie_stew")
                .Level(29)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("fat_greedie", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 27, targetSweet: 1, targetSpice: 5, innerRadius: 9, outerRadius: 26, freshness: 68, xpPercent: 120);

            _builder.Create(RecipeType.SnakeStew, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("snake_stew")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("viper_guts", 2)
                .Component("viper_meat", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 37, targetSweet: 0, targetSpice: 13, innerRadius: 7, outerRadius: 31, freshness: 75, xpPercent: 120);

            // Idol Sushi
            _builder.Create(RecipeType.IdolSushi, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("idol_sushi")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("moorish_idol", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 23, targetSweet: 2, targetSpice: 10, innerRadius: 8, outerRadius: 25, freshness: 74, xpPercent: 120);

            // Cooking Enhancement - Duration III
            _builder.Create(RecipeType.CookingEnhancementDuration3, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_dur3")
                .Level(21)
                .Quantity(1)
                .Component("aradile_meat", 2)
                .Component("aradile_tail", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 24, targetSweet: 0, targetSpice: 5, innerRadius: 11, outerRadius: 20, freshness: 64, xpPercent: 120);

            // Cooking Enhancement - FP III
            _builder.Create(RecipeType.CookingEnhancementFP3, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_fp3")
                .Level(23)
                .Quantity(1)
                .Component("aradile_tail", 2)
                .Component("aradile_meat", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 21, targetSweet: 0, targetSpice: 12, innerRadius: 10, outerRadius: 22, freshness: 68, xpPercent: 120);

            // Cooking Enhancement - FP Regen III
            _builder.Create(RecipeType.CookingEnhancementFPRegen3, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_fpr3")
                .Level(28)
                .Quantity(1)
                .Component("q_tiger_paw", 4)
                .Component("herb_c", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 36, targetSweet: 0, targetSpice: 21, innerRadius: 8, outerRadius: 26, freshness: 82, xpPercent: 120);

            // Cooking Enhancement - HP III
            _builder.Create(RecipeType.CookingEnhancementHP3, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_hp3")
                .Level(24)
                .Quantity(1)
                .Component("aradile_meat", 3)
                .Component("viper_guts", 3)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 54, targetSweet: 1, targetSpice: 17, innerRadius: 10, outerRadius: 33, freshness: 78, xpPercent: 120);

            // Cooking Enhancement - HP Regen III
            _builder.Create(RecipeType.CookingEnhancementHPRegen3, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_hpr3")
                .Level(26)
                .Quantity(1)
                .Component("byysk_meat", 3)
                .Component("herb_c", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 43, targetSweet: 0, targetSpice: 12, innerRadius: 7, outerRadius: 30, freshness: 72, xpPercent: 120);

            // Cooking Enhancement - Combat Readiness III
            _builder.Create(RecipeType.CookingEnhancementCombatReadiness3, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_recast3")
                .Level(28)
                .Quantity(1)
                .Component("amphi_blood", 3)
                .Component("byysk_tail", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 40, targetSweet: 0, targetSpice: 18, innerRadius: 10, outerRadius: 27, freshness: 76, xpPercent: 120);

            // Cooking Enhancement - Rest Regen III
            _builder.Create(RecipeType.CookingEnhancementRestRegen3, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_rest3")
                .Level(22)
                .Quantity(1)
                .Component("herb_c", 2)
                .Component("aradile_innards", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 33, targetSweet: 2, targetSpice: 11, innerRadius: 11, outerRadius: 23, freshness: 67, xpPercent: 120);

            // Cooking Enhancement - STM III
            _builder.Create(RecipeType.CookingEnhancementSTM3, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_stm3")
                .Level(24)
                .Quantity(1)
                .Component("viper_meat", 3)
                .Component("byysk_tail", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 46, targetSweet: 0, targetSpice: 3, innerRadius: 7, outerRadius: 35, freshness: 76, xpPercent: 120);

            // Cooking Enhancement - STM Regen III
            _builder.Create(RecipeType.CookingEnhancementSTMRegen3, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_stmr3")
                .Level(27)
                .Quantity(1)
                .Component("s_pineapple", 4)
                .Component("herb_c", 3)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 22, targetSweet: 33, targetSpice: 16, innerRadius: 10, outerRadius: 26, freshness: 82, xpPercent: 120);

            // Cooking Enhancement - XP Bonus III
            _builder.Create(RecipeType.CookingEnhancementXPBonus3, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_xp3")
                .Level(26)
                .Quantity(1)
                .Component("viper_guts", 3)
                .Component("aradile_tail", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 37, targetSweet: 0, targetSpice: 14, innerRadius: 10, outerRadius: 34, freshness: 78, xpPercent: 120);

            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceFire3, "cen_res_fir3", 25, "amphi_blood", 3, "byysk_tail", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 34, targetSweet: 2, targetSpice: 15, innerRadius: 11, outerRadius: 34, freshness: 78, xpPercent: 120));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistancePoison3, "cen_res_psn3", 26, "byysk_meat", 3, "herb_c", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 35, targetSweet: 5, targetSpice: 8, innerRadius: 9, outerRadius: 29, freshness: 79, xpPercent: 120));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceElectrical3, "cen_res_elec3", 27, "aradile_tail", 3, "aradile_meat", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 40, targetSweet: 0, targetSpice: 9, innerRadius: 7, outerRadius: 34, freshness: 79, xpPercent: 120));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceIce3, "cen_res_ice3", 28, "s_pineapple", 3, "herb_c", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 10, targetSweet: 22, targetSpice: 13, innerRadius: 9, outerRadius: 23, freshness: 80, xpPercent: 120));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceMind3, "cen_res_mnd3", 25, "aradile_meat", 3, "aradile_tail", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 38, targetSweet: 0, targetSpice: 11, innerRadius: 10, outerRadius: 29, freshness: 74, xpPercent: 120));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceMobility3, "cen_res_mob3", 26, "viper_meat", 3, "byysk_tail", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 48, targetSweet: 4, targetSpice: 3, innerRadius: 10, outerRadius: 32, freshness: 72, xpPercent: 120));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceTrauma3, "cen_res_tra3", 27, "viper_guts", 3, "aradile_tail", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 40, targetSweet: 0, targetSpice: 19, innerRadius: 7, outerRadius: 34, freshness: 74, xpPercent: 120));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceDisruption3, "cen_res_dis3", 28, "q_tiger_paw", 3, "herb_c", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 35, targetSweet: 0, targetSpice: 10, innerRadius: 11, outerRadius: 32, freshness: 73, xpPercent: 120));
        }

        private void Tier4()
        {
            _builder.Create(RecipeType.WompRatMeatBall, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("womp_mball")
                .Level(31)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("wompratmeat", 3)
                .Component("womp_innards", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 53, targetSweet: 0, targetSpice: 14, innerRadius: 7, outerRadius: 26, freshness: 77, xpPercent: 100);

            _builder.Create(RecipeType.SandDemonPotPie, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("sanddem_potpie")
                .Level(31)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("sanddemon_meat", 3)
                .Component("sand_demon_leg", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 44, targetSweet: 4, targetSpice: 9, innerRadius: 4, outerRadius: 29, freshness: 76, xpPercent: 100);

            // Gurnard Stew
            _builder.Create(RecipeType.GurnardStew, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("gurnard_stew")
                .Level(31)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("gurnard", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 26, targetSweet: 6, targetSpice: 6, innerRadius: 8, outerRadius: 22, freshness: 74, xpPercent: 100);

            _builder.Create(RecipeType.GingerCookies, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("ging_cookies")
                .Level(32)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("ginger", 4)
                .Component("r_flour", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 0, targetSweet: 24, targetSpice: 32, innerRadius: 4, outerRadius: 24, freshness: 88, xpPercent: 100);

            // Baked Nebimonite
            _builder.Create(RecipeType.BakedNebimonite, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("baked_nebimon")
                .Level(32)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("nebimonite", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 29, targetSweet: 0, targetSpice: 9, innerRadius: 6, outerRadius: 25, freshness: 73, xpPercent: 100);

            _builder.Create(RecipeType.MelonJuice, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("melon_juice")
                .Level(33)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("melon", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 0, targetSweet: 26, targetSpice: 0, innerRadius: 4, outerRadius: 22, freshness: 69, xpPercent: 100);


            _builder.Create(RecipeType.MushroomSoup, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("mush_soup")
                .Level(33)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("mushroom", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 26, targetSweet: 0, targetSpice: 0, innerRadius: 7, outerRadius: 18, freshness: 72, xpPercent: 100);

            _builder.Create(RecipeType.WompRatBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("womp_broth")
                .Level(33)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("womp_innards", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 30, targetSweet: 0, targetSpice: 14, innerRadius: 7, outerRadius: 20, freshness: 73, xpPercent: 100);

            // Tricolored Sushi
            _builder.Create(RecipeType.TricoloredSushi, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("tricolored_sushi")
                .Level(33)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("tricolored_carp", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 31, targetSweet: 1, targetSpice: 0, innerRadius: 7, outerRadius: 21, freshness: 74, xpPercent: 100);

            _builder.Create(RecipeType.SoyRamen, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("soy_ramen")
                .Level(34)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("r_flour", 4)
                .Component("plant_butter", 1)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 12, targetSweet: 20, targetSpice: 4, innerRadius: 8, outerRadius: 23, freshness: 82, xpPercent: 100);

            _builder.Create(RecipeType.SurpriseSandwich, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("surprise_sandwic")
                .Level(34)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("sanddemon_meat", 3)
                .Component("r_flour", 2)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 34, targetSweet: 2, targetSpice: 0, innerRadius: 4, outerRadius: 28, freshness: 78, xpPercent: 100);

            _builder.Create(RecipeType.DathomirPie, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("dathomir_pie")
                .Level(34)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("spider_leg", 3)
                .Component("spider_guts", 2)
                .Component("spider_thread", 1)
                .Component("r_flour", 2)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 47, targetSweet: 2, targetSpice: 17, innerRadius: 5, outerRadius: 27, freshness: 87, xpPercent: 100);

            // Fish & Chips
            _builder.Create(RecipeType.FishChips, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("fish_n_chips")
                .Level(34)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("blindfish", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 28, targetSweet: 0, targetSpice: 11, innerRadius: 5, outerRadius: 19, freshness: 68, xpPercent: 100);

            _builder.Create(RecipeType.TuskenMeatBall, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("tusken_mball")
                .Level(35)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("tusken_meat", 3)
                .Component("tusken_bones", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 52, targetSweet: 3, targetSpice: 11, innerRadius: 5, outerRadius: 28, freshness: 82, xpPercent: 100);

            // Roast Pipira
            _builder.Create(RecipeType.RoastPipira, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("roast_pipira")
                .Level(35)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("pipira", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 21, targetSweet: 1, targetSpice: 4, innerRadius: 6, outerRadius: 20, freshness: 73, xpPercent: 100);

            _builder.Create(RecipeType.WalnutCookies, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("walnut_cookies")
                .Level(36)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("sugar", 2)
                .Component("r_flour", 1)
                .Component("walnut", 3)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 17, targetSweet: 36, targetSpice: 0, innerRadius: 5, outerRadius: 18, freshness: 81, xpPercent: 100);

            _builder.Create(RecipeType.DesertHerbSoup, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("des_herbsoup")
                .Level(36)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("herb_t", 3)
                .Component("distilled_water", 1)
                .Component("sugar", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 9, targetSweet: 19, targetSpice: 30, innerRadius: 10, outerRadius: 25, freshness: 80, xpPercent: 100);

            _builder.Create(RecipeType.YellowCurry, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("yellow_curry")
                .Level(36)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("ginger", 2)
                .Component("distilled_water", 1)
                .Component("cornucopia", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 9, targetSweet: 16, targetSpice: 28, innerRadius: 9, outerRadius: 22, freshness: 72, xpPercent: 100);

            // Sliced Cod
            _builder.Create(RecipeType.SlicedCod, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("sliced_cod")
                .Level(36)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("tiger_cod", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 22, targetSweet: 1, targetSpice: 8, innerRadius: 5, outerRadius: 19, freshness: 73, xpPercent: 100);

            _builder.Create(RecipeType.MelonAuLait, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("melon_aulait")
                .Level(37)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("melon", 3)
                .Component("distilled_water", 2)
                .Component("herb_t", 3)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 7, targetSweet: 30, targetSpice: 31, innerRadius: 4, outerRadius: 18, freshness: 90, xpPercent: 100);

            // Bonefish Broth
            _builder.Create(RecipeType.BonefishBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("bonefish_broth")
                .Level(37)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("bonefish", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 24, targetSweet: 5, targetSpice: 0, innerRadius: 10, outerRadius: 19, freshness: 68, xpPercent: 100);

            _builder.Create(RecipeType.TuskenBloodBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("tusk_b_broth")
                .Level(38)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("tusken_blood", 3)
                .Component("distilled_water", 1)
                .Component("herb_t", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 24, targetSweet: 8, targetSpice: 24, innerRadius: 7, outerRadius: 31, freshness: 77, xpPercent: 100);

            // Steamed Catfish
            _builder.Create(RecipeType.SteamedCatfish, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("steamed_catfish")
                .Level(38)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("giant_catfish", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 31, targetSweet: 0, targetSpice: 3, innerRadius: 7, outerRadius: 26, freshness: 74, xpPercent: 100);

            _builder.Create(RecipeType.SandDemonSandwich, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("dem_sandwich")
                .Level(39)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("sanddemon_meat", 3)
                .Component("distilled_water", 1)
                .Component("herb_t", 3)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 41, targetSweet: 2, targetSpice: 27, innerRadius: 5, outerRadius: 18, freshness: 88, xpPercent: 100);

            // Cooked Yayinbaligi
            _builder.Create(RecipeType.CookedYayinbaligi, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("cooked_yayin")
                .Level(39)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("yayinbaligi", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 32, targetSweet: 5, targetSpice: 4, innerRadius: 4, outerRadius: 18, freshness: 74, xpPercent: 100);

            _builder.Create(RecipeType.SandDemonStew, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("demon_stew")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("sand_demon_leg", 2)
                .Component("sanddemon_meat", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 40, targetSweet: 6, targetSpice: 4, innerRadius: 5, outerRadius: 22, freshness: 73, xpPercent: 100);

            // Dead Stew
            _builder.Create(RecipeType.DeadStew, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("dead_stew")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("deadmoiselle", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 29, targetSweet: 7, targetSpice: 1, innerRadius: 9, outerRadius: 23, freshness: 67, xpPercent: 100);

            // Cooking Enhancement - Duration IV
            _builder.Create(RecipeType.CookingEnhancementDuration4, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_dur4")
                .Level(31)
                .Quantity(1)
                .Component("wompratmeat", 2)
                .Component("womp_innards", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 27, targetSweet: 3, targetSpice: 7, innerRadius: 7, outerRadius: 25, freshness: 70, xpPercent: 100);

            // Cooking Enhancement - FP IV
            _builder.Create(RecipeType.CookingEnhancementFP4, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_fp4")
                .Level(33)
                .Quantity(1)
                .Component("womp_innards", 2)
                .Component("wompratmeat", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 24, targetSweet: 0, targetSpice: 13, innerRadius: 7, outerRadius: 22, freshness: 71, xpPercent: 100);

            // Cooking Enhancement - FP Regen IV
            _builder.Create(RecipeType.CookingEnhancementFPRegen4, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_fpr4")
                .Level(38)
                .Quantity(1)
                .Component("tusken_bones", 4)
                .Component("herb_t", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 30, targetSweet: 0, targetSpice: 27, innerRadius: 6, outerRadius: 30, freshness: 79, xpPercent: 100);

            // Cooking Enhancement - HP IV
            _builder.Create(RecipeType.CookingEnhancementHP4, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_hp4")
                .Level(34)
                .Quantity(1)
                .Component("sanddemon_meat", 3)
                .Component("plant_butter", 3)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 37, targetSweet: 3, targetSpice: 8, innerRadius: 10, outerRadius: 25, freshness: 81, xpPercent: 100);

            // Cooking Enhancement - HP Regen IV
            _builder.Create(RecipeType.CookingEnhancementHPRegen4, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_hpr4")
                .Level(36)
                .Quantity(1)
                .Component("sanddemon_meat", 3)
                .Component("herb_t", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 40, targetSweet: 5, targetSpice: 16, innerRadius: 8, outerRadius: 24, freshness: 72, xpPercent: 100);

            // Cooking Enhancement - Combat Readiness IV
            _builder.Create(RecipeType.CookingEnhancementCombatReadiness4, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_recast4")
                .Level(38)
                .Quantity(1)
                .Component("sand_demon_leg", 3)
                .Component("tusken_bones", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 32, targetSweet: 0, targetSpice: 17, innerRadius: 6, outerRadius: 31, freshness: 77, xpPercent: 100);

            // Cooking Enhancement - Rest Regen IV
            _builder.Create(RecipeType.CookingEnhancementRestRegen4, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_rest4")
                .Level(32)
                .Quantity(1)
                .Component("herb_t", 2)
                .Component("mushroom", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 13, targetSweet: 3, targetSpice: 19, innerRadius: 7, outerRadius: 23, freshness: 71, xpPercent: 100);

            // Cooking Enhancement - STM IV
            _builder.Create(RecipeType.CookingEnhancementSTM4, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_stm4")
                .Level(34)
                .Quantity(1)
                .Component("sand_demon_leg", 3)
                .Component("sanddemon_meat", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 39, targetSweet: 5, targetSpice: 8, innerRadius: 7, outerRadius: 25, freshness: 80, xpPercent: 100);

            // Cooking Enhancement - STM Regen IV
            _builder.Create(RecipeType.CookingEnhancementSTMRegen4, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_stmr4")
                .Level(37)
                .Quantity(1)
                .Component("melon", 4)
                .Component("herb_t", 3)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 13, targetSweet: 40, targetSpice: 28, innerRadius: 8, outerRadius: 27, freshness: 84, xpPercent: 100);

            // Cooking Enhancement - XP Bonus IV
            _builder.Create(RecipeType.CookingEnhancementXPBonus4, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_xp4")
                .Level(36)
                .Quantity(1)
                .Component("gimp_blood", 3)
                .Component("kinrath_limb", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 37, targetSweet: 3, targetSpice: 14, innerRadius: 10, outerRadius: 23, freshness: 80, xpPercent: 100);

            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceFire4, "cen_res_fir4", 35, "sand_demon_leg", 3, "tusken_bones", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 37, targetSweet: 5, targetSpice: 16, innerRadius: 8, outerRadius: 30, freshness: 76, xpPercent: 100));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistancePoison4, "cen_res_psn4", 36, "sanddemon_meat", 3, "herb_t", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 38, targetSweet: 6, targetSpice: 19, innerRadius: 8, outerRadius: 26, freshness: 73, xpPercent: 100));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceElectrical4, "cen_res_elec4", 37, "tusken_bones", 3, "wompratmeat", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 43, targetSweet: 1, targetSpice: 14, innerRadius: 9, outerRadius: 31, freshness: 78, xpPercent: 100));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceIce4, "cen_res_ice4", 38, "melon", 3, "herb_t", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 4, targetSweet: 27, targetSpice: 19, innerRadius: 9, outerRadius: 23, freshness: 74, xpPercent: 100));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceMind4, "cen_res_mnd4", 35, "wompratmeat", 3, "womp_innards", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 52, targetSweet: 4, targetSpice: 9, innerRadius: 10, outerRadius: 29, freshness: 80, xpPercent: 100));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceMobility4, "cen_res_mob4", 36, "sand_demon_leg", 3, "sanddemon_meat", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 36, targetSweet: 1, targetSpice: 15, innerRadius: 7, outerRadius: 28, freshness: 73, xpPercent: 100));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceTrauma4, "cen_res_tra4", 37, "sanddemon_meat", 3, "plant_butter", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 38, targetSweet: 10, targetSpice: 12, innerRadius: 8, outerRadius: 26, freshness: 76, xpPercent: 100));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceDisruption4, "cen_res_dis4", 38, "tusken_bones", 3, "herb_t", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 23, targetSweet: 0, targetSpice: 21, innerRadius: 7, outerRadius: 25, freshness: 78, xpPercent: 100));
        }

        private void Tier5()
        {
            _builder.Create(RecipeType.WildMeatBall, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("wild_mball")
                .Level(41)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("wild_meat", 3)
                .Component("wild_innards", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 45, targetSweet: 0, targetSpice: 9, innerRadius: 4, outerRadius: 18, freshness: 82, xpPercent: 100);

            _builder.Create(RecipeType.WildPotPie, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("wild_potpie")
                .Level(41)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("wild_meat", 3)
                .Component("wild_leg", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 41, targetSweet: 0, targetSpice: 7, innerRadius: 4, outerRadius: 24, freshness: 76, xpPercent: 100);

            // Long Ling Lung
            _builder.Create(RecipeType.LongLingLung, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("long_ling_lung")
                .Level(41)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("lungfish", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 31, targetSweet: 0, targetSpice: 2, innerRadius: 4, outerRadius: 21, freshness: 68, xpPercent: 100);

            _builder.Create(RecipeType.WildCookies, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("wild_cookies")
                .Level(42)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("turnip", 4)
                .Component("bread_flour", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 20, targetSweet: 18, targetSpice: 0, innerRadius: 4, outerRadius: 19, freshness: 88, xpPercent: 100);

            // Bass Meuniere
            _builder.Create(RecipeType.BassMeuniere, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("bass_meuniere")
                .Level(42)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("dark_bass", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 29, targetSweet: 8, targetSpice: 8, innerRadius: 7, outerRadius: 20, freshness: 73, xpPercent: 100);

            // Munch Fungus Bread
            _builder.Create(RecipeType.MunchFungusBread, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("munch_fungusb")
                .Level(42)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("mushroom", 3)
                .Component("b_flour", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 31, targetSweet: 6, targetSpice: 1, innerRadius: 5, outerRadius: 25, freshness: 80, xpPercent: 100);

            _builder.Create(RecipeType.TomatoJuice, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("tomato_juice")
                .Level(43)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("tomato", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 20, targetSweet: 12, targetSpice: 4, innerRadius: 4, outerRadius: 21, freshness: 72, xpPercent: 100);

            _builder.Create(RecipeType.MisoSoup, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("miso_soup")
                .Level(43)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("tofu", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 18, targetSweet: 14, targetSpice: 4, innerRadius: 7, outerRadius: 23, freshness: 77, xpPercent: 100);

            _builder.Create(RecipeType.WildBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("wild_broth")
                .Level(43)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("wild_innards", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 24, targetSweet: 0, targetSpice: 17, innerRadius: 8, outerRadius: 22, freshness: 75, xpPercent: 100);

            // Crystal Sushi
            _builder.Create(RecipeType.CrystalSushi, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("crystal_sushi")
                .Level(43)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("crystal_bass", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 30, targetSweet: 6, targetSpice: 11, innerRadius: 6, outerRadius: 22, freshness: 69, xpPercent: 100);

            _builder.Create(RecipeType.MisoRamen, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("miso_ramen")
                .Level(44)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("bread_flour", 4)
                .Component("cultured_butter", 1)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 18, targetSweet: 14, targetSpice: 0, innerRadius: 8, outerRadius: 18, freshness: 83, xpPercent: 100);

            _builder.Create(RecipeType.WildSandwich, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("wild_sandwich")
                .Level(44)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("wild_meat", 3)
                .Component("bread_flour", 2)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 32, targetSweet: 6, targetSpice: 7, innerRadius: 4, outerRadius: 22, freshness: 79, xpPercent: 100);

            // Eelectric Soup
            _builder.Create(RecipeType.EelectricSoup, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("eelectric_soup")
                .Level(44)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("ogre_eel", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 27, targetSweet: 5, targetSpice: 7, innerRadius: 9, outerRadius: 24, freshness: 70, xpPercent: 100);

            _builder.Create(RecipeType.GrandioseMeatBall, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("grand_mball")
                .Level(45)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("wild_meat", 3)
                .Component("wild_leg", 2)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 52, targetSweet: 0, targetSpice: 8, innerRadius: 4, outerRadius: 18, freshness: 78, xpPercent: 100);

            // Shining Stew
            _builder.Create(RecipeType.ShiningStew, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("shining_stew")
                .Level(45)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("shining_trout", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 26, targetSweet: 5, targetSpice: 3, innerRadius: 5, outerRadius: 18, freshness: 69, xpPercent: 100);

            _builder.Create(RecipeType.WizardCookies, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("wizard_cookies")
                .Level(46)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("sugar", 2)
                .Component("bread_flour", 1)
                .Component("tomato", 3)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 16, targetSweet: 28, targetSpice: 7, innerRadius: 4, outerRadius: 18, freshness: 79, xpPercent: 100);

            _builder.Create(RecipeType.DathHerbSoup, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("dath_hsoup")
                .Level(46)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("herb_x", 3)
                .Component("distilled_water", 1)
                .Component("sugar", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 7, targetSweet: 17, targetSpice: 26, innerRadius: 5, outerRadius: 27, freshness: 83, xpPercent: 100);

            _builder.Create(RecipeType.WildCurry, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("wild_curry")
                .Level(46)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("tofu", 2)
                .Component("distilled_water", 1)
                .Component("dried_bonito", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 26, targetSweet: 6, targetSpice: 4, innerRadius: 5, outerRadius: 23, freshness: 79, xpPercent: 100);

            // Popper Bowl
            _builder.Create(RecipeType.PopperBowl, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("popper_bowl")
                .Level(46)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("blowfish", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 29, targetSweet: 0, targetSpice: 4, innerRadius: 7, outerRadius: 22, freshness: 69, xpPercent: 100);

            _builder.Create(RecipeType.TomatoAuLait, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("tomato_aulait")
                .Level(47)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("tomato", 3)
                .Component("distilled_water", 2)
                .Component("herb_x", 3)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 16, targetSweet: 8, targetSpice: 30, innerRadius: 4, outerRadius: 18, freshness: 91, xpPercent: 100);

            // Pickled Herring
            _builder.Create(RecipeType.PickledHerring, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("pickled_herring")
                .Level(47)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("nosteau_herring", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 31, targetSweet: 1, targetSpice: 11, innerRadius: 4, outerRadius: 21, freshness: 68, xpPercent: 100);

            _builder.Create(RecipeType.WildBloodBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("wild_bbroth")
                .Level(48)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("wild_blood", 3)
                .Component("distilled_water", 1)
                .Component("herb_x", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 17, targetSweet: 0, targetSpice: 27, innerRadius: 5, outerRadius: 28, freshness: 84, xpPercent: 100);

            // Zoni Broth
            _builder.Create(RecipeType.ZoniBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("zoni_broth")
                .Level(48)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("lakerda", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 29, targetSweet: 0, targetSpice: 11, innerRadius: 6, outerRadius: 25, freshness: 68, xpPercent: 100);

            _builder.Create(RecipeType.GrandioseSandwich, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("grand_sandwich")
                .Level(49)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("wild_meat", 3)
                .Component("distilled_water", 1)
                .Component("herb_x", 3)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 36, targetSweet: 0, targetSpice: 29, innerRadius: 4, outerRadius: 19, freshness: 85, xpPercent: 100);

            // Sea Bass Croute
            _builder.Create(RecipeType.SeaBassCroute, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("sea_bass_croute")
                .Level(49)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("zafmlug_bass", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 31, targetSweet: 8, targetSpice: 0, innerRadius: 5, outerRadius: 23, freshness: 73, xpPercent: 100);

            _builder.Create(RecipeType.WildStew, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("wild_stew")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("wild_leg", 2)
                .Component("wild_meat", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 36, targetSweet: 0, targetSpice: 14, innerRadius: 6, outerRadius: 21, freshness: 69, xpPercent: 100);

            // Shimmering Broth
            _builder.Create(RecipeType.ShimmeringBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("shimm_broth")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("ruddy_seema", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 25, targetSweet: 0, targetSpice: 6, innerRadius: 7, outerRadius: 20, freshness: 68, xpPercent: 100);

            // Rancid Broth
            _builder.Create(RecipeType.RancidBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("rancid_broth")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("frigorifish", 3)
                .Component("distilled_water", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 27, targetSweet: 1, targetSpice: 8, innerRadius: 5, outerRadius: 18, freshness: 70, xpPercent: 100);

            // Bream Sushi
            _builder.Create(RecipeType.BreamSushi, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("bream_sushi")
                .Level(52)
                .Quantity(1)
                .ResearchCostModifier(0.2f)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("mercanbaligi", 6)
                .Component("distilled_water", 2)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 51, targetSweet: 9, targetSpice: 14, innerRadius: 6, outerRadius: 18, freshness: 76, xpPercent: 100);

            // Octopus Sushi
            _builder.Create(RecipeType.OctopusSushi, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("octo_sushi")
                .Level(52)
                .Quantity(1)
                .ResearchCostModifier(0.2f)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("nashmau", 6)
                .Component("distilled_water", 2)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 58, targetSweet: 5, targetSpice: 14, innerRadius: 4, outerRadius: 18, freshness: 78, xpPercent: 100);

            // Ikran Sushi
            _builder.Create(RecipeType.IkranSushi, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("ikran_sushi")
                .Level(52)
                .Quantity(1)
                .ResearchCostModifier(0.2f)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("rhinochimera", 6)
                .Component("distilled_water", 2)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 59, targetSweet: 7, targetSpice: 11, innerRadius: 4, outerRadius: 18, freshness: 80, xpPercent: 100);

            // Wild Sushi
            _builder.Create(RecipeType.WildSushi, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("wild_sushi")
                .Level(52)
                .Quantity(1)
                .ResearchCostModifier(0.2f)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("mhaura", 6)
                .Component("distilled_water", 2)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 51, targetSweet: 4, targetSpice: 4, innerRadius: 6, outerRadius: 18, freshness: 76, xpPercent: 100);

            // Tentacle Sushi
            _builder.Create(RecipeType.TentacleSushi, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("tent_sushi")
                .Level(52)
                .Quantity(1)
                .ResearchCostModifier(0.2f)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("zitah", 6)
                .Component("distilled_water", 2)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 58, targetSweet: 1, targetSpice: 7, innerRadius: 6, outerRadius: 18, freshness: 79, xpPercent: 100);

            // Dorado Sushi
            _builder.Create(RecipeType.DoradoSushi, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("dorado_sushi")
                .Level(52)
                .Quantity(1)
                .ResearchCostModifier(0.2f)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("alzabi", 6)
                .Component("distilled_water", 2)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 48, targetSweet: 6, targetSpice: 5, innerRadius: 5, outerRadius: 20, freshness: 76, xpPercent: 100);

            // Dantooine Flapjacks
            _builder.Create(RecipeType.DantooineFlapJack, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("dan_flapjack")
                .Level(52)
                .Quantity(1)
                .ResearchCostModifier(0.2f)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("milk", 6)
                .Component("b_flour", 3)
                .Component("yotbean", 1)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 16, targetSweet: 40, targetSpice: 3, innerRadius: 4, outerRadius: 18, freshness: 92, xpPercent: 100);

            // Dantooine Carrotcake
            _builder.Create(RecipeType.DantooineCarrotCake, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("dan_carrotcake")
                .Level(52)
                .Quantity(1)
                .ResearchCostModifier(0.2f)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("milk", 6)
                .Component("carrot", 3)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 12, targetSweet: 52, targetSpice: 2, innerRadius: 4, outerRadius: 19, freshness: 80, xpPercent: 100);

            // Krafter's Kebab
            _builder.Create(RecipeType.KraftersKebab, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("krafters_kebab")
                .Level(52)
                .Quantity(1)
                .ResearchCostModifier(0.2f)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("thune_meat", 10)
                .Component("thune_blood", 2)
                .Component("cultured_butter", 3)
                .Component("ref_jasioclase", 1)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 100, targetSweet: 11, targetSpice: 36, innerRadius: 4, outerRadius: 18, freshness: 98, xpPercent: 100);

            // Forbidden Gymbo
            _builder.Create(RecipeType.ForbiddenGumbo, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("forbid_gumbo")
                .Level(52)
                .Quantity(1)
                .ResearchCostModifier(0.2f)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("froglegs", 10)
                .Component("frogguts", 2)
                .Component("cultured_butter", 3)
                .Component("thune_blood", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 98, targetSweet: 2, targetSpice: 37, innerRadius: 7, outerRadius: 20, freshness: 96, xpPercent: 100);

            // Nal Hutta Fizz
            _builder.Create(RecipeType.NalHuttaFizz, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("nalhutta_fizz")
                .Level(52)
                .Quantity(1)
                .ResearchCostModifier(0.2f)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("mushroom", 8)
                .Component("ns_holo_jelly", 4)
                .Component("distilled_water", 2)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 63, targetSweet: 40, targetSpice: 15, innerRadius: 4, outerRadius: 18, freshness: 101, xpPercent: 100);

            // Boga Noga
            _builder.Create(RecipeType.BogaNoga, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("boga_noga")
                .Level(52)
                .Quantity(1)
                .ResearchCostModifier(0.2f)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("ns_sludge_eel", 4)
                .Component("distilled_water", 4)
                .Component("sugar", 2)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 27, targetSweet: 16, targetSpice: 23, innerRadius: 6, outerRadius: 18, freshness: 99, xpPercent: 100);

            // Gardula
            _builder.Create(RecipeType.Gardula, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("gardula_drink")
                .Level(52)
                .Quantity(1)
                .ResearchCostModifier(0.2f)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("distilled_water", 4)
                .Component("ns_holo_jelly", 2)
                .Component("sugar", 2)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 0, targetSweet: 45, targetSpice: 4, innerRadius: 5, outerRadius: 18, freshness: 91, xpPercent: 100);

            // Keebadas Binggona
            _builder.Create(RecipeType.KeebadasBinggona, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("keeb_binggona")
                .Level(52)
                .Quantity(1)
                .ResearchCostModifier(0.2f)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("thune_meat", 8)
                .Component("ns_rack_meat", 4)
                .Component("cultured_butter", 2)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 100, targetSweet: 5, targetSpice: 22, innerRadius: 6, outerRadius: 18, freshness: 94, xpPercent: 100);

            // Sando g'dizzards
            _builder.Create(RecipeType.SandoGDizzards, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("sando_gdizz")
                .Level(52)
                .Quantity(1)
                .ResearchCostModifier(0.2f)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("ns_rack_meat", 8)
                .Component("ns_moonspice", 2)
                .Component("distilled_water", 2)
                .KitchenParameters(dish: KitchenDish.Roast, targetSavory: 91, targetSweet: 9, targetSpice: 37, innerRadius: 6, outerRadius: 18, freshness: 89, xpPercent: 100);

            // Cartel Cakes
            _builder.Create(RecipeType.CartelCakes, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("cartel_cakes")
                .Level(52)
                .Quantity(1)
                .ResearchCostModifier(0.2f)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("mushroom", 4)
                .Component("ns_holo_jelly", 4)
                .Component("sugar", 3)
                .Component("ns_neon_salt", 1)
                .KitchenParameters(dish: KitchenDish.Sweet, targetSavory: 35, targetSweet: 73, targetSpice: 14, innerRadius: 5, outerRadius: 18, freshness: 105, xpPercent: 100);

            // Cooking Enhancement - Duration V
            _builder.Create(RecipeType.CookingEnhancementDuration5, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_dur5")
                .Level(41)
                .Quantity(1)
                .Component("wild_meat", 2)
                .Component("wild_leg", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 27, targetSweet: 5, targetSpice: 0, innerRadius: 9, outerRadius: 27, freshness: 70, xpPercent: 100);

            // Cooking Enhancement - FP V
            _builder.Create(RecipeType.CookingEnhancementFP5, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_fp5")
                .Level(43)
                .Quantity(1)
                .Component("wild_leg", 2)
                .Component("wild_meat", 1)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 28, targetSweet: 0, targetSpice: 2, innerRadius: 5, outerRadius: 24, freshness: 68, xpPercent: 100);

            // Cooking Enhancement - FP Regen V
            _builder.Create(RecipeType.CookingEnhancementFPRegen5, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_fpr5")
                .Level(48)
                .Quantity(1)
                .Component("wild_innards", 4)
                .Component("herb_x", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 36, targetSweet: 3, targetSpice: 38, innerRadius: 5, outerRadius: 22, freshness: 78, xpPercent: 100);

            // Cooking Enhancement - HP V
            _builder.Create(RecipeType.CookingEnhancementHP5, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_hp5")
                .Level(44)
                .Quantity(1)
                .Component("warocas_meat", 3)
                .Component("bread_flour", 3)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 39, targetSweet: 10, targetSpice: 2, innerRadius: 6, outerRadius: 23, freshness: 81, xpPercent: 100);

            // Cooking Enhancement - HP Regen V
            _builder.Create(RecipeType.CookingEnhancementHPRegen5, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_hpr5")
                .Level(46)
                .Quantity(1)
                .Component("wild_innards", 3)
                .Component("herb_x", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 28, targetSweet: 0, targetSpice: 24, innerRadius: 8, outerRadius: 20, freshness: 76, xpPercent: 100);

            // Cooking Enhancement - Combat Readiness V
            _builder.Create(RecipeType.CookingEnhancementCombatReadiness5, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_recast5")
                .Level(48)
                .Quantity(1)
                .Component("wild_leg", 3)
                .Component("cultured_butter", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 34, targetSweet: 0, targetSpice: 6, innerRadius: 6, outerRadius: 28, freshness: 74, xpPercent: 100);

            // Cooking Enhancement - Rest Regen V
            _builder.Create(RecipeType.CookingEnhancementRestRegen5, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_rest5")
                .Level(42)
                .Quantity(1)
                .Component("herb_x", 2)
                .Component("wild_blood", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 17, targetSweet: 1, targetSpice: 29, innerRadius: 9, outerRadius: 24, freshness: 68, xpPercent: 100);

            // Cooking Enhancement - STM V
            _builder.Create(RecipeType.CookingEnhancementSTM5, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_stm5")
                .Level(44)
                .Quantity(1)
                .Component("wild_leg", 3)
                .Component("wild_meat", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 42, targetSweet: 0, targetSpice: 7, innerRadius: 7, outerRadius: 24, freshness: 78, xpPercent: 100);

            // Cooking Enhancement - STM Regen V
            _builder.Create(RecipeType.CookingEnhancementSTMRegen5, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_stmr5")
                .Level(47)
                .Quantity(1)
                .Component("tomato", 4)
                .Component("herb_x", 3)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 22, targetSweet: 15, targetSpice: 32, innerRadius: 7, outerRadius: 20, freshness: 85, xpPercent: 100);

            // Cooking Enhancement - XP Bonus V
            _builder.Create(RecipeType.CookingEnhancementXPBonus5, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref("cen_xp5")
                .Level(46)
                .Quantity(1)
                .Component("wild_innards", 3)
                .Component("wild_blood", 2)
                .KitchenParameters(dish: KitchenDish.Soup, targetSavory: 32, targetSweet: 2, targetSpice: 21, innerRadius: 4, outerRadius: 24, freshness: 77, xpPercent: 100);

            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceFire5, "cen_res_fir5", 45, "wild_leg", 3, "cultured_butter", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 39, targetSweet: 5, targetSpice: 12, innerRadius: 5, outerRadius: 27, freshness: 78, xpPercent: 100));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistancePoison5, "cen_res_psn5", 46, "wild_innards", 3, "herb_x", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 22, targetSweet: 2, targetSpice: 27, innerRadius: 7, outerRadius: 25, freshness: 75, xpPercent: 100));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceElectrical5, "cen_res_elec5", 47, "wild_blood", 3, "wild_meat", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 42, targetSweet: 0, targetSpice: 20, innerRadius: 7, outerRadius: 20, freshness: 74, xpPercent: 100));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceIce5, "cen_res_ice5", 48, "tomato", 3, "herb_x", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 17, targetSweet: 13, targetSpice: 24, innerRadius: 6, outerRadius: 28, freshness: 74, xpPercent: 100));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceMind5, "cen_res_mnd5", 45, "wild_meat", 3, "wild_leg", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 46, targetSweet: 0, targetSpice: 13, innerRadius: 8, outerRadius: 28, freshness: 79, xpPercent: 100));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceMobility5, "cen_res_mob5", 46, "wild_leg", 3, "wild_meat", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 40, targetSweet: 5, targetSpice: 15, innerRadius: 7, outerRadius: 22, freshness: 73, xpPercent: 100));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceTrauma5, "cen_res_tra5", 47, "wild_innards", 3, "wild_blood", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 32, targetSweet: 0, targetSpice: 22, innerRadius: 7, outerRadius: 26, freshness: 77, xpPercent: 100));
            CreateCookingResistanceEnhancement(RecipeType.CookingEnhancementResistanceDisruption5, "cen_res_dis5", 48, "wild_blood", 3, "herb_x", 2, parameters: new KitchenParameters(dish: KitchenDish.Soup, targetSavory: 27, targetSweet: 0, targetSpice: 28, innerRadius: 5, outerRadius: 25, freshness: 75, xpPercent: 100));
        }

        private void CreateCookingResistanceEnhancement(
            RecipeType recipe,
            string resref,
            int level,
            string component1,
            int component1Quantity,
            string component2,
            int component2Quantity,
            RecipeMinigameParameters parameters = null)
        {
            _builder.Create(recipe, SkillType.Agriculture)
                .Category(RecipeCategoryType.CookingEnhancement)
                .Resref(resref)
                .Level(level)
                .Quantity(1)
                .Component(component1, component1Quantity)
                .Component(component2, component2Quantity)
                .MinigameParameters(parameters);
        }
    }
}
