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
                .ForgeParameters(rangeCenter: 39, rangeWidth: 41, coolingRate: 4, startingThermal: 44, shapeShare: 70);

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
                .ForgeParameters(rangeCenter: 39, rangeWidth: 37, coolingRate: 2, startingThermal: 24, shapeShare: 62);

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
                .ForgeParameters(rangeCenter: 39, rangeWidth: 39, coolingRate: 1, startingThermal: 34, shapeShare: 62);

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
                .ForgeParameters(rangeCenter: 39, rangeWidth: 40, coolingRate: 1, startingThermal: 53, shapeShare: 54);

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
                .ForgeParameters(rangeCenter: 39, rangeWidth: 38, coolingRate: 1, startingThermal: 22, shapeShare: 54);

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
                .ForgeParameters(rangeCenter: 39, rangeWidth: 38, coolingRate: 3, startingThermal: 53, shapeShare: 38);

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
                .ForgeParameters(rangeCenter: 39, rangeWidth: 38, coolingRate: 5, startingThermal: 36, shapeShare: 38);

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
                .ForgeParameters(rangeCenter: 39, rangeWidth: 41, coolingRate: 1, startingThermal: 37, shapeShare: 62);

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
                .ForgeParameters(rangeCenter: 39, rangeWidth: 40, coolingRate: 1, startingThermal: 32, shapeShare: 62);

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
                .ForgeParameters(rangeCenter: 39, rangeWidth: 38, coolingRate: 3, startingThermal: 29, shapeShare: 38);
        }
    }
}
