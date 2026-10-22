using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class FeetRecipes: IRecipeListDefinition
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
            // Battlemaster Leggings
            _builder.Create(RecipeType.BattlemasterLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("bm_leggings")
                .Level(5)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 3)
                .Component("fiberp_ruined", 2)
                .ForgeParameters(rangeCenter: 57, rangeWidth: 55, coolingRate: 2, startingThermal: 74, shapeShare: 59);

            // Spiritmaster Boots
            _builder.Create(RecipeType.SpiritmasterBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("sm_boots")
                .Level(5)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 3)
                .Component("fiberp_ruined", 2)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 58, coolingRate: 1, startingThermal: 59, shapeShare: 53);

            // Combat Boots
            _builder.Create(RecipeType.CombatBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("com_boots")
                .Level(5)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .ForgeParameters(rangeCenter: 36, rangeWidth: 58, coolingRate: 1, startingThermal: 63, shapeShare: 58);

            // Advent Leggings
            _builder.Create(RecipeType.AdventLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("advent_leggings")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 60, rangeWidth: 51, coolingRate: 4, startingThermal: 72, shapeShare: 57);

            // Amateur Leggings
            _builder.Create(RecipeType.AmateurLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("engi_leggings_1")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 47, coolingRate: 3, startingThermal: 74, shapeShare: 63);

            // Cloth Leggings
            _builder.Create(RecipeType.ClothLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("fabr_leggings_1")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 56, rangeWidth: 46, coolingRate: 2, startingThermal: 71, shapeShare: 61);

            // Chef Leggings
            _builder.Create(RecipeType.ChefLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("chef_leggings_1")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 58, rangeWidth: 48, coolingRate: 3, startingThermal: 73, shapeShare: 61);
        }

        private void Tier2()
        {
            // Titan Leggings
            _builder.Create(RecipeType.TitanLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("tit_leggings")
                .Level(15)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 3)
                .Component("fiberp_flawed", 2)
                .ForgeParameters(rangeCenter: 62, rangeWidth: 51, coolingRate: 2, startingThermal: 74, shapeShare: 61);

            // Vivid Boots
            _builder.Create(RecipeType.VividBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("viv_boots")
                .Level(15)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 60, coolingRate: 3, startingThermal: 53, shapeShare: 58);

            // Valor Boots
            _builder.Create(RecipeType.ValorBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("val_boots")
                .Level(15)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .ForgeParameters(rangeCenter: 40, rangeWidth: 60, coolingRate: 2, startingThermal: 56, shapeShare: 57);

            // Frontier Leggings
            _builder.Create(RecipeType.FrontierLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("frontier_legging")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 57, rangeWidth: 40, coolingRate: 3, startingThermal: 72, shapeShare: 62);

            // Worker Leggings
            _builder.Create(RecipeType.WorkerLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("engi_leggings_2")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 59, rangeWidth: 44, coolingRate: 4, startingThermal: 69, shapeShare: 63);

            // Linen Leggings
            _builder.Create(RecipeType.LinenLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("fabr_leggings_2")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 41, coolingRate: 4, startingThermal: 69, shapeShare: 61);

            // Velveteen Leggings
            _builder.Create(RecipeType.VelveteenLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("chef_leggings_2")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 44, coolingRate: 4, startingThermal: 68, shapeShare: 63);
        }

        private void Tier3()
        {
            // Quark Leggings
            _builder.Create(RecipeType.QuarkLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("qk_leggings")
                .Level(25)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 3)
                .Component("fiberp_good", 2)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 41, coolingRate: 3, startingThermal: 70, shapeShare: 62);

            // Reginal Boots
            _builder.Create(RecipeType.ReginalBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("reg_boots")
                .Level(25)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 3)
                .Component("fiberp_good", 2)
                .ForgeParameters(rangeCenter: 42, rangeWidth: 58, coolingRate: 2, startingThermal: 50, shapeShare: 56);

            // Forza Boots
            _builder.Create(RecipeType.ForzaBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("for_boots")
                .Level(25)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 2)
                .Component("fiberp_good", 1)
                .ForgeParameters(rangeCenter: 43, rangeWidth: 60, coolingRate: 4, startingThermal: 48, shapeShare: 59);

            // Majestic Leggings
            _builder.Create(RecipeType.MajesticLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("majestic_legging")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 56, rangeWidth: 39, coolingRate: 4, startingThermal: 63, shapeShare: 60);

            // Mechanic Leggings
            _builder.Create(RecipeType.MechanicLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("engi_leggings_3")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 60, rangeWidth: 33, coolingRate: 3, startingThermal: 63, shapeShare: 63);

            // Designer Leggings
            _builder.Create(RecipeType.DesignerLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("fabr_leggings_3")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 56, rangeWidth: 41, coolingRate: 4, startingThermal: 64, shapeShare: 62);

            // Silk Leggings
            _builder.Create(RecipeType.SilkLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("chef_leggings_3")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 37, coolingRate: 5, startingThermal: 60, shapeShare: 61);
        }

        private void Tier4()
        {
            // Argos Leggings
            _builder.Create(RecipeType.ArgosLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("ar_leggings")
                .Level(35)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 3)
                .Component("fiberp_imperfect", 2)
                .ForgeParameters(rangeCenter: 57, rangeWidth: 31, coolingRate: 4, startingThermal: 66, shapeShare: 59);

            // Grenada Boots
            _builder.Create(RecipeType.GrenadaBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("gr_boots")
                .Level(35)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 3)
                .Component("fiberp_imperfect", 2)
                .ForgeParameters(rangeCenter: 42, rangeWidth: 54, coolingRate: 5, startingThermal: 42, shapeShare: 57);

            // Survival Boots
            _builder.Create(RecipeType.SurvivalBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("sur_boots")
                .Level(35)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 2)
                .Component("fiberp_imperfect", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 48, coolingRate: 5, startingThermal: 42, shapeShare: 58);

            // Dream Leggings
            _builder.Create(RecipeType.DreamLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("dream_leggings")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 54, rangeWidth: 32, coolingRate: 4, startingThermal: 57, shapeShare: 61);

            // Devotion Leggings
            _builder.Create(RecipeType.DevotionLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("engi_leggings_4")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 54, rangeWidth: 27, coolingRate: 5, startingThermal: 62, shapeShare: 58);

            // Oasis Leggings
            _builder.Create(RecipeType.OasisLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("fabr_leggings_4")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 62, rangeWidth: 34, coolingRate: 5, startingThermal: 59, shapeShare: 59);

            // Vintage Leggings
            _builder.Create(RecipeType.VintageLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("chef_leggings_4")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 60, rangeWidth: 32, coolingRate: 6, startingThermal: 63, shapeShare: 63);
        }

        private void Tier5()
        {
            // Eclipse Leggings
            _builder.Create(RecipeType.EclipseLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("ec_leggings")
                .Level(45)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 3)
                .Component("fiberp_high", 2)
                .ForgeParameters(rangeCenter: 60, rangeWidth: 23, coolingRate: 5, startingThermal: 59, shapeShare: 61);

            // Transcendent Boots
            _builder.Create(RecipeType.TranscendentBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("tran_boots")
                .Level(45)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 3)
                .Component("fiberp_high", 2)
                .ForgeParameters(rangeCenter: 44, rangeWidth: 40, coolingRate: 6, startingThermal: 40, shapeShare: 59);

            // Supreme Boots
            _builder.Create(RecipeType.SupremeBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("sup_boots")
                .Level(45)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 2)
                .Component("fiberp_high", 1)
                .ForgeParameters(rangeCenter: 40, rangeWidth: 43, coolingRate: 6, startingThermal: 41, shapeShare: 53);

            // Eternal Leggings
            _builder.Create(RecipeType.EternalLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("eternal_leggings")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 23, coolingRate: 7, startingThermal: 62, shapeShare: 59);

            // Skysteel Leggings
            _builder.Create(RecipeType.SkysteelLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("engi_leggings_5")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3)
                .ForgeParameters(rangeCenter: 62, rangeWidth: 23, coolingRate: 7, startingThermal: 57, shapeShare: 63);

            // Rose Leggings
            _builder.Create(RecipeType.RoseLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("fabr_leggings_5")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3)
                .ForgeParameters(rangeCenter: 57, rangeWidth: 21, coolingRate: 5, startingThermal: 62, shapeShare: 59);

            // Moonflame Leggings
            _builder.Create(RecipeType.MoonflameLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("chef_leggings_5")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 21, coolingRate: 5, startingThermal: 62, shapeShare: 61);

            // Chaos Leggings
            _builder.Create(RecipeType.ChaosLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("ch_leggings")
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
                .ForgeParameters(rangeCenter: 57, rangeWidth: 22, coolingRate: 6, startingThermal: 61, shapeShare: 61);

            // Magus Boots
            _builder.Create(RecipeType.MagusBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("mag_boots")
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
                .ForgeParameters(rangeCenter: 57, rangeWidth: 22, coolingRate: 7, startingThermal: 57, shapeShare: 55);

            // Immortal Boots
            _builder.Create(RecipeType.ImmortalBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("imm_boots")
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
                .ForgeParameters(rangeCenter: 61, rangeWidth: 26, coolingRate: 6, startingThermal: 57, shapeShare: 53);
        }
        private void Tier1A()
        {
            // Warden Leggings
            _builder.Create(RecipeType.WardenLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("fld_bul_legs")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("fiberp_ruined", 3)
                .Component("lth_ruined", 2)
                .ForgeParameters(rangeCenter: 60, rangeWidth: 48, coolingRate: 2, startingThermal: 77, shapeShare: 62);

            // Mystic Boots
            _builder.Create(RecipeType.MysticBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("fld_chn_boots")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 5)
                .Component("fiberp_ruined", 3)
                .Component("jade", 2)
                .ForgeParameters(rangeCenter: 34, rangeWidth: 60, coolingRate: 2, startingThermal: 57, shapeShare: 57);

            // Vanguard Boots
            _builder.Create(RecipeType.VanguardBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("fld_skm_boots")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 5)
                .Component("fiberp_ruined", 3)
                .Component("elec_ruined", 2)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 60, coolingRate: 3, startingThermal: 55, shapeShare: 54);
        }

        private void Tier2A()
        {
            // Bastion Leggings
            _builder.Create(RecipeType.BastionLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("vet_bul_legs")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("fiberp_flawed", 3)
                .Component("lth_flawed", 2)
                .ForgeParameters(rangeCenter: 59, rangeWidth: 44, coolingRate: 4, startingThermal: 72, shapeShare: 58);

            // Oracle Boots
            _builder.Create(RecipeType.OracleBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("vet_chn_boots")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 5)
                .Component("fiberp_flawed", 3)
                .Component("agate", 2)
                .ForgeParameters(rangeCenter: 42, rangeWidth: 60, coolingRate: 4, startingThermal: 54, shapeShare: 53);

            // Onslaught Boots
            _builder.Create(RecipeType.OnslaughtBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("vet_skm_boots")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 5)
                .Component("fiberp_flawed", 3)
                .Component("elec_flawed", 2)
                .ForgeParameters(rangeCenter: 40, rangeWidth: 60, coolingRate: 4, startingThermal: 55, shapeShare: 54);
        }

        private void Tier3A()
        {
            // Sentinel Leggings
            _builder.Create(RecipeType.SentinelLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("prm_bul_legs")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("fiberp_good", 3)
                .Component("lth_good", 2)
                .ForgeParameters(rangeCenter: 61, rangeWidth: 35, coolingRate: 4, startingThermal: 66, shapeShare: 59);

            // Arcanist Boots
            _builder.Create(RecipeType.ArcanistBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("prm_chn_boots")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 5)
                .Component("fiberp_good", 3)
                .Component("citrine", 2)
                .ForgeParameters(rangeCenter: 44, rangeWidth: 50, coolingRate: 4, startingThermal: 45, shapeShare: 56);

            // Maverick Boots
            _builder.Create(RecipeType.MaverickBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("prm_skm_boots")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 5)
                .Component("fiberp_good", 3)
                .Component("elec_good", 2)
                .ForgeParameters(rangeCenter: 42, rangeWidth: 49, coolingRate: 2, startingThermal: 46, shapeShare: 54);
        }

        private void Tier4A()
        {
            // Aegis Leggings
            _builder.Create(RecipeType.AegisLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("asc_bul_legs")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("fiberp_imperfect", 3)
                .Component("lth_imperfect", 2)
                .ForgeParameters(rangeCenter: 62, rangeWidth: 27, coolingRate: 5, startingThermal: 57, shapeShare: 57);

            // Luminary Boots
            _builder.Create(RecipeType.LuminaryBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("asc_chn_boots")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 5)
                .Component("fiberp_imperfect", 3)
                .Component("ruby", 2)
                .ForgeParameters(rangeCenter: 41, rangeWidth: 43, coolingRate: 3, startingThermal: 42, shapeShare: 59);

            // Apex Boots
            _builder.Create(RecipeType.ApexBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("asc_skm_boots")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 5)
                .Component("fiberp_imperfect", 3)
                .Component("elec_imperfect", 2)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 47, coolingRate: 5, startingThermal: 46, shapeShare: 54);
        }

}
}
