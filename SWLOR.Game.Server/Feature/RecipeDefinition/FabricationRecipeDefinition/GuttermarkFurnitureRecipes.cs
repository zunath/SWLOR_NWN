using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition
{
    public class GuttermarkFurnitureRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Guttermark Field Cot
            _builder.Create(RecipeType.GuttermarkFieldCot, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0341")
                .Level(11)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_ruined", 3)
                .Component("fiberp_ruined", 2)
                .Component("nv_pin", 1);

            // Guttermark Signal Lamp
            _builder.Create(RecipeType.GuttermarkSignalLamp, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0342")
                .Level(12)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_ruined", 3)
                .Component("ref_veldite", 1)
                .Component("nv_pin", 1);

            // Guttermark Trophy Stand
            _builder.Create(RecipeType.GuttermarkTrophyStand, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0343")
                .Level(15)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_veldite", 2)
                .Component("nv_pin", 1);

            // Guttermark Low Table
            _builder.Create(RecipeType.GuttermarkLowTable, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0344")
                .Level(13)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("fiberp_ruined", 2)
                .Component("nv_pin", 1);

            // Guttermark Wall Banner
            _builder.Create(RecipeType.GuttermarkWallBanner, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0345")
                .Level(14)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_ruined", 3)
                .Component("lth_ruined", 2)
                .Component("nv_pin", 1);

            // Guttermark Supply Locker
            _builder.Create(RecipeType.GuttermarkSupplyLocker, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0346")
                .Level(16)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_veldite", 2)
                .Component("nv_pin", 1);

            // Guttermark Floor Mat
            _builder.Create(RecipeType.GuttermarkFloorMat, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("structure_0347")
                .Level(11)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_ruined", 3)
                .Component("fiberp_ruined", 2)
                .Component("nv_pin", 1);

            // Guttermark Data Console
            _builder.Create(RecipeType.GuttermarkDataConsole, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0348")
                .Level(16)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("elec_ruined", 3)
                .Component("fiberp_ruined", 2)
                .Component("nv_pin", 1);

            // Guttermark Display Plinth
            _builder.Create(RecipeType.GuttermarkDisplayPlinth, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0349")
                .Level(15)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_veldite", 2)
                .Component("nv_pin", 1);

            // Guttermark Work Stool
            _builder.Create(RecipeType.GuttermarkWorkStool, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0350")
                .Level(12)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("lth_ruined", 2)
                .Component("nv_pin", 1);
        }
    }
}
