using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class BeltRecipes : IRecipeListDefinition
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
            // Battlemaster Belt
            _builder.Create(RecipeType.BattlemasterBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("bm_belt")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 5)
                .Component("fiberp_ruined", 3);

            // Spiritmaster Belt
            _builder.Create(RecipeType.SpiritmasterBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("sm_belt")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 5)
                .Component("fiberp_ruined", 3);

            // Combat Belt
            _builder.Create(RecipeType.CombatBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("com_belt")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 5)
                .Component("fiberp_ruined", 3);

            // Advent Belt
            _builder.Create(RecipeType.AdventBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("advent_belt")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // Amateur Belt
            _builder.Create(RecipeType.AmateurBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("engi_belt_1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // Cloth Belt
            _builder.Create(RecipeType.ClothBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("fabr_cloak_1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("lth_ruined", 5)
                .Component("jade", 3);

            // Chef Belt
            _builder.Create(RecipeType.ChefBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("chef_belt_1")
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
            // Titan Belt
            _builder.Create(RecipeType.TitanBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("tit_belt")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 5)
                .Component("fiberp_flawed", 3);

            // Vivid Belt
            _builder.Create(RecipeType.VividBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("viv_belt")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 5)
                .Component("fiberp_flawed", 3);

            // Valor Belt
            _builder.Create(RecipeType.ValorBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("val_belt")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_flawed", 5)
                .Component("fiberp_flawed", 3);

            // Frontier Belt
            _builder.Create(RecipeType.FrontierBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("frontier_belt")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // Worker Belt
            _builder.Create(RecipeType.WorkerBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("engi_belt_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // Linen Belt
            _builder.Create(RecipeType.LinenBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("fabr_belt_2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_scordspar", 5)
                .Component("lth_flawed", 5)
                .Component("agate", 3);

            // Velveteen Belt
            _builder.Create(RecipeType.VelveteenBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("chef_belt_2")
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
            // Quark Belt
            _builder.Create(RecipeType.QuarkBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("qk_belt")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 5)
                .Component("fiberp_good", 3);

            // Reginal Belt
            _builder.Create(RecipeType.ReginalBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("reg_belt")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 5)
                .Component("fiberp_good", 3);

            // Forza Belt
            _builder.Create(RecipeType.ForzaBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("for_belt")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 5)
                .Component("fiberp_good", 3);

            // Majestic Belt
            _builder.Create(RecipeType.MajesticBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("majestic_belt")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // Mechanic Belt
            _builder.Create(RecipeType.MechanicBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("engi_belt_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // Designer Belt
            _builder.Create(RecipeType.DesignerBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("fabr_belt_3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("lth_good", 5)
                .Component("citrine", 3);

            // Silk Belt
            _builder.Create(RecipeType.SilkBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("chef_belt_3")
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
            // Argos Belt
            _builder.Create(RecipeType.ArgosBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("ar_belt")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 5)
                .Component("fiberp_imperfect", 3);

            // Grenada Belt
            _builder.Create(RecipeType.GrenadaBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("gre_belt")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 5)
                .Component("fiberp_imperfect", 3);

            // Survival Belt
            _builder.Create(RecipeType.SurvivalBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("sur_belt")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 5)
                .Component("fiberp_imperfect", 3);

            // Dream Belt
            _builder.Create(RecipeType.DreamBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("dream_belt")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // Devotion Belt
            _builder.Create(RecipeType.DevotionBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("engi_belt_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // Oasis Belt
            _builder.Create(RecipeType.OasisBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("fabr_belt_4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 5)
                .Component("ruby", 3);

            // Vintage Belt
            _builder.Create(RecipeType.VintageBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("chef_belt_4")
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
            // Eclipse Belt
            _builder.Create(RecipeType.EclipseBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("ec_belt")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 5)
                .Component("fiberp_high", 3);

            // Transcendent Belt
            _builder.Create(RecipeType.TranscendentBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("tran_belt")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 5)
                .Component("fiberp_high", 3);

            // Supreme Belt
            _builder.Create(RecipeType.SupremeBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("sup_belt")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 5)
                .Component("fiberp_high", 3);

            // Eternal Belt
            _builder.Create(RecipeType.EternalBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("eternal_belt")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Skysteel Belt
            _builder.Create(RecipeType.SkysteelBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("engi_belt_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Rose Belt
            _builder.Create(RecipeType.RoseBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("fabr_belt_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Moonflame Belt
            _builder.Create(RecipeType.MoonflameBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("chef_belt_5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 5)
                .Component("emerald", 3);

            // Chaos Belt
            _builder.Create(RecipeType.ChaosBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("ch_belt")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 20)
                .Component("fiberp_high", 20)
                .Component("chiro_shard", 2);

            // Magus Belt
            _builder.Create(RecipeType.MagusBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("mag_belt")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 20)
                .Component("fiberp_high", 20)
                .Component("chiro_shard", 2);

            // Immortal Belt
            _builder.Create(RecipeType.ImmortalBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("imm_belt")
                .Level(52)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 20)
                .Component("fiberp_high", 20)
                .Component("chiro_shard", 2);
        }
    }
}