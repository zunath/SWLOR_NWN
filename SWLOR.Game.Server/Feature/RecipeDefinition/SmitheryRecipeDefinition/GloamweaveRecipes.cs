using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class GloamweaveRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Gloamweave Harness
            _builder.Create(RecipeType.GloamweaveHarness, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("ss_harness")
                .Level(14)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 4)
                .Component("fiberp_ruined", 2)
                .Component("ss_silk", 1)
                .ForgeParameters(rangeCenter: 40, rangeWidth: 60, coolingRate: 1, startingThermal: 55, shapeShare: 62);

            // Gloamweave Wraps
            _builder.Create(RecipeType.GloamweaveWraps, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("ss_wraps")
                .Level(13)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("ss_silk", 1)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 57, coolingRate: 1, startingThermal: 54, shapeShare: 56);

            // Gloamweave Treads
            _builder.Create(RecipeType.GloamweaveTreads, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("ss_treads")
                .Level(12)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("ss_silk", 1)
                .ForgeParameters(rangeCenter: 41, rangeWidth: 60, coolingRate: 1, startingThermal: 51, shapeShare: 58);

            // Gloamweave Sash
            _builder.Create(RecipeType.GloamweaveSash, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("ss_sash")
                .Level(13)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("ss_silk", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 56, coolingRate: 1, startingThermal: 58, shapeShare: 54);

            // Gloamweave Mantle
            _builder.Create(RecipeType.GloamweaveMantle, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("ss_mantle")
                .Level(16)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("ss_silk", 1)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 60, coolingRate: 1, startingThermal: 56, shapeShare: 55);

            // Gloamweave Gorget
            _builder.Create(RecipeType.GloamweaveGorget, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("ss_gorget")
                .Level(14)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("ss_silk", 1)
                .ForgeParameters(rangeCenter: 35, rangeWidth: 60, coolingRate: 1, startingThermal: 54, shapeShare: 44);

            // Gloamweave Band
            _builder.Create(RecipeType.GloamweaveBand, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("ss_band")
                .Level(15)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("ss_silk", 1)
                .ForgeParameters(rangeCenter: 34, rangeWidth: 60, coolingRate: 2, startingThermal: 59, shapeShare: 44);

            // Gloamweave Guard
            _builder.Create(RecipeType.GloamweaveGuard, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("ss_guard")
                .Level(17)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("ss_silk", 1)
                .ForgeParameters(rangeCenter: 35, rangeWidth: 58, coolingRate: 1, startingThermal: 56, shapeShare: 58);

            // Gloamweave Visor
            _builder.Create(RecipeType.GloamweaveVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("ss_visor")
                .Level(16)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 3)
                .Component("fiberp_ruined", 2)
                .Component("ss_silk", 1)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 60, coolingRate: 1, startingThermal: 53, shapeShare: 59);

            // Gloamweave Charm
            _builder.Create(RecipeType.GloamweaveCharm, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("ss_charm")
                .Level(15)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("ss_silk", 1)
                .ForgeParameters(rangeCenter: 35, rangeWidth: 60, coolingRate: 1, startingThermal: 53, shapeShare: 45);
        }
    }
}
