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
                .Component("vs_mask", 1);

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
                .Component("vs_mask", 1);

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
                .Component("vs_mask", 1);

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
                .Component("vs_mask", 1);

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
                .Component("vs_mask", 1);

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
                .Component("vs_mask", 1);

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
                .Component("vs_mask", 1);

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
                .Component("vs_mask", 1);

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
                .Component("vs_mask", 1);

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
                .Component("vs_mask", 1);
        }
    }
}
