using System.Collections.Concurrent;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Legacy.Crafting
{
    public sealed class LegacyEnhancementOptions
    {
        public static readonly IReadOnlyList<SkillType> DefaultSkills = new[]
        {
            SkillType.Smithery, SkillType.Engineering, SkillType.Fabrication, SkillType.Agriculture
        };

        public int Runs { get; init; } = 200;
        public int Seed { get; init; } = 1;

        /// <summary>Enhanceable recipes played per skill and band: an even sample across the band, repeatable.</summary>
        public int RecipesPerBand { get; init; } = 6;

        public IReadOnlyList<SkillType> Skills { get; init; } = DefaultSkills;

        /// <summary>The numbers of filled enhancement slots to play.</summary>
        public IReadOnlyList<int> SlotCounts { get; init; } = new[] { 1, 2, 3 };

        /// <summary>The bands of the sweep, played at rank = recipe level (at most 50) with each gear level and each enhancement level the recipe accepts.</summary>
        public IReadOnlyList<RecipeBand> SweepBands { get; init; } = RecipeBand.Default;

        /// <summary>The bands of the gate cohort, played at <see cref="GateRank"/> with the best gear and the best enhancement the recipe accepts.</summary>
        public IReadOnlyList<RecipeBand> GateBands { get; init; } = new[]
        {
            new RecipeBand("41-45", 41, 45),
            new RecipeBand("46-50", 46, 50),
            new RecipeBand("51-53", 51, int.MaxValue)
        };

        public int GateRank { get; init; } = ScenarioCohort.MaximumRank;

        /// <summary>When false the sweep plays the best gear only; the gate cohort always does.</summary>
        public bool SweepEveryGearLevel { get; init; } = true;
    }

    /// <summary>One recipe at one enhancement and slot count, played by one policy at one gear level.</summary>
    /// <param name="IsGateCohort">True for the rank-50, best-gear, at-level cohort of gate 7; false for the sweep over bands and tiers.</param>
    public sealed record LegacyEnhancementSample(
        SkillType Skill, string Band, bool IsGateCohort, RecipeEntry Recipe, EnhancementItem Item, int Slots, int Rank, string Gear, LegacyEnhancementCell Cell)
    {
        public string Policy => Cell.Policy;
    }

    public sealed class LegacyEnhancementResults
    {
        public LegacyEnhancementResults(
            LegacyEnhancementOptions options, IReadOnlyList<LegacyEnhancementSample> samples, IReadOnlyList<GearLevel> gearLevels)
        {
            Options = options;
            Samples = samples;
            GearLevels = gearLevels;
        }

        public LegacyEnhancementOptions Options { get; }
        public IReadOnlyList<LegacyEnhancementSample> Samples { get; }
        public IReadOnlyList<GearLevel> GearLevels { get; }
    }

    /// <summary>
    /// Plays the old minigame with 1, 2 and 3 enhancements slotted, at the representative real enhancement item of each recipe, for the
    /// gate-7 cohort (rank 50, best gear, at level) and across recipe bands, enhancement tiers and gear levels.
    /// </summary>
    public static class LegacyEnhancementSweep
    {
        public const long MaximumCrafts = 20_000_000;

        public static LegacyEnhancementResults Run(
            RecipeCatalog recipes, CraftingGearCatalog gear, EnhancementCatalog enhancements, LegacyEnhancementOptions options)
        {
            var requests = new List<Request>();
            foreach (var skill in options.Skills)
            {
                var gearLevels = gear.Levels(skill);
                var enhanceable = RecipeCatalog.From(recipes.ForSkill(skill).Where(r => r.Level >= 1 && r.EnhancementSlots > 0 && r.EnhancementType != RecipeEnhancementType.None));

                // The gate cohort: the best enhancement a recipe accepts, rank 50, the best gear, both policies.
                foreach (var band in Bands(enhanceable, skill, options.GateBands, options.RecipesPerBand))
                {
                    foreach (var recipe in band.Recipes)
                    {
                        var item = enhancements.ForRecipe(recipe.EnhancementType, recipe.Level);
                        if (item == null)
                            continue;

                        for (var policy = 0; policy < LegacyEnhancementBaseline.Policies.Count; policy++)
                        {
                            foreach (var slots in options.SlotCounts)
                                requests.Add(new Request(skill, band.Band.Name, true, recipe, item, slots, options.GateRank, gearLevels.Count - 1, gearLevels[^1], policy));
                        }
                    }
                }

                // The sweep: every enhancement level a recipe accepts, at the recipe's own rank, with the lookahead player.
                foreach (var band in Bands(enhanceable, skill, options.SweepBands, options.RecipesPerBand))
                {
                    foreach (var recipe in band.Recipes)
                    {
                        foreach (var level in enhancements.Levels(recipe.EnhancementType).Where(l => LegacyEnhancementRules.IsAllowedFor(l, recipe.Level)))
                        {
                            var item = enhancements.Representative(recipe.EnhancementType, level);
                            for (var gearIndex = options.SweepEveryGearLevel ? 0 : gearLevels.Count - 1; gearIndex < gearLevels.Count; gearIndex++)
                            {
                                foreach (var slots in options.SlotCounts)
                                {
                                    requests.Add(new Request(
                                        skill, band.Band.Name, false, recipe, item, slots, Math.Min(recipe.Level, ScenarioCohort.MaximumRank), gearIndex,
                                        gearLevels[gearIndex], LegacyEnhancementBaseline.LookaheadIndex));
                                }
                            }
                        }
                    }
                }
            }

            var distinct = requests.GroupBy(r => r.Key).ToDictionary(g => g.Key, g => g.First());
            if ((long)distinct.Count * options.Runs > MaximumCrafts)
            {
                throw new ArgumentException(
                    $"{options.Runs} runs of {distinct.Count} cells is more than {MaximumCrafts} crafts; use fewer runs, recipes or --skills.");
            }

            var cells = new ConcurrentDictionary<CellKey, LegacyEnhancementCell>();
            Parallel.ForEach(distinct, entry =>
            {
                var (key, request) = (entry.Key, entry.Value);
                cells[key] = LegacyEnhancementBaseline.PlayCell(
                    key.Level, key.Rank, request.Gear, key.GearIndex, key.Policy, new CraftEnhancementConfiguration(key.Count, key.Penalty),
                    options.Runs, options.Seed);
            });

            var samples = requests
                .Select(r => new LegacyEnhancementSample(r.Skill, r.Band, r.IsGateCohort, r.Recipe, r.Item, r.Slots, r.Rank, r.Gear.Name, cells[r.Key]))
                .ToList();
            var gearLevelsUsed = requests.Select(r => r.Gear).GroupBy(g => (g.Skill, g.Name)).Select(g => g.First()).OrderBy(g => (int)g.Skill).ThenBy(g => g.Tier).ToList();
            return new LegacyEnhancementResults(options, samples, gearLevelsUsed);
        }

        private static IReadOnlyList<CohortBand> Bands(
            RecipeCatalog enhanceable, SkillType skill, IReadOnlyList<RecipeBand> bands, int recipesPerBand) =>
            ScenarioCohort.ForSkill(skill).Select(enhanceable, recipesPerBand, bands);

        private readonly record struct CellKey(SkillType Skill, int Level, int Rank, int GearIndex, int Policy, int Count, int Penalty);

        private sealed record Request(
            SkillType Skill, string Band, bool IsGateCohort, RecipeEntry Recipe, EnhancementItem Item, int Slots, int Rank, int GearIndex, GearLevel Gear, int Policy)
        {
            public CellKey Key => new(Skill, Recipe.Level, Rank, GearIndex, Policy, Slots, Item.ProgressPenalty);
        }
    }
}
