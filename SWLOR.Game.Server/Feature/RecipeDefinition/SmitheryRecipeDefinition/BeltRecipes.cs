using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class BeltRecipes : IRecipeListDefinition
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
            // Battlemaster Belt
            _builder.Create(RecipeType.BattlemasterBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("bm_belt")
                .Level(9)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 5)
                .Component("fiberp_ruined", 3)
                .ForgeParameters(rangeCenter: 41, rangeWidth: 60, coolingRate: 2, startingThermal: 53, shapeShare: 54);

            // Spiritmaster Belt
            _builder.Create(RecipeType.SpiritmasterBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("sm_belt")
                .Level(9)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 5)
                .Component("fiberp_ruined", 3)
                .ForgeParameters(rangeCenter: 36, rangeWidth: 58, coolingRate: 3, startingThermal: 54, shapeShare: 51);

            // Combat Belt
            _builder.Create(RecipeType.CombatBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("com_belt")
                .Level(9)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 5)
                .Component("fiberp_ruined", 3)
                .ForgeParameters(rangeCenter: 38, rangeWidth: 60, coolingRate: 3, startingThermal: 57, shapeShare: 50);

            // Advent Belt
            _builder.Create(RecipeType.AdventBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("advent_belt")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 47, rangeWidth: 48, coolingRate: 3, startingThermal: 61, shapeShare: 55);

            // Amateur Belt
            _builder.Create(RecipeType.AmateurBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("engi_belt_1")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 47, rangeWidth: 53, coolingRate: 4, startingThermal: 66, shapeShare: 54);

            // Cloth Belt
            _builder.Create(RecipeType.ClothBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("fabr_belt_1")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 49, coolingRate: 4, startingThermal: 60, shapeShare: 51);

            // Chef Belt
            _builder.Create(RecipeType.ChefBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("chef_belt_1")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 44, rangeWidth: 50, coolingRate: 4, startingThermal: 63, shapeShare: 53);

}

        private void Tier2()
        {
            // Titan Belt
            _builder.Create(RecipeType.TitanBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("tit_belt")
                .Level(19)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 5)
                .Component("fiberp_flawed", 3)
                .ForgeParameters(rangeCenter: 35, rangeWidth: 57, coolingRate: 2, startingThermal: 56, shapeShare: 49);

            // Vivid Belt
            _builder.Create(RecipeType.VividBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("viv_belt")
                .Level(19)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 5)
                .Component("fiberp_flawed", 3)
                .ForgeParameters(rangeCenter: 35, rangeWidth: 60, coolingRate: 2, startingThermal: 53, shapeShare: 53);

            // Valor Belt
            _builder.Create(RecipeType.ValorBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("val_belt")
                .Level(19)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 5)
                .Component("fiberp_flawed", 3)
                .ForgeParameters(rangeCenter: 34, rangeWidth: 56, coolingRate: 2, startingThermal: 56, shapeShare: 54);

            // Frontier Belt
            _builder.Create(RecipeType.FrontierBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("frontier_belt")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 48, coolingRate: 4, startingThermal: 63, shapeShare: 51);

            // Worker Belt
            _builder.Create(RecipeType.WorkerBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("engi_belt_2")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 44, coolingRate: 3, startingThermal: 56, shapeShare: 51);

            // Linen Belt
            _builder.Create(RecipeType.LinenBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("fabr_belt_2")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 42, coolingRate: 4, startingThermal: 59, shapeShare: 50);

            // Velveteen Belt
            _builder.Create(RecipeType.VelveteenBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("chef_belt_2")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 44, coolingRate: 5, startingThermal: 63, shapeShare: 51);
        }

        private void Tier3()
        {
            // Quark Belt
            _builder.Create(RecipeType.QuarkBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("qk_belt")
                .Level(29)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 5)
                .Component("fiberp_good", 3)
                .ForgeParameters(rangeCenter: 41, rangeWidth: 54, coolingRate: 4, startingThermal: 52, shapeShare: 55);

            // Reginal Belt
            _builder.Create(RecipeType.ReginalBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("reg_belt")
                .Level(29)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 5)
                .Component("fiberp_good", 3)
                .ForgeParameters(rangeCenter: 36, rangeWidth: 58, coolingRate: 4, startingThermal: 48, shapeShare: 49);

            // Forza Belt
            _builder.Create(RecipeType.ForzaBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("for_belt")
                .Level(29)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 5)
                .Component("fiberp_good", 3)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 51, coolingRate: 2, startingThermal: 45, shapeShare: 54);

            // Majestic Belt
            _builder.Create(RecipeType.MajesticBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("majestic_belt")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 47, rangeWidth: 35, coolingRate: 3, startingThermal: 51, shapeShare: 50);

            // Mechanic Belt
            _builder.Create(RecipeType.MechanicBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("engi_belt_3")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 44, rangeWidth: 38, coolingRate: 3, startingThermal: 51, shapeShare: 50);

            // Designer Belt
            _builder.Create(RecipeType.DesignerBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("fabr_belt_3")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 44, rangeWidth: 35, coolingRate: 4, startingThermal: 52, shapeShare: 51);

            // Silk Belt
            _builder.Create(RecipeType.SilkBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("chef_belt_3")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 52, rangeWidth: 38, coolingRate: 4, startingThermal: 57, shapeShare: 54);
        }

        private void Tier4()
        {
            // Argos Belt
            _builder.Create(RecipeType.ArgosBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("ar_belt")
                .Level(39)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 5)
                .Component("fiberp_imperfect", 3)
                .ForgeParameters(rangeCenter: 41, rangeWidth: 50, coolingRate: 5, startingThermal: 42, shapeShare: 55);

            // Grenada Belt
            _builder.Create(RecipeType.GrenadaBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("gre_belt")
                .Level(39)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 5)
                .Component("fiberp_imperfect", 3)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 48, coolingRate: 3, startingThermal: 42, shapeShare: 51);

            // Survival Belt
            _builder.Create(RecipeType.SurvivalBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("sur_belt")
                .Level(39)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 5)
                .Component("fiberp_imperfect", 3)
                .ForgeParameters(rangeCenter: 45, rangeWidth: 48, coolingRate: 5, startingThermal: 44, shapeShare: 54);

            // Dream Belt
            _builder.Create(RecipeType.DreamBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("dream_belt")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 47, rangeWidth: 33, coolingRate: 5, startingThermal: 53, shapeShare: 54);

            // Devotion Belt
            _builder.Create(RecipeType.DevotionBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("engi_belt_4")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 50, rangeWidth: 33, coolingRate: 6, startingThermal: 48, shapeShare: 54);

            // Oasis Belt
            _builder.Create(RecipeType.OasisBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("fabr_belt_4")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 44, rangeWidth: 27, coolingRate: 6, startingThermal: 51, shapeShare: 49);

            // Vintage Belt
            _builder.Create(RecipeType.VintageBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("chef_belt_4")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 29, coolingRate: 4, startingThermal: 48, shapeShare: 54);
        }

        private void Tier5()
        {
            // Eclipse Belt
            _builder.Create(RecipeType.EclipseBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("ec_belt")
                .Level(49)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 5)
                .Component("fiberp_high", 3)
                .ForgeParameters(rangeCenter: 42, rangeWidth: 38, coolingRate: 6, startingThermal: 46, shapeShare: 55);

            // Transcendent Belt
            _builder.Create(RecipeType.TranscendentBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("tran_belt")
                .Level(49)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 5)
                .Component("fiberp_high", 3)
                .ForgeParameters(rangeCenter: 40, rangeWidth: 45, coolingRate: 6, startingThermal: 45, shapeShare: 50);

            // Supreme Belt
            _builder.Create(RecipeType.SupremeBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("sup_belt")
                .Level(49)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 5)
                .Component("fiberp_high", 3)
                .ForgeParameters(rangeCenter: 41, rangeWidth: 44, coolingRate: 5, startingThermal: 47, shapeShare: 50);

            // Eternal Belt
            _builder.Create(RecipeType.EternalBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("eternal_belt")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 23, coolingRate: 7, startingThermal: 49, shapeShare: 50);

            // Skysteel Belt
            _builder.Create(RecipeType.SkysteelBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("engi_belt_5")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3)
                .ForgeParameters(rangeCenter: 50, rangeWidth: 28, coolingRate: 7, startingThermal: 45, shapeShare: 49);

            // Rose Belt
            _builder.Create(RecipeType.RoseBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("fabr_belt_5")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3)
                .ForgeParameters(rangeCenter: 44, rangeWidth: 20, coolingRate: 7, startingThermal: 44, shapeShare: 54);

            // Moonflame Belt
            _builder.Create(RecipeType.MoonflameBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("chef_belt_5")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3)
                .ForgeParameters(rangeCenter: 50, rangeWidth: 28, coolingRate: 6, startingThermal: 52, shapeShare: 50);

            // Chaos Belt
            _builder.Create(RecipeType.ChaosBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("ch_belt")
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
                .ForgeParameters(rangeCenter: 47, rangeWidth: 24, coolingRate: 7, startingThermal: 48, shapeShare: 53);

            // Magus Belt
            _builder.Create(RecipeType.MagusBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("mag_belt")
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
                .ForgeParameters(rangeCenter: 45, rangeWidth: 26, coolingRate: 6, startingThermal: 44, shapeShare: 51);

            // Immortal Belt
            _builder.Create(RecipeType.ImmortalBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("imm_belt")
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
                .ForgeParameters(rangeCenter: 50, rangeWidth: 21, coolingRate: 6, startingThermal: 44, shapeShare: 54);
        }
        private void Tier1A()
        {
            // Warden Belt
            _builder.Create(RecipeType.WardenBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("fld_bul_belt")
                .Level(14)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 8)
                .Component("fiberp_ruined", 5)
                .Component("ref_veldite", 2)
                .ForgeParameters(rangeCenter: 49, rangeWidth: 47, coolingRate: 3, startingThermal: 64, shapeShare: 55);

            // Mystic Belt
            _builder.Create(RecipeType.MysticBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("fld_chn_belt")
                .Level(14)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 8)
                .Component("fiberp_ruined", 5)
                .Component("jade", 2)
                .ForgeParameters(rangeCenter: 42, rangeWidth: 57, coolingRate: 1, startingThermal: 53, shapeShare: 54);

            // Vanguard Belt
            _builder.Create(RecipeType.VanguardBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("fld_skm_belt")
                .Level(14)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 8)
                .Component("fiberp_ruined", 5)
                .Component("elec_ruined", 2)
                .ForgeParameters(rangeCenter: 42, rangeWidth: 58, coolingRate: 1, startingThermal: 53, shapeShare: 52);
        }

        private void Tier2A()
        {
            // Bastion Belt
            _builder.Create(RecipeType.BastionBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("vet_bul_belt")
                .Level(24)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 8)
                .Component("fiberp_flawed", 5)
                .Component("ref_scordspar", 2)
                .ForgeParameters(rangeCenter: 49, rangeWidth: 38, coolingRate: 4, startingThermal: 59, shapeShare: 53);

            // Oracle Belt
            _builder.Create(RecipeType.OracleBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("vet_chn_belt")
                .Level(24)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 8)
                .Component("fiberp_flawed", 5)
                .Component("agate", 2)
                .ForgeParameters(rangeCenter: 35, rangeWidth: 57, coolingRate: 2, startingThermal: 54, shapeShare: 50);

            // Onslaught Belt
            _builder.Create(RecipeType.OnslaughtBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("vet_skm_belt")
                .Level(24)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 8)
                .Component("fiberp_flawed", 5)
                .Component("elec_flawed", 2)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 53, coolingRate: 3, startingThermal: 52, shapeShare: 49);
        }

        private void Tier3A()
        {
            // Sentinel Belt
            _builder.Create(RecipeType.SentinelBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("prm_bul_belt")
                .Level(34)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 8)
                .Component("fiberp_good", 5)
                .Component("ref_plagionite", 2)
                .ForgeParameters(rangeCenter: 47, rangeWidth: 34, coolingRate: 4, startingThermal: 56, shapeShare: 55);

            // Arcanist Belt
            _builder.Create(RecipeType.ArcanistBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("prm_chn_belt")
                .Level(34)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 8)
                .Component("fiberp_good", 5)
                .Component("citrine", 2)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 52, coolingRate: 4, startingThermal: 47, shapeShare: 51);

            // Maverick Belt
            _builder.Create(RecipeType.MaverickBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("prm_skm_belt")
                .Level(34)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 8)
                .Component("fiberp_good", 5)
                .Component("elec_good", 2)
                .ForgeParameters(rangeCenter: 36, rangeWidth: 50, coolingRate: 5, startingThermal: 43, shapeShare: 54);
        }

        private void Tier4A()
        {
            // Aegis Belt
            _builder.Create(RecipeType.AegisBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("asc_bul_belt")
                .Level(44)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 8)
                .Component("fiberp_imperfect", 5)
                .Component("ref_keromber", 2)
                .ForgeParameters(rangeCenter: 45, rangeWidth: 30, coolingRate: 7, startingThermal: 51, shapeShare: 54);

            // Luminary Belt
            _builder.Create(RecipeType.LuminaryBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("asc_chn_belt")
                .Level(44)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 8)
                .Component("fiberp_imperfect", 5)
                .Component("ruby", 2)
                .ForgeParameters(rangeCenter: 40, rangeWidth: 44, coolingRate: 4, startingThermal: 42, shapeShare: 51);

            // Apex Belt
            _builder.Create(RecipeType.ApexBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("asc_skm_belt")
                .Level(44)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 8)
                .Component("fiberp_imperfect", 5)
                .Component("elec_imperfect", 2)
                .ForgeParameters(rangeCenter: 40, rangeWidth: 44, coolingRate: 4, startingThermal: 39, shapeShare: 53);
        }

}
}
