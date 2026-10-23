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
                .ForgeParameters(rangeCenter: 58, rangeWidth: 32, coolingRate: 1, startingThermal: 62, shapeShare: 70);

            // Spiritmaster Boots
            _builder.Create(RecipeType.SpiritmasterBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("sm_boots")
                .Level(5)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 3)
                .Component("fiberp_ruined", 2)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 48, coolingRate: 1, startingThermal: 46, shapeShare: 62);

            // Combat Boots
            _builder.Create(RecipeType.CombatBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("com_boots")
                .Level(5)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 55, coolingRate: 1, startingThermal: 49, shapeShare: 62);

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
                .ForgeParameters(rangeCenter: 57, rangeWidth: 39, coolingRate: 2, startingThermal: 64, shapeShare: 70);

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
                .ForgeParameters(rangeCenter: 60, rangeWidth: 36, coolingRate: 1, startingThermal: 66, shapeShare: 70);

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
                .ForgeParameters(rangeCenter: 56, rangeWidth: 36, coolingRate: 1, startingThermal: 62, shapeShare: 70);

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
                .ForgeParameters(rangeCenter: 58, rangeWidth: 34, coolingRate: 1, startingThermal: 63, shapeShare: 70);
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
                .ForgeParameters(rangeCenter: 57, rangeWidth: 33, coolingRate: 3, startingThermal: 67, shapeShare: 70);

            // Vivid Boots
            _builder.Create(RecipeType.VividBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("viv_boots")
                .Level(15)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2)
                .ForgeParameters(rangeCenter: 38, rangeWidth: 58, coolingRate: 1, startingThermal: 54, shapeShare: 63);

            // Valor Boots
            _builder.Create(RecipeType.ValorBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("val_boots")
                .Level(15)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .ForgeParameters(rangeCenter: 38, rangeWidth: 60, coolingRate: 5, startingThermal: 55, shapeShare: 62);

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
                .ForgeParameters(rangeCenter: 61, rangeWidth: 36, coolingRate: 1, startingThermal: 70, shapeShare: 71);

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
                .ForgeParameters(rangeCenter: 58, rangeWidth: 44, coolingRate: 6, startingThermal: 63, shapeShare: 69);

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
                .ForgeParameters(rangeCenter: 58, rangeWidth: 43, coolingRate: 3, startingThermal: 71, shapeShare: 71);

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
                .ForgeParameters(rangeCenter: 54, rangeWidth: 44, coolingRate: 2, startingThermal: 63, shapeShare: 69);
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
                .ForgeParameters(rangeCenter: 58, rangeWidth: 22, coolingRate: 2, startingThermal: 59, shapeShare: 70);

            // Reginal Boots
            _builder.Create(RecipeType.ReginalBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("reg_boots")
                .Level(25)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 3)
                .Component("fiberp_good", 2)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 42, coolingRate: 4, startingThermal: 33, shapeShare: 62);

            // Forza Boots
            _builder.Create(RecipeType.ForzaBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("for_boots")
                .Level(25)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 2)
                .Component("fiberp_good", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 36, coolingRate: 2, startingThermal: 43, shapeShare: 62);

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
                .ForgeParameters(rangeCenter: 59, rangeWidth: 21, coolingRate: 5, startingThermal: 49, shapeShare: 70);

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
                .ForgeParameters(rangeCenter: 58, rangeWidth: 23, coolingRate: 4, startingThermal: 48, shapeShare: 70);

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
                .ForgeParameters(rangeCenter: 58, rangeWidth: 25, coolingRate: 5, startingThermal: 52, shapeShare: 70);

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
                .ForgeParameters(rangeCenter: 59, rangeWidth: 25, coolingRate: 5, startingThermal: 50, shapeShare: 70);
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
                .ForgeParameters(rangeCenter: 57, rangeWidth: 25, coolingRate: 4, startingThermal: 60, shapeShare: 70);

            // Grenada Boots
            _builder.Create(RecipeType.GrenadaBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("gr_boots")
                .Level(35)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 3)
                .Component("fiberp_imperfect", 2)
                .ForgeParameters(rangeCenter: 40, rangeWidth: 43, coolingRate: 3, startingThermal: 46, shapeShare: 62);

            // Survival Boots
            _builder.Create(RecipeType.SurvivalBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("sur_boots")
                .Level(35)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 2)
                .Component("fiberp_imperfect", 1)
                .ForgeParameters(rangeCenter: 40, rangeWidth: 37, coolingRate: 3, startingThermal: 45, shapeShare: 62);

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
                .ForgeParameters(rangeCenter: 59, rangeWidth: 23, coolingRate: 4, startingThermal: 61, shapeShare: 70);

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
                .ForgeParameters(rangeCenter: 55, rangeWidth: 26, coolingRate: 4, startingThermal: 57, shapeShare: 70);

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
                .ForgeParameters(rangeCenter: 59, rangeWidth: 25, coolingRate: 4, startingThermal: 61, shapeShare: 70);

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
                .ForgeParameters(rangeCenter: 59, rangeWidth: 21, coolingRate: 4, startingThermal: 61, shapeShare: 70);
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
                .ForgeParameters(rangeCenter: 56, rangeWidth: 24, coolingRate: 3, startingThermal: 54, shapeShare: 70);

            // Transcendent Boots
            _builder.Create(RecipeType.TranscendentBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("tran_boots")
                .Level(45)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 3)
                .Component("fiberp_high", 2)
                .ForgeParameters(rangeCenter: 41, rangeWidth: 40, coolingRate: 2, startingThermal: 42, shapeShare: 62);

            // Supreme Boots
            _builder.Create(RecipeType.SupremeBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("sup_boots")
                .Level(45)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 2)
                .Component("fiberp_high", 1)
                .ForgeParameters(rangeCenter: 41, rangeWidth: 40, coolingRate: 2, startingThermal: 44, shapeShare: 62);

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
                .ForgeParameters(rangeCenter: 60, rangeWidth: 24, coolingRate: 4, startingThermal: 61, shapeShare: 70);

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
                .ForgeParameters(rangeCenter: 54, rangeWidth: 24, coolingRate: 4, startingThermal: 60, shapeShare: 70);

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
                .ForgeParameters(rangeCenter: 55, rangeWidth: 24, coolingRate: 5, startingThermal: 59, shapeShare: 70);

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
                .ForgeParameters(rangeCenter: 55, rangeWidth: 24, coolingRate: 4, startingThermal: 56, shapeShare: 70);

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
                .ForgeParameters(rangeCenter: 58, rangeWidth: 24, coolingRate: 4, startingThermal: 64, shapeShare: 70);

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
                .ForgeParameters(rangeCenter: 61, rangeWidth: 24, coolingRate: 5, startingThermal: 67, shapeShare: 62);

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
                .ForgeParameters(rangeCenter: 61, rangeWidth: 24, coolingRate: 5, startingThermal: 69, shapeShare: 62);
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
                .ForgeParameters(rangeCenter: 59, rangeWidth: 33, coolingRate: 1, startingThermal: 64, shapeShare: 70);

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
                .ForgeParameters(rangeCenter: 37, rangeWidth: 50, coolingRate: 1, startingThermal: 47, shapeShare: 62);

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
                .ForgeParameters(rangeCenter: 37, rangeWidth: 57, coolingRate: 1, startingThermal: 50, shapeShare: 62);
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
                .ForgeParameters(rangeCenter: 55, rangeWidth: 47, coolingRate: 1, startingThermal: 63, shapeShare: 69);

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
                .ForgeParameters(rangeCenter: 38, rangeWidth: 58, coolingRate: 5, startingThermal: 49, shapeShare: 61);

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
                .ForgeParameters(rangeCenter: 38, rangeWidth: 56, coolingRate: 1, startingThermal: 51, shapeShare: 63);
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
                .ForgeParameters(rangeCenter: 59, rangeWidth: 21, coolingRate: 5, startingThermal: 63, shapeShare: 70);

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
                .ForgeParameters(rangeCenter: 40, rangeWidth: 35, coolingRate: 4, startingThermal: 48, shapeShare: 62);

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
                .ForgeParameters(rangeCenter: 40, rangeWidth: 42, coolingRate: 1, startingThermal: 41, shapeShare: 62);
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
                .ForgeParameters(rangeCenter: 58, rangeWidth: 24, coolingRate: 4, startingThermal: 58, shapeShare: 70);

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
                .ForgeParameters(rangeCenter: 41, rangeWidth: 40, coolingRate: 3, startingThermal: 44, shapeShare: 62);

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
                .ForgeParameters(rangeCenter: 41, rangeWidth: 42, coolingRate: 3, startingThermal: 45, shapeShare: 62);
        }

}
}
