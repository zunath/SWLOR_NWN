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
                .Component("sr_token", 1);

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
                .Component("sr_token", 1);

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
                .Component("sr_token", 1);

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
                .Component("sr_token", 1);

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
                .Component("sr_token", 1);

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
                .Component("sr_token", 1);

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
                .Component("sr_token", 1);

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
                .Component("sr_token", 1);

            // Ashmark Display Plinth
            _builder.Create(RecipeType.AshmarkDisplayPlinth, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0339")
                .Level(14)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 3)
                .Component("ref_veldite", 2)
                .Component("sr_token", 1);

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
                .Component("sr_token", 1);
        }
    }
}
