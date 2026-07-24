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
            Add(RecipeType.RustlineDataTerminal, "structure_0431", 5, "elec_ruined", "fiberp_ruined");
            Add(RecipeType.CipherfileCabinet, "structure_0432", 15, "elec_flawed", "fiberp_flawed");
            Add(RecipeType.ListeningPostMonitor, "structure_0433", 25, "elec_imperfect", "fiberp_imperfect");
            Add(RecipeType.GhostChannelConsole, "structure_0434", 35, "elec_high", "fiberp_high");
            Add(RecipeType.BlacksiteAnalysisStation, "structure_0435", 45, "elec_perfect", "fiberp_perfect");
            return _builder.Build();
        }

        private void Add(RecipeType type, string resref, int level, string electronics, string polymer)
        {
            _builder.Create(type, SkillType.Fabrication)
                .RequirementUnlocked()
                .Category(RecipeCategoryType.Electronics)
                .Resref(resref)
                .Level(level)
                .EnhancementSlots(RecipeEnhancementType.Structure, 1)
                .Component(electronics, 4)
                .Component(polymer, 3);
        }
    }
}
