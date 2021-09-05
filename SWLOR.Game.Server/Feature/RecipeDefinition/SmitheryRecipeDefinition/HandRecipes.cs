using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;

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
                .ModSlots(RecipeModType.Armor, 1)
                .Component("ref_veldite", 4)
                .Component("fiberp_ruined", 2);

            // Spiritmaster Gloves
            _builder.Create(RecipeType.SpiritmasterGloves, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("sm_gloves")
                .Level(3)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .ModSlots(RecipeModType.Armor, 1)
                .Component("lth_ruined", 4)
                .Component("fiberp_ruined", 2);

            // Combat Gloves
            _builder.Create(RecipeType.CombatGloves, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("com_gloves")
                .Level(3)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .ModSlots(RecipeModType.Armor, 1)
                .Component("lth_ruined", 4)
                .Component("fiberp_ruined", 2);
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
                .ModSlots(RecipeModType.Armor, 1)
                .Component("ref_scordspar", 4)
                .Component("fiberp_flawed", 2);

            // Vivid Gloves
            _builder.Create(RecipeType.VividGloves, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("viv_gloves")
                .Level(13)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .ModSlots(RecipeModType.Armor, 1)
                .Component("lth_flawed", 4)
                .Component("fiberp_flawed", 2);

            // Valor Gloves
            _builder.Create(RecipeType.ValorGloves, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("val_gloves")
                .Level(13)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .ModSlots(RecipeModType.Armor, 1)
                .Component("lth_flawed", 4)
                .Component("fiberp_flawed", 2);
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
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_plagionite", 4)
                .Component("fiberp_good", 2);

            // Reginal Gloves
            _builder.Create(RecipeType.ReginalGloves, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("reg_gloves")
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("lth_good", 4)
                .Component("fiberp_good", 2);

            // Forza Gloves
            _builder.Create(RecipeType.ForzaGloves, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("for_gloves")
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("lth_good", 4)
                .Component("fiberp_good", 2);
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
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_keromber", 4)
                .Component("fiberp_imperfect", 2);

            // Grenada Gloves
            _builder.Create(RecipeType.GrenadaGloves, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("gr_gloves")
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("lth_imperfect", 4)
                .Component("fiberp_imperfect", 2);

            // Survival Gloves
            _builder.Create(RecipeType.SurvivalGloves, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("sur_gloves")
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("lth_imperfect", 4)
                .Component("fiberp_imperfect", 2);
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
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_jasioclase", 4)
                .Component("fiberp_high", 2);

            // Transcendent Gloves
            _builder.Create(RecipeType.TranscendentGloves, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("tran_gloves")
                .Level(43)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("lth_high", 4)
                .Component("fiberp_high", 2);

            // Supreme Gloves
            _builder.Create(RecipeType.SupremeGloves, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("sup_gloves")
                .Level(43)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("lth_high", 4)
                .Component("fiberp_high", 2);
        }
    }
}
