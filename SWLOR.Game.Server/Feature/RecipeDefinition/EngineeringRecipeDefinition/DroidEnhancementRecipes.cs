using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class DroidEnhancementRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            StatEnhancements();
            MemoryAugmentations();
            ResistanceEnhancements();
            SkillEnhancements();

            return _builder.Build();
        }

        private void StatEnhancements()
        {
            // Sturdiness I
            _builder.Create(RecipeType.Sturdiness1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_sturdiness1")
                .Level(21)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("diag_circuit3", 1)
                .Component("diag_circuit2", 2)
                .Component("diag_circuit1", 3);

            // Ocular Filter I
            _builder.Create(RecipeType.OcularFilter1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_ocfilt1")
                .Level(22)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("d_sensor3", 1)
                .Component("d_sensor2", 2)
                .Component("d_sensor1", 3);

            // Verve I
            _builder.Create(RecipeType.Verve1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_verve1")
                .Level(23)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys3", 1)
                .Component("dmotive_sys2", 2)
                .Component("dmotive_sys1", 3);

            // Neural Boost I
            _builder.Create(RecipeType.NeuralBoost1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_neuboost1")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("l_unit3", 1)
                .Component("l_unit2", 2)
                .Component("l_unit1", 3);

            // Mobility Boost I
            _builder.Create(RecipeType.MobilityBoost1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_mobboost1")
                .Level(25)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys3", 1)
                .Component("dmotive_sys2", 2)
                .Component("dmotive_sys1", 3);

            // Social Adaption I
            _builder.Create(RecipeType.SocialAdaption1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_socadapt1")
                .Level(26)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("ca_unit3", 1)
                .Component("ca_unit2", 2)
                .Component("ca_unit1", 3);

            // Sturdiness II
            _builder.Create(RecipeType.Sturdiness2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_sturdiness2")
                .Level(41)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("diag_circuit5", 3)
                .Component("diag_circuit4", 2)
                .Component("diag_circuit3", 1);

            // Ocular Filter II
            _builder.Create(RecipeType.OcularFilter2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_ocfilt2")
                .Level(42)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("d_sensor5", 3)
                .Component("d_sensor4", 2)
                .Component("d_sensor3", 1);

            // Verve II
            _builder.Create(RecipeType.Verve2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_verve2")
                .Level(43)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys5", 3)
                .Component("dmotive_sys4", 2)
                .Component("dmotive_sys3", 1);

            // Neural Boost II
            _builder.Create(RecipeType.NeuralBoost2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_neuboost2")
                .Level(44)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("l_unit5", 3)
                .Component("l_unit4", 2)
                .Component("l_unit3", 1);

            // Mobility Boost II
            _builder.Create(RecipeType.MobilityBoost2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_mobboost2")
                .Level(45)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys5", 3)
                .Component("dmotive_sys4", 2)
                .Component("dmotive_sys3", 1);

            // Social Adaption II
            _builder.Create(RecipeType.SocialAdaption2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_socadapt2")
                .Level(46)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("ca_unit5", 3)
                .Component("ca_unit4", 2)
                .Component("ca_unit3", 1);
        }

        private void MemoryAugmentations()
        {

            // Memory Augmentation I
            _builder.Create(RecipeType.MemoryAugmentation1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_memaug1")
                .Level(6)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("l_unit1", 2)
                .Component("ca_unit1", 1);

            // Memory Augmentation II
            _builder.Create(RecipeType.MemoryAugmentation2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_memaug2")
                .Level(16)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("l_unit2", 2)
                .Component("ca_unit2", 1);

            // Memory Augmentation III
            _builder.Create(RecipeType.MemoryAugmentation3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_memaug3")
                .Level(26)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("l_unit3", 2)
                .Component("ca_unit3", 1);

            // Memory Augmentation IV
            _builder.Create(RecipeType.MemoryAugmentation4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_memaug4")
                .Level(36)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("l_unit4", 2)
                .Component("ca_unit4", 1);

            // Memory Augmentation V
            _builder.Create(RecipeType.MemoryAugmentation5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_memaug5")
                .Level(46)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("l_unit5", 2)
                .Component("ca_unit5", 1);
        }

        private void ResistanceEnhancements()
        {
            CreateResistanceEnhancement(RecipeType.DroidResistanceFire1, "de_res_fir1", 31, "diag_circuit3", "d_sensor3");
            CreateResistanceEnhancement(RecipeType.DroidResistancePoison1, "de_res_psn1", 31, "diag_circuit3", "l_unit3");
            CreateResistanceEnhancement(RecipeType.DroidResistanceElectrical1, "de_res_elec1", 32, "d_sensor3", "l_unit3");
            CreateResistanceEnhancement(RecipeType.DroidResistanceIce1, "de_res_ice1", 32, "dmotive_sys3", "d_sensor3");
            CreateResistanceEnhancement(RecipeType.DroidResistanceMind1, "de_res_mnd1", 33, "l_unit3", "ca_unit3");
            CreateResistanceEnhancement(RecipeType.DroidResistanceMobility1, "de_res_mob1", 33, "dmotive_sys3", "l_unit3");
            CreateResistanceEnhancement(RecipeType.DroidResistanceTrauma1, "de_res_tra1", 34, "diag_circuit3", "dmotive_sys3");
            CreateResistanceEnhancement(RecipeType.DroidResistanceDisruption1, "de_res_dis1", 34, "l_unit3", "d_sensor3");

            CreateResistanceEnhancement(RecipeType.DroidResistanceFire2, "de_res_fir2", 47, "diag_circuit5", "d_sensor5");
            CreateResistanceEnhancement(RecipeType.DroidResistancePoison2, "de_res_psn2", 47, "diag_circuit5", "l_unit5");
            CreateResistanceEnhancement(RecipeType.DroidResistanceElectrical2, "de_res_elec2", 48, "d_sensor5", "l_unit5");
            CreateResistanceEnhancement(RecipeType.DroidResistanceIce2, "de_res_ice2", 48, "dmotive_sys5", "d_sensor5");
            CreateResistanceEnhancement(RecipeType.DroidResistanceMind2, "de_res_mnd2", 49, "l_unit5", "ca_unit5");
            CreateResistanceEnhancement(RecipeType.DroidResistanceMobility2, "de_res_mob2", 49, "dmotive_sys5", "l_unit5");
            CreateResistanceEnhancement(RecipeType.DroidResistanceTrauma2, "de_res_tra2", 50, "diag_circuit5", "dmotive_sys5");
            CreateResistanceEnhancement(RecipeType.DroidResistanceDisruption2, "de_res_dis2", 50, "l_unit5", "d_sensor5");
        }

        private void CreateResistanceEnhancement(
            RecipeType recipeType,
            string resref,
            int level,
            string component1,
            string component2)
        {
            _builder.Create(recipeType, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref(resref)
                .Level(level)
                .Quantity(1)
                .RequirementUnlocked()
                .Component(component1, 3)
                .Component(component2, 2);
        }

        private void SkillEnhancements()
        {
            // Droid Vibroblade Boost I
            _builder.Create(RecipeType.DroidVibrobladeBoost1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_vblade_b1")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys3", 3)
                .Component("d_sensor3", 2);

            // Droid Vibroknife Boost I
            _builder.Create(RecipeType.DroidVibroknifeBoost1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_vknife_b1")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys3", 3)
                .Component("d_sensor3", 2);

            // Droid Lightsaber Boost I
            _builder.Create(RecipeType.DroidLightsaberBoost1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_lsab_b1")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys3", 3)
                .Component("d_sensor3", 2);

            // Droid Heavy Vibroblade Boost I
            _builder.Create(RecipeType.DroidHeavyVibrobladeBoost1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_hvblade_b1")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys3", 3)
                .Component("d_sensor3", 2);

            // Droid Spear Boost I
            _builder.Create(RecipeType.DroidSpearBoost1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_spear_b1")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys3", 3)
                .Component("d_sensor3", 2);

            // Droid Twin Blade Boost I
            _builder.Create(RecipeType.DroidTwinBladeBoost1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_tblade_b1")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys3", 3)
                .Component("d_sensor3", 2);

            // Droid Saberstaff Boost I
            _builder.Create(RecipeType.DroidSaberstaffBoost1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_sabst_b1")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys3", 3)
                .Component("d_sensor3", 2);

            // Droid Katar Boost I
            _builder.Create(RecipeType.DroidKatarBoost1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_katar_b1")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys3", 3)
                .Component("d_sensor3", 2);

            // Droid Staff Boost I
            _builder.Create(RecipeType.DroidStaffBoost1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_staff_b1")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys3", 3)
                .Component("d_sensor3", 2);

            // Droid Pistol Boost I
            _builder.Create(RecipeType.DroidPistolBoost1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_pistol_b1")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys3", 3)
                .Component("d_sensor3", 2);

            // Droid Rifle Boost I
            _builder.Create(RecipeType.DroidRifleBoost1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_rifle_b1")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys3", 3)
                .Component("d_sensor3", 2);

            // Droid Throwing Boost I
            _builder.Create(RecipeType.DroidThrowingBoost1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_throw_b1")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys3", 3)
                .Component("d_sensor3", 2);

            // Droid Vibroblade Boost II
            _builder.Create(RecipeType.DroidVibrobladeBoost2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_vblade_b2")
                .Level(47)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys5", 3)
                .Component("d_sensor5", 2);

            // Droid Vibroknife Boost II
            _builder.Create(RecipeType.DroidVibroknifeBoost2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_vknife_b2")
                .Level(47)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys5", 3)
                .Component("d_sensor5", 2);

            // Droid Lightsaber Boost II
            _builder.Create(RecipeType.DroidLightsaberBoost2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_lsab_b2")
                .Level(47)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys5", 3)
                .Component("d_sensor5", 2);

            // Droid Heavy Vibroblade Boost II
            _builder.Create(RecipeType.DroidHeavyVibrobladeBoost2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_hvblade_b2")
                .Level(48)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys5", 3)
                .Component("d_sensor5", 2);

            // Droid Spear Boost II
            _builder.Create(RecipeType.DroidSpearBoost2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_spear_b2")
                .Level(48)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys5", 3)
                .Component("d_sensor5", 2);

            // Droid Twin Blade Boost II
            _builder.Create(RecipeType.DroidTwinBladeBoost2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_tblade_b2")
                .Level(48)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys5", 3)
                .Component("d_sensor5", 2);

            // Droid Saberstaff Boost II
            _builder.Create(RecipeType.DroidSaberstaffBoost2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_sabst_b2")
                .Level(48)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys5", 3)
                .Component("d_sensor5", 2);

            // Droid Katar Boost II
            _builder.Create(RecipeType.DroidKatarBoost2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_katar_b2")
                .Level(49)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys5", 3)
                .Component("d_sensor5", 2);

            // Droid Staff Boost II
            _builder.Create(RecipeType.DroidStaffBoost2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_staff_b2")
                .Level(49)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys5", 3)
                .Component("d_sensor5", 2);

            // Droid Pistol Boost II
            _builder.Create(RecipeType.DroidPistolBoost2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_pistol_b2")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys5", 3)
                .Component("d_sensor5", 2);

            // Droid Rifle Boost II
            _builder.Create(RecipeType.DroidRifleBoost2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_rifle_b2")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys5", 3)
                .Component("d_sensor5", 2);

            // Droid Throwing Boost II
            _builder.Create(RecipeType.DroidThrowingBoost2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_throw_b2")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .Component("dmotive_sys5", 3)
                .Component("d_sensor5", 2);
        }
    }
}
