using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class ShatterhideRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Shatterhide Harness
            _builder.Create(RecipeType.ShatterhideHarness, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("sh_harness")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 4)
                .Component("fiberp_good", 2)
                .Component("sh_chitin", 1)
                .ForgeParameters(rangeCenter: 42, rangeWidth: 53, coolingRate: 2, startingThermal: 49, shapeShare: 57);

            // Shatterhide Wraps
            _builder.Create(RecipeType.ShatterhideWraps, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("sh_wraps")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_good", 1)
                .Component("sh_chitin", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 58, coolingRate: 2, startingThermal: 49, shapeShare: 53);

            // Shatterhide Treads
            _builder.Create(RecipeType.ShatterhideTreads, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("sh_treads")
                .Level(28)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_good", 1)
                .Component("sh_chitin", 1)
                .ForgeParameters(rangeCenter: 38, rangeWidth: 52, coolingRate: 4, startingThermal: 49, shapeShare: 57);

            // Shatterhide Sash
            _builder.Create(RecipeType.ShatterhideSash, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("sh_sash")
                .Level(29)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_good", 1)
                .Component("sh_chitin", 1)
                .ForgeParameters(rangeCenter: 43, rangeWidth: 50, coolingRate: 2, startingThermal: 52, shapeShare: 52);

            // Shatterhide Mantle
            _builder.Create(RecipeType.ShatterhideMantle, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("sh_mantle")
                .Level(32)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_good", 1)
                .Component("sh_chitin", 1)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 52, coolingRate: 5, startingThermal: 44, shapeShare: 49);

            // Shatterhide Gorget
            _builder.Create(RecipeType.ShatterhideGorget, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("sh_gorget")
                .Level(30)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_good", 1)
                .Component("sh_chitin", 1)
                .ForgeParameters(rangeCenter: 41, rangeWidth: 49, coolingRate: 3, startingThermal: 46, shapeShare: 45);

            // Shatterhide Band
            _builder.Create(RecipeType.ShatterhideBand, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("sh_band")
                .Level(31)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_good", 1)
                .Component("sh_chitin", 1)
                .ForgeParameters(rangeCenter: 44, rangeWidth: 49, coolingRate: 4, startingThermal: 44, shapeShare: 42);

            // Shatterhide Guard
            _builder.Create(RecipeType.ShatterhideGuard, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("sh_guard")
                .Level(33)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_good", 1)
                .Component("sh_chitin", 1)
                .ForgeParameters(rangeCenter: 43, rangeWidth: 54, coolingRate: 5, startingThermal: 48, shapeShare: 58);

            // Shatterhide Visor
            _builder.Create(RecipeType.ShatterhideVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("sh_visor")
                .Level(32)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 3)
                .Component("fiberp_good", 2)
                .Component("sh_chitin", 1)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 54, coolingRate: 3, startingThermal: 49, shapeShare: 56);

            // Shatterhide Charm
            _builder.Create(RecipeType.ShatterhideCharm, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("sh_charm")
                .Level(31)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_high", 2)
                .Component("fiberp_good", 1)
                .Component("sh_chitin", 1)
                .ForgeParameters(rangeCenter: 38, rangeWidth: 48, coolingRate: 4, startingThermal: 48, shapeShare: 42);
        }
    }
}
