using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class NecklaceRecipes : IRecipeListDefinition
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
            // Battlemaster Necklace
            _builder.Create(RecipeType.BattlemasterNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("bm_necklace")
                .Level(2)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 2)
                .Component("jade", 1)
                .ForgeParameters(rangeCenter: 50, rangeWidth: 55, coolingRate: 1, startingThermal: 66, shapeShare: 47);

            // Spiritmaster Necklace
            _builder.Create(RecipeType.SpiritmasterNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("sm_necklace")
                .Level(2)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 2)
                .Component("jade", 1)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 57, coolingRate: 2, startingThermal: 68, shapeShare: 41);

            // Combat Necklace
            _builder.Create(RecipeType.CombatNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("com_necklace")
                .Level(2)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 2)
                .Component("jade", 1)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 55, coolingRate: 1, startingThermal: 73, shapeShare: 46);

            // Advent Necklace
            _builder.Create(RecipeType.AdventNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("advent_necklace")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 49, rangeWidth: 53, coolingRate: 3, startingThermal: 64, shapeShare: 45);

            // Amateur Necklace
            _builder.Create(RecipeType.AmateurNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("engi_necklace_1")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 45, rangeWidth: 46, coolingRate: 3, startingThermal: 61, shapeShare: 42);

            // Cloth Necklace
            _builder.Create(RecipeType.ClothNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("fabr_necklace_1")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 52, rangeWidth: 47, coolingRate: 3, startingThermal: 61, shapeShare: 42);

            // Chef Necklace
            _builder.Create(RecipeType.ChefNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("chef_necklace_1")
                .Level(10)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3)
                .ForgeParameters(rangeCenter: 44, rangeWidth: 46, coolingRate: 1, startingThermal: 65, shapeShare: 43);
        }

        private void Tier2()
        {
            // Titan Necklace
            _builder.Create(RecipeType.TitanNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("tit_necklace")
                .Level(12)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 2)
                .Component("agate", 1)
                .ForgeParameters(rangeCenter: 47, rangeWidth: 48, coolingRate: 1, startingThermal: 66, shapeShare: 43);

            // Vivid Necklace
            _builder.Create(RecipeType.VividNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("viv_necklace")
                .Level(12)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 2)
                .Component("agate", 1)
                .ForgeParameters(rangeCenter: 52, rangeWidth: 53, coolingRate: 1, startingThermal: 65, shapeShare: 45);

            // Valor Necklace
            _builder.Create(RecipeType.ValorNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("val_necklace")
                .Level(12)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 2)
                .Component("agate", 1)
                .ForgeParameters(rangeCenter: 50, rangeWidth: 46, coolingRate: 1, startingThermal: 64, shapeShare: 44);

            // Frontier Necklace
            _builder.Create(RecipeType.FrontierNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("frontier_necklac")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 52, rangeWidth: 42, coolingRate: 2, startingThermal: 60, shapeShare: 47);

            // Worker Necklace
            _builder.Create(RecipeType.WorkerNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("engi_necklace_2")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 46, rangeWidth: 42, coolingRate: 2, startingThermal: 58, shapeShare: 43);

            // Linen Necklace
            _builder.Create(RecipeType.LinenNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("fabr_necklace_2")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 46, coolingRate: 2, startingThermal: 58, shapeShare: 42);

            // Velveteen Necklace
            _builder.Create(RecipeType.VelveteenNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("chef_necklace_2")
                .Level(20)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3)
                .ForgeParameters(rangeCenter: 46, rangeWidth: 42, coolingRate: 4, startingThermal: 56, shapeShare: 44);
        }

        private void Tier3()
        {
            // Quark Necklace
            _builder.Create(RecipeType.QuarkNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("qk_necklace")
                .Level(22)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 2)
                .Component("citrine", 1)
                .ForgeParameters(rangeCenter: 44, rangeWidth: 41, coolingRate: 4, startingThermal: 60, shapeShare: 47);

            // Reginal Necklace
            _builder.Create(RecipeType.ReginalNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("reg_necklace")
                .Level(22)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 2)
                .Component("citrine", 1)
                .ForgeParameters(rangeCenter: 52, rangeWidth: 44, coolingRate: 4, startingThermal: 59, shapeShare: 45);

            // Forza Necklace
            _builder.Create(RecipeType.ForzaNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("for_necklace")
                .Level(22)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 2)
                .Component("citrine", 1)
                .ForgeParameters(rangeCenter: 50, rangeWidth: 46, coolingRate: 3, startingThermal: 54, shapeShare: 43);

            // Majestic Necklace
            _builder.Create(RecipeType.MajesticNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("majestic_necklac")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 46, rangeWidth: 33, coolingRate: 5, startingThermal: 58, shapeShare: 44);

            // Mechanic Necklace
            _builder.Create(RecipeType.MechanicNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("engi_necklace_3")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 47, rangeWidth: 34, coolingRate: 4, startingThermal: 57, shapeShare: 44);

            // Designer Necklace
            _builder.Create(RecipeType.DesignerNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("fabr_necklace_3")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 49, rangeWidth: 39, coolingRate: 4, startingThermal: 50, shapeShare: 43);

            // Silk Necklace
            _builder.Create(RecipeType.SilkNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("chef_necklace_3")
                .Level(30)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 34, coolingRate: 3, startingThermal: 53, shapeShare: 45);
        }

        private void Tier4()
        {
            // Argos Necklace
            _builder.Create(RecipeType.ArgosNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("ar_necklace")
                .Level(32)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 2)
                .Component("ruby", 1)
                .ForgeParameters(rangeCenter: 47, rangeWidth: 34, coolingRate: 5, startingThermal: 55, shapeShare: 47);

            // Grenada Necklace
            _builder.Create(RecipeType.GrenadaNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("gr_necklace")
                .Level(32)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 2)
                .Component("ruby", 1)
                .ForgeParameters(rangeCenter: 44, rangeWidth: 36, coolingRate: 3, startingThermal: 54, shapeShare: 41);

            // Survival Necklace
            _builder.Create(RecipeType.SurvivalNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("sur_necklace")
                .Level(32)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 2)
                .Component("ruby", 1)
                .ForgeParameters(rangeCenter: 49, rangeWidth: 34, coolingRate: 3, startingThermal: 51, shapeShare: 47);

            // Dream Necklace
            _builder.Create(RecipeType.DreamNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("dream_necklace")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 35, coolingRate: 6, startingThermal: 47, shapeShare: 41);

            // Devotion Necklace
            _builder.Create(RecipeType.DevotionNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("engi_necklace_4")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 45, rangeWidth: 27, coolingRate: 6, startingThermal: 52, shapeShare: 47);

            // Oasis Necklace
            _builder.Create(RecipeType.OasisNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("fabr_necklace_4")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 30, coolingRate: 4, startingThermal: 49, shapeShare: 44);

            // Vintage Necklace
            _builder.Create(RecipeType.VintageNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("chef_necklace_4")
                .Level(40)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3)
                .ForgeParameters(rangeCenter: 45, rangeWidth: 31, coolingRate: 4, startingThermal: 52, shapeShare: 44);
        }

        private void Tier5()
        {
            // Eclipse Necklace
            _builder.Create(RecipeType.EclipseNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("ec_necklace")
                .Level(42)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 2)
                .Component("emerald", 1)
                .ForgeParameters(rangeCenter: 47, rangeWidth: 27, coolingRate: 6, startingThermal: 47, shapeShare: 45);

            // Transcendent Necklace
            _builder.Create(RecipeType.TranscendentNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("tran_necklace")
                .Level(42)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 2)
                .Component("emerald", 1)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 31, coolingRate: 6, startingThermal: 47, shapeShare: 43);

            // Supreme Necklace
            _builder.Create(RecipeType.SupremeNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("sup_necklace")
                .Level(42)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 2)
                .Component("emerald", 1)
                .ForgeParameters(rangeCenter: 47, rangeWidth: 33, coolingRate: 5, startingThermal: 48, shapeShare: 43);

            // Eternal Necklace
            _builder.Create(RecipeType.EternalNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("eternal_necklace")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3)
                .ForgeParameters(rangeCenter: 52, rangeWidth: 26, coolingRate: 5, startingThermal: 51, shapeShare: 42);

            // Skysteel Necklace
            _builder.Create(RecipeType.SkysteelNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("engi_necklace_5")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3)
                .ForgeParameters(rangeCenter: 49, rangeWidth: 22, coolingRate: 5, startingThermal: 52, shapeShare: 42);

            // Rose Necklace
            _builder.Create(RecipeType.RoseNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("fabr_necklace_5")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3)
                .ForgeParameters(rangeCenter: 52, rangeWidth: 26, coolingRate: 6, startingThermal: 48, shapeShare: 45);

            // Moonflame Necklace
            _builder.Create(RecipeType.MoonflameNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("chef_necklace_5")
                .Level(50)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 26, coolingRate: 7, startingThermal: 48, shapeShare: 43);

            // Chaos Necklace
            _builder.Create(RecipeType.ChaosNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("ch_necklace")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("emerald", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 49, rangeWidth: 21, coolingRate: 7, startingThermal: 49, shapeShare: 44);

            // Magus Necklace
            _builder.Create(RecipeType.MagusNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("mag_necklace")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("emerald", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 52, rangeWidth: 20, coolingRate: 7, startingThermal: 45, shapeShare: 47);

            // Immortal Necklace
            _builder.Create(RecipeType.ImmortalNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("imm_necklace")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("emerald", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 24, coolingRate: 6, startingThermal: 48, shapeShare: 42);
        }

        private void Tier1A()
        {
            // Warden Necklace
            _builder.Create(RecipeType.WardenNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("fld_bul_neck")
                .Level(7)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 3)
                .Component("jade", 2)
                .Component("lth_ruined", 1)
                .ForgeParameters(rangeCenter: 45, rangeWidth: 51, coolingRate: 1, startingThermal: 62, shapeShare: 44);

            // Mystic Necklace
            _builder.Create(RecipeType.MysticNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("fld_chn_neck")
                .Level(7)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 3)
                .Component("jade", 2)
                .Component("fiberp_ruined", 1)
                .ForgeParameters(rangeCenter: 52, rangeWidth: 54, coolingRate: 3, startingThermal: 66, shapeShare: 46);

            // Vanguard Necklace
            _builder.Create(RecipeType.VanguardNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("fld_skm_neck")
                .Level(7)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 3)
                .Component("jade", 2)
                .Component("elec_ruined", 1)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 51, coolingRate: 1, startingThermal: 69, shapeShare: 47);
        }

        private void Tier2A()
        {
            // Bastion Necklace
            _builder.Create(RecipeType.BastionNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("vet_bul_neck")
                .Level(17)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 3)
                .Component("agate", 2)
                .Component("lth_flawed", 1)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 45, coolingRate: 2, startingThermal: 64, shapeShare: 44);

            // Oracle Necklace
            _builder.Create(RecipeType.OracleNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("vet_chn_neck")
                .Level(17)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 3)
                .Component("agate", 2)
                .Component("fiberp_flawed", 1)
                .ForgeParameters(rangeCenter: 44, rangeWidth: 47, coolingRate: 4, startingThermal: 56, shapeShare: 42);

            // Onslaught Necklace
            _builder.Create(RecipeType.OnslaughtNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("vet_skm_neck")
                .Level(17)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 3)
                .Component("agate", 2)
                .Component("elec_flawed", 1)
                .ForgeParameters(rangeCenter: 49, rangeWidth: 48, coolingRate: 2, startingThermal: 62, shapeShare: 47);
        }

        private void Tier3A()
        {
            // Sentinel Necklace
            _builder.Create(RecipeType.SentinelNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("prm_bul_neck")
                .Level(27)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 3)
                .Component("citrine", 2)
                .Component("lth_good", 1)
                .ForgeParameters(rangeCenter: 49, rangeWidth: 40, coolingRate: 4, startingThermal: 56, shapeShare: 43);

            // Arcanist Necklace
            _builder.Create(RecipeType.ArcanistNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("prm_chn_neck")
                .Level(27)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 3)
                .Component("citrine", 2)
                .Component("fiberp_good", 1)
                .ForgeParameters(rangeCenter: 51, rangeWidth: 43, coolingRate: 5, startingThermal: 60, shapeShare: 45);

            // Maverick Necklace
            _builder.Create(RecipeType.MaverickNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("prm_skm_neck")
                .Level(27)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 3)
                .Component("citrine", 2)
                .Component("elec_good", 1)
                .ForgeParameters(rangeCenter: 47, rangeWidth: 35, coolingRate: 4, startingThermal: 53, shapeShare: 41);
        }

        private void Tier4A()
        {
            // Aegis Necklace
            _builder.Create(RecipeType.AegisNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("asc_bul_neck")
                .Level(37)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 3)
                .Component("ruby", 2)
                .Component("lth_imperfect", 1)
                .ForgeParameters(rangeCenter: 44, rangeWidth: 31, coolingRate: 4, startingThermal: 50, shapeShare: 42);

            // Luminary Necklace
            _builder.Create(RecipeType.LuminaryNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("asc_chn_neck")
                .Level(37)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 3)
                .Component("ruby", 2)
                .Component("fiberp_imperfect", 1)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 30, coolingRate: 6, startingThermal: 50, shapeShare: 44);

            // Apex Necklace
            _builder.Create(RecipeType.ApexNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("asc_skm_neck")
                .Level(37)
                .Quantity(1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 3)
                .Component("ruby", 2)
                .Component("elec_imperfect", 1)
                .ForgeParameters(rangeCenter: 48, rangeWidth: 32, coolingRate: 5, startingThermal: 55, shapeShare: 44);
        }

}
}
