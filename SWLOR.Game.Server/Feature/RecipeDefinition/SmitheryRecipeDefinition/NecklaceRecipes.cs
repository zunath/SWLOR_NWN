using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class NecklaceRecipes : IRecipeListDefinition
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
            // Battlemaster Necklace
            _builder.Create(RecipeType.BattlemasterNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("bm_necklace")
                .Level(2)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 2)
                .Component("jade", 1);

            // Spiritmaster Necklace
            _builder.Create(RecipeType.SpiritmasterNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("sm_necklace")
                .Level(2)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 2)
                .Component("jade", 1);

            // Combat Necklace
            _builder.Create(RecipeType.CombatNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("com_necklace")
                .Level(2)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 2)
                .Component("jade", 1);

            // Advent Necklace
            _builder.Create(RecipeType.AdventNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("advent_necklace")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // Amateur Necklace
            _builder.Create(RecipeType.AmateurNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("engi_necklace_1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // Cloth Necklace
            _builder.Create(RecipeType.ClothNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("fabr_necklace_1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // Chef Necklace
            _builder.Create(RecipeType.ChefNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("chef_necklace_1")
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
            // Titan Necklace
            _builder.Create(RecipeType.TitanNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("tit_necklace")
                .Level(12)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 2)
                .Component("agate", 1);

            // Vivid Necklace
            _builder.Create(RecipeType.VividNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("viv_necklace")
                .Level(12)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 2)
                .Component("agate", 1);

            // Valor Necklace
            _builder.Create(RecipeType.ValorNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("val_necklace")
                .Level(12)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 2)
                .Component("agate", 1);

            // Frontier Necklace
            _builder.Create(RecipeType.FrontierNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("frontier_necklace")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // Worker Necklace
            _builder.Create(RecipeType.WorkerNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("engi_necklace_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // Linen Necklace
            _builder.Create(RecipeType.LinenNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("fabr_necklace_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // Velveteen Necklace
            _builder.Create(RecipeType.VelveteenNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("chef_necklace_2")
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
            // Quark Necklace
            _builder.Create(RecipeType.QuarkNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("qk_necklace")
                .Level(22)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 2)
                .Component("citrine", 1);

            // Reginal Necklace
            _builder.Create(RecipeType.ReginalNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("reg_necklace")
                .Level(22)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 2)
                .Component("citrine", 1);

            // Forza Necklace
            _builder.Create(RecipeType.ForzaNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("for_necklace")
                .Level(22)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 2)
                .Component("citrine", 1);

            // Majestic Necklace
            _builder.Create(RecipeType.MajesticNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("majestic_necklac")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // Mechanic Necklace
            _builder.Create(RecipeType.MechanicNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("engi_necklace_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // Designer Necklace
            _builder.Create(RecipeType.DesignerNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("fabr_necklace_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // Silk Necklace
            _builder.Create(RecipeType.SilkNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("chef_necklace_3")
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
            // Argos Necklace
            _builder.Create(RecipeType.ArgosNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("ar_necklace")
                .Level(32)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 2)
                .Component("ruby", 1);

            // Grenada Necklace
            _builder.Create(RecipeType.GrenadaNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("gr_necklace")
                .Level(32)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 2)
                .Component("ruby", 1);

            // Survival Necklace
            _builder.Create(RecipeType.SurvivalNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("sur_necklace")
                .Level(32)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 2)
                .Component("ruby", 1);

            // Dream Necklace
            _builder.Create(RecipeType.DreamNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("dream_necklace")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // Devotion Necklace
            _builder.Create(RecipeType.DevotionNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("engi_necklace_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // Oasis Necklace
            _builder.Create(RecipeType.OasisNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("fabr_necklace_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // Vintage Necklace
            _builder.Create(RecipeType.VintageNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("chef_necklace_4")
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
            // Eclipse Necklace
            _builder.Create(RecipeType.EclipseNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("ec_necklace")
                .Level(42)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 2)
                .Component("emerald", 1);

            // Transcendent Necklace
            _builder.Create(RecipeType.TranscendentNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("tran_necklace")
                .Level(42)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 2)
                .Component("emerald", 1);

            // Supreme Necklace
            _builder.Create(RecipeType.SupremeNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("sup_necklace")
                .Level(42)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 2)
                .Component("emerald", 1);

            // Eternal Necklace
            _builder.Create(RecipeType.EternalNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("eternal_necklace")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Skysteel Necklace
            _builder.Create(RecipeType.SkysteelNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("engi_necklace_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Rose Necklace
            _builder.Create(RecipeType.RoseNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("fabr_necklace_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Moonflame Necklace
            _builder.Create(RecipeType.MoonflameNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("chef_necklace_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Chaos Necklace
            _builder.Create(RecipeType.ChaosNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("ch_necklace")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("emerald", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5);

            // Magus Necklace
            _builder.Create(RecipeType.MagusNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("mag_necklace")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("emerald", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5);

            // Immortal Necklace
            _builder.Create(RecipeType.ImmortalNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("imm_necklace")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("emerald", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5);
        }

    }
}