using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition
{
    public class AshmarkFurnitureRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Ashmark Field Cot
            _builder.Create(RecipeType.AshmarkFieldCot, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0331")
                .Level(10)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_ruined", 3)
                .Component("fiberp_ruined", 2)
                .Component("sr_token", 1)
                .FabricatorParameters(frameSizePercent: 51, shellSizePercent: 25, frameQualityPercent: 61, shellQualityPercent: 23, wearRate: 4);

            // Ashmark Signal Lamp
            _builder.Create(RecipeType.AshmarkSignalLamp, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0332")
                .Level(11)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_ruined", 3)
                .Component("ref_veldite", 1)
                .Component("sr_token", 1)
                .FabricatorParameters(frameSizePercent: 23, shellSizePercent: 32, frameQualityPercent: 15, shellQualityPercent: 35, wearRate: 3);

            // Ashmark Trophy Stand
            _builder.Create(RecipeType.AshmarkTrophyStand, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0333")
                .Level(14)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_veldite", 2)
                .Component("sr_token", 1)
                .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 23, frameQualityPercent: 35, shellQualityPercent: 21, wearRate: 4);

            // Ashmark Low Table
            _builder.Create(RecipeType.AshmarkLowTable, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0334")
                .Level(12)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("fiberp_ruined", 2)
                .Component("sr_token", 1)
                .FabricatorParameters(frameSizePercent: 42, shellSizePercent: 40, frameQualityPercent: 36, shellQualityPercent: 45, wearRate: 3);

            // Ashmark Wall Banner
            _builder.Create(RecipeType.AshmarkWallBanner, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0335")
                .Level(13)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_ruined", 3)
                .Component("lth_ruined", 2)
                .Component("sr_token", 1)
                .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 52, frameQualityPercent: 33, shellQualityPercent: 52, wearRate: 2);

            // Ashmark Supply Locker
            _builder.Create(RecipeType.AshmarkSupplyLocker, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0336")
                .Level(15)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_veldite", 2)
                .Component("sr_token", 1)
                .FabricatorParameters(frameSizePercent: 35, shellSizePercent: 28, frameQualityPercent: 39, shellQualityPercent: 38, wearRate: 4);

            // Ashmark Floor Mat
            _builder.Create(RecipeType.AshmarkFloorMat, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("structure_0337")
                .Level(10)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_ruined", 3)
                .Component("fiberp_ruined", 2)
                .Component("sr_token", 1)
                .FabricatorParameters(frameSizePercent: 20, shellSizePercent: 65, frameQualityPercent: 20, shellQualityPercent: 61, wearRate: 4);

            // Ashmark Data Console
            _builder.Create(RecipeType.AshmarkDataConsole, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0338")
                .Level(15)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("elec_ruined", 3)
                .Component("fiberp_ruined", 2)
                .Component("sr_token", 1)
                .FabricatorParameters(frameSizePercent: 30, shellSizePercent: 27, frameQualityPercent: 16, shellQualityPercent: 24, wearRate: 4);

            // Ashmark Altar
            _builder.Create(RecipeType.AshmarkAltar, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0339")
                .Level(14)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_veldite", 2)
                .Component("sr_token", 1)
                .FabricatorParameters(frameSizePercent: 26, shellSizePercent: 54, frameQualityPercent: 25, shellQualityPercent: 58, wearRate: 4);

            // Ashmark Work Stool
            _builder.Create(RecipeType.AshmarkWorkStool, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0340")
                .Level(11)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("lth_ruined", 2)
                .Component("sr_token", 1)
                .FabricatorParameters(frameSizePercent: 44, shellSizePercent: 37, frameQualityPercent: 43, shellQualityPercent: 37, wearRate: 2);
        }
    }
}
