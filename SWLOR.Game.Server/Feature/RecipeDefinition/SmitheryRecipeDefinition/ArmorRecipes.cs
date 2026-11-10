using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class ArmorRecipes : IRecipeListDefinition
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
            // Battlemaster Breastplate
            _builder.Create(RecipeType.BattlemasterBreastplate, SkillType.Smithery)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("bm_armor")
                .Level(7)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 4)
                .Component("fiberp_ruined", 2)
                .ForgeParameters(rangeCenter: 65, rangeWidth: 34, coolingRate: 1, startingThermal: 69, shapeShare: 70);

            // Spiritmaster Tunic
            _builder.Create(RecipeType.SpiritmasterTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("sm_tunic")
                .Level(7)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 4)
                .Component("fiberp_ruined", 2)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 52, coolingRate: 1, startingThermal: 47, shapeShare: 70);

            // Combat Tunic
            _builder.Create(RecipeType.CombatTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("com_tunic")
                .Level(7)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 4)
                .Component("fiberp_ruined", 2)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 54, coolingRate: 1, startingThermal: 48, shapeShare: 70);

            // Advent Tunic
            _builder.Create(RecipeType.AdventTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("smth_armor_1")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 55, rangeWidth: 37, coolingRate: 1, startingThermal: 61, shapeShare: 70);

            // Amateur Tunic
            _builder.Create(RecipeType.AmateurTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("engi_armor_1")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 60, rangeWidth: 43, coolingRate: 1, startingThermal: 68, shapeShare: 70);

            // Cloth Tunic
            _builder.Create(RecipeType.ClothTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("fabr_armor_1")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 58, rangeWidth: 37, coolingRate: 1, startingThermal: 64, shapeShare: 70);

            // Chef Tunic
            _builder.Create(RecipeType.ChefTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("cook_armor_1")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 59, rangeWidth: 38, coolingRate: 1, startingThermal: 65, shapeShare: 70);
        }

        private void Tier2()
        {
            // Titan Breastplate
            _builder.Create(RecipeType.TitanBreastplate, SkillType.Smithery)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("tit_armor")
                .Level(17)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 4)
                .Component("fiberp_flawed", 2)
                .ForgeParameters(rangeCenter: 66, rangeWidth: 41, coolingRate: 1, startingThermal: 77, shapeShare: 69);

            // Vivid Tunic
            _builder.Create(RecipeType.VividTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("viv_tunic")
                .Level(17)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 4)
                .Component("fiberp_flawed", 2)
                .ForgeParameters(rangeCenter: 38, rangeWidth: 60, coolingRate: 1, startingThermal: 54, shapeShare: 71);

            // Valor Tunic
            _builder.Create(RecipeType.ValorTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("val_tunic")
                .Level(17)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 4)
                .Component("fiberp_flawed", 2)
                .ForgeParameters(rangeCenter: 38, rangeWidth: 60, coolingRate: 1, startingThermal: 54, shapeShare: 71);

            // Frontier Tunic
            _builder.Create(RecipeType.FrontierTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("smth_armor_2")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 30, coolingRate: 3, startingThermal: 64, shapeShare: 69);

            // Worker Tunic
            _builder.Create(RecipeType.WorkerTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("engi_armor_2")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 55, rangeWidth: 39, coolingRate: 3, startingThermal: 65, shapeShare: 71);

            // Linen Tunic
            _builder.Create(RecipeType.LinenTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("fabr_armor_2")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 54, rangeWidth: 35, coolingRate: 4, startingThermal: 58, shapeShare: 71);

            // Velveteen Tunic
            _builder.Create(RecipeType.VelveteenTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("cook_armor_2")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 54, rangeWidth: 35, coolingRate: 2, startingThermal: 62, shapeShare: 70);
        }

        private void Tier3()
        {
            // Quark Breastplate
            _builder.Create(RecipeType.QuarkBreastplate, SkillType.Smithery)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("qk_armor")
                .Level(27)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 4)
                .Component("fiberp_good", 2)
                .ForgeParameters(rangeCenter: 64, rangeWidth: 21, coolingRate: 3, startingThermal: 73, shapeShare: 70);

            // Reginal Tunic
            _builder.Create(RecipeType.ReginalTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("reg_tunic")
                .Level(27)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 4)
                .Component("fiberp_good", 2)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 40, coolingRate: 5, startingThermal: 30, shapeShare: 70);

            // Forza Tunic
            _builder.Create(RecipeType.ForzaTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("for_tunic")
                .Level(27)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 4)
                .Component("fiberp_good", 2)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 37, coolingRate: 5, startingThermal: 42, shapeShare: 70);

            // Majestic Tunic
            _builder.Create(RecipeType.MajesticTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("smth_armor_3")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 59, rangeWidth: 24, coolingRate: 3, startingThermal: 62, shapeShare: 70);

            // Mechanic Tunic
            _builder.Create(RecipeType.MechanicTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("engi_armor_3")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 57, rangeWidth: 22, coolingRate: 1, startingThermal: 48, shapeShare: 70);

            // Designer Tunic
            _builder.Create(RecipeType.DesignerTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("fabr_armor_3")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 58, rangeWidth: 23, coolingRate: 1, startingThermal: 52, shapeShare: 70);

            // Silk Tunic
            _builder.Create(RecipeType.SilkTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("cook_armor_3")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 58, rangeWidth: 19, coolingRate: 3, startingThermal: 57, shapeShare: 70);
        }

        private void Tier4()
        {
            // Argos Breastplate
            _builder.Create(RecipeType.ArgosBreastplate, SkillType.Smithery)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("ar_armor")
                .Level(37)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 4)
                .Component("fiberp_imperfect", 2)
                .ForgeParameters(rangeCenter: 67, rangeWidth: 21, coolingRate: 1, startingThermal: 70, shapeShare: 70);

            // Grenada Tunic
            _builder.Create(RecipeType.GrenadaTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("gr_tunic")
                .Level(37)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 4)
                .Component("fiberp_imperfect", 2)
                .ForgeParameters(rangeCenter: 40, rangeWidth: 42, coolingRate: 3, startingThermal: 44, shapeShare: 70);

            // Survival Tunic
            _builder.Create(RecipeType.SurvivalTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("sur_tunic")
                .Level(37)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 4)
                .Component("fiberp_imperfect", 2)
                .ForgeParameters(rangeCenter: 40, rangeWidth: 37, coolingRate: 3, startingThermal: 44, shapeShare: 70);

            // Dream Tunic
            _builder.Create(RecipeType.DreamTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("smth_armor_4")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 60, rangeWidth: 22, coolingRate: 4, startingThermal: 64, shapeShare: 70);

            // DevotionTunic
            _builder.Create(RecipeType.DevotionTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("engi_armor_4")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 25, coolingRate: 4, startingThermal: 64, shapeShare: 70);

            // Oasis Tunic
            _builder.Create(RecipeType.OasisTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("fabr_armor_4")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 54, rangeWidth: 23, coolingRate: 4, startingThermal: 56, shapeShare: 70);

            // Vintage Tunic
            _builder.Create(RecipeType.VintageTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("cook_armor_4")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 25, coolingRate: 4, startingThermal: 62, shapeShare: 70);
        }

        private void Tier5()
        {
            // Eclipse Breastplate
            _builder.Create(RecipeType.EclipseBreastplate, SkillType.Smithery)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("ec_armor")
                .Level(47)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 4)
                .Component("fiberp_high", 2)
                .ForgeParameters(rangeCenter: 67, rangeWidth: 24, coolingRate: 1, startingThermal: 76, shapeShare: 70);

            // Transcendent Tunic
            _builder.Create(RecipeType.TranscendentTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("tran_tunic")
                .Level(47)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 4)
                .Component("fiberp_high", 2)
                .ForgeParameters(rangeCenter: 42, rangeWidth: 40, coolingRate: 2, startingThermal: 45, shapeShare: 70);

            // Supreme Tunic
            _builder.Create(RecipeType.SupremeTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("sup_tunic")
                .Level(47)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 4)
                .Component("fiberp_high", 2)
                .ForgeParameters(rangeCenter: 42, rangeWidth: 40, coolingRate: 2, startingThermal: 35, shapeShare: 70);

            // Eternal Tunic
            _builder.Create(RecipeType.EternalTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("smth_armor_5")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3)
                .ForgeParameters(rangeCenter: 54, rangeWidth: 24, coolingRate: 4, startingThermal: 56, shapeShare: 70);

            // Skysteel Tunic
            _builder.Create(RecipeType.SkysteelTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("engi_armor_5")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3)
                .ForgeParameters(rangeCenter: 58, rangeWidth: 24, coolingRate: 4, startingThermal: 60, shapeShare: 70);

            // Rose Tunic
            _builder.Create(RecipeType.RoseTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("fabr_armor_5")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3)
                .ForgeParameters(rangeCenter: 60, rangeWidth: 24, coolingRate: 5, startingThermal: 67, shapeShare: 70);

            // Moonflame Tunic
            _builder.Create(RecipeType.MoonflameTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("cook_armor_5")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3)
                .ForgeParameters(rangeCenter: 60, rangeWidth: 24, coolingRate: 4, startingThermal: 64, shapeShare: 70);

            // Chaos Breastplate
            _builder.Create(RecipeType.ChaosBreastplate, SkillType.Smithery)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("ch_armor")
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
                .ForgeParameters(rangeCenter: 62, rangeWidth: 24, coolingRate: 3, startingThermal: 68, shapeShare: 70);

            // Magus Tunic
            _builder.Create(RecipeType.MagusTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("mag_tunic")
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
                .ForgeParameters(rangeCenter: 56, rangeWidth: 24, coolingRate: 4, startingThermal: 62, shapeShare: 70);

            // Immortal Tunic
            _builder.Create(RecipeType.ImmortalTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("imm_tunic")
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
                .ForgeParameters(rangeCenter: 54, rangeWidth: 24, coolingRate: 4, startingThermal: 61, shapeShare: 70);

            // Arkanian Dragon Breastplate
            _builder.Create(RecipeType.ArkanianDragonArmor, SkillType.Smithery)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("ark_armor")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("lth_high", 20)
                .Component("ark_drg_scales", 20)
                .Component("chiro_shard", 2)
                .Component("ark_dragon_troph", 3)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 63, rangeWidth: 24, coolingRate: 3, startingThermal: 69, shapeShare: 70);
        }

        private void Tier1A()
        {
            // Warden Breastplate
            _builder.Create(RecipeType.WardenBreastplate, SkillType.Smithery)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("fld_bul_armor")
                .Level(12)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 6)
                .Component("fiberp_ruined", 3)
                .Component("lth_ruined", 3)
                .ForgeParameters(rangeCenter: 63, rangeWidth: 44, coolingRate: 1, startingThermal: 76, shapeShare: 68);

            // Mystic Tunic
            _builder.Create(RecipeType.MysticTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("fld_chn_tunic")
                .Level(12)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 6)
                .Component("fiberp_ruined", 3)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 60, coolingRate: 1, startingThermal: 55, shapeShare: 71);

            // Vanguard Tunic
            _builder.Create(RecipeType.VanguardTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("fld_skm_tunic")
                .Level(12)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 6)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 3)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 60, coolingRate: 1, startingThermal: 55, shapeShare: 69);
        }

        private void Tier2A()
        {
            // Bastion Breastplate
            _builder.Create(RecipeType.BastionBreastplate, SkillType.Smithery)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("vet_bul_armor")
                .Level(22)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 6)
                .Component("fiberp_flawed", 3)
                .Component("lth_flawed", 3)
                .ForgeParameters(rangeCenter: 64, rangeWidth: 23, coolingRate: 5, startingThermal: 65, shapeShare: 70);

            // Oracle Tunic
            _builder.Create(RecipeType.OracleTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("vet_chn_tunic")
                .Level(22)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 6)
                .Component("fiberp_flawed", 3)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 40, coolingRate: 1, startingThermal: 56, shapeShare: 70);

            // Onslaught Tunic
            _builder.Create(RecipeType.OnslaughtTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("vet_skm_tunic")
                .Level(22)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 6)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 3)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 42, coolingRate: 2, startingThermal: 47, shapeShare: 70);
        }

        private void Tier3A()
        {
            // Sentinel Breastplate
            _builder.Create(RecipeType.SentinelBreastplate, SkillType.Smithery)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("prm_bul_armor")
                .Level(32)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 6)
                .Component("fiberp_good", 3)
                .Component("lth_good", 3)
                .ForgeParameters(rangeCenter: 65, rangeWidth: 23, coolingRate: 1, startingThermal: 69, shapeShare: 70);

            // Arcanist Tunic
            _builder.Create(RecipeType.ArcanistTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("prm_chn_tunic")
                .Level(32)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 6)
                .Component("fiberp_good", 3)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 40, rangeWidth: 42, coolingRate: 3, startingThermal: 46, shapeShare: 70);

            // Maverick Tunic
            _builder.Create(RecipeType.MaverickTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("prm_skm_tunic")
                .Level(32)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 6)
                .Component("fiberp_good", 3)
                .Component("elec_good", 3)
                .ForgeParameters(rangeCenter: 40, rangeWidth: 39, coolingRate: 3, startingThermal: 46, shapeShare: 70);
        }

        private void Tier4A()
        {
            // Aegis Breastplate
            _builder.Create(RecipeType.AegisBreastplate, SkillType.Smithery)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("asc_bul_armor")
                .Level(42)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 6)
                .Component("fiberp_imperfect", 3)
                .Component("lth_imperfect", 3)
                .ForgeParameters(rangeCenter: 67, rangeWidth: 24, coolingRate: 1, startingThermal: 69, shapeShare: 70);

            // Luminary Tunic
            _builder.Create(RecipeType.LuminaryTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("asc_chn_tunic")
                .Level(42)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 6)
                .Component("fiberp_imperfect", 3)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 41, rangeWidth: 40, coolingRate: 2, startingThermal: 40, shapeShare: 70);

            // Apex Tunic
            _builder.Create(RecipeType.ApexTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("asc_skm_tunic")
                .Level(42)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 6)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 3)
                .ForgeParameters(rangeCenter: 41, rangeWidth: 40, coolingRate: 2, startingThermal: 38, shapeShare: 70);
        }

    }
}
