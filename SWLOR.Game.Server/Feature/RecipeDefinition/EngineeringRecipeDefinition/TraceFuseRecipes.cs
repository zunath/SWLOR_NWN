using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class TraceFuseRecipes : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Add(RecipeType.CopperTraceFuse, "trace_fuse_1", 5, "elec_ruined", "fiberp_ruined", parameters: new CircuitParameters(overloadLine: 3, loadBoostPercent: 10, assemblySharePercent: 52));
            Add(RecipeType.BraidedTraceFuse, "trace_fuse_2", 15, "elec_flawed", "fiberp_flawed", parameters: new CircuitParameters(overloadLine: 4, loadBoostPercent: 10, assemblySharePercent: 48));
            Add(RecipeType.PhaseTraceFuse, "trace_fuse_3", 25, "elec_imperfect", "fiberp_imperfect", parameters: new CircuitParameters(overloadLine: 7, loadBoostPercent: 9, assemblySharePercent: 51));
            Add(RecipeType.CryoTraceFuse, "trace_fuse_4", 35, "elec_high", "fiberp_high", parameters: new CircuitParameters(overloadLine: 7, loadBoostPercent: 11, assemblySharePercent: 47));
            Add(RecipeType.NullTraceFuse, "trace_fuse_5", 45, "elec_perfect", "fiberp_perfect", parameters: new CircuitParameters(overloadLine: 9, loadBoostPercent: 10, assemblySharePercent: 52))
                .EffectDescription(new[]
                {
                    "In the slicing window:",
                    "Select this fuse with < or >.",
                    "Click Activate Tool.",
                    "+1 trace on your first move.",
                    "Consumed on that move.",
                    "Works on security tiers 1-5."
                });
            return _builder.Build();
        }

        private RecipeBuilder Add(RecipeType type, string resref, int level, string electronics, string polymer, RecipeMinigameParameters parameters = null)
        {
            return _builder.Create(type, SkillType.Engineering)
                .RequirementUnlocked()
                .Category(RecipeCategoryType.Tool)
                .Resref(resref)
                .Level(level)
                .Component(electronics, 3)
                .Component(polymer, 2)
                .MinigameParameters(parameters);
        }
    }
}
