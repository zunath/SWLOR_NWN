using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class FeetRecipes: IRecipeListDefinition
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
            // Battlemaster Leggings
            _builder.Create(RecipeType.BattlemasterLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("bm_leggings")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 3)
                .Component("fiberp_ruined", 2);

            // Spiritmaster Boots
            _builder.Create(RecipeType.SpiritmasterBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("sm_boots")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 3)
                .Component("fiberp_ruined", 2);

            // Combat Boots
            _builder.Create(RecipeType.CombatBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("com_boots")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1);

            // Advent Leggings
            _builder.Create(RecipeType.AdventLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("advent_leggings")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // Amateur Leggings
            _builder.Create(RecipeType.AmateurLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("engi_leggings_1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // Cloth Leggings
            _builder.Create(RecipeType.ClothLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("fabr_leggings_1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // Chef Leggings
            _builder.Create(RecipeType.ChefLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("chef_leggings_1")
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
            // Titan Leggings
            _builder.Create(RecipeType.TitanLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("tit_leggings")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 3)
                .Component("fiberp_flawed", 2);

            // Vivid Boots
            _builder.Create(RecipeType.VividBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("viv_boots")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2);

            // Valor Boots
            _builder.Create(RecipeType.ValorBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("val_boots")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1);

            // Frontier Leggings
            _builder.Create(RecipeType.FrontierLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("frontier_legging")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // Worker Leggings
            _builder.Create(RecipeType.WorkerLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("engi_leggings_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // Linen Leggings
            _builder.Create(RecipeType.LinenLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("fabr_leggings_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // Velveteen Leggings
            _builder.Create(RecipeType.VelveteenLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("chef_leggings_2")
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
            // Quark Leggings
            _builder.Create(RecipeType.QuarkLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("qk_leggings")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 3)
                .Component("fiberp_good", 2);

            // Reginal Boots
            _builder.Create(RecipeType.ReginalBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("reg_boots")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 3)
                .Component("fiberp_good", 2);

            // Forza Boots
            _builder.Create(RecipeType.ForzaBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("for_boots")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 2)
                .Component("fiberp_good", 1);

            // Majestic Leggings
            _builder.Create(RecipeType.MajesticLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("majestic_legging")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // Mechanic Leggings
            _builder.Create(RecipeType.MechanicLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("engi_leggings_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // Designer Leggings
            _builder.Create(RecipeType.DesignerLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("fabr_leggings_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // Silk Leggings
            _builder.Create(RecipeType.SilkLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("chef_leggings_3")
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
            // Argos Leggings
            _builder.Create(RecipeType.ArgosLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("ar_leggings")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 3)
                .Component("fiberp_imperfect", 2);

            // Grenada Boots
            _builder.Create(RecipeType.GrenadaBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("gr_boots")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 3)
                .Component("fiberp_imperfect", 2);

            // Survival Boots
            _builder.Create(RecipeType.SurvivalBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("sur_boots")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 2)
                .Component("fiberp_imperfect", 1);

            // Dream Leggings
            _builder.Create(RecipeType.DreamLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("dream_leggings")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // Devotion Leggings
            _builder.Create(RecipeType.DevotionLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("engi_leggings_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // Oasis Leggings
            _builder.Create(RecipeType.OasisLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("fabr_leggings_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // Vintage Leggings
            _builder.Create(RecipeType.VintageLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("chef_leggings_4")
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
            // Eclipse Leggings
            _builder.Create(RecipeType.EclipseLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("ec_leggings")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 3)
                .Component("fiberp_high", 2);

            // Transcendent Boots
            _builder.Create(RecipeType.TranscendentBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("tran_boots")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 3)
                .Component("fiberp_high", 2);

            // Supreme Boots
            _builder.Create(RecipeType.SupremeBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("sup_boots")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 2)
                .Component("fiberp_high", 1);

            // Eternal Leggings
            _builder.Create(RecipeType.EternalLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("eternal_leggings")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Skysteel Leggings
            _builder.Create(RecipeType.SkysteelLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("engi_leggings_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Rose Leggings
            _builder.Create(RecipeType.RoseLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("fabr_leggings_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Moonflame Leggings
            _builder.Create(RecipeType.MoonflameLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("chef_leggings_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Chaos Leggings
            _builder.Create(RecipeType.ChaosLeggings, SkillType.Smithery)
                .Category(RecipeCategoryType.Legging)
                .Resref("ch_leggings")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
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
                .Component("ref_keromber", 5);

            // Magus Boots
            _builder.Create(RecipeType.MagusBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("mag_boots")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
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
                .Component("ref_keromber", 5);

            // Immortal Boots
            _builder.Create(RecipeType.ImmortalBoots, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("imm_boots")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
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
                .Component("ref_keromber", 5);
        }
    }
}
