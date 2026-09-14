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
                .Component("ae_echo", 1);

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
                .Component("ae_echo", 1);

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
                .Component("ae_echo", 1);

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
                .Component("ae_echo", 1);

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
                .Component("ae_echo", 1);

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
                .Component("ae_echo", 1);

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
                .Component("ae_echo", 1);

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
                .Component("ae_echo", 1);

            // Resonant Display Plinth
            _builder.Create(RecipeType.ResonantDisplayPlinth, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0409")
                .Level(7)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_veldite", 2)
                .Component("ae_echo", 1);

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
                .Component("ae_echo", 1);
        }
    }
}
