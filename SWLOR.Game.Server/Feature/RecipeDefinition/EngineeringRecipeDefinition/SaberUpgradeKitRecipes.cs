using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class SaberUpgradeKitRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            LightsaberKits();
            SaberstaffKits();

            return _builder.Build();
        }

        private void LightsaberKits()
        {
            // Lightsaber Upgrade Kit II
            _builder.Create(RecipeType.LightsaberUpgradeKit2, SkillType.Engineering)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("saber_upg2")
                .Level(18)
                .Quantity(1)
                .Component("elec_flawed", 4)
                .Component("fiberp_flawed", 2)
                .Component("ref_scordspar", 3);

            // Lightsaber Upgrade Kit III
            _builder.Create(RecipeType.LightsaberUpgradeKit3, SkillType.Engineering)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("saber_upg3")
                .Level(28)
                .Quantity(1)
                .Component("elec_good", 4)
                .Component("fiberp_good", 2)
                .Component("ref_plagionite", 3);

            // Lightsaber Upgrade Kit IV
            _builder.Create(RecipeType.LightsaberUpgradeKit4, SkillType.Engineering)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("saber_upg4")
                .Level(38)
                .Quantity(1)
                .Component("elec_imperfect", 4)
                .Component("fiberp_imperfect", 2)
                .Component("ref_keromber", 3);

            // Lightsaber Upgrade Kit V
            _builder.Create(RecipeType.LightsaberUpgradeKit5, SkillType.Engineering)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("saber_upg5")
                .Level(48)
                .Quantity(1)
                .Component("elec_high", 4)
                .Component("fiberp_high", 2)
                .Component("ref_jasioclase", 3);

            // Chiro Lightsaber Upgrade Kit
            _builder.Create(RecipeType.ChiroLightsaberUpgradeKit, SkillType.Engineering)
                .Category(RecipeCategoryType.Lightsaber)
                .Resref("saber_upgchi")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.None, 0)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5);
        }

        private void SaberstaffKits()
        {
            // Saberstaff Upgrade Kit II
            _builder.Create(RecipeType.SaberstaffUpgradeKit2, SkillType.Engineering)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("staff_upg2")
                .Level(19)
                .Quantity(1)
                .Component("elec_flawed", 5)
                .Component("fiberp_flawed", 2)
                .Component("ref_scordspar", 3);

            // Saberstaff Upgrade Kit III
            _builder.Create(RecipeType.SaberstaffUpgradeKit3, SkillType.Engineering)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("staff_upg3")
                .Level(29)
                .Quantity(1)
                .Component("elec_good", 5)
                .Component("fiberp_good", 2)
                .Component("ref_plagionite", 3);

            // Saberstaff Upgrade Kit IV
            _builder.Create(RecipeType.SaberstaffUpgradeKit4, SkillType.Engineering)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("staff_upg4")
                .Level(39)
                .Quantity(1)
                .Component("elec_imperfect", 5)
                .Component("fiberp_imperfect", 2)
                .Component("ref_keromber", 3);

            // Saberstaff Upgrade Kit V
            _builder.Create(RecipeType.SaberstaffUpgradeKit5, SkillType.Engineering)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("staff_upg5")
                .Level(49)
                .Quantity(1)
                .Component("elec_high", 5)
                .Component("fiberp_high", 2)
                .Component("ref_jasioclase", 3);

            // Chiro Saberstaff Upgrade Kit
            _builder.Create(RecipeType.ChiroSaberstaffUpgradeKit, SkillType.Engineering)
                .Category(RecipeCategoryType.Saberstaff)
                .Resref("staff_upgchi")
                .Level(52)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.None, 0)
                .ResearchCostModifier(0.2f)
                .Component("ref_arkoxit", 2)
                .Component("ref_jasioclase", 10)
                .Component("elec_high", 20)
                .Component("chiro_shard", 2)
                .Component("ref_veldite", 5)
                .Component("ref_scordspar", 5)
                .Component("ref_plagionite", 5)
                .Component("ref_keromber", 5);
        }
    }
}
