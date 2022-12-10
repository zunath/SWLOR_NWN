using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class HeadRecipes: IRecipeListDefinition
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
            // Battlemaster Helmet
            _builder.Create(RecipeType.BattlemasterHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("bm_helmet")
                .Level(4)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 3)
                .Component("fiberp_ruined", 2);

            // Spiritmaster Cap
            _builder.Create(RecipeType.SpiritmasterCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("sm_cap")
                .Level(4)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 3)
                .Component("fiberp_ruined", 2);

            // Combat Cap
            _builder.Create(RecipeType.CombatCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("com_cap")
                .Level(4)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 3)
                .Component("fiberp_ruined", 2);

            // Advent Helmet
            _builder.Create(RecipeType.AdventHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("advent_helmet")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // Amateur Helmet
            _builder.Create(RecipeType.AmateurHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("engi_helmet_1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // Cloth Helmet
            _builder.Create(RecipeType.ClothHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("fabr_helmet_1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // Chef Helmet
            _builder.Create(RecipeType.ChefHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("chef_helmet_1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);
        }

        private void Tier2()
        {
            // Titan Helmet
            _builder.Create(RecipeType.TitanHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("tit_helmet")
                .Level(14)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 3)
                .Component("fiberp_flawed", 2);

            // Vivid Cap
            _builder.Create(RecipeType.VividCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("viv_cap")
                .Level(14)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2);

            // Valor Cap
            _builder.Create(RecipeType.ValorCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("val_cap")
                .Level(14)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2);

            // Frontier Helmet
            _builder.Create(RecipeType.FrontierHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("frontier_helmet")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // Worker Helmet
            _builder.Create(RecipeType.WorkerHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("engi_helmet_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // Linen Helmet
            _builder.Create(RecipeType.LinenHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("fabr_helmet_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // Velveteen Helmet
            _builder.Create(RecipeType.VelveteenHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("chef_helmet_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);
        }

        private void Tier3()
        {
            // Quark Helmet
            _builder.Create(RecipeType.QuarkHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("qk_helmet")
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 3)
                .Component("fiberp_good", 2);

            // Reginal Cap
            _builder.Create(RecipeType.ReginalCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("reg_cap")
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 3)
                .Component("fiberp_good", 2);

            // Forza Cap
            _builder.Create(RecipeType.ForzaCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("for_cap")
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 3)
                .Component("fiberp_good", 2);

            // Majestic Helmet
            _builder.Create(RecipeType.MajesticHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("majestic_helmet")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // Designer Helmet
            _builder.Create(RecipeType.DesignerHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("fabr_helmet_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // Silk Helmet
            _builder.Create(RecipeType.SilkHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("chef_helmet_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);
        }

        private void Tier4()
        {
            // Argos Helmet
            _builder.Create(RecipeType.ArgosHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("ar_helmet")
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 3)
                .Component("fiberp_imperfect", 2);

            // Grenada Cap
            _builder.Create(RecipeType.GrenadaCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("gr_cap")
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 3)
                .Component("fiberp_imperfect", 2);

            // Survival Cap
            _builder.Create(RecipeType.SurvivalCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("sur_cap")
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 3)
                .Component("fiberp_imperfect", 2);

            // Dream Helmet
            _builder.Create(RecipeType.DreamHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("dream_helmet")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // Devotion Helmet
            _builder.Create(RecipeType.DevotionHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("engi_helmet_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // Oasis Helmet
            _builder.Create(RecipeType.OasisHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("fabr_helmet_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // Vintage Helmet
            _builder.Create(RecipeType.VintageHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("chef_helmet_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);
        }

        private void Tier5()
        {
            // Eclipse Helmet
            _builder.Create(RecipeType.EclipseHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("ec_helmet")
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 3)
                .Component("fiberp_high", 2);

            // Transcendent Cap
            _builder.Create(RecipeType.TranscendentCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("tran_cap")
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 3)
                .Component("fiberp_high", 2);

            // Supreme Cap
            _builder.Create(RecipeType.SupremeCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("sup_cap")
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 3)
                .Component("fiberp_high", 2);

            // Eternal Helmet
            _builder.Create(RecipeType.EternalHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("eternal_helmet")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Skysteel Helmet
            _builder.Create(RecipeType.SkysteelHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("engi_helmet_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Rose Helmet
            _builder.Create(RecipeType.RoseHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("fabr_helmet_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Moonflame Helmet
            _builder.Create(RecipeType.MoonflameHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("chef_helmet_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Chaos Helmet
            _builder.Create(RecipeType.ChaosHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("ch_helmet")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 20)
                .Component("fiberp_high", 20)
                .Component("chiro_shard", 1);

            // Magus Cap
            _builder.Create(RecipeType.MagusCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("mag_cap")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 20)
                .Component("fiberp_high", 20)
                .Component("chiro_shard", 1);

            // Immortal Cap
            _builder.Create(RecipeType.ImmortalCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("imm_cap")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 20)
                .Component("fiberp_high", 20)
                .Component("chiro_shard", 1);
        }
    }
}
