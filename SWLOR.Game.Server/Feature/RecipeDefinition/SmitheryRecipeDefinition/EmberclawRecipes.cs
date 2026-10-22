using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class EmberclawRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Emberclaw Harness
            _builder.Create(RecipeType.EmberclawHarness, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("rk_harness")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 4)
                .Component("fiberp_flawed", 2)
                .Component("rk_claw", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 58, coolingRate: 2, startingThermal: 46, shapeShare: 61);

            // Emberclaw Wraps
            _builder.Create(RecipeType.EmberclawWraps, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("rk_wraps")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("rk_claw", 1)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 52, coolingRate: 2, startingThermal: 50, shapeShare: 54);

            // Emberclaw Treads
            _builder.Create(RecipeType.EmberclawTreads, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("rk_treads")
                .Level(26)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("rk_claw", 1)
                .ForgeParameters(rangeCenter: 43, rangeWidth: 56, coolingRate: 3, startingThermal: 50, shapeShare: 58);

            // Emberclaw Sash
            _builder.Create(RecipeType.EmberclawSash, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("rk_sash")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("rk_claw", 1)
                .ForgeParameters(rangeCenter: 38, rangeWidth: 52, coolingRate: 4, startingThermal: 51, shapeShare: 51);

            // Emberclaw Mantle
            _builder.Create(RecipeType.EmberclawMantle, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("rk_mantle")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("rk_claw", 1)
                .ForgeParameters(rangeCenter: 36, rangeWidth: 49, coolingRate: 2, startingThermal: 52, shapeShare: 52);

            // Emberclaw Gorget
            _builder.Create(RecipeType.EmberclawGorget, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("rk_gorget")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("rk_claw", 1)
                .ForgeParameters(rangeCenter: 35, rangeWidth: 50, coolingRate: 4, startingThermal: 50, shapeShare: 42);

            // Emberclaw Band
            _builder.Create(RecipeType.EmberclawBand, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("rk_band")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("rk_claw", 1)
                .ForgeParameters(rangeCenter: 42, rangeWidth: 55, coolingRate: 2, startingThermal: 50, shapeShare: 42);

            // Emberclaw Guard
            _builder.Create(RecipeType.EmberclawGuard, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("rk_guard")
                .Level(31)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("rk_claw", 1)
                .ForgeParameters(rangeCenter: 43, rangeWidth: 54, coolingRate: 2, startingThermal: 46, shapeShare: 56);

            // Emberclaw Visor
            _builder.Create(RecipeType.EmberclawVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("rk_visor")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2)
                .Component("rk_claw", 1)
                .ForgeParameters(rangeCenter: 44, rangeWidth: 56, coolingRate: 3, startingThermal: 52, shapeShare: 54);

            // Emberclaw Charm
            _builder.Create(RecipeType.EmberclawCharm, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("rk_charm")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("rk_claw", 1)
                .ForgeParameters(rangeCenter: 40, rangeWidth: 58, coolingRate: 3, startingThermal: 47, shapeShare: 44);
        }
    }
}
