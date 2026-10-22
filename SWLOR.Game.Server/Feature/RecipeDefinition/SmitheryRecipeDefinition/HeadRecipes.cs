using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class HeadRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Tier1();
            Tier1A();
            Tier2();
            Tier2A();
            Tier3();
            Tier3A();
            Tier4();
            Tier4A();
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
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 3)
                .Component("fiberp_ruined", 2)
                .ForgeParameters(rangeCenter: 54, rangeWidth: 55, coolingRate: 1, startingThermal: 80, shapeShare: 56);

            // Spiritmaster Cap
            _builder.Create(RecipeType.SpiritmasterCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("sm_cap")
                .Level(4)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 3)
                .Component("fiberp_ruined", 2)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 57, coolingRate: 1, startingThermal: 54, shapeShare: 54);

            // Combat Cap
            _builder.Create(RecipeType.CombatCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("com_cap")
                .Level(4)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 3)
                .Component("fiberp_ruined", 2)
                .ForgeParameters(rangeCenter: 38, rangeWidth: 60, coolingRate: 2, startingThermal: 56, shapeShare: 53);

            // Advent Helmet
            _builder.Create(RecipeType.AdventHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("advent_helmet")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 55, rangeWidth: 48, coolingRate: 3, startingThermal: 74, shapeShare: 54);

            // Amateur Helmet
            _builder.Create(RecipeType.AmateurHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("engi_helmet_1")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 57, rangeWidth: 46, coolingRate: 3, startingThermal: 70, shapeShare: 56);

            // Cloth Helmet
            _builder.Create(RecipeType.ClothHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("fabr_helmet_1")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 58, rangeWidth: 51, coolingRate: 4, startingThermal: 75, shapeShare: 54);

            // Chef Helmet
            _builder.Create(RecipeType.ChefHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("chef_helmet_1")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 60, rangeWidth: 54, coolingRate: 4, startingThermal: 77, shapeShare: 54);
        }

        private void Tier2()
        {
            // Titan Helmet
            _builder.Create(RecipeType.TitanHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("tit_helmet")
                .Level(14)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 3)
                .Component("fiberp_flawed", 2)
                .ForgeParameters(rangeCenter: 60, rangeWidth: 44, coolingRate: 4, startingThermal: 74, shapeShare: 57);

            // Vivid Cap
            _builder.Create(RecipeType.VividCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("viv_cap")
                .Level(14)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2)
                .ForgeParameters(rangeCenter: 42, rangeWidth: 59, coolingRate: 3, startingThermal: 55, shapeShare: 56);

            // Valor Cap
            _builder.Create(RecipeType.ValorCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("val_cap")
                .Level(14)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 60, coolingRate: 3, startingThermal: 58, shapeShare: 56);

            // Frontier Helmet
            _builder.Create(RecipeType.FrontierHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("frontier_helmet")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 62, rangeWidth: 44, coolingRate: 5, startingThermal: 72, shapeShare: 59);

            // Worker Helmet
            _builder.Create(RecipeType.WorkerHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("engi_helmet_2")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 55, rangeWidth: 40, coolingRate: 4, startingThermal: 66, shapeShare: 58);

            // Linen Helmet
            _builder.Create(RecipeType.LinenHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("fabr_helmet_2")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 62, rangeWidth: 42, coolingRate: 4, startingThermal: 67, shapeShare: 59);

            // Velveteen Helmet
            _builder.Create(RecipeType.VelveteenHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("chef_helmet_2")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 56, rangeWidth: 48, coolingRate: 4, startingThermal: 70, shapeShare: 53);
        }

        private void Tier3()
        {
            // Quark Helmet
            _builder.Create(RecipeType.QuarkHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("qk_helmet")
                .Level(24)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 3)
                .Component("fiberp_good", 2)
                .ForgeParameters(rangeCenter: 55, rangeWidth: 40, coolingRate: 3, startingThermal: 65, shapeShare: 57);

            // Reginal Cap
            _builder.Create(RecipeType.ReginalCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("reg_cap")
                .Level(24)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 3)
                .Component("fiberp_good", 2)
                .ForgeParameters(rangeCenter: 38, rangeWidth: 60, coolingRate: 4, startingThermal: 49, shapeShare: 56);

            // Forza Cap
            _builder.Create(RecipeType.ForzaCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("for_cap")
                .Level(24)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 3)
                .Component("fiberp_good", 2)
                .ForgeParameters(rangeCenter: 36, rangeWidth: 53, coolingRate: 4, startingThermal: 53, shapeShare: 56);

            // Majestic Helmet
            _builder.Create(RecipeType.MajesticHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("majestic_helmet")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 55, rangeWidth: 37, coolingRate: 4, startingThermal: 65, shapeShare: 54);

            // Mechanic Helmet
            _builder.Create(RecipeType.MechanicHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("engi_helmet_3")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 41, coolingRate: 5, startingThermal: 63, shapeShare: 55);

            // Designer Helmet
            _builder.Create(RecipeType.DesignerHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("fabr_helmet_3")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 54, rangeWidth: 33, coolingRate: 4, startingThermal: 66, shapeShare: 56);

            // Silk Helmet
            _builder.Create(RecipeType.SilkHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("chef_helmet_3")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 57, rangeWidth: 41, coolingRate: 5, startingThermal: 64, shapeShare: 54);
        }

        private void Tier4()
        {
            // Argos Helmet
            _builder.Create(RecipeType.ArgosHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("ar_helmet")
                .Level(34)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 3)
                .Component("fiberp_imperfect", 2)
                .ForgeParameters(rangeCenter: 56, rangeWidth: 34, coolingRate: 6, startingThermal: 64, shapeShare: 59);

            // Grenada Cap
            _builder.Create(RecipeType.GrenadaCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("gr_cap")
                .Level(34)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 3)
                .Component("fiberp_imperfect", 2)
                .ForgeParameters(rangeCenter: 40, rangeWidth: 54, coolingRate: 4, startingThermal: 50, shapeShare: 58);

            // Survival Cap
            _builder.Create(RecipeType.SurvivalCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("sur_cap")
                .Level(34)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 3)
                .Component("fiberp_imperfect", 2)
                .ForgeParameters(rangeCenter: 44, rangeWidth: 55, coolingRate: 4, startingThermal: 50, shapeShare: 56);

            // Dream Helmet
            _builder.Create(RecipeType.DreamHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("dream_helmet")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 57, rangeWidth: 27, coolingRate: 5, startingThermal: 59, shapeShare: 57);

            // Devotion Helmet
            _builder.Create(RecipeType.DevotionHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("engi_helmet_4")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 57, rangeWidth: 29, coolingRate: 4, startingThermal: 64, shapeShare: 55);

            // Oasis Helmet
            _builder.Create(RecipeType.OasisHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("fabr_helmet_4")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 54, rangeWidth: 34, coolingRate: 5, startingThermal: 59, shapeShare: 59);

            // Vintage Helmet
            _builder.Create(RecipeType.VintageHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("chef_helmet_4")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 60, rangeWidth: 31, coolingRate: 6, startingThermal: 60, shapeShare: 59);
        }

        private void Tier5()
        {
            // Eclipse Helmet
            _builder.Create(RecipeType.EclipseHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("ec_helmet")
                .Level(44)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 3)
                .Component("fiberp_high", 2)
                .ForgeParameters(rangeCenter: 57, rangeWidth: 31, coolingRate: 7, startingThermal: 63, shapeShare: 57);

            // Transcendent Cap
            _builder.Create(RecipeType.TranscendentCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("tran_cap")
                .Level(44)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 3)
                .Component("fiberp_high", 2)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 47, coolingRate: 5, startingThermal: 46, shapeShare: 56);

            // Supreme Cap
            _builder.Create(RecipeType.SupremeCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("sup_cap")
                .Level(44)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 3)
                .Component("fiberp_high", 2)
                .ForgeParameters(rangeCenter: 42, rangeWidth: 41, coolingRate: 5, startingThermal: 40, shapeShare: 58);

            // Eternal Helmet
            _builder.Create(RecipeType.EternalHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("eternal_helmet")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3)
                .ForgeParameters(rangeCenter: 60, rangeWidth: 21, coolingRate: 6, startingThermal: 57, shapeShare: 55);

            // Skysteel Helmet
            _builder.Create(RecipeType.SkysteelHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("engi_helmet_5")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 24, coolingRate: 7, startingThermal: 61, shapeShare: 54);

            // Rose Helmet
            _builder.Create(RecipeType.RoseHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("fabr_helmet_5")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3)
                .ForgeParameters(rangeCenter: 54, rangeWidth: 23, coolingRate: 5, startingThermal: 59, shapeShare: 59);

            // Moonflame Helmet
            _builder.Create(RecipeType.MoonflameHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("chef_helmet_5")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3)
                .ForgeParameters(rangeCenter: 58, rangeWidth: 26, coolingRate: 7, startingThermal: 59, shapeShare: 55);

            // Chaos Helmet
            _builder.Create(RecipeType.ChaosHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("ch_helmet")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("fiberp_high", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 62, rangeWidth: 21, coolingRate: 7, startingThermal: 55, shapeShare: 57);

            // Arkanian Dragon Helm
            _builder.Create(RecipeType.ArkanianDragonHelm, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("ark_helm")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("ark_drg_scales", 10)
                .Component("chiro_shard", 2)
                .Component("ark_dragon_troph", 1)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 60, rangeWidth: 20, coolingRate: 6, startingThermal: 61, shapeShare: 55);

            // Magus Cap
            _builder.Create(RecipeType.MagusCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("mag_cap")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("lth_high", 20)
                .Component("fiberp_high", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 58, rangeWidth: 26, coolingRate: 5, startingThermal: 59, shapeShare: 53);

            // Immortal Cap
            _builder.Create(RecipeType.ImmortalCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("imm_cap")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("lth_high", 20)
                .Component("fiberp_high", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 60, rangeWidth: 21, coolingRate: 6, startingThermal: 54, shapeShare: 57);
        }
        private void Tier1A()
        {
            // Warden Helmet
            _builder.Create(RecipeType.WardenHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("fld_bul_helm")
                .Level(9)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("fiberp_ruined", 3)
                .Component("lth_ruined", 2)
                .ForgeParameters(rangeCenter: 58, rangeWidth: 50, coolingRate: 3, startingThermal: 78, shapeShare: 58);

            // Mystic Cap
            _builder.Create(RecipeType.MysticCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("fld_chn_cap")
                .Level(9)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 5)
                .Component("fiberp_ruined", 3)
                .Component("jade", 2)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 60, coolingRate: 2, startingThermal: 60, shapeShare: 59);

            // Vanguard Cap
            _builder.Create(RecipeType.VanguardCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("fld_skm_cap")
                .Level(9)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 5)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 2)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 58, coolingRate: 2, startingThermal: 58, shapeShare: 55);
        }

        private void Tier2A()
        {
            // Bastion Helmet
            _builder.Create(RecipeType.BastionHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("vet_bul_helm")
                .Level(19)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("fiberp_flawed", 3)
                .Component("lth_flawed", 2)
                .ForgeParameters(rangeCenter: 62, rangeWidth: 48, coolingRate: 4, startingThermal: 69, shapeShare: 56);

            // Oracle Cap
            _builder.Create(RecipeType.OracleCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("vet_chn_cap")
                .Level(19)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 5)
                .Component("fiberp_flawed", 3)
                .Component("agate", 2)
                .ForgeParameters(rangeCenter: 38, rangeWidth: 60, coolingRate: 1, startingThermal: 55, shapeShare: 55);

            // Onslaught Cap
            _builder.Create(RecipeType.OnslaughtCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("vet_skm_cap")
                .Level(19)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 5)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 2)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 56, coolingRate: 3, startingThermal: 57, shapeShare: 55);
        }

        private void Tier3A()
        {
            // Sentinel Helmet
            _builder.Create(RecipeType.SentinelHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("prm_bul_helm")
                .Level(29)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("fiberp_good", 3)
                .Component("lth_good", 2)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 39, coolingRate: 4, startingThermal: 63, shapeShare: 55);

            // Arcanist Cap
            _builder.Create(RecipeType.ArcanistCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("prm_chn_cap")
                .Level(29)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 5)
                .Component("fiberp_good", 3)
                .Component("citrine", 2)
                .ForgeParameters(rangeCenter: 43, rangeWidth: 56, coolingRate: 3, startingThermal: 47, shapeShare: 54);

            // Maverick Cap
            _builder.Create(RecipeType.MaverickCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("prm_skm_cap")
                .Level(29)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 5)
                .Component("fiberp_good", 3)
                .Component("elec_good", 2)
                .ForgeParameters(rangeCenter: 40, rangeWidth: 56, coolingRate: 4, startingThermal: 50, shapeShare: 55);
        }

        private void Tier4A()
        {
            // Aegis Helmet
            _builder.Create(RecipeType.AegisHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("asc_bul_helm")
                .Level(39)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("fiberp_imperfect", 3)
                .Component("lth_imperfect", 2)
                .ForgeParameters(rangeCenter: 56, rangeWidth: 34, coolingRate: 6, startingThermal: 63, shapeShare: 53);

            // Luminary Cap
            _builder.Create(RecipeType.LuminaryCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("asc_chn_cap")
                .Level(39)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 5)
                .Component("fiberp_imperfect", 3)
                .Component("ruby", 2)
                .ForgeParameters(rangeCenter: 40, rangeWidth: 45, coolingRate: 3, startingThermal: 42, shapeShare: 57);

            // Apex Cap
            _builder.Create(RecipeType.ApexCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("asc_skm_cap")
                .Level(39)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 5)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 2)
                .ForgeParameters(rangeCenter: 45, rangeWidth: 49, coolingRate: 3, startingThermal: 48, shapeShare: 59);
        }

}
}
