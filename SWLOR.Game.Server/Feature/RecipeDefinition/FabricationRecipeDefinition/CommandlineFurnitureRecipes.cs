using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition
{
    public class CommandlineFurnitureRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Commandline Field Cot
            _builder.Create(RecipeType.CommandlineFieldCot, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0421")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_high", 3)
                .Component("fiberp_high", 2)
                .Component("command_key", 1)
                .FabricatorParameters(frameSizePercent: 50, shellSizePercent: 31, frameQualityPercent: 60, shellQualityPercent: 22, wearRate: 8);

            // Commandline Signal Lamp
            _builder.Create(RecipeType.CommandlineSignalLamp, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0422")
                .Level(31)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("elec_imperfect", 3)
                .Component("ref_arkoxit", 2)
                .Component("command_key", 1)
                .FabricatorParameters(frameSizePercent: 18, shellSizePercent: 35, frameQualityPercent: 15, shellQualityPercent: 33, wearRate: 8);

            // Commandline Trophy Stand
            _builder.Create(RecipeType.CommandlineTrophyStand, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0423")
                .Level(34)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 5)
                .Component("elec_imperfect", 2)
                .Component("command_key", 1)
                .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 23, frameQualityPercent: 31, shellQualityPercent: 28, wearRate: 4);

            // Commandline Low Table
            _builder.Create(RecipeType.CommandlineLowTable, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0424")
                .Level(32)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 5)
                .Component("fiberp_high", 2)
                .Component("command_key", 1)
                .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 47, frameQualityPercent: 39, shellQualityPercent: 42, wearRate: 4);

            // Commandline Wall Banner
            _builder.Create(RecipeType.CommandlineWallBanner, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0425")
                .Level(33)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_high", 3)
                .Component("lth_high", 2)
                .Component("command_key", 1)
                .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 53, frameQualityPercent: 25, shellQualityPercent: 54, wearRate: 6);

            // Commandline Supply Locker
            _builder.Create(RecipeType.CommandlineSupplyLocker, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0426")
                .Level(35)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 5)
                .Component("elec_imperfect", 2)
                .Component("command_key", 1)
                .FabricatorParameters(frameSizePercent: 39, shellSizePercent: 34, frameQualityPercent: 30, shellQualityPercent: 29, wearRate: 4);

            // Commandline Floor Mat
            _builder.Create(RecipeType.CommandlineFloorMat, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("structure_0427")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_high", 3)
                .Component("fiberp_high", 2)
                .Component("command_key", 1)
                .FabricatorParameters(frameSizePercent: 23, shellSizePercent: 61, frameQualityPercent: 23, shellQualityPercent: 58, wearRate: 7);

            // Commandline Data Console
            _builder.Create(RecipeType.CommandlineDataConsole, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0428")
                .Level(35)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("elec_imperfect", 4)
                .Component("fiberp_high", 2)
                .Component("command_key", 1)
                .FabricatorParameters(frameSizePercent: 28, shellSizePercent: 28, frameQualityPercent: 21, shellQualityPercent: 29, wearRate: 7);

            // Commandline Bust
            _builder.Create(RecipeType.CommandlineBust, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0429")
                .Level(34)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 5)
                .Component("ref_arkoxit", 2)
                .Component("command_key", 1)
                .FabricatorParameters(frameSizePercent: 25, shellSizePercent: 54, frameQualityPercent: 21, shellQualityPercent: 55, wearRate: 4);

            // Commandline Work Stool
            _builder.Create(RecipeType.CommandlineWorkStool, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0430")
                .Level(31)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 5)
                .Component("lth_high", 2)
                .Component("command_key", 1)
                .FabricatorParameters(frameSizePercent: 41, shellSizePercent: 36, frameQualityPercent: 41, shellQualityPercent: 38, wearRate: 5);
        }
    }
}
