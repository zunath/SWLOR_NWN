using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition
{
    public class SurgewakeFurnitureRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Surgewake Field Cot
            _builder.Create(RecipeType.SurgewakeFieldCot, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("vx_cot")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_flawed", 8)
                .Component("fiberp_high", 4)
                .Component("vx_core", 1);

            // Surgewake Signal Lamp
            _builder.Create(RecipeType.SurgewakeSignalLamp, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("vx_lamp")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_high", 8)
                .Component("ref_arkoxit", 4)
                .Component("vx_core", 1);

            // Surgewake Trophy Stand
            _builder.Create(RecipeType.SurgewakeTrophyStand, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("vx_stand")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_arkoxit", 8)
                .Component("fiberp_high", 4)
                .Component("vx_core", 1);

            // Surgewake Low Table
            _builder.Create(RecipeType.SurgewakeLowTable, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("vx_table")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_arkoxit", 8)
                .Component("fiberp_high", 4)
                .Component("vx_core", 1);

            // Surgewake Wall Banner
            _builder.Create(RecipeType.SurgewakeWallBanner, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("vx_banner")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_high", 8)
                .Component("lth_flawed", 4)
                .Component("vx_core", 1);

            // Surgewake Supply Locker
            _builder.Create(RecipeType.SurgewakeSupplyLocker, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("vx_locker")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_arkoxit", 8)
                .Component("elec_high", 4)
                .Component("vx_core", 1);

            // Surgewake Floor Mat
            _builder.Create(RecipeType.SurgewakeFloorMat, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("vx_mat")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_flawed", 8)
                .Component("fiberp_high", 4)
                .Component("vx_core", 1);

            // Surgewake Data Console
            _builder.Create(RecipeType.SurgewakeDataConsole, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("vx_console")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("elec_high", 8)
                .Component("fiberp_high", 4)
                .Component("vx_core", 1);

            // Surgewake Display Plinth
            _builder.Create(RecipeType.SurgewakeDisplayPlinth, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("vx_plinth")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_arkoxit", 8)
                .Component("fiberp_high", 4)
                .Component("vx_core", 1);

            // Surgewake Work Stool
            _builder.Create(RecipeType.SurgewakeWorkStool, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("vx_stool")
                .Level(50)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("ref_arkoxit", 8)
                .Component("lth_flawed", 4)
                .Component("vx_core", 1);
        }
    }
}
