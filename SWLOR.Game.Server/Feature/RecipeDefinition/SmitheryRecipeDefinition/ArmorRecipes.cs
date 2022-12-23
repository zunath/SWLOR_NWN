using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class ArmorRecipes : IRecipeListDefinition
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
            // Battlemaster Breastplate
            _builder.Create(RecipeType.BattlemasterBreastplate, SkillType.Smithery)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("bm_armor")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 4)
                .Component("fiberp_ruined", 2);

            // Spiritmaster Tunic
            _builder.Create(RecipeType.SpiritmasterTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("sm_tunic")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 4)
                .Component("fiberp_ruined", 2);

            // Combat Tunic
            _builder.Create(RecipeType.CombatTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("com_tunic")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 4)
                .Component("fiberp_ruined", 2);

            // Advent Tunic
            _builder.Create(RecipeType.AdventTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("smth_armor_1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // Amateur Tunic
            _builder.Create(RecipeType.AmateurTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("engi_armor_1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // Cloth Tunic
            _builder.Create(RecipeType.ClothTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("fabr_armor_1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // Chef Tunic
            _builder.Create(RecipeType.ChefTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("cook_armor_1")
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
            // Titan Breastplate
            _builder.Create(RecipeType.TitanBreastplate, SkillType.Smithery)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("tit_armor")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 4)
                .Component("fiberp_flawed", 2);

            // Vivid Tunic
            _builder.Create(RecipeType.VividTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("viv_tunic")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 4)
                .Component("fiberp_flawed", 2);

            // Valor Tunic
            _builder.Create(RecipeType.ValorTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("val_tunic")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 4)
                .Component("fiberp_flawed", 2);

            // Frontier Tunic
            _builder.Create(RecipeType.FrontierTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("smth_armor_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // Worker Tunic
            _builder.Create(RecipeType.WorkerTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("engi_armor_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // Linen Tunic
            _builder.Create(RecipeType.LinenTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("fabr_armor_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // Velveteen Tunic
            _builder.Create(RecipeType.VelveteenTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("cook_armor_2")
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
            // Quark Breastplate
            _builder.Create(RecipeType.QuarkBreastplate, SkillType.Smithery)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("qk_armor")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 4)
                .Component("fiberp_good", 2);

            // Reginal Tunic
            _builder.Create(RecipeType.ReginalTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("reg_tunic")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 4)
                .Component("fiberp_good", 2);

            // Forza Tunic
            _builder.Create(RecipeType.ForzaTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("for_tunic")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 4)
                .Component("fiberp_good", 2);

            // Majestic Tunic
            _builder.Create(RecipeType.MajesticTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("smth_armor_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // Mechanic Tunic
            _builder.Create(RecipeType.MechanicTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("engi_armor_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // Designer Tunic
            _builder.Create(RecipeType.DesignerTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("fabr_armor_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // Silk Tunic
            _builder.Create(RecipeType.SilkTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("cook_armor_3")
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
            // Argos Breastplate
            _builder.Create(RecipeType.ArgosBreastplate, SkillType.Smithery)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("ar_armor")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 4)
                .Component("fiberp_imperfect", 2);

            // Grenada Tunic
            _builder.Create(RecipeType.GrenadaTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("gr_tunic")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 4)
                .Component("fiberp_imperfect", 2);

            // Survival Tunic
            _builder.Create(RecipeType.SurvivalTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("sur_tunic")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 4)
                .Component("fiberp_imperfect", 2);

            // Dream Tunic
            _builder.Create(RecipeType.DreamTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("smth_armor_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // DevotionTunic
            _builder.Create(RecipeType.DevotionTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("engi_armor_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // Oasis Tunic
            _builder.Create(RecipeType.OasisTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("fabr_armor_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // Vintage Tunic
            _builder.Create(RecipeType.VintageTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("cook_armor_4")
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
            // Eclipse Breastplate
            _builder.Create(RecipeType.EclipseBreastplate, SkillType.Smithery)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("ec_armor")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 4)
                .Component("fiberp_high", 2);

            // Transcendent Tunic
            _builder.Create(RecipeType.TranscendentTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("tran_tunic")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 4)
                .Component("fiberp_high", 2);

            // Supreme Tunic
            _builder.Create(RecipeType.SupremeTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("sup_tunic")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 4)
                .Component("fiberp_high", 2);

            // Eternal Tunic
            _builder.Create(RecipeType.EternalTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("smth_armor_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Skysteel Tunic
            _builder.Create(RecipeType.SkysteelTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("engi_armor_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Rose Tunic
            _builder.Create(RecipeType.RoseTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("fabr_armor_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Moonflame Tunic
            _builder.Create(RecipeType.MoonflameTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("cook_armor_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Chaos Breastplate
            _builder.Create(RecipeType.ChaosBreastplate, SkillType.Smithery)
                .Category(RecipeCategoryType.Breastplate)
                .Resref("ch_armor")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("fiberp_high", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5);

            // Magus Tunic
            _builder.Create(RecipeType.MagusTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("mag_tunic")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_arkoxit", 2)
                .Component("lth_high", 20)
                .Component("fiberp_high", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5);

            // Immortal Tunic
            _builder.Create(RecipeType.ImmortalTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("imm_tunic")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
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