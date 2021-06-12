using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class ArmorRecipes3 : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            BreastplateII();
            TunicII();
            BattlemasterHelmetII();
            ChannelerCapII();
            DuelistHelmetII();
            GunslingerCapII();
            ShieldII();
            MightBeltII();
            ThiefBeltII();
            WarriorBeltII();
            BattlemasterLeggingII();
            ChannelerBootII();
            DuelistLeggingII();
            GunslingerBootII();
            DuelistBracerII();
            BattlemasterBracerII();
            ChannelerGloveII();
            GunslingerGloveII();
            NecklaceII();
            QuicknessRingII();
            MightCloakII();
            ThiefCloakII();
            WarriorCloakII();

            return _builder.Build();
        }

        private void BreastplateII()
        {
            _builder.Create(RecipeType.BreastplateII, SkillType.Smithery)
                .Category(RecipeCategoryType.Breastplate)
                .Level(22)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .Resref("breastplate_2")
                .Component("ref_plagionite", 8)
                .Component("lth_good", 2);
        }

        private void TunicII()
        {
            _builder.Create(RecipeType.TunicII, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Level(21)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .Resref("tunic_2")
                .Component("ref_plagionite", 2)
                .Component("lth_good", 8);
        }

        private void BattlemasterHelmetII()
        {
            _builder.Create(RecipeType.BattlemasterHelmetII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .Resref("bm_helm_2")
                .Component("ref_plagionite", 4)
                .Component("lth_good", 4);
        }

        private void ChannelerCapII()
        {
            _builder.Create(RecipeType.ChannelerCapII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .Resref("cn_cap_2")
                .Component("ref_plagionite", 3)
                .Component("lth_good", 5);
        }

        private void DuelistHelmetII()
        {
            _builder.Create(RecipeType.DuelistHelmetII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .Resref("du_helm_2")
                .Component("ref_plagionite", 6)
                .Component("lth_good", 2);
        }

        private void GunslingerCapII()
        {
            _builder.Create(RecipeType.GunslingerCapII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .Resref("gn_cap_2")
                .Component("ref_plagionite", 2)
                .Component("lth_good", 6);
        }

        private void ShieldII()
        {
            _builder.Create(RecipeType.ShieldII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .Resref("shield_2")
                .Component("ref_plagionite", 8)
                .Component("lth_good", 3);
        }

        private void MightBeltII()
        {
            _builder.Create(RecipeType.MightBeltII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(22)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .Resref("mght_belt_2")
                .Component("ref_plagionite", 3)
                .Component("lth_good", 5);
        }

        private void ThiefBeltII()
        {
            _builder.Create(RecipeType.ThiefBeltII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .Resref("thf_belt_2")
                .Component("ref_plagionite", 4)
                .Component("lth_good", 4);
        }

        private void WarriorBeltII()
        {
            _builder.Create(RecipeType.WarriorBeltII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .Resref("war_belt_2")
                .Component("ref_plagionite", 8)
                .Component("lth_good", 2);
        }

        private void BattlemasterLeggingII()
        {
            _builder.Create(RecipeType.BattlemasterLeggingII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(22)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .Resref("bm_legging_2")
                .Component("ref_plagionite", 7)
                .Component("lth_good", 5);
        }

        private void ChannelerBootII()
        {
            _builder.Create(RecipeType.ChannelerBootII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .Resref("cn_boot_2")
                .Component("ref_plagionite", 5)
                .Component("lth_good", 7);
        }

        private void DuelistLeggingII()
        {
            _builder.Create(RecipeType.DuelistLeggingII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .Resref("du_legging_2")
                .Component("ref_plagionite", 6)
                .Component("lth_good", 5);
        }

        private void GunslingerBootII()
        {
            _builder.Create(RecipeType.GunslingerBootII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .Resref("gn_boot_2")
                .Component("ref_plagionite", 5)
                .Component("lth_good", 6);
        }

        private void DuelistBracerII()
        {
            _builder.Create(RecipeType.DuelistBracerII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(23)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .Resref("du_bracer_2")
                .Component("ref_plagionite", 7)
                .Component("lth_good", 1);
        }

        private void BattlemasterBracerII()
        {
            _builder.Create(RecipeType.BattlemasterBracerII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(24)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .Resref("bm_bracer_2")
                .Component("ref_plagionite", 7)
                .Component("lth_good", 2);
        }

        private void ChannelerGloveII()
        {
            _builder.Create(RecipeType.ChannelerGloveII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .Resref("cn_glove_2")
                .Component("ref_plagionite", 2)
                .Component("lth_good", 7);
        }

        private void GunslingerGloveII()
        {
            _builder.Create(RecipeType.GunslingerGloveII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .Resref("gn_glove_2")
                .Component("ref_plagionite", 3)
                .Component("lth_good", 6);
        }

        private void NecklaceII()
        {
            _builder.Create(RecipeType.NecklaceII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .Resref("necklace_2")
                .Component("ref_plagionite", 4)
                .Component("lth_good", 2);
        }

        private void QuicknessRingII()
        {
            _builder.Create(RecipeType.QuicknessRingII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 3)
                .Resref("qck_ring_2")
                .Component("ref_plagionite", 4)
                .Component("lth_good", 2);
        }

        private void MightCloakII()
        {
            _builder.Create(RecipeType.MightCloakII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .Resref("mght_cloak_2")
                .Component("ref_plagionite", 2)
                .Component("lth_good", 9);
        }

        private void ThiefCloakII()
        {
            _builder.Create(RecipeType.ThiefCloakII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .Resref("thf_cloak_2")
                .Component("ref_plagionite", 3)
                .Component("lth_good", 7);
        }

        private void WarriorCloakII()
        {
            _builder.Create(RecipeType.WarriorCloakII, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 3)
                .Resref("war_cloak_2")
                .Component("ref_plagionite", 4)
                .Component("lth_good", 6);
        }
    }
}