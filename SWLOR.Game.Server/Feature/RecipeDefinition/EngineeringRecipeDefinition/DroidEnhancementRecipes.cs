using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
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
                .RequirementPerk(PerkType.EnhancementBlueprints, 3)
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
                .RequirementPerk(PerkType.EnhancementBlueprints, 3)
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
                .RequirementPerk(PerkType.EnhancementBlueprints, 3)
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
                .RequirementPerk(PerkType.EnhancementBlueprints, 3)
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
                .RequirementPerk(PerkType.EnhancementBlueprints, 3)
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
                .RequirementPerk(PerkType.EnhancementBlueprints, 3)
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
                .RequirementPerk(PerkType.EnhancementBlueprints, 5)
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
                .RequirementPerk(PerkType.EnhancementBlueprints, 5)
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
                .RequirementPerk(PerkType.EnhancementBlueprints, 5)
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
                .RequirementPerk(PerkType.EnhancementBlueprints, 5)
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
                .RequirementPerk(PerkType.EnhancementBlueprints, 5)
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
                .RequirementPerk(PerkType.EnhancementBlueprints, 5)
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
                .RequirementPerk(PerkType.EnhancementBlueprints, 1)
                .RequirementUnlocked()
                .Component("l_unit1", 2)
                .Component("ca_unit1", 1);

            // Memory Augmentation II
            _builder.Create(RecipeType.MemoryAugmentation2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_memaug2")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 2)
                .RequirementUnlocked()
                .Component("l_unit2", 2)
                .Component("ca_unit2", 1);

            // Memory Augmentation III
            _builder.Create(RecipeType.MemoryAugmentation3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_memaug3")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 3)
                .RequirementUnlocked()
                .Component("l_unit3", 2)
                .Component("ca_unit3", 1);

            // Memory Augmentation IV
            _builder.Create(RecipeType.MemoryAugmentation4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_memaug4")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 4)
                .RequirementUnlocked()
                .Component("l_unit4", 2)
                .Component("ca_unit4", 1);

            // Memory Augmentation V
            _builder.Create(RecipeType.MemoryAugmentation5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_memaug5")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 5)
                .RequirementUnlocked()
                .Component("l_unit5", 2)
                .Component("ca_unit5", 1);
        }

        private void SkillEnhancements()
        {
            // One-Handed Boost I
            _builder.Create(RecipeType.OneHandedBoost1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_1handboost1")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 3)
                .RequirementUnlocked()
                .Component("dmotive_sys3", 3)
                .Component("d_sensor3", 2);

            // Two-Handed Boost I
            _builder.Create(RecipeType.TwoHandedBoost1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_2handboost1")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 3)
                .RequirementUnlocked()
                .Component("dmotive_sys3", 3)
                .Component("d_sensor3", 2);

            // Martial Arts Boost I
            _builder.Create(RecipeType.MartialArtsBoost1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_martialarts1")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 3)
                .RequirementUnlocked()
                .Component("dmotive_sys3", 3)
                .Component("d_sensor3", 2);

            // Ranged Boost I
            _builder.Create(RecipeType.RangedBoost1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_rangedboost1")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 3)
                .RequirementUnlocked()
                .Component("dmotive_sys3", 3)
                .Component("d_sensor3", 2);

            // One-Handed Boost II
            _builder.Create(RecipeType.OneHandedBoost2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_1handboost2")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 5)
                .RequirementUnlocked()
                .Component("dmotive_sys5", 3)
                .Component("d_sensor5", 2);

            // Two-Handed Boost II
            _builder.Create(RecipeType.TwoHandedBoost2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_2handboost2")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 5)
                .RequirementUnlocked()
                .Component("dmotive_sys5", 3)
                .Component("d_sensor5", 2);

            // Martial Arts Boost II
            _builder.Create(RecipeType.MartialArtsBoost2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_martialarts2")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 5)
                .RequirementUnlocked()
                .Component("dmotive_sys5", 3)
                .Component("d_sensor5", 2);

            // Ranged Boost II
            _builder.Create(RecipeType.RangedBoost2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidEnhancement)
                .Resref("de_rangedboost2")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.EnhancementBlueprints, 5)
                .RequirementUnlocked()
                .Component("dmotive_sys5", 3)
                .Component("d_sensor5", 2);
        }
    }
}
