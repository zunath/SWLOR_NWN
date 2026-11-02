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
                .ForgeParameters(rangeCenter: 40, rangeWidth: 43, coolingRate: 3, startingThermal: 46, shapeShare: 70);

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
                .ForgeParameters(rangeCenter: 40, rangeWidth: 37, coolingRate: 2, startingThermal: 40, shapeShare: 62);

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
                .ForgeParameters(rangeCenter: 39, rangeWidth: 40, coolingRate: 5, startingThermal: 48, shapeShare: 62);

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
                .ForgeParameters(rangeCenter: 40, rangeWidth: 40, coolingRate: 2, startingThermal: 51, shapeShare: 54);

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
                .ForgeParameters(rangeCenter: 40, rangeWidth: 38, coolingRate: 6, startingThermal: 45, shapeShare: 54);

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
                .ForgeParameters(rangeCenter: 40, rangeWidth: 42, coolingRate: 7, startingThermal: 41, shapeShare: 38);

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
                .ForgeParameters(rangeCenter: 40, rangeWidth: 41, coolingRate: 8, startingThermal: 46, shapeShare: 38);

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
                .ForgeParameters(rangeCenter: 40, rangeWidth: 39, coolingRate: 5, startingThermal: 45, shapeShare: 62);

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
                .ForgeParameters(rangeCenter: 40, rangeWidth: 40, coolingRate: 5, startingThermal: 45, shapeShare: 62);

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
                .ForgeParameters(rangeCenter: 40, rangeWidth: 41, coolingRate: 8, startingThermal: 46, shapeShare: 38);
        }
    }
}
