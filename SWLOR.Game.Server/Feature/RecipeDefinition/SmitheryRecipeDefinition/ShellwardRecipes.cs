using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class ShellwardRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Shellward Harness
            _builder.Create(RecipeType.ShellwardHarness, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("mb_harness")
                .Level(23)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 4)
                .Component("fiberp_flawed", 2)
                .Component("mb_shell", 1)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 55, coolingRate: 3, startingThermal: 49, shapeShare: 61);

            // Shellward Wraps
            _builder.Create(RecipeType.ShellwardWraps, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("mb_wraps")
                .Level(22)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mb_shell", 1)
                .ForgeParameters(rangeCenter: 40, rangeWidth: 55, coolingRate: 4, startingThermal: 53, shapeShare: 54);

            // Shellward Treads
            _builder.Create(RecipeType.ShellwardTreads, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("mb_treads")
                .Level(21)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mb_shell", 1)
                .ForgeParameters(rangeCenter: 42, rangeWidth: 56, coolingRate: 3, startingThermal: 51, shapeShare: 59);

            // Shellward Sash
            _builder.Create(RecipeType.ShellwardSash, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("mb_sash")
                .Level(22)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mb_shell", 1)
                .ForgeParameters(rangeCenter: 36, rangeWidth: 60, coolingRate: 2, startingThermal: 52, shapeShare: 51);

            // Shellward Mantle
            _builder.Create(RecipeType.ShellwardMantle, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("mb_mantle")
                .Level(25)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mb_shell", 1)
                .ForgeParameters(rangeCenter: 35, rangeWidth: 57, coolingRate: 2, startingThermal: 48, shapeShare: 51);

            // Shellward Gorget
            _builder.Create(RecipeType.ShellwardGorget, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("mb_gorget")
                .Level(23)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mb_shell", 1)
                .ForgeParameters(rangeCenter: 41, rangeWidth: 59, coolingRate: 4, startingThermal: 48, shapeShare: 44);

            // Shellward Band
            _builder.Create(RecipeType.ShellwardBand, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("mb_band")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mb_shell", 1)
                .ForgeParameters(rangeCenter: 40, rangeWidth: 58, coolingRate: 2, startingThermal: 54, shapeShare: 43);

            // Shellward Guard
            _builder.Create(RecipeType.ShellwardGuard, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("mb_guard")
                .Level(26)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mb_shell", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 56, coolingRate: 3, startingThermal: 53, shapeShare: 53);

            // Shellward Visor
            _builder.Create(RecipeType.ShellwardVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("mb_visor")
                .Level(25)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2)
                .Component("mb_shell", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 58, coolingRate: 4, startingThermal: 49, shapeShare: 54);

            // Shellward Charm
            _builder.Create(RecipeType.ShellwardCharm, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("mb_charm")
                .Level(24)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("mb_shell", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 60, coolingRate: 2, startingThermal: 47, shapeShare: 47);
        }
    }
}
