using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class PrismhideRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Prismhide Harness
            _builder.Create(RecipeType.PrismhideHarness, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("se_harness")
                .Level(20)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 4)
                .Component("fiberp_flawed", 2)
                .Component("se_eye", 1)
                .ForgeParameters(rangeCenter: 38, rangeWidth: 57, coolingRate: 1, startingThermal: 50, shapeShare: 69);

            // Prismhide Wraps
            _builder.Create(RecipeType.PrismhideWraps, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("se_wraps")
                .Level(19)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("se_eye", 1)
                .ForgeParameters(rangeCenter: 38, rangeWidth: 57, coolingRate: 3, startingThermal: 53, shapeShare: 62);

            // Prismhide Treads
            _builder.Create(RecipeType.PrismhideTreads, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("se_treads")
                .Level(18)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("se_eye", 1)
                .ForgeParameters(rangeCenter: 38, rangeWidth: 48, coolingRate: 2, startingThermal: 50, shapeShare: 64);

            // Prismhide Sash
            _builder.Create(RecipeType.PrismhideSash, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("se_sash")
                .Level(19)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("se_eye", 1)
                .ForgeParameters(rangeCenter: 38, rangeWidth: 51, coolingRate: 6, startingThermal: 51, shapeShare: 52);

            // Prismhide Mantle
            _builder.Create(RecipeType.PrismhideMantle, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("se_mantle")
                .Level(22)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("se_eye", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 39, coolingRate: 7, startingThermal: 44, shapeShare: 54);

            // Prismhide Gorget
            _builder.Create(RecipeType.PrismhideGorget, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("se_gorget")
                .Level(20)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("se_eye", 1)
                .ForgeParameters(rangeCenter: 38, rangeWidth: 60, coolingRate: 4, startingThermal: 49, shapeShare: 38);

            // Prismhide Band
            _builder.Create(RecipeType.PrismhideBand, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("se_band")
                .Level(21)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("se_eye", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 42, coolingRate: 8, startingThermal: 47, shapeShare: 38);

            // Prismhide Guard
            _builder.Create(RecipeType.PrismhideGuard, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("se_guard")
                .Level(23)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("se_eye", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 36, coolingRate: 1, startingThermal: 42, shapeShare: 62);

            // Prismhide Visor
            _builder.Create(RecipeType.PrismhideVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("se_visor")
                .Level(22)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 3)
                .Component("fiberp_flawed", 2)
                .Component("se_eye", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 36, coolingRate: 4, startingThermal: 54, shapeShare: 62);

            // Prismhide Charm
            _builder.Create(RecipeType.PrismhideCharm, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("se_charm")
                .Level(21)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_flawed", 2)
                .Component("fiberp_flawed", 1)
                .Component("se_eye", 1)
                .ForgeParameters(rangeCenter: 39, rangeWidth: 42, coolingRate: 6, startingThermal: 49, shapeShare: 38);
        }
    }
}
