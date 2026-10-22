using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition
{
    public class FieldlineFurnitureRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Fieldline Field Cot
            _builder.Create(RecipeType.FieldlineFieldCot, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0411")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_good", 3)
                .Component("fiberp_good", 2)
                .Component("field_chip", 1)
                .FabricatorParameters(frameSizePercent: 54, shellSizePercent: 30, frameQualityPercent: 55, shellQualityPercent: 30, wearRate: 4);

            // Fieldline Signal Lamp
            _builder.Create(RecipeType.FieldlineSignalLamp, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0412")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("elec_good", 3)
                .Component("ref_veldite", 2)
                .Component("field_chip", 1)
                .FabricatorParameters(frameSizePercent: 19, shellSizePercent: 32, frameQualityPercent: 15, shellQualityPercent: 35, wearRate: 4);

            // Fieldline Trophy Stand
            _builder.Create(RecipeType.FieldlineTrophyStand, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0413")
                .Level(31)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 4)
                .Component("elec_good", 2)
                .Component("field_chip", 1)
                .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 27, frameQualityPercent: 35, shellQualityPercent: 25, wearRate: 4);

            // Fieldline Low Table
            _builder.Create(RecipeType.FieldlineLowTable, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0414")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 4)
                .Component("fiberp_good", 2)
                .Component("field_chip", 1)
                .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 43, frameQualityPercent: 40, shellQualityPercent: 45, wearRate: 4);

            // Fieldline Wall Banner
            _builder.Create(RecipeType.FieldlineWallBanner, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0415")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_good", 3)
                .Component("lth_good", 2)
                .Component("field_chip", 1)
                .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 49, frameQualityPercent: 29, shellQualityPercent: 50, wearRate: 6);

            // Fieldline Supply Locker
            _builder.Create(RecipeType.FieldlineSupplyLocker, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0416")
                .Level(32)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 4)
                .Component("elec_good", 2)
                .Component("field_chip", 1)
                .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 28, frameQualityPercent: 33, shellQualityPercent: 32, wearRate: 5);

            // Fieldline Floor Mat
            _builder.Create(RecipeType.FieldlineFloorMat, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("structure_0417")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_good", 3)
                .Component("fiberp_good", 2)
                .Component("field_chip", 1)
                .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 55, frameQualityPercent: 29, shellQualityPercent: 56, wearRate: 6);

            // Fieldline Data Console
            _builder.Create(RecipeType.FieldlineDataConsole, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0418")
                .Level(32)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("elec_good", 4)
                .Component("fiberp_good", 2)
                .Component("field_chip", 1)
                .FabricatorParameters(frameSizePercent: 24, shellSizePercent: 29, frameQualityPercent: 19, shellQualityPercent: 22, wearRate: 6);

            // Fieldline Display Plinth
            _builder.Create(RecipeType.FieldlineDisplayPlinth, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0419")
                .Level(31)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 4)
                .Component("ref_veldite", 2)
                .Component("field_chip", 1)
                .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 54, frameQualityPercent: 25, shellQualityPercent: 55, wearRate: 4);

            // Fieldline Work Stool
            _builder.Create(RecipeType.FieldlineWorkStool, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0420")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 4)
                .Component("lth_good", 2)
                .Component("field_chip", 1)
                .FabricatorParameters(frameSizePercent: 43, shellSizePercent: 41, frameQualityPercent: 42, shellQualityPercent: 41, wearRate: 4);
        }
    }
}
