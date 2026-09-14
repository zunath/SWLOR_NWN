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
                .Component("tk_badge", 1);

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
                .Component("tk_badge", 1);

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
                .Component("tk_badge", 1);

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
                .Component("tk_badge", 1);

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
                .Component("tk_badge", 1);

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
                .Component("tk_badge", 1);

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
                .Component("tk_badge", 1);

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
                .Component("tk_badge", 1);

            // Trailmark Display Plinth
            _builder.Create(RecipeType.TrailmarkDisplayPlinth, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0359")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_scordspar", 2)
                .Component("tk_badge", 1);

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
                .Component("tk_badge", 1);
        }
    }
}
