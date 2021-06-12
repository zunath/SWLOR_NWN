using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class ArmorRecipes5 : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            BreastplateIV();
            TunicIV();
            BattlemasterHelmetIV();
            ChannelerCapIV();
            DuelistHelmetIV();
            GunslingerCapIV();
            ShieldIV();
            MightBeltIV();
            ThiefBeltIV();
            WarriorBeltIV();
            BattlemasterLeggingIV();
            ChannelerBootIV();
            DuelistLeggingIV();
            GunslingerBootIV();
            DuelistBracerIV();
            BattlemasterBracerIV();
            ChannelerGloveIV();
            GunslingerGloveIV();
            NecklaceIV();
            QuicknessRingIV();
            MightCloakIV();
            ThiefCloakIV();
            WarriorCloakIV();

            return _builder.Build();
        }

        private void BreastplateIV()
        {
            _builder.Create(RecipeType.BreastplateIV, SkillType.Smithery)
                .Category(RecipeCategoryType.Breastplate)
                .Level(42)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .Resref("breastplate_4")
                .Component("ref_jasioclase", 8)
                .Component("lth_high", 2);
        }

        private void TunicIV()
        {
            _builder.Create(RecipeType.TunicIV, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Level(41)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .Resref("tunic_4")
                .Component("ref_jasioclase", 2)
                .Component("lth_high", 8);
        }

        private void BattlemasterHelmetIV()
        {
            _builder.Create(RecipeType.BattlemasterHelmetIV, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(43)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .Resref("bm_helm_4")
                .Component("ref_jasioclase", 4)
                .Component("lth_high", 4);
        }

        private void ChannelerCapIV()
        {
            _builder.Create(RecipeType.ChannelerCapIV, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .Resref("cn_cap_4")
                .Component("ref_jasioclase", 3)
                .Component("lth_high", 5);
        }

        private void DuelistHelmetIV()
        {
            _builder.Create(RecipeType.DuelistHelmetIV, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .Resref("du_helm_4")
                .Component("ref_jasioclase", 6)
                .Component("lth_high", 2);
        }

        private void GunslingerCapIV()
        {
            _builder.Create(RecipeType.GunslingerCapIV, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .Resref("gn_cap_4")
                .Component("ref_jasioclase", 2)
                .Component("lth_high", 6);
        }

        private void ShieldIV()
        {
            _builder.Create(RecipeType.ShieldIV, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .Resref("shield_4")
                .Component("ref_jasioclase", 8)
                .Component("lth_high", 3);
        }

        private void MightBeltIV()
        {
            _builder.Create(RecipeType.MightBeltIV, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(42)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .Resref("mght_belt_4")
                .Component("ref_jasioclase", 3)
                .Component("lth_high", 5);
        }

        private void ThiefBeltIV()
        {
            _builder.Create(RecipeType.ThiefBeltIV, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(43)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .Resref("thf_belt_4")
                .Component("ref_jasioclase", 4)
                .Component("lth_high", 4);
        }

        private void WarriorBeltIV()
        {
            _builder.Create(RecipeType.WarriorBeltIV, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .Resref("war_belt_4")
                .Component("ref_jasioclase", 8)
                .Component("lth_high", 2);
        }

        private void BattlemasterLeggingIV()
        {
            _builder.Create(RecipeType.BattlemasterLeggingIV, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(42)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .Resref("bm_legging_4")
                .Component("ref_jasioclase", 7)
                .Component("lth_high", 5);
        }

        private void ChannelerBootIV()
        {
            _builder.Create(RecipeType.ChannelerBootIV, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .Resref("cn_boot_4")
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 7);
        }

        private void DuelistLeggingIV()
        {
            _builder.Create(RecipeType.DuelistLeggingIV, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(43)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .Resref("du_legging_4")
                .Component("ref_jasioclase", 6)
                .Component("lth_high", 5);
        }

        private void GunslingerBootIV()
        {
            _builder.Create(RecipeType.GunslingerBootIV, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .Resref("gn_boot_4")
                .Component("ref_jasioclase", 5)
                .Component("lth_high", 6);
        }

        private void DuelistBracerIV()
        {
            _builder.Create(RecipeType.DuelistBracerIV, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(43)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .Resref("du_bracer_4")
                .Component("ref_jasioclase", 7)
                .Component("lth_high", 1);
        }

        private void BattlemasterBracerIV()
        {
            _builder.Create(RecipeType.BattlemasterBracerIV, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(44)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .Resref("bm_bracer_4")
                .Component("ref_jasioclase", 7)
                .Component("lth_high", 2);
        }

        private void ChannelerGloveIV()
        {
            _builder.Create(RecipeType.ChannelerGloveIV, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .Resref("cn_glove_4")
                .Component("ref_jasioclase", 2)
                .Component("lth_high", 7);
        }

        private void GunslingerGloveIV()
        {
            _builder.Create(RecipeType.GunslingerGloveIV, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .Resref("gn_glove_4")
                .Component("ref_jasioclase", 3)
                .Component("lth_high", 6);
        }

        private void NecklaceIV()
        {
            _builder.Create(RecipeType.NecklaceIV, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .Resref("necklace_4")
                .Component("ref_jasioclase", 4)
                .Component("lth_high", 2);
        }

        private void QuicknessRingIV()
        {
            _builder.Create(RecipeType.QuicknessRingIV, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 5)
                .Resref("qck_ring_4")
                .Component("ref_jasioclase", 4)
                .Component("lth_high", 2);
        }

        private void MightCloakIV()
        {
            _builder.Create(RecipeType.MightCloakIV, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .Resref("mght_cloak_4")
                .Component("ref_jasioclase", 2)
                .Component("lth_high", 9);
        }

        private void ThiefCloakIV()
        {
            _builder.Create(RecipeType.ThiefCloakIV, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .Resref("thf_cloak_4")
                .Component("ref_jasioclase", 3)
                .Component("lth_high", 7);
        }

        private void WarriorCloakIV()
        {
            _builder.Create(RecipeType.WarriorCloakIV, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 5)
                .Resref("war_cloak_4")
                .Component("ref_jasioclase", 4)
                .Component("lth_high", 6);
        }
    }
}