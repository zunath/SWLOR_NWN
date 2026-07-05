using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition
{
    public class AegislineFurnitureRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Aegisline Field Cot
            _builder.Create(RecipeType.AegislineFieldCot, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0371")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2)
                .Component("hv_plate", 1);

            // Aegisline Signal Lamp
            _builder.Create(RecipeType.AegislineSignalLamp, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0372")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_flawed", 3)
                .Component("ref_scordspar", 1)
                .Component("hv_plate", 1);

            // Aegisline Trophy Stand
            _builder.Create(RecipeType.AegislineTrophyStand, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0373")
                .Level(31)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_scordspar", 2)
                .Component("hv_plate", 1);

            // Aegisline Low Table
            _builder.Create(RecipeType.AegislineLowTable, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0374")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("fiberp_flawed", 2)
                .Component("hv_plate", 1);

            // Aegisline Wall Banner
            _builder.Create(RecipeType.AegislineWallBanner, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0375")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_flawed", 3)
                .Component("lth_flawed", 2)
                .Component("hv_plate", 1);

            // Aegisline Supply Locker
            _builder.Create(RecipeType.AegislineSupplyLocker, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0376")
                .Level(32)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_scordspar", 2)
                .Component("hv_plate", 1);

            // Aegisline Floor Mat
            _builder.Create(RecipeType.AegislineFloorMat, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("structure_0377")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2)
                .Component("hv_plate", 1);

            // Aegisline Data Console
            _builder.Create(RecipeType.AegislineDataConsole, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0378")
                .Level(32)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("elec_flawed", 3)
                .Component("fiberp_flawed", 2)
                .Component("hv_plate", 1);

            // Aegisline Display Plinth
            _builder.Create(RecipeType.AegislineDisplayPlinth, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0379")
                .Level(31)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_scordspar", 2)
                .Component("hv_plate", 1);

            // Aegisline Work Stool
            _builder.Create(RecipeType.AegislineWorkStool, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0380")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("lth_flawed", 2)
                .Component("hv_plate", 1);
        }
    }
}
