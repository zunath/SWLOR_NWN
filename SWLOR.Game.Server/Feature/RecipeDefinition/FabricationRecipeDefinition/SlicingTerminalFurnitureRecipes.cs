using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.FabricationRecipeDefinition
{
    public class SlicingTerminalFurnitureRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Add(RecipeType.RustlineDataTerminal, "structure_0431", 5, "elec_ruined", "fiberp_ruined", parameters: new FabricatorParameters(frameSizePercent: 29, shellSizePercent: 23, frameQualityPercent: 16, shellQualityPercent: 21, wearRate: 2));
            Add(RecipeType.CipherfileCabinet, "structure_0432", 15, "elec_flawed", "fiberp_flawed", parameters: new FabricatorParameters(frameSizePercent: 21, shellSizePercent: 26, frameQualityPercent: 19, shellQualityPercent: 30, wearRate: 5));
            Add(RecipeType.ListeningPostMonitor, "structure_0433", 25, "elec_imperfect", "fiberp_imperfect", parameters: new FabricatorParameters(frameSizePercent: 28, shellSizePercent: 26, frameQualityPercent: 15, shellQualityPercent: 22, wearRate: 7));
            Add(RecipeType.GhostChannelConsole, "structure_0434", 35, "elec_high", "fiberp_high", parameters: new FabricatorParameters(frameSizePercent: 23, shellSizePercent: 26, frameQualityPercent: 25, shellQualityPercent: 28, wearRate: 7));
            Add(RecipeType.BlacksiteAnalysisStation, "structure_0435", 45, "elec_perfect", "fiberp_perfect", parameters: new FabricatorParameters(frameSizePercent: 26, shellSizePercent: 20, frameQualityPercent: 20, shellQualityPercent: 23, wearRate: 8));
            return _builder.Build();
        }

        private void Add(RecipeType type, string resref, int level, string electronics, string polymer, RecipeMinigameParameters parameters = null)
        {
            _builder.Create(type, SkillType.Fabrication)
                .RequirementUnlocked()
                .Category(RecipeCategoryType.Electronics)
                .Resref(resref)
                .Level(level)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component(electronics, 4)
                .Component(polymer, 3)
                .MinigameParameters(parameters);
        }
    }
}
