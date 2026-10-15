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
            Add(RecipeType.CopperTraceFuse, "trace_fuse_1", 5, "elec_ruined", "fiberp_ruined");
            Add(RecipeType.BraidedTraceFuse, "trace_fuse_2", 15, "elec_flawed", "fiberp_flawed");
            Add(RecipeType.PhaseTraceFuse, "trace_fuse_3", 25, "elec_imperfect", "fiberp_imperfect");
            Add(RecipeType.CryoTraceFuse, "trace_fuse_4", 35, "elec_high", "fiberp_high");
            Add(RecipeType.NullTraceFuse, "trace_fuse_5", 45, "elec_perfect", "fiberp_perfect")
                .EffectDescription(new[]
                {
                    "Prime in the slicing window.",
                    "+1 trace on your first move.",
                    "Consumed on that move.",
                    "Works on security tiers 1-5."
                });
            return _builder.Build();
        }

        private RecipeBuilder Add(RecipeType type, string resref, int level, string electronics, string polymer)
        {
            return _builder.Create(type, SkillType.Engineering)
                .RequirementUnlocked()
                .Category(RecipeCategoryType.Tool)
                .Resref(resref)
                .Level(level)
                .Component(electronics, 3)
                .Component(polymer, 2);
        }
    }
}
