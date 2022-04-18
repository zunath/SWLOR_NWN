using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
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
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 5)
                .Component("fiberp_flawed", 3);

            // Vivid Belt
            _builder.Create(RecipeType.VividBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("viv_belt")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 5)
                .Component("fiberp_flawed", 3);

            // Valor Belt
            _builder.Create(RecipeType.ValorBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("val_belt")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 5)
                .Component("fiberp_flawed", 3);
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
        }
    }
}