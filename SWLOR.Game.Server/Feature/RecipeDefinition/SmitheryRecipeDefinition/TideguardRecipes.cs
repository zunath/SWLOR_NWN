using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class TideguardRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Tideguard Harness
            _builder.Create(RecipeType.TideguardHarness, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("tg_harness")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_good", 4)
                .Component("fiberp_good", 2)
                .Component("tide_scale", 1)
                .ForgeParameters(rangeCenter: 38, rangeWidth: 55, coolingRate: 4, startingThermal: 52, shapeShare: 57);

            // Tideguard Wraps
            _builder.Create(RecipeType.TideguardWraps, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("tg_wraps")
                .Level(26)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_good", 2)
                .Component("fiberp_good", 1)
                .Component("tide_scale", 1)
                .ForgeParameters(rangeCenter: 40, rangeWidth: 54, coolingRate: 4, startingThermal: 49, shapeShare: 54);

            // Tideguard Treads
            _builder.Create(RecipeType.TideguardTreads, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("tg_treads")
                .Level(25)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_good", 2)
                .Component("fiberp_good", 1)
                .Component("tide_scale", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 59, coolingRate: 4, startingThermal: 46, shapeShare: 57);

            // Tideguard Sash
            _builder.Create(RecipeType.TideguardSash, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("tg_sash")
                .Level(26)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_good", 2)
                .Component("fiberp_good", 1)
                .Component("tide_scale", 1)
                .ForgeParameters(rangeCenter: 36, rangeWidth: 53, coolingRate: 3, startingThermal: 49, shapeShare: 53);

            // Tideguard Mantle
            _builder.Create(RecipeType.TideguardMantle, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("tg_mantle")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_good", 2)
                .Component("fiberp_good", 1)
                .Component("tide_scale", 1)
                .ForgeParameters(rangeCenter: 43, rangeWidth: 54, coolingRate: 2, startingThermal: 49, shapeShare: 55);

            // Tideguard Gorget
            _builder.Create(RecipeType.TideguardGorget, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("tg_gorget")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_good", 2)
                .Component("fiberp_good", 1)
                .Component("tide_scale", 1)
                .ForgeParameters(rangeCenter: 35, rangeWidth: 58, coolingRate: 3, startingThermal: 49, shapeShare: 47);

            // Tideguard Band
            _builder.Create(RecipeType.TideguardBand, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("tg_band")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_good", 2)
                .Component("fiberp_good", 1)
                .Component("tide_scale", 1)
                .ForgeParameters(rangeCenter: 36, rangeWidth: 53, coolingRate: 2, startingThermal: 53, shapeShare: 41);

            // Tideguard Guard
            _builder.Create(RecipeType.TideguardGuard, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("tg_guard")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_good", 2)
                .Component("fiberp_good", 1)
                .Component("tide_scale", 1)
                .ForgeParameters(rangeCenter: 42, rangeWidth: 53, coolingRate: 4, startingThermal: 52, shapeShare: 53);

            // Tideguard Visor
            _builder.Create(RecipeType.TideguardVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("tg_visor")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_good", 3)
                .Component("fiberp_good", 2)
                .Component("tide_scale", 1)
                .ForgeParameters(rangeCenter: 41, rangeWidth: 51, coolingRate: 2, startingThermal: 51, shapeShare: 57);

            // Tideguard Charm
            _builder.Create(RecipeType.TideguardCharm, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("tg_charm")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_good", 2)
                .Component("fiberp_good", 1)
                .Component("tide_scale", 1)
                .ForgeParameters(rangeCenter: 36, rangeWidth: 50, coolingRate: 3, startingThermal: 47, shapeShare: 44);
        }
    }
}
