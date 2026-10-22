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
                .ForgeParameters(rangeCenter: 42, rangeWidth: 60, coolingRate: 3, startingThermal: 54, shapeShare: 58);

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
                .ForgeParameters(rangeCenter: 37, rangeWidth: 60, coolingRate: 3, startingThermal: 54, shapeShare: 59);

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
                .ForgeParameters(rangeCenter: 38, rangeWidth: 60, coolingRate: 1, startingThermal: 50, shapeShare: 56);

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
                .ForgeParameters(rangeCenter: 34, rangeWidth: 58, coolingRate: 2, startingThermal: 52, shapeShare: 54);

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
                .ForgeParameters(rangeCenter: 39, rangeWidth: 54, coolingRate: 4, startingThermal: 52, shapeShare: 51);

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
                .ForgeParameters(rangeCenter: 38, rangeWidth: 60, coolingRate: 4, startingThermal: 49, shapeShare: 44);

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
                .ForgeParameters(rangeCenter: 35, rangeWidth: 60, coolingRate: 2, startingThermal: 50, shapeShare: 47);

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
                .ForgeParameters(rangeCenter: 42, rangeWidth: 60, coolingRate: 4, startingThermal: 49, shapeShare: 57);

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
                .ForgeParameters(rangeCenter: 38, rangeWidth: 57, coolingRate: 4, startingThermal: 48, shapeShare: 57);

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
                .ForgeParameters(rangeCenter: 42, rangeWidth: 56, coolingRate: 3, startingThermal: 50, shapeShare: 45);
        }
    }
}
