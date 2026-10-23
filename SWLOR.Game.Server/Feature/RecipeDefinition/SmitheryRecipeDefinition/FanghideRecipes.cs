using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class FanghideRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Recipes();

            return _builder.Build();
        }

        private void Recipes()
        {
            // Fanghide Vest
            _builder.Create(RecipeType.FanghideVest, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Resref("os_hidevest")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 4)
                .Component("fiberp_ruined", 2)
                .Component("oldscar_troph", 1)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 56, coolingRate: 1, startingThermal: 48, shapeShare: 70);

            // Fanghide Wraps
            _builder.Create(RecipeType.FanghideWraps, SkillType.Smithery)
                .Category(RecipeCategoryType.Glove)
                .Resref("os_scarwraps")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("oldscar_troph", 1)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 54, coolingRate: 1, startingThermal: 47, shapeShare: 62);

            // Fanghide Treads
            _builder.Create(RecipeType.FanghideTreads, SkillType.Smithery)
                .Category(RecipeCategoryType.Boots)
                .Resref("os_treads")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("oldscar_troph", 1)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 49, coolingRate: 1, startingThermal: 46, shapeShare: 62);

            // Fanghide Sash
            _builder.Create(RecipeType.FanghideSash, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("os_sash")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("oldscar_troph", 1)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 51, coolingRate: 1, startingThermal: 47, shapeShare: 54);

            // Fanghide Mantle
            _builder.Create(RecipeType.FanghideMantle, SkillType.Smithery)
                .Category(RecipeCategoryType.Cloak)
                .Resref("os_mantle")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("oldscar_troph", 1)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 55, coolingRate: 1, startingThermal: 48, shapeShare: 54);

            // Fanghide Collar
            _builder.Create(RecipeType.FanghideCollar, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("os_collar")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("oldscar_troph", 1)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 52, coolingRate: 2, startingThermal: 46, shapeShare: 38);

            // Fanghide Band
            _builder.Create(RecipeType.FanghideBand, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("os_band")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("oldscar_troph", 1)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 49, coolingRate: 2, startingThermal: 46, shapeShare: 38);

            // Fanghide Guard
            _builder.Create(RecipeType.FanghideGuard, SkillType.Smithery)
                .Category(RecipeCategoryType.Bracer)
                .Resref("os_guard")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("oldscar_troph", 1)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 53, coolingRate: 1, startingThermal: 47, shapeShare: 62);

            // Fanghide Visor
            _builder.Create(RecipeType.FanghideVisor, SkillType.Smithery)
                .Category(RecipeCategoryType.Cap)
                .Resref("os_visor")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 3)
                .Component("fiberp_ruined", 2)
                .Component("oldscar_troph", 1)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 56, coolingRate: 1, startingThermal: 48, shapeShare: 62);

            // Fanghide Charm
            _builder.Create(RecipeType.FanghideCharm, SkillType.Smithery)
                .Category(RecipeCategoryType.Necklace)
                .Resref("os_charm")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("oldscar_troph", 1)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 55, coolingRate: 2, startingThermal: 48, shapeShare: 38);

            // Fangmarked Band
            _builder.Create(RecipeType.FangmarkedBand, SkillType.Smithery)
                .Category(RecipeCategoryType.Ring)
                .Resref("os_trophy")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("oldscar_troph", 1)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 53, coolingRate: 2, startingThermal: 47, shapeShare: 38);

            // Fanghide Binding
            _builder.Create(RecipeType.FanghideBinding, SkillType.Smithery)
                .Category(RecipeCategoryType.Belt)
                .Resref("os_hideband")
                .Level(8)
                .Quantity(1)
                .RequirementUnlocked()
                .EnhancementSlots(RecipeEnhancementType.Armor, 1)
                .Component("lth_ruined", 2)
                .Component("fiberp_ruined", 1)
                .Component("oldscar_troph", 1)
                .ForgeParameters(rangeCenter: 37, rangeWidth: 50, coolingRate: 1, startingThermal: 46, shapeShare: 54);
        }
    }
}
