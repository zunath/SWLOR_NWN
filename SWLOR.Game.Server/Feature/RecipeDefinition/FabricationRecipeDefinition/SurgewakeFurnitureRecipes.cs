using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition
{
    public class SurgewakeFurnitureRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Surgewake Field Cot
            _builder.Create(RecipeType.SurgewakeFieldCot, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0391")
                .Level(46)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_flawed", 8)
                .Component("fiberp_high", 4)
                .Component("vx_core", 1)
                .FabricatorParameters(frameSizePercent: 56, shellSizePercent: 27, frameQualityPercent: 56, shellQualityPercent: 26, wearRate: 5);

            // Surgewake Signal Lamp
            _builder.Create(RecipeType.SurgewakeSignalLamp, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0392")
                .Level(47)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_high", 8)
                .Component("ref_arkoxit", 4)
                .Component("vx_core", 1)
                .FabricatorParameters(frameSizePercent: 19, shellSizePercent: 32, frameQualityPercent: 15, shellQualityPercent: 36, wearRate: 8);

            // Surgewake Trophy Stand
            _builder.Create(RecipeType.SurgewakeTrophyStand, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0393")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_arkoxit", 8)
                .Component("fiberp_high", 4)
                .Component("vx_core", 1)
                .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 24, frameQualityPercent: 32, shellQualityPercent: 29, wearRate: 7);

            // Surgewake Low Table
            _builder.Create(RecipeType.SurgewakeLowTable, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0394")
                .Level(48)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_arkoxit", 8)
                .Component("fiberp_high", 4)
                .Component("vx_core", 1)
                .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 43, frameQualityPercent: 35, shellQualityPercent: 45, wearRate: 9);

            // Surgewake Wall Banner
            _builder.Create(RecipeType.SurgewakeWallBanner, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0395")
                .Level(49)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_high", 8)
                .Component("lth_flawed", 4)
                .Component("vx_core", 1)
                .FabricatorParameters(frameSizePercent: 40, shellSizePercent: 45, frameQualityPercent: 27, shellQualityPercent: 56, wearRate: 6);

            // Surgewake Supply Locker
            _builder.Create(RecipeType.SurgewakeSupplyLocker, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0396")
                .Level(51)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_arkoxit", 8)
                .Component("elec_high", 4)
                .Component("vx_core", 1)
                .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 32, frameQualityPercent: 31, shellQualityPercent: 29, wearRate: 7);

            // Surgewake Floor Mat
            _builder.Create(RecipeType.SurgewakeFloorMat, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("structure_0397")
                .Level(46)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_flawed", 8)
                .Component("fiberp_high", 4)
                .Component("vx_core", 1)
                .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 55, frameQualityPercent: 25, shellQualityPercent: 59, wearRate: 6);

            // Surgewake Data Console
            _builder.Create(RecipeType.SurgewakeDataConsole, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0398")
                .Level(51)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("elec_high", 8)
                .Component("fiberp_high", 4)
                .Component("vx_core", 1)
                .FabricatorParameters(frameSizePercent: 22, shellSizePercent: 22, frameQualityPercent: 17, shellQualityPercent: 22, wearRate: 7);

            // Surgewake Holo Projector
            _builder.Create(RecipeType.SurgewakeHoloProjector, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0399")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_arkoxit", 8)
                .Component("fiberp_high", 4)
                .Component("vx_core", 1)
                .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 52, frameQualityPercent: 24, shellQualityPercent: 59, wearRate: 8);

            // Surgewake Work Stool
            _builder.Create(RecipeType.SurgewakeWorkStool, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0400")
                .Level(47)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_arkoxit", 8)
                .Component("lth_flawed", 4)
                .Component("vx_core", 1)
                .FabricatorParameters(frameSizePercent: 45, shellSizePercent: 35, frameQualityPercent: 41, shellQualityPercent: 37, wearRate: 8);
        }
    }
}
