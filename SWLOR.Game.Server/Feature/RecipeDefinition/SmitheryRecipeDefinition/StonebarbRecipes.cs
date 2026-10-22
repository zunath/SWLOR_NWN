using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class StonebarbRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Stonebarb Harness
            _builder.Create(RecipeType.StonebarbHarness, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("gs_harness")
                .Level(25)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 4)
                .Component("fiberp_flawed", 2)
                .Component("gs_spine", 1)
                .ForgeParameters(rangeCenter: 35, rangeWidth: 59, coolingRate: 2, startingThermal: 48, shapeShare: 60);

            // Stonebarb Wraps
            _builder.Create(RecipeType.StonebarbWraps, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("gs_wraps")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("gs_spine", 1)
                .ForgeParameters(rangeCenter: 35, rangeWidth: 53, coolingRate: 3, startingThermal: 47, shapeShare: 56);

            // Stonebarb Treads
            _builder.Create(RecipeType.StonebarbTreads, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("gs_treads")
                .Level(23)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("gs_spine", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 60, coolingRate: 2, startingThermal: 51, shapeShare: 59);

            // Stonebarb Sash
            _builder.Create(RecipeType.StonebarbSash, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("gs_sash")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("gs_spine", 1)
                .ForgeParameters(rangeCenter: 42, rangeWidth: 60, coolingRate: 2, startingThermal: 49, shapeShare: 54);

            // Stonebarb Mantle
            _builder.Create(RecipeType.StonebarbMantle, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("gs_mantle")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("gs_spine", 1)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 56, coolingRate: 3, startingThermal: 46, shapeShare: 50);

            // Stonebarb Gorget
            _builder.Create(RecipeType.StonebarbGorget, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("gs_gorget")
                .Level(25)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("gs_spine", 1)
                .ForgeParameters(rangeCenter: 40, rangeWidth: 56, coolingRate: 2, startingThermal: 49, shapeShare: 42);

            // Stonebarb Band
            _builder.Create(RecipeType.StonebarbBand, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("gs_band")
                .Level(26)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("gs_spine", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 54, coolingRate: 2, startingThermal: 46, shapeShare: 46);

            // Stonebarb Guard
            _builder.Create(RecipeType.StonebarbGuard, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("gs_guard")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("gs_spine", 1)
                .ForgeParameters(rangeCenter: 43, rangeWidth: 55, coolingRate: 3, startingThermal: 53, shapeShare: 53);

            // Stonebarb Visor
            _builder.Create(RecipeType.StonebarbVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("gs_visor")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2)
                .Component("gs_spine", 1)
                .ForgeParameters(rangeCenter: 43, rangeWidth: 53, coolingRate: 3, startingThermal: 54, shapeShare: 53);

            // Stonebarb Charm
            _builder.Create(RecipeType.StonebarbCharm, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("gs_charm")
                .Level(26)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("gs_spine", 1)
                .ForgeParameters(rangeCenter: 35, rangeWidth: 60, coolingRate: 3, startingThermal: 46, shapeShare: 46);
        }
    }
}
