using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition
{
    public class TrailmarkFurnitureRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Trailmark Field Cot
            _builder.Create(RecipeType.TrailmarkFieldCot, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0351")
                .Level(25)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2)
                .Component("tk_badge", 1)
                .FabricatorParameters(frameSizePercent: 60, shellSizePercent: 25, frameQualityPercent: 55, shellQualityPercent: 29, wearRate: 5);

            // Trailmark Signal Lamp
            _builder.Create(RecipeType.TrailmarkSignalLamp, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0352")
                .Level(26)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_flawed", 3)
                .Component("ref_scordspar", 1)
                .Component("tk_badge", 1)
                .FabricatorParameters(frameSizePercent: 22, shellSizePercent: 30, frameQualityPercent: 16, shellQualityPercent: 40, wearRate: 5);

            // Trailmark Trophy Stand
            _builder.Create(RecipeType.TrailmarkTrophyStand, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0353")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_scordspar", 2)
                .Component("tk_badge", 1)
                .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 23, frameQualityPercent: 27, shellQualityPercent: 28, wearRate: 5);

            // Trailmark Low Table
            _builder.Create(RecipeType.TrailmarkLowTable, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0354")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("fiberp_flawed", 2)
                .Component("tk_badge", 1)
                .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 45, frameQualityPercent: 41, shellQualityPercent: 44, wearRate: 5);

            // Trailmark Wall Banner
            _builder.Create(RecipeType.TrailmarkWallBanner, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0355")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_flawed", 3)
                .Component("lth_flawed", 2)
                .Component("tk_badge", 1)
                .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 47, frameQualityPercent: 26, shellQualityPercent: 58, wearRate: 5);

            // Trailmark Supply Locker
            _builder.Create(RecipeType.TrailmarkSupplyLocker, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0356")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_scordspar", 2)
                .Component("tk_badge", 1)
                .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 33, frameQualityPercent: 33, shellQualityPercent: 28, wearRate: 5);

            // Trailmark Floor Mat
            _builder.Create(RecipeType.TrailmarkFloorMat, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("structure_0357")
                .Level(25)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2)
                .Component("tk_badge", 1)
                .FabricatorParameters(frameSizePercent: 21, shellSizePercent: 62, frameQualityPercent: 26, shellQualityPercent: 56, wearRate: 3);

            // Trailmark Data Console
            _builder.Create(RecipeType.TrailmarkDataConsole, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0358")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("elec_flawed", 3)
                .Component("fiberp_flawed", 2)
                .Component("tk_badge", 1)
                .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 26, frameQualityPercent: 15, shellQualityPercent: 26, wearRate: 5);

            // Trailmark Waystone
            _builder.Create(RecipeType.TrailmarkWaystone, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0359")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_scordspar", 2)
                .Component("tk_badge", 1)
                .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 55, frameQualityPercent: 25, shellQualityPercent: 60, wearRate: 5);

            // Trailmark Work Stool
            _builder.Create(RecipeType.TrailmarkWorkStool, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0360")
                .Level(26)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("lth_flawed", 2)
                .Component("tk_badge", 1)
                .FabricatorParameters(frameSizePercent: 41, shellSizePercent: 41, frameQualityPercent: 43, shellQualityPercent: 39, wearRate: 4);
        }
    }
}
