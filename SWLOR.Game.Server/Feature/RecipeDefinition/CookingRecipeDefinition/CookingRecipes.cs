using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.CookingRecipeDefinition
{
    public class CookingRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

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
                .RequirementPerk(PerkType.CookingRecipes, 1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("mynock_meat", 3)
                .Component("mynock_wing", 2)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.WarocasPotPie, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("waro_potpie")
                .Level(1)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("warocas_meat", 3)
                .Component("waro_leg", 2)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.SugarCookies, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("sugar_cookies")
                .Level(2)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("sugar", 4)
                .Component("v_flour", 3)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.OrangeJuice, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("orange_juice")
                .Level(3)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("v_orange", 3)
                .Component("distilled_water", 1);


            _builder.Create(RecipeType.PebbleSoup, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("pebble_soup")
                .Level(3)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("v_pebble", 3)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.MynockBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("mynock_broth")
                .Level(3)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("mynock_wing", 3)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.Noodles, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("noodles")
                .Level(4)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("v_flour", 4)
                .Component("butter_1", 1)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.KathSandwich, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("kath_sandwich")
                .Level(4)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("kath_meat_1", 3)
                .Component("v_flour", 2);

            _builder.Create(RecipeType.KinrathMeatBall, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("kinrath_mball")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("kinrath_meat", 3)
                .Component("kinrath_limb", 2)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.LemonCookies, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("lemon_cookies")
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("sugar", 2)
                .Component("v_flour", 1)
                .Component("v_lemon", 3);

            _builder.Create(RecipeType.ViscaranHerbSoup, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("v_herb_soup")
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("herb_v", 3)
                .Component("distilled_water", 1)
                .Component("sugar", 2);

            _builder.Create(RecipeType.OrangeCurry, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("orange_curry")
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("v_orange", 4)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.OrangeAuLait, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("o_aulait")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("v_orange", 3)
                .Component("distilled_water", 2)
                .Component("herb_v", 3);

            _builder.Create(RecipeType.KathBloodBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("k_blood_broth")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("kath_blood", 3)
                .Component("distilled_water", 1)
                .Component("herb_v", 2);

            _builder.Create(RecipeType.GimpassaSandwich, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("g_sandwich")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("gimp_meat", 3)
                .Component("distilled_water", 1)
                .Component("herb_v", 3);

            _builder.Create(RecipeType.GimpassaStew, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("g_stew")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 1)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("gimp_blood", 2)
                .Component("gimp_meat", 2);
        }

        private void Tier2()
        {
            _builder.Create(RecipeType.RaivorMeatBall, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("raivor_mball")
                .Level(11)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 2)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("raivor_meat", 3)
                .Component("raivor_scale", 2)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.CairnmogPotPie, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("cairn_potpie")
                .Level(11)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 2)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("cairnmog_meat", 3)
                .Component("cairnmog_tooth", 2)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.ChocolateCookies, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("choco_cookies")
                .Level(12)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 2)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("bubble_choc", 4)
                .Component("b_flour", 3)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.KlorslugSurprise, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("slug_surprise")
                .Level(12)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 2)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("klorslug_meat", 3)
                .Component("klorslug_tail", 2)
                .Component("klorslug_innards", 1);

            _builder.Create(RecipeType.AppleJuice, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("apple_juice")
                .Level(13)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 2)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("v_apple", 3)
                .Component("distilled_water", 1);


            _builder.Create(RecipeType.PeaSoup, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("pea_soup")
                .Level(13)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 2)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("v_peas", 3)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.RaivorBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("raivor_broth")
                .Level(13)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 2)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("raivor_scale", 3)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.SobaNoodles, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("soba_noodles")
                .Level(14)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 2)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("b_flour", 4)
                .Component("sweet_butter", 1)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.CairnmogSandwich, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("cairn_sandwich")
                .Level(14)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 2)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("cairnmog_meat", 3)
                .Component("b_flour", 2);

            _builder.Create(RecipeType.NashtahMeatBall, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("nash_mball")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 2)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("nashtah_meat", 3)
                .Component("nashtah_foot", 2)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.MysteryCookies, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("mystery_cookies")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 2)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("sugar", 2)
                .Component("b_flour", 1)
                .Component("passion_fruit", 3);

            _builder.Create(RecipeType.MandoHerbSoup, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("mando_herbsoup")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 2)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("herb_m", 3)
                .Component("distilled_water", 1)
                .Component("sugar", 2);

            _builder.Create(RecipeType.GreenCurry, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("green_curry")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 2)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("passion_fruit", 4)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.AppleAuLait, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("apple_aulait")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 2)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("v_apple", 3)
                .Component("distilled_water", 2)
                .Component("herb_m", 3);

            _builder.Create(RecipeType.RaivorBloodBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("raiv_bloodbroth")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 2)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("raivor_blood", 3)
                .Component("distilled_water", 1)
                .Component("herb_m", 2);

            _builder.Create(RecipeType.NashtahSandwich, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("nash_sandwich")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 2)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("nashtah_meat", 3)
                .Component("distilled_water", 1)
                .Component("herb_m", 3);

            _builder.Create(RecipeType.NashtahStew, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("nash_stew")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 2)
                .EnhancementSlots(RecipeEnhancementType.Food, 1)
                .Component("nash_blood", 2)
                .Component("nashtah_meat", 2);
        }

        private void Tier3()
        {
            _builder.Create(RecipeType.AradileMeatBall, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("aradile_mball")
                .Level(21)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 3)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("aradile_meat", 3)
                .Component("aradile_tail", 2)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.TigerPotPie, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("tiger_potpie")
                .Level(21)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 3)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("tiger_meat", 3)
                .Component("q_tiger_paw", 2)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.AcornCookies, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("acorn_cookies")
                .Level(22)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 3)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("h_acorn", 4)
                .Component("p_flour", 3)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.PineappleJuice, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("pine_juice")
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 3)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("s_pineapple", 3)
                .Component("distilled_water", 1);


            _builder.Create(RecipeType.VegetableSoup, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("veg_soup")
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 3)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("veggie_clump", 3)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.AradileBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("ara_broth")
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 3)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("aradile_innards", 3)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.RamenNoodles, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("ramen_noodles")
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 3)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("p_flour", 4)
                .Component("c_butter", 1)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.AradileSandwich, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("ara_sandwich")
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 3)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("aradile_meat", 3)
                .Component("p_flour", 2);

            _builder.Create(RecipeType.ByyskMeatBall, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("byysk_mball")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 3)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("byysk_meat", 3)
                .Component("byysk_tail", 2)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.CinnaCookies, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("cinna_cookies")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 3)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("sugar", 2)
                .Component("p_flour", 1)
                .Component("h_acorn", 3);

            _builder.Create(RecipeType.MonCalaHerbSoup, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("moncal_hsoup")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 3)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("herb_c", 3)
                .Component("distilled_water", 1)
                .Component("sugar", 2);

            _builder.Create(RecipeType.RedCurry, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("red_curry")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 3)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("h_acorn", 2)
                .Component("distilled_water", 1)
                .Component("veggie_clump", 2);

            _builder.Create(RecipeType.PineappleAuLait, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("pine_aulait")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 3)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("s_pineapple", 3)
                .Component("distilled_water", 2)
                .Component("herb_c", 3);

            _builder.Create(RecipeType.AmphiHydrusBloodBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("amphi_bbroth")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 3)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("amphi_blood", 3)
                .Component("distilled_water", 1)
                .Component("herb_c", 2);

            _builder.Create(RecipeType.SnakeSandwich, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("snake_sandwich")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 3)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("viper_meat", 3)
                .Component("distilled_water", 1)
                .Component("herb_c", 3);

            _builder.Create(RecipeType.SnakeStew, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("snake_stew")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 3)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("viper_guts", 2)
                .Component("viper_meat", 2);
        }

        private void Tier4()
        {
            _builder.Create(RecipeType.WompRatMeatBall, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("womp_mball")
                .Level(31)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 4)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("wompratmeat", 3)
                .Component("womp_innards", 2)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.SandDemonPotPie, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("sanddem_potpie")
                .Level(31)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 4)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("sanddemon_meat", 3)
                .Component("sand_demon_leg", 2)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.GingerCookies, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("ging_cookies")
                .Level(32)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 4)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("ginger", 4)
                .Component("r_flour", 3)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.MelonJuice, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("melon_juice")
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 4)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("melon", 3)
                .Component("distilled_water", 1);


            _builder.Create(RecipeType.MushroomSoup, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("mush_soup")
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 4)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("mushroom", 3)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.WompRatBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("womp_broth")
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 4)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("womp_innards", 3)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.SoyRamen, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("soy_ramen")
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 4)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("r_flour", 4)
                .Component("plant_butter", 1)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.SurpriseSandwich, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("surprise_sandwich")
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 4)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("sanddemon_meat", 3)
                .Component("r_flour", 2);

            _builder.Create(RecipeType.TuskenMeatBall, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("tusken_mball")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 4)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("tusken_meat", 3)
                .Component("tusken_bones", 2)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.WalnutCookies, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("walnut_cookies")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 4)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("sugar", 2)
                .Component("r_flour", 1)
                .Component("walnut", 3);

            _builder.Create(RecipeType.DesertHerbSoup, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("des_herbsoup")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 4)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("herb_t", 3)
                .Component("distilled_water", 1)
                .Component("sugar", 2);

            _builder.Create(RecipeType.YellowCurry, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("yellow_curry")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 4)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("ginger", 2)
                .Component("distilled_water", 1)
                .Component("cornucopia", 2);

            _builder.Create(RecipeType.MelonAuLait, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("melon_aulait")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 4)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("melon", 3)
                .Component("distilled_water", 2)
                .Component("herb_t", 3);

            _builder.Create(RecipeType.TuskenBloodBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("tusk_b_broth")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 4)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("tusken_blood", 3)
                .Component("distilled_water", 1)
                .Component("herb_t", 2);

            _builder.Create(RecipeType.SandDemonSandwich, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("dem_sandwich")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 4)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("sanddemon_meat", 3)
                .Component("distilled_water", 1)
                .Component("herb_t", 3);

            _builder.Create(RecipeType.SandDemonStew, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("demon_stew")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 4)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("sand_demon_leg", 2)
                .Component("sanddemon_meat", 2);
        }

        private void Tier5()
        {
            _builder.Create(RecipeType.WildMeatBall, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("wild_mball")
                .Level(41)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 5)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("wild_meat", 3)
                .Component("wild_innards", 2)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.WildPotPie, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("wild_potpie")
                .Level(41)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 5)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("wild_meat", 3)
                .Component("wild_leg", 2)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.WildCookies, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("wild_cookies")
                .Level(42)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 5)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("turnip", 4)
                .Component("bread_flour", 3)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.TomatoJuice, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("tomato_juice")
                .Level(43)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 5)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("tomato", 3)
                .Component("distilled_water", 1);


            _builder.Create(RecipeType.MisoSoup, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("miso_soup")
                .Level(43)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 5)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("tofu", 3)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.WildBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("wild_broth")
                .Level(43)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 5)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("wild_innards", 3)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.MisoRamen, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("miso_ramen")
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 5)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("bread_flour", 4)
                .Component("cultured_butter", 1)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.WildSandwich, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("wild_sandwich")
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 5)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("wild_meat", 3)
                .Component("bread_flour", 2);

            _builder.Create(RecipeType.GrandioseMeatBall, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("grand_mball")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 5)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("wild_meat", 3)
                .Component("wild_leg", 2)
                .Component("distilled_water", 1);

            _builder.Create(RecipeType.WizardCookies, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("wizard_cookies")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 5)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("sugar", 2)
                .Component("bread_flour", 1)
                .Component("tomato", 3);

            _builder.Create(RecipeType.DathHerbSoup, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("dath_hsoup")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 5)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("herb_x", 3)
                .Component("distilled_water", 1)
                .Component("sugar", 2);

            _builder.Create(RecipeType.WildCurry, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("wild_curry")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 5)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("tofu", 2)
                .Component("distilled_water", 1)
                .Component("dried_bonito", 2);

            _builder.Create(RecipeType.TomatoAuLait, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("tomato_aulait")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 5)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("tomato", 3)
                .Component("distilled_water", 2)
                .Component("herb_x", 3);

            _builder.Create(RecipeType.WildBloodBroth, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("wild_bbroth")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 5)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("wild_blood", 3)
                .Component("distilled_water", 1)
                .Component("herb_x", 2);

            _builder.Create(RecipeType.GrandioseSandwich, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("grand_sandwich")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 5)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("wild_meat", 3)
                .Component("distilled_water", 1)
                .Component("herb_x", 3);

            _builder.Create(RecipeType.WildStew, SkillType.Agriculture)
                .Category(RecipeCategoryType.Food)
                .Resref("wild_stew")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.CookingRecipes, 5)
                .EnhancementSlots(RecipeEnhancementType.Food, 2)
                .Component("wild_leg", 2)
                .Component("wild_meat", 2);
        }
    }
}
