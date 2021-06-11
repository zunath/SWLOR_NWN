using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class ArmorRecipes4 : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            BreastplateIII();
            TunicIII();
            BattlemasterHelmetIII();
            ChannelerCapIII();
            DuelistHelmetIII();
            GunslingerCapIII();
            ShieldIII();
            MightBeltIII();
            ThiefBeltIII();
            WarriorBeltIII();
            BattlemasterLeggingIII();
            ChannelerBootIII();
            DuelistLeggingIII();
            GunslingerBootIII();
            DuelistBracerIII();
            BattlemasterBracerIII();
            ChannelerGloveIII();
            GunslingerGloveIII();
            NecklaceIII();
            QuicknessRingIII();
            MightCloakIII();
            ThiefCloakIII();
            WarriorCloakIII();

            return _builder.Build();
        }

        private void BreastplateIII()
        {
            _builder.Create(RecipeType.BreastplateIII, SkillType.Smithery)
                .Category(RecipeCategoryType.Breastplate)
                .Level(32)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .Resref("breastplate_3")
                .Component("ref_keromber", 8)
                .Component("lth_imperfect", 2);
        }

        private void TunicIII()
        {
            _builder.Create(RecipeType.TunicIII, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Level(31)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .Resref("tunic_3")
                .Component("ref_keromber", 2)
                .Component("lth_imperfect", 8);
        }

        private void BattlemasterHelmetIII()
        {
            _builder.Create(RecipeType.BattlemasterHelmetIII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .Resref("bm_helm_3")
                .Component("ref_keromber", 4)
                .Component("lth_imperfect", 4);
        }

        private void ChannelerCapIII()
        {
            _builder.Create(RecipeType.ChannelerCapIII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .Resref("cn_cap_3")
                .Component("ref_keromber", 3)
                .Component("lth_imperfect", 5);
        }

        private void DuelistHelmetIII()
        {
            _builder.Create(RecipeType.DuelistHelmetIII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .Resref("du_helm_3")
                .Component("ref_keromber", 6)
                .Component("lth_imperfect", 2);
        }

        private void GunslingerCapIII()
        {
            _builder.Create(RecipeType.GunslingerCapIII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .Resref("gn_cap_3")
                .Component("ref_keromber", 2)
                .Component("lth_imperfect", 6);
        }

        private void ShieldIII()
        {
            _builder.Create(RecipeType.ShieldIII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .Resref("shield_3")
                .Component("ref_keromber", 8)
                .Component("lth_imperfect", 3);
        }

        private void MightBeltIII()
        {
            _builder.Create(RecipeType.MightBeltIII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(32)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .Resref("mght_belt_3")
                .Component("ref_keromber", 3)
                .Component("lth_imperfect", 5);
        }

        private void ThiefBeltIII()
        {
            _builder.Create(RecipeType.ThiefBeltIII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .Resref("thf_belt_3")
                .Component("ref_keromber", 4)
                .Component("lth_imperfect", 4);
        }

        private void WarriorBeltIII()
        {
            _builder.Create(RecipeType.WarriorBeltIII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .Resref("war_belt_3")
                .Component("ref_keromber", 8)
                .Component("lth_imperfect", 2);
        }

        private void BattlemasterLeggingIII()
        {
            _builder.Create(RecipeType.BattlemasterLeggingIII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(32)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .Resref("bm_legging_3")
                .Component("ref_keromber", 7)
                .Component("lth_imperfect", 5);
        }

        private void ChannelerBootIII()
        {
            _builder.Create(RecipeType.ChannelerBootIII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .Resref("cn_boot_3")
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 7);
        }

        private void DuelistLeggingIII()
        {
            _builder.Create(RecipeType.DuelistLeggingIII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .Resref("du_legging_3")
                .Component("ref_keromber", 6)
                .Component("lth_imperfect", 5);
        }

        private void GunslingerBootIII()
        {
            _builder.Create(RecipeType.GunslingerBootIII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .Resref("gn_boot_3")
                .Component("ref_keromber", 5)
                .Component("lth_imperfect", 6);
        }

        private void DuelistBracerIII()
        {
            _builder.Create(RecipeType.DuelistBracerIII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(33)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .Resref("du_bracer_3")
                .Component("ref_keromber", 7)
                .Component("lth_imperfect", 1);
        }

        private void BattlemasterBracerIII()
        {
            _builder.Create(RecipeType.BattlemasterBracerIII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(34)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .Resref("bm_bracer_3")
                .Component("ref_keromber", 7)
                .Component("lth_imperfect", 2);
        }

        private void ChannelerGloveIII()
        {
            _builder.Create(RecipeType.ChannelerGloveIII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .Resref("cn_glove_3")
                .Component("ref_keromber", 2)
                .Component("lth_imperfect", 7);
        }

        private void GunslingerGloveIII()
        {
            _builder.Create(RecipeType.GunslingerGloveIII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .Resref("gn_glove_3")
                .Component("ref_keromber", 3)
                .Component("lth_imperfect", 6);
        }

        private void NecklaceIII()
        {
            _builder.Create(RecipeType.NecklaceIII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .Resref("necklace_3")
                .Component("ref_keromber", 4)
                .Component("lth_imperfect", 2);
        }

        private void QuicknessRingIII()
        {
            _builder.Create(RecipeType.QuicknessRingIII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 4)
                .Resref("qck_ring_3")
                .Component("ref_keromber", 4)
                .Component("lth_imperfect", 2);
        }

        private void MightCloakIII()
        {
            _builder.Create(RecipeType.MightCloakIII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .Resref("mght_cloak_3")
                .Component("ref_keromber", 2)
                .Component("lth_imperfect", 9);
        }

        private void ThiefCloakIII()
        {
            _builder.Create(RecipeType.ThiefCloakIII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .Resref("thf_cloak_3")
                .Component("ref_keromber", 3)
                .Component("lth_imperfect", 7);
        }

        private void WarriorCloakIII()
        {
            _builder.Create(RecipeType.WarriorCloakIII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 4)
                .Resref("war_cloak_3")
                .Component("ref_keromber", 4)
                .Component("lth_imperfect", 6);
        }
    }
}