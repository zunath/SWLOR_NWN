using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class MarshguardRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Marshguard Harness
            _builder.Create(RecipeType.MarshguardHarness, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("rc_harness")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 4)
                .Component("fiberp_flawed", 2)
                .Component("rc_vine", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 53, coolingRate: 2, startingThermal: 54, shapeShare: 58);

            // Marshguard Wraps
            _builder.Create(RecipeType.MarshguardWraps, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("rc_wraps")
                .Level(23)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("rc_vine", 1)
                .ForgeParameters(rangeCenter: 40, rangeWidth: 55, coolingRate: 3, startingThermal: 51, shapeShare: 54);

            // Marshguard Treads
            _builder.Create(RecipeType.MarshguardTreads, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("rc_treads")
                .Level(22)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("rc_vine", 1)
                .ForgeParameters(rangeCenter: 38, rangeWidth: 55, coolingRate: 1, startingThermal: 50, shapeShare: 59);

            // Marshguard Sash
            _builder.Create(RecipeType.MarshguardSash, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("rc_sash")
                .Level(23)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("rc_vine", 1)
                .ForgeParameters(rangeCenter: 43, rangeWidth: 55, coolingRate: 1, startingThermal: 56, shapeShare: 51);

            // Marshguard Mantle
            _builder.Create(RecipeType.MarshguardMantle, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("rc_mantle")
                .Level(26)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("rc_vine", 1)
                .ForgeParameters(rangeCenter: 41, rangeWidth: 57, coolingRate: 3, startingThermal: 50, shapeShare: 53);

            // Marshguard Gorget
            _builder.Create(RecipeType.MarshguardGorget, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("rc_gorget")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("rc_vine", 1)
                .ForgeParameters(rangeCenter: 43, rangeWidth: 53, coolingRate: 2, startingThermal: 51, shapeShare: 42);

            // Marshguard Band
            _builder.Create(RecipeType.MarshguardBand, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("rc_band")
                .Level(25)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("rc_vine", 1)
                .ForgeParameters(rangeCenter: 41, rangeWidth: 56, coolingRate: 3, startingThermal: 46, shapeShare: 41);

            // Marshguard Guard
            _builder.Create(RecipeType.MarshguardGuard, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("rc_guard")
                .Level(27)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("rc_vine", 1)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 56, coolingRate: 2, startingThermal: 53, shapeShare: 57);

            // Marshguard Visor
            _builder.Create(RecipeType.MarshguardVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("rc_visor")
                .Level(26)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2)
                .Component("rc_vine", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 53, coolingRate: 4, startingThermal: 54, shapeShare: 57);

            // Marshguard Charm
            _builder.Create(RecipeType.MarshguardCharm, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("rc_charm")
                .Level(25)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("rc_vine", 1)
                .ForgeParameters(rangeCenter: 41, rangeWidth: 54, coolingRate: 2, startingThermal: 52, shapeShare: 43);
        }
    }
}
