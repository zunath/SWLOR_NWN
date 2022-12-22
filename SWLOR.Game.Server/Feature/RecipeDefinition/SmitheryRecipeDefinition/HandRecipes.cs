using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class HandRecipes: IRecipeListDefinition
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
            // Battlemaster Bracer
            _builder.Create(RecipeType.BattlemasterBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("bm_bracer")
                .Level(3)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 2)
                .Component("fiberp_ruined", 1);

            // Spiritmaster Gloves
            _builder.Create(RecipeType.SpiritmasterGloves, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("sm_gloves")
                .Level(3)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1);

            // Combat Gloves
            _builder.Create(RecipeType.CombatGloves, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("com_gloves")
                .Level(3)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1);

            // Advent Bracer
            _builder.Create(RecipeType.AdventBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("advent_bracer")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // Amateur Bracer
            _builder.Create(RecipeType.AmateurBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("engi_bracer_1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // Cloth Bracer
            _builder.Create(RecipeType.ClothBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("fabr_bracer_1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // Chef Bracer
            _builder.Create(RecipeType.ChefBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("chef_bracer_1")
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
            // Titan Bracer
            _builder.Create(RecipeType.TitanBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("tit_bracer")
                .Level(13)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 2)
                .Component("fiberp_flawed", 1);

            // Vivid Gloves
            _builder.Create(RecipeType.VividGloves, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("viv_gloves")
                .Level(13)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1);

            // Valor Gloves
            _builder.Create(RecipeType.ValorGloves, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("val_gloves")
                .Level(13)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1);

            // Frontier Bracer
            _builder.Create(RecipeType.FrontierBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("frontier_bracer")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // Worker Bracer
            _builder.Create(RecipeType.WorkerBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("engi_bracer_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // Linen Bracer
            _builder.Create(RecipeType.LinenBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("fabr_bracer_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // Velveteen Bracer
            _builder.Create(RecipeType.VelveteenBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("chef_bracer_2")
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
            // Quark Bracer
            _builder.Create(RecipeType.QuarkBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("qk_bracer")
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 2)
                .Component("fiberp_good", 1);

            // Reginal Gloves
            _builder.Create(RecipeType.ReginalGloves, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("reg_gloves")
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 2)
                .Component("fiberp_good", 1);

            // Forza Gloves
            _builder.Create(RecipeType.ForzaGloves, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("for_gloves")
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 2)
                .Component("fiberp_good", 1);

            // Majestic Bracer
            _builder.Create(RecipeType.MajesticBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("majestic_bracer")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // Mechanic Bracer
            _builder.Create(RecipeType.MechanicBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("engi_bracer_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // Designer Bracer
            _builder.Create(RecipeType.DesignerBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("fabr_bracer_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // Silk Bracer
            _builder.Create(RecipeType.SilkBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("chef_bracer_3")
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
            // Argos Bracer
            _builder.Create(RecipeType.ArgosBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("ar_bracer")
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 2)
                .Component("fiberp_imperfect", 1);

            // Grenada Gloves
            _builder.Create(RecipeType.GrenadaGloves, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("gr_gloves")
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 2)
                .Component("fiberp_imperfect", 1);

            // Survival Gloves
            _builder.Create(RecipeType.SurvivalGloves, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("sur_gloves")
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 2)
                .Component("fiberp_imperfect", 1);

            // Dream Bracer
            _builder.Create(RecipeType.DreamBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("dream_bracer")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // Devotion Bracer
            _builder.Create(RecipeType.DevotionBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("engi_bracer_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // Oasis Bracer
            _builder.Create(RecipeType.OasisBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("fabr_bracer_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // Vintage Bracer
            _builder.Create(RecipeType.VintageBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("chef_bracer_4")
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
            // Eclipse Bracer
            _builder.Create(RecipeType.EclipseBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("ec_bracer")
                .Level(43)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 2)
                .Component("fiberp_high", 1);

            // Transcendent Gloves
            _builder.Create(RecipeType.TranscendentGloves, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("tran_gloves")
                .Level(43)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 2)
                .Component("fiberp_high", 1);

            // Supreme Gloves
            _builder.Create(RecipeType.SupremeGloves, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("sup_gloves")
                .Level(43)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 2)
                .Component("fiberp_high", 1);

            // Eternal Bracer
            _builder.Create(RecipeType.EternalBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("eternal_bracer")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Skysteel Bracer
            _builder.Create(RecipeType.SkysteelBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("engi_bracer_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Rose Bracer
            _builder.Create(RecipeType.RoseBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("fabr_bracer_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Moonflame Bracer
            _builder.Create(RecipeType.MoonflameBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("chef_bracer_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Chaos Bracer
            _builder.Create(RecipeType.ChaosBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("ch_bracer")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 20)
                .Component("fiberp_high", 20)
                .Component("chiro_shard", 2);

            // Magus Gloves
            _builder.Create(RecipeType.MagusGloves, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("mag_gloves")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 20)
                .Component("fiberp_high", 20)
                .Component("chiro_shard", 2);

            // Immortal Gloves
            _builder.Create(RecipeType.ImmortalGloves, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("imm_gloves")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 20)
                .Component("fiberp_high", 20)
                .Component("chiro_shard", 2);
        }
    }
}
