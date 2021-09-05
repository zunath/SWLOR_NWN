using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;

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
                .ModSlots(RecipeModType.Armor, 1)
                .Component("ref_veldite", 8)
                .Component("fiberp_ruined", 4);

            // Spiritmaster Tunic
            _builder.Create(RecipeType.SpiritmasterTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("sm_tunic")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .ModSlots(RecipeModType.Armor, 1)
                .Component("lth_ruined", 8)
                .Component("fiberp_ruined", 4);

            // Combat Tunic
            _builder.Create(RecipeType.CombatTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("com_tunic")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .ModSlots(RecipeModType.Armor, 1)
                .Component("lth_ruined", 8)
                .Component("fiberp_ruined", 4);
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
                .ModSlots(RecipeModType.Armor, 1)
                .Component("ref_scordspar", 8)
                .Component("fiberp_flawed", 4);

            // Vivid Tunic
            _builder.Create(RecipeType.SpiritmasterTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("viv_tunic")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .ModSlots(RecipeModType.Armor, 1)
                .Component("lth_flawed", 8)
                .Component("fiberp_flawed", 4);

            // Valor Tunic
            _builder.Create(RecipeType.CombatTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("val_tunic")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .ModSlots(RecipeModType.Armor, 1)
                .Component("lth_flawed", 8)
                .Component("fiberp_flawed", 4);
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
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_plagionite", 8)
                .Component("fiberp_good", 4);

            // Reginal Tunic
            _builder.Create(RecipeType.ReginalTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("reg_tunic")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("lth_good", 8)
                .Component("fiberp_good", 4);

            // Forza Tunic
            _builder.Create(RecipeType.ForzaTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("for_tunic")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("lth_good", 8)
                .Component("fiberp_good", 4);
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
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_keromber", 8)
                .Component("fiberp_imperfect", 4);

            // Grenada Tunic
            _builder.Create(RecipeType.GrenadaTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("gr_tunic")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("lth_imperfect", 8)
                .Component("fiberp_imperfect", 4);

            // Survival Tunic
            _builder.Create(RecipeType.SurvivalTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("sur_tunic")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("lth_imperfect", 8)
                .Component("fiberp_imperfect", 4);
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
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_jasioclase", 8)
                .Component("fiberp_high", 4);

            // Transcendent Tunic
            _builder.Create(RecipeType.TranscendentTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("tran_tunic")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("lth_high", 8)
                .Component("fiberp_high", 4);

            // Supreme Tunic
            _builder.Create(RecipeType.SupremeTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("sup_tunic")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("lth_high", 8)
                .Component("fiberp_high", 4);
        }
    }
}