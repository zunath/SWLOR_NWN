using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
        public class ArmorRecipes2 : IRecipeListDefinition
        {
            private readonly RecipeBuilder _builder = new RecipeBuilder();

            public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
            {
                BreastplateI();
                TunicI();
                BattlemasterHelmetI();
                ChannelerCapI();
                DuelistHelmetI();
                GunslingerCapI();
                ShieldI();
                MightBeltI();
                ThiefBeltI();
                WarriorBeltI();
                BattlemasterLeggingI();
                ChannelerBootI();
                DuelistLeggingI();
                GunslingerBootI();
                DuelistBracerI();
                BattlemasterBracerI();
                ChannelerGloveI();
                GunslingerGloveI();
                NecklaceI();
                QuicknessRingI();
                MightCloakI();
                ThiefCloakI();
                WarriorCloakI();

                return _builder.Build();
            }

            private void BreastplateI()
            {
                _builder.Create(RecipeType.BreastplateI, SkillType.Smithery)
                    .Category(RecipeCategoryType.Breastplate)
                    .Level(12)
                    .Quantity(1)
                    .RequirementPerk(PerkType.ArmorBlueprints, 2)
                    .Resref("breastplate_1")
                    .Component("ref_scordspar", 8)
                    .Component("lth_flawed", 2);
            }

            private void TunicI()
            {
                _builder.Create(RecipeType.TunicI, SkillType.Smithery)
                    .Category(RecipeCategoryType.Tunic)
                    .Level(11)
                    .Quantity(1)
                    .RequirementPerk(PerkType.ArmorBlueprints, 2)
                    .Resref("tunic_1")
                    .Component("ref_scordspar", 2)
                    .Component("lth_flawed", 8);
            }

            private void BattlemasterHelmetI()
            {
                _builder.Create(RecipeType.BattlemasterHelmetI, SkillType.Smithery)
                    .Category(RecipeCategoryType.Helmet)
                    .Level(13)
                    .Quantity(1)
                    .RequirementPerk(PerkType.ArmorBlueprints, 2)
                    .Resref("bm_helm_1")
                    .Component("ref_scordspar", 4)
                    .Component("lth_flawed", 4);
            }

            private void ChannelerCapI()
            {
                _builder.Create(RecipeType.ChannelerCapI, SkillType.Smithery)
                    .Category(RecipeCategoryType.Helmet)
                    .Level(14)
                    .Quantity(1)
                    .RequirementPerk(PerkType.ArmorBlueprints, 2)
                    .Resref("cn_cap_1")
                    .Component("ref_scordspar", 3)
                    .Component("lth_flawed", 5);
            }

            private void DuelistHelmetI()
            {
                _builder.Create(RecipeType.DuelistHelmetI, SkillType.Smithery)
                    .Category(RecipeCategoryType.Helmet)
                    .Level(15)
                    .Quantity(1)
                    .RequirementPerk(PerkType.ArmorBlueprints, 2)
                    .Resref("du_helm_1")
                    .Component("ref_scordspar", 6)
                    .Component("lth_flawed", 2);
            }

            private void GunslingerCapI()
            {
                _builder.Create(RecipeType.GunslingerCapI, SkillType.Smithery)
                    .Category(RecipeCategoryType.Helmet)
                    .Level(14)
                    .Quantity(1)
                    .RequirementPerk(PerkType.ArmorBlueprints, 2)
                    .Resref("gn_cap_1")
                    .Component("ref_scordspar", 2)
                    .Component("lth_flawed", 6);
            }

            private void ShieldI()
            {
                _builder.Create(RecipeType.ShieldI, SkillType.Smithery)
                    .Category(RecipeCategoryType.Helmet)
                    .Level(15)
                    .Quantity(1)
                    .RequirementPerk(PerkType.ArmorBlueprints, 2)
                    .Resref("shield_1")
                    .Component("ref_scordspar", 8)
                    .Component("lth_flawed", 3);
            }

            private void MightBeltI()
            {
                _builder.Create(RecipeType.MightBeltI, SkillType.Smithery)
                    .Category(RecipeCategoryType.Helmet)
                    .Level(12)
                    .Quantity(1)
                    .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                    .Resref("mght_belt_1")
                    .Component("ref_scordspar", 3)
                    .Component("lth_flawed", 5);
            }

            private void ThiefBeltI()
            {
                _builder.Create(RecipeType.ThiefBeltI, SkillType.Smithery)
                    .Category(RecipeCategoryType.Helmet)
                    .Level(13)
                    .Quantity(1)
                    .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                    .Resref("thf_belt_1")
                    .Component("ref_scordspar", 4)
                    .Component("lth_flawed", 4);
            }

            private void WarriorBeltI()
            {
                _builder.Create(RecipeType.WarriorBeltI, SkillType.Smithery)
                    .Category(RecipeCategoryType.Helmet)
                    .Level(17)
                    .Quantity(1)
                    .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                    .Resref("war_belt_1")
                    .Component("ref_scordspar", 8)
                    .Component("lth_flawed", 2);
            }

            private void BattlemasterLeggingI()
            {
                _builder.Create(RecipeType.BattlemasterLeggingI, SkillType.Smithery)
                    .Category(RecipeCategoryType.Helmet)
                    .Level(12)
                    .Quantity(1)
                    .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                    .Resref("bm_legging_1")
                    .Component("ref_scordspar", 7)
                    .Component("lth_flawed", 5);
            }

            private void ChannelerBootI()
            {
                _builder.Create(RecipeType.ChannelerBootI, SkillType.Smithery)
                    .Category(RecipeCategoryType.Helmet)
                    .Level(14)
                    .Quantity(1)
                    .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                    .Resref("cn_boot_1")
                    .Component("ref_scordspar", 5)
                    .Component("lth_flawed", 7);
            }

            private void DuelistLeggingI()
            {
                _builder.Create(RecipeType.DuelistLeggingI, SkillType.Smithery)
                    .Category(RecipeCategoryType.Helmet)
                    .Level(13)
                    .Quantity(1)
                    .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                    .Resref("du_legging_1")
                    .Component("ref_scordspar", 6)
                    .Component("lth_flawed", 5);
            }

            private void GunslingerBootI()
            {
                _builder.Create(RecipeType.GunslingerBootI, SkillType.Smithery)
                    .Category(RecipeCategoryType.Helmet)
                    .Level(15)
                    .Quantity(1)
                    .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                    .Resref("gn_boot_1")
                    .Component("ref_scordspar", 5)
                    .Component("lth_flawed", 6);
            }

            private void DuelistBracerI()
            {
                _builder.Create(RecipeType.DuelistBracerI, SkillType.Smithery)
                    .Category(RecipeCategoryType.Helmet)
                    .Level(13)
                    .Quantity(1)
                    .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                    .Resref("du_bracer_1")
                    .Component("ref_scordspar", 7)
                    .Component("lth_flawed", 1);
            }

            private void BattlemasterBracerI()
            {
                _builder.Create(RecipeType.BattlemasterBracerI, SkillType.Smithery)
                    .Category(RecipeCategoryType.Helmet)
                    .Level(14)
                    .Quantity(1)
                    .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                    .Resref("bm_bracer_1")
                    .Component("ref_scordspar", 7)
                    .Component("lth_flawed", 2);
            }

            private void ChannelerGloveI()
            {
                _builder.Create(RecipeType.ChannelerGloveI, SkillType.Smithery)
                    .Category(RecipeCategoryType.Helmet)
                    .Level(17)
                    .Quantity(1)
                    .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                    .Resref("cn_glove_1")
                    .Component("ref_scordspar", 2)
                    .Component("lth_flawed", 7);
            }

            private void GunslingerGloveI()
            {
                _builder.Create(RecipeType.GunslingerGloveI, SkillType.Smithery)
                    .Category(RecipeCategoryType.Helmet)
                    .Level(16)
                    .Quantity(1)
                    .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                    .Resref("gn_glove_1")
                    .Component("ref_scordspar", 3)
                    .Component("lth_flawed", 6);
            }

            private void NecklaceI()
            {
                _builder.Create(RecipeType.NecklaceI, SkillType.Smithery)
                    .Category(RecipeCategoryType.Helmet)
                    .Level(19)
                    .Quantity(1)
                    .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                    .Resref("necklace_1")
                    .Component("ref_scordspar", 4)
                    .Component("lth_flawed", 2);
            }

            private void QuicknessRingI()
            {
                _builder.Create(RecipeType.QuicknessRingI, SkillType.Smithery)
                    .Category(RecipeCategoryType.Helmet)
                    .Level(20)
                    .Quantity(1)
                    .RequirementPerk(PerkType.AccessoryBlueprints, 2)
                    .Resref("qck_ring_1")
                    .Component("ref_scordspar", 4)
                    .Component("lth_flawed", 2);
            }

            private void MightCloakI()
            {
                _builder.Create(RecipeType.MightCloakI, SkillType.Smithery)
                    .Category(RecipeCategoryType.Helmet)
                    .Level(18)
                    .Quantity(1)
                    .RequirementPerk(PerkType.ArmorBlueprints, 2)
                    .Resref("mght_cloak_1")
                    .Component("ref_scordspar", 2)
                    .Component("lth_flawed", 9);
            }

            private void ThiefCloakI()
            {
                _builder.Create(RecipeType.ThiefCloakI, SkillType.Smithery)
                    .Category(RecipeCategoryType.Helmet)
                    .Level(17)
                    .Quantity(1)
                    .RequirementPerk(PerkType.ArmorBlueprints, 2)
                    .Resref("thf_cloak_1")
                    .Component("ref_scordspar", 3)
                    .Component("lth_flawed", 7);
            }

            private void WarriorCloakI()
            {
                _builder.Create(RecipeType.WarriorCloakI, SkillType.Smithery)
                    .Category(RecipeCategoryType.Helmet)
                    .Level(16)
                    .Quantity(1)
                    .RequirementPerk(PerkType.ArmorBlueprints, 2)
                    .Resref("war_cloak_1")
                    .Component("ref_scordspar", 4)
                    .Component("lth_flawed", 6);
            }
        }
    }