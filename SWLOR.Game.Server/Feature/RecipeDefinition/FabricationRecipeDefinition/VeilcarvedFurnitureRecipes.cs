using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition
{
    public class VeilcarvedFurnitureRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Veilcarved Field Cot
            _builder.Create(RecipeType.VeilcarvedFieldCot, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0381")
                .Level(32)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2)
                .Component("mg_totem", 1)
                .FabricatorParameters(frameSizePercent: 50, shellSizePercent: 27, frameQualityPercent: 63, shellQualityPercent: 20, wearRate: 4);

            // Veilcarved Signal Lamp
            _builder.Create(RecipeType.VeilcarvedSignalLamp, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0382")
                .Level(33)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_flawed", 3)
                .Component("ref_scordspar", 1)
                .Component("mg_totem", 1)
                .FabricatorParameters(frameSizePercent: 18, shellSizePercent: 27, frameQualityPercent: 15, shellQualityPercent: 30, wearRate: 4);

            // Veilcarved Trophy Stand
            _builder.Create(RecipeType.VeilcarvedTrophyStand, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0383")
                .Level(36)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_scordspar", 2)
                .Component("mg_totem", 1)
                .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 22, frameQualityPercent: 28, shellQualityPercent: 20, wearRate: 4);

            // Veilcarved Low Table
            _builder.Create(RecipeType.VeilcarvedLowTable, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0384")
                .Level(34)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("fiberp_flawed", 2)
                .Component("mg_totem", 1)
                .FabricatorParameters(frameSizePercent: 42, shellSizePercent: 41, frameQualityPercent: 39, shellQualityPercent: 40, wearRate: 5);

            // Veilcarved Wall Banner
            _builder.Create(RecipeType.VeilcarvedWallBanner, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0385")
                .Level(35)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_flawed", 3)
                .Component("lth_flawed", 2)
                .Component("mg_totem", 1)
                .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 49, frameQualityPercent: 25, shellQualityPercent: 56, wearRate: 5);

            // Veilcarved Supply Locker
            _builder.Create(RecipeType.VeilcarvedSupplyLocker, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0386")
                .Level(37)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_scordspar", 2)
                .Component("mg_totem", 1)
                .FabricatorParameters(frameSizePercent: 32, shellSizePercent: 36, frameQualityPercent: 39, shellQualityPercent: 36, wearRate: 5);

            // Veilcarved Floor Mat
            _builder.Create(RecipeType.VeilcarvedFloorMat, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("structure_0387")
                .Level(32)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2)
                .Component("mg_totem", 1)
                .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 56, frameQualityPercent: 24, shellQualityPercent: 59, wearRate: 4);

            // Veilcarved Data Console
            _builder.Create(RecipeType.VeilcarvedDataConsole, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0388")
                .Level(37)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("elec_flawed", 3)
                .Component("fiberp_flawed", 2)
                .Component("mg_totem", 1)
                .FabricatorParameters(frameSizePercent: 20, shellSizePercent: 28, frameQualityPercent: 20, shellQualityPercent: 30, wearRate: 4);

            // Veilcarved Display Plinth
            _builder.Create(RecipeType.VeilcarvedDisplayPlinth, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0389")
                .Level(36)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_scordspar", 2)
                .Component("mg_totem", 1)
                .FabricatorParameters(frameSizePercent: 33, shellSizePercent: 51, frameQualityPercent: 20, shellQualityPercent: 64, wearRate: 4);

            // Veilcarved Work Stool
            _builder.Create(RecipeType.VeilcarvedWorkStool, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0390")
                .Level(33)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("lth_flawed", 2)
                .Component("mg_totem", 1)
                .FabricatorParameters(frameSizePercent: 44, shellSizePercent: 35, frameQualityPercent: 45, shellQualityPercent: 40, wearRate: 4);
        }
    }
}
