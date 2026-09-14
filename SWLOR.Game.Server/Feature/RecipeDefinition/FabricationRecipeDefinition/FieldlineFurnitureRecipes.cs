using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition
{
    public class FieldlineFurnitureRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Fieldline Field Cot
            _builder.Create(RecipeType.FieldlineFieldCot, SkillType.Fabrication)
                .Category(RecipeCategoryType.Bed)
                .Resref("structure_0411")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_good", 3)
                .Component("fiberp_good", 2)
                .Component("field_chip", 1);

            // Fieldline Signal Lamp
            _builder.Create(RecipeType.FieldlineSignalLamp, SkillType.Fabrication)
                .Category(RecipeCategoryType.Lighting)
                .Resref("structure_0412")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("elec_good", 3)
                .Component("ref_veldite", 2)
                .Component("field_chip", 1);

            // Fieldline Trophy Stand
            _builder.Create(RecipeType.FieldlineTrophyStand, SkillType.Fabrication)
                .Category(RecipeCategoryType.Fixtures)
                .Resref("structure_0413")
                .Level(31)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 4)
                .Component("elec_good", 2)
                .Component("field_chip", 1);

            // Fieldline Low Table
            _builder.Create(RecipeType.FieldlineLowTable, SkillType.Fabrication)
                .Category(RecipeCategoryType.Surfaces)
                .Resref("structure_0414")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 4)
                .Component("fiberp_good", 2)
                .Component("field_chip", 1);

            // Fieldline Wall Banner
            _builder.Create(RecipeType.FieldlineWallBanner, SkillType.Fabrication)
                .Category(RecipeCategoryType.Wall)
                .Resref("structure_0415")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("fiberp_good", 3)
                .Component("lth_good", 2)
                .Component("field_chip", 1);

            // Fieldline Supply Locker
            _builder.Create(RecipeType.FieldlineSupplyLocker, SkillType.Fabrication)
                .Category(RecipeCategoryType.MiscellaneousFurniture)
                .Resref("structure_0416")
                .Level(32)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 4)
                .Component("elec_good", 2)
                .Component("field_chip", 1);

            // Fieldline Floor Mat
            _builder.Create(RecipeType.FieldlineFloorMat, SkillType.Fabrication)
                .Category(RecipeCategoryType.Flooring)
                .Resref("structure_0417")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("lth_good", 3)
                .Component("fiberp_good", 2)
                .Component("field_chip", 1);

            // Fieldline Data Console
            _builder.Create(RecipeType.FieldlineDataConsole, SkillType.Fabrication)
                .Category(RecipeCategoryType.Electronics)
                .Resref("structure_0418")
                .Level(32)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("elec_good", 4)
                .Component("fiberp_good", 2)
                .Component("field_chip", 1);

            // Fieldline Display Plinth
            _builder.Create(RecipeType.FieldlineDisplayPlinth, SkillType.Fabrication)
                .Category(RecipeCategoryType.Statues)
                .Resref("structure_0419")
                .Level(31)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 4)
                .Component("ref_veldite", 2)
                .Component("field_chip", 1);

            // Fieldline Work Stool
            _builder.Create(RecipeType.FieldlineWorkStool, SkillType.Fabrication)
                .Category(RecipeCategoryType.Seating)
                .Resref("structure_0420")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component("wood", 4)
                .Component("lth_good", 2)
                .Component("field_chip", 1);
        }
    }
}
