using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class InkguardRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Inkguard Harness
            _builder.Create(RecipeType.InkguardHarness, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("ig_harness")
                .Level(31)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 4)
                .Component("fiberp_high", 2)
                .Component("midink_sac", 1)
                .ForgeParameters(rangeCenter: 36, rangeWidth: 49, coolingRate: 4, startingThermal: 44, shapeShare: 63);

            // Inkguard Wraps
            _builder.Create(RecipeType.InkguardWraps, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("ig_wraps")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_high", 1)
                .Component("midink_sac", 1)
                .ForgeParameters(rangeCenter: 38, rangeWidth: 49, coolingRate: 2, startingThermal: 51, shapeShare: 56);

            // Inkguard Treads
            _builder.Create(RecipeType.InkguardTreads, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("ig_treads")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_high", 1)
                .Component("midink_sac", 1)
                .ForgeParameters(rangeCenter: 38, rangeWidth: 52, coolingRate: 3, startingThermal: 50, shapeShare: 54);

            // Inkguard Sash
            _builder.Create(RecipeType.InkguardSash, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("ig_sash")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_high", 1)
                .Component("midink_sac", 1)
                .ForgeParameters(rangeCenter: 44, rangeWidth: 55, coolingRate: 3, startingThermal: 51, shapeShare: 54);

            // Inkguard Mantle
            _builder.Create(RecipeType.InkguardMantle, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("ig_mantle")
                .Level(33)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_high", 1)
                .Component("midink_sac", 1)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 49, coolingRate: 2, startingThermal: 43, shapeShare: 50);

            // Inkguard Gorget
            _builder.Create(RecipeType.InkguardGorget, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("ig_gorget")
                .Level(31)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_high", 1)
                .Component("midink_sac", 1)
                .ForgeParameters(rangeCenter: 41, rangeWidth: 48, coolingRate: 3, startingThermal: 51, shapeShare: 42);

            // Inkguard Band
            _builder.Create(RecipeType.InkguardBand, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("ig_band")
                .Level(32)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_high", 1)
                .Component("midink_sac", 1)
                .ForgeParameters(rangeCenter: 38, rangeWidth: 53, coolingRate: 3, startingThermal: 51, shapeShare: 42);

            // Inkguard Guard
            _builder.Create(RecipeType.InkguardGuard, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("ig_guard")
                .Level(34)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_high", 1)
                .Component("midink_sac", 1)
                .ForgeParameters(rangeCenter: 41, rangeWidth: 51, coolingRate: 3, startingThermal: 43, shapeShare: 53);

            // Inkguard Visor
            _builder.Create(RecipeType.InkguardVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("ig_visor")
                .Level(33)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 3)
                .Component("fiberp_high", 2)
                .Component("midink_sac", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 55, coolingRate: 3, startingThermal: 45, shapeShare: 58);

            // Inkguard Charm
            _builder.Create(RecipeType.InkguardCharm, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("ig_charm")
                .Level(32)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_high", 1)
                .Component("midink_sac", 1)
                .ForgeParameters(rangeCenter: 41, rangeWidth: 54, coolingRate: 2, startingThermal: 49, shapeShare: 43);
        }
    }
}
