using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition
{
    public class ResonantFurnitureRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Resonant Field Cot
            _builder.Create(RecipeType.ResonantFieldCot, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0401")
                .Level(3)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_ruined", 3)
                .Component("fiberp_ruined", 2)
                .Component("ae_echo", 1)
                .FabricatorParameters(frameSizePercent: 51, shellSizePercent: 34, frameQualityPercent: 61, shellQualityPercent: 23, wearRate: 3);

            // Resonant Signal Lamp
            _builder.Create(RecipeType.ResonantSignalLamp, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0402")
                .Level(4)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_ruined", 3)
                .Component("ref_veldite", 1)
                .Component("ae_echo", 1)
                .FabricatorParameters(frameSizePercent: 16, shellSizePercent: 33, frameQualityPercent: 15, shellQualityPercent: 34, wearRate: 3);

            // Resonant Trophy Stand
            _builder.Create(RecipeType.ResonantTrophyStand, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0403")
                .Level(7)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_veldite", 2)
                .Component("ae_echo", 1)
                .FabricatorParameters(frameSizePercent: 34, shellSizePercent: 21, frameQualityPercent: 30, shellQualityPercent: 23, wearRate: 4);

            // Resonant Low Table
            _builder.Create(RecipeType.ResonantLowTable, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0404")
                .Level(5)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("fiberp_ruined", 2)
                .Component("ae_echo", 1)
                .FabricatorParameters(frameSizePercent: 38, shellSizePercent: 41, frameQualityPercent: 37, shellQualityPercent: 44, wearRate: 4);

            // Resonant Wall Banner
            _builder.Create(RecipeType.ResonantWallBanner, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0405")
                .Level(6)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_ruined", 3)
                .Component("lth_ruined", 2)
                .Component("ae_echo", 1)
                .FabricatorParameters(frameSizePercent: 36, shellSizePercent: 49, frameQualityPercent: 29, shellQualityPercent: 56, wearRate: 4);

            // Resonant Supply Locker
            _builder.Create(RecipeType.ResonantSupplyLocker, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0406")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_veldite", 2)
                .Component("ae_echo", 1)
                .FabricatorParameters(frameSizePercent: 31, shellSizePercent: 35, frameQualityPercent: 30, shellQualityPercent: 36, wearRate: 3);

            // Resonant Floor Mat
            _builder.Create(RecipeType.ResonantFloorMat, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("structure_0407")
                .Level(3)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_ruined", 3)
                .Component("fiberp_ruined", 2)
                .Component("ae_echo", 1)
                .FabricatorParameters(frameSizePercent: 22, shellSizePercent: 58, frameQualityPercent: 20, shellQualityPercent: 61, wearRate: 3);

            // Resonant Data Console
            _builder.Create(RecipeType.ResonantDataConsole, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0408")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("elec_ruined", 3)
                .Component("fiberp_ruined", 2)
                .Component("ae_echo", 1)
                .FabricatorParameters(frameSizePercent: 22, shellSizePercent: 23, frameQualityPercent: 20, shellQualityPercent: 23, wearRate: 2);

            // Resonant Crystal
            _builder.Create(RecipeType.ResonantCrystal, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0409")
                .Level(7)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_veldite", 2)
                .Component("ae_echo", 1)
                .FabricatorParameters(frameSizePercent: 29, shellSizePercent: 52, frameQualityPercent: 21, shellQualityPercent: 58, wearRate: 3);

            // Resonant Work Stool
            _builder.Create(RecipeType.ResonantWorkStool, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0410")
                .Level(4)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("lth_ruined", 2)
                .Component("ae_echo", 1)
                .FabricatorParameters(frameSizePercent: 48, shellSizePercent: 35, frameQualityPercent: 44, shellQualityPercent: 40, wearRate: 4);
        }
    }
}
