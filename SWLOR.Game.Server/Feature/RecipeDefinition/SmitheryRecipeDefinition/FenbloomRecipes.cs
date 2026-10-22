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
                .ForgeParameters(rangeCenter: 43, rangeWidth: 53, coolingRate: 3, startingThermal: 49, shapeShare: 57);

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
                .ForgeParameters(rangeCenter: 35, rangeWidth: 54, coolingRate: 2, startingThermal: 54, shapeShare: 59);

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
                .ForgeParameters(rangeCenter: 35, rangeWidth: 55, coolingRate: 3, startingThermal: 50, shapeShare: 53);

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
                .ForgeParameters(rangeCenter: 42, rangeWidth: 54, coolingRate: 1, startingThermal: 53, shapeShare: 51);

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
                .ForgeParameters(rangeCenter: 41, rangeWidth: 57, coolingRate: 4, startingThermal: 48, shapeShare: 54);

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
                .ForgeParameters(rangeCenter: 42, rangeWidth: 56, coolingRate: 2, startingThermal: 54, shapeShare: 41);

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
                .ForgeParameters(rangeCenter: 43, rangeWidth: 60, coolingRate: 3, startingThermal: 53, shapeShare: 45);

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
                .ForgeParameters(rangeCenter: 38, rangeWidth: 55, coolingRate: 3, startingThermal: 53, shapeShare: 55);

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
                .ForgeParameters(rangeCenter: 37, rangeWidth: 56, coolingRate: 3, startingThermal: 52, shapeShare: 53);

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
                .ForgeParameters(rangeCenter: 41, rangeWidth: 55, coolingRate: 4, startingThermal: 51, shapeShare: 43);
        }
    }
}
