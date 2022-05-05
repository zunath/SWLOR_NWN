using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class CloakRecipes : IRecipeListDefinition
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
            // Battlemaster Cloak
            _builder.Create(RecipeType.BattlemasterCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("bm_cloak")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 5)
                .Component("fiberp_ruined", 3);

            // Spiritmaster Cloak
            _builder.Create(RecipeType.SpiritmasterCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("sm_cloak")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 5)
                .Component("fiberp_ruined", 3);

            // Combat Cloak
            _builder.Create(RecipeType.CombatCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("com_cloak")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 5)
                .Component("fiberp_ruined", 3);
        }

        private void Tier2()
        {
            // Titan Cloak
            _builder.Create(RecipeType.TitanCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("tit_cloak")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 5)
                .Component("fiberp_flawed", 3);

            // Vivid Cloak
            _builder.Create(RecipeType.VividCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("viv_cloak")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 5)
                .Component("fiberp_flawed", 3);

            // Valor Cloak
            _builder.Create(RecipeType.ValorCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("val_cloak")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 5)
                .Component("fiberp_flawed", 3);
        }

        private void Tier3()
        {
            // Quark Cloak
            _builder.Create(RecipeType.QuarkCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("qk_cloak")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 5)
                .Component("fiberp_good", 3);

            // Reginal Cloak
            _builder.Create(RecipeType.ReginalCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("reg_cloak")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 5)
                .Component("fiberp_good", 3);

            // Forza Cloak
            _builder.Create(RecipeType.ForzaCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("for_belt")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_good", 5)
                .Component("fiberp_good", 3);

        }

        private void Tier4()
        {
            // Argos Cloak
            _builder.Create(RecipeType.ArgosCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("ar_cloak")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 5)
                .Component("fiberp_imperfect", 3);

            // Grenada Cloak
            _builder.Create(RecipeType.GrenadaCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("gre_cloak")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 5)
                .Component("fiberp_imperfect", 3);

            // Survival Cloak
            _builder.Create(RecipeType.SurvivalCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("sur_cloak")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_imperfect", 5)
                .Component("fiberp_imperfect", 3);
        }

        private void Tier5()
        {
            // Eclipse Cloak
            _builder.Create(RecipeType.EclipseCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("ec_cloak")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 5)
                .Component("fiberp_high", 3);

            // Transcendent Cloak
            _builder.Create(RecipeType.TranscendentCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("tran_cloak")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 5)
                .Component("fiberp_high", 3);

            // Supreme Cloak
            _builder.Create(RecipeType.SupremeCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("sup_cloak")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Armor, 2)
                .Component("lth_high", 5)
                .Component("fiberp_high", 3);
        }
    }
}