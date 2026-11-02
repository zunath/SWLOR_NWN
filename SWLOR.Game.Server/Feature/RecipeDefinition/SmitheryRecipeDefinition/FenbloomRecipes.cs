using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class FenbloomRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Fenbloom Harness
            _builder.Create(RecipeType.FenbloomHarness, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("mv_harness")
                .Level(25)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 4)
                .Component("fiberp_flawed", 2)
                .Component("mv_core", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 34, coolingRate: 1, startingThermal: 33, shapeShare: 70);

            // Fenbloom Wraps
            _builder.Create(RecipeType.FenbloomWraps, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("mv_wraps")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mv_core", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 36, coolingRate: 7, startingThermal: 43, shapeShare: 62);

            // Fenbloom Treads
            _builder.Create(RecipeType.FenbloomTreads, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("mv_treads")
                .Level(23)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mv_core", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 40, coolingRate: 1, startingThermal: 39, shapeShare: 62);

            // Fenbloom Sash
            _builder.Create(RecipeType.FenbloomSash, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("mv_sash")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mv_core", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 34, coolingRate: 5, startingThermal: 42, shapeShare: 54);

            // Fenbloom Mantle
            _builder.Create(RecipeType.FenbloomMantle, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("mv_mantle")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mv_core", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 41, coolingRate: 3, startingThermal: 36, shapeShare: 54);

            // Fenbloom Gorget
            _builder.Create(RecipeType.FenbloomGorget, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("mv_gorget")
                .Level(25)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mv_core", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 34, coolingRate: 2, startingThermal: 23, shapeShare: 38);

            // Fenbloom Band
            _builder.Create(RecipeType.FenbloomBand, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("mv_band")
                .Level(26)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mv_core", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 38, coolingRate: 3, startingThermal: 32, shapeShare: 38);

            // Fenbloom Guard
            _builder.Create(RecipeType.FenbloomGuard, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("mv_guard")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mv_core", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 34, coolingRate: 4, startingThermal: 35, shapeShare: 62);

            // Fenbloom Visor
            _builder.Create(RecipeType.FenbloomVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("mv_visor")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2)
                .Component("mv_core", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 42, coolingRate: 6, startingThermal: 41, shapeShare: 62);

            // Fenbloom Charm
            _builder.Create(RecipeType.FenbloomCharm, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("mv_charm")
                .Level(26)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mv_core", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 38, coolingRate: 7, startingThermal: 37, shapeShare: 38);
        }
    }
}
