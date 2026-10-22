using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition
{
    public class StonewakeFurnitureRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Stonewake Field Cot
            _builder.Create(RecipeType.StonewakeFieldCot, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0361")
                .Level(26)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2)
                .Component("vs_mask", 1)
                .FabricatorParameters(frameSizePercent: 50, shellSizePercent: 35, frameQualityPercent: 61, shellQualityPercent: 20, wearRate: 5);

            // Stonewake Signal Lamp
            _builder.Create(RecipeType.StonewakeSignalLamp, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0362")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_flawed", 3)
                .Component("ref_scordspar", 1)
                .Component("vs_mask", 1)
                .FabricatorParameters(frameSizePercent: 17, shellSizePercent: 26, frameQualityPercent: 15, shellQualityPercent: 39, wearRate: 5);

            // Stonewake Trophy Stand
            _builder.Create(RecipeType.StonewakeTrophyStand, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0363")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_scordspar", 2)
                .Component("vs_mask", 1)
                .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 29, frameQualityPercent: 32, shellQualityPercent: 21, wearRate: 5);

            // Stonewake Low Table
            _builder.Create(RecipeType.StonewakeLowTable, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0364")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("fiberp_flawed", 2)
                .Component("vs_mask", 1)
                .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 45, frameQualityPercent: 41, shellQualityPercent: 41, wearRate: 4);

            // Stonewake Wall Banner
            _builder.Create(RecipeType.StonewakeWallBanner, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0365")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_flawed", 3)
                .Component("lth_flawed", 2)
                .Component("vs_mask", 1)
                .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 45, frameQualityPercent: 26, shellQualityPercent: 59, wearRate: 4);

            // Stonewake Supply Locker
            _builder.Create(RecipeType.StonewakeSupplyLocker, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0366")
                .Level(31)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_scordspar", 2)
                .Component("vs_mask", 1)
                .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 31, frameQualityPercent: 34, shellQualityPercent: 38, wearRate: 5);

            // Stonewake Floor Mat
            _builder.Create(RecipeType.StonewakeFloorMat, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("structure_0367")
                .Level(26)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2)
                .Component("vs_mask", 1)
                .FabricatorParameters(frameSizePercent: 24, shellSizePercent: 58, frameQualityPercent: 22, shellQualityPercent: 60, wearRate: 4);

            // Stonewake Data Console
            _builder.Create(RecipeType.StonewakeDataConsole, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0368")
                .Level(31)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("elec_flawed", 3)
                .Component("fiberp_flawed", 2)
                .Component("vs_mask", 1)
                .FabricatorParameters(frameSizePercent: 21, shellSizePercent: 27, frameQualityPercent: 18, shellQualityPercent: 28, wearRate: 4);

            // Stonewake Display Plinth
            _builder.Create(RecipeType.StonewakeDisplayPlinth, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0369")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_scordspar", 2)
                .Component("vs_mask", 1)
                .FabricatorParameters(frameSizePercent: 26, shellSizePercent: 57, frameQualityPercent: 22, shellQualityPercent: 57, wearRate: 5);

            // Stonewake Work Stool
            _builder.Create(RecipeType.StonewakeWorkStool, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0370")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("lth_flawed", 2)
                .Component("vs_mask", 1)
                .FabricatorParameters(frameSizePercent: 46, shellSizePercent: 36, frameQualityPercent: 46, shellQualityPercent: 39, wearRate: 4);
        }
    }
}
