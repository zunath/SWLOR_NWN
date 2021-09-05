using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class HeadRecipes: IRecipeListDefinition
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
            // Battlemaster Helmet
            _builder.Create(RecipeType.BattlemasterHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("bm_helmet")
                .Level(4)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .ModSlots(RecipeModType.Armor, 1)
                .Component("ref_veldite", 5)
                .Component("fiberp_ruined", 3);

            // Spiritmaster Cap
            _builder.Create(RecipeType.SpiritmasterCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("sm_cap")
                .Level(4)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .ModSlots(RecipeModType.Armor, 1)
                .Component("lth_ruined", 5)
                .Component("fiberp_ruined", 3);

            // Combat Cap
            _builder.Create(RecipeType.CombatCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("com_cap")
                .Level(4)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .ModSlots(RecipeModType.Armor, 1)
                .Component("lth_ruined", 5)
                .Component("fiberp_ruined", 3);
        }

        private void Tier2()
        {
            // Titan Helmet
            _builder.Create(RecipeType.TitanHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("tit_helmet")
                .Level(14)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .ModSlots(RecipeModType.Armor, 1)
                .Component("ref_scordspar", 5)
                .Component("fiberp_flawed", 3);

            // Vivid Cap
            _builder.Create(RecipeType.VividCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("viv_cap")
                .Level(14)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .ModSlots(RecipeModType.Armor, 1)
                .Component("lth_flawed", 5)
                .Component("fiberp_flawed", 3);

            // Valor Cap
            _builder.Create(RecipeType.ValorCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("val_cap")
                .Level(14)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 2)
                .ModSlots(RecipeModType.Armor, 1)
                .Component("lth_flawed", 5)
                .Component("fiberp_flawed", 3);
        }

        private void Tier3()
        {
            // Quark Helmet
            _builder.Create(RecipeType.QuarkHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("qk_helmet")
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_plagionite", 5)
                .Component("fiberp_good", 3);

            // Reginal Cap
            _builder.Create(RecipeType.ReginalCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("reg_cap")
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("lth_good", 5)
                .Component("fiberp_good", 3);

            // Forza Cap
            _builder.Create(RecipeType.ForzaCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("for_cap")
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("lth_good", 5)
                .Component("fiberp_good", 3);
        }

        private void Tier4()
        {
            // Argos Helmet
            _builder.Create(RecipeType.ArgosHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("ar_helmet")
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_keromber", 5)
                .Component("fiberp_imperfect", 3);

            // Grenada Cap
            _builder.Create(RecipeType.GrenadaCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("gr_cap")
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("lth_imperfect", 5)
                .Component("fiberp_imperfect", 3);

            // Survival Cap
            _builder.Create(RecipeType.SurvivalCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("sur_cap")
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("lth_imperfect", 5)
                .Component("fiberp_imperfect", 3);
        }

        private void Tier5()
        {
            // Eclipse Helmet
            _builder.Create(RecipeType.EclipseHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Resref("ec_helmet")
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("ref_jasioclase", 5)
                .Component("fiberp_high", 3);

            // Transcendent Cap
            _builder.Create(RecipeType.TranscendentCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("tran_cap")
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("lth_high", 5)
                .Component("fiberp_high", 3);

            // Supreme Cap
            _builder.Create(RecipeType.SupremeCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("sup_cap")
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .ModSlots(RecipeModType.Armor, 2)
                .Component("lth_high", 5)
                .Component("fiberp_high", 3);
        }
    }
}
