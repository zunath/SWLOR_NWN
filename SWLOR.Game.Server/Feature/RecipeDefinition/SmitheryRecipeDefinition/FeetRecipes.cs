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
        }
    }
}
