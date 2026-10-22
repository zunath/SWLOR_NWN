using System.Collections.Concurrent;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Legacy.Crafting;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Rng;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Gates
{
    /// <summary>The results of many attempts at one recipe by one adaptive player: one rank, gear level and style.</summary>
    public sealed class CohortCell
    {
        public RecipeEntry Recipe { get; init; }
        public int Rank { get; init; }
        public string Gear { get; init; }
        public CraftStyle Style { get; init; }
        public int Runs { get; init; }

        /// <summary>Attempts that made the item.</summary>
        public int Successes { get; set; }

        /// <summary>Attempts that neither made the item nor broke the piece within the action limit; they count as failures.</summary>
        public int Stalled { get; set; }

        /// <summary>Attempts that made the item with the quality meter full.</summary>
        public int FullQuality { get; set; }

        /// <summary>The summed quality fraction of the attempts that made the item.</summary>
        public double QualitySum { get; set; }

        /// <summary>For each whole quality percentage (0-100, rounded down as the outcome rules do), the attempts that made the item at that quality.</summary>
        public int[] QualityPercentCounts { get; } = new int[101];

        public long Actions { get; set; }
        public long CpSpent { get; set; }

        /// <summary>The summed XP of every attempt: a success's XP, or a failure's 15%.</summary>
        public double XpSum { get; set; }

        public double SuccessRate => Runs == 0 ? 0 : Successes / (double)Runs;

        /// <summary>The mean quality fraction of the attempts, a failure counting 0: the number the gates compare.</summary>
        public double MeanScore => Runs == 0 ? 0 : QualitySum / Runs;

        public double MeanQualityOfSuccesses => Successes == 0 ? 0 : QualitySum / Successes;

        public double FullQualityShare => Runs == 0 ? 0 : FullQuality / (double)Runs;

        public double ActionsPerAttempt => Runs == 0 ? 0 : Actions / (double)Runs;

        public double XpPerAttempt => Runs == 0 ? 0 : XpSum / Runs;
    }

    /// <summary>The results of many attempts of the old minigame's lookahead player at one recipe level, rank and gear level.</summary>
    public sealed class LegacyCell
    {
        public int Level { get; init; }
        public int Rank { get; init; }
        public string Gear { get; init; }
        public int Runs { get; init; }
        public int Successes { get; init; }
        public long Actions { get; init; }

        /// <summary>The quality fraction of each run (0 for a failure), in run order.</summary>
        public IReadOnlyList<double> Qualities { get; init; }

        public IReadOnlyList<bool> Succeeded { get; init; }

        public double SuccessRate => Runs == 0 ? 0 : Successes / (double)Runs;
        public double ActionsPerAttempt => Runs == 0 ? 0 : Actions / (double)Runs;
        public double MeanScore => Runs == 0 ? 0 : Qualities.Sum() / Runs;

        /// <summary>The mean XP of one attempt for a recipe whose base XP is <paramref name="baseXp"/> (no blueprint, not a first craft).</summary>
        public double XpPerAttempt(int baseXp)
        {
            var total = 0.0;
            for (var run = 0; run < Runs; run++)
            {
                total += Succeeded[run]
                    ? CraftOutcomeRules.CalculateXp(baseXp, 0, false, (float)Qualities[run])
                    : CraftOutcomeRules.CalculateFailureXp(baseXp, 0);
            }

            return total / Runs;
        }
    }

    /// <summary>A recipe that can hold an enhancement, with the representative real enhancement item it is played with.</summary>
    public sealed record EnhancedRecipe(RecipeEntry Recipe, EnhancementItem Item)
    {
        public CraftEnhancementConfiguration Configuration(int count) => new(count, Item.ProgressPenalty);
    }

    /// <summary>One band of a cohort restricted to recipes that can be enhanced, and the sampled recipes that were left out for lack of an enhancement to use.</summary>
    public sealed record EnhancedBand(CohortBand Band, IReadOnlyList<EnhancedRecipe> Recipes, IReadOnlyList<string> Dropped);

    /// <summary>
    /// The plays one evaluation of a scenario shares between its gates: the cohort, the gear levels, and a memo of every adaptive cell
    /// and legacy cell so no gate plays what another already has. Everything is repeatable from the context's seed; cells are played in
    /// parallel and filed by key, so the machine's thread count cannot change a result.
    /// </summary>
    internal sealed class ScenarioRun
    {
        private readonly ConcurrentDictionary<(int Recipe, int Rank, int Gear, string Style), Lazy<CohortCell>> _adaptive = new();
        private readonly ConcurrentDictionary<(int Level, int Rank, int Gear), Lazy<LegacyCell>> _legacy = new();
        private readonly ConcurrentDictionary<(int Recipe, int Rank, int Gear, int Count, int Penalty), Lazy<CohortCell>> _adaptiveEnhanced = new();
        private readonly ConcurrentDictionary<(int Level, int Rank, int Gear, int Count, int Penalty), Lazy<LegacyEnhancementCell>> _legacyEnhanced = new();

        public ScenarioRun(ICraftScenario scenario, GateThresholds thresholds, GateContext context)
        {
            Scenario = scenario;
            Thresholds = thresholds;
            Context = context;
            Gear = context.Gear.Levels(scenario.Skill);
            Cohort = scenario.Cohort;
            Bands = Cohort.Select(context.Recipes, thresholds.RecipesPerBand);

            // The XP chart loads once, before the parallel plays read it.
            ServerXp.ForDelta(0);
        }

        public ICraftScenario Scenario { get; }
        public GateThresholds Thresholds { get; }
        public GateContext Context { get; }
        public IReadOnlyList<GearLevel> Gear { get; }

        /// <summary>The scenario's cohort, read once.</summary>
        public ScenarioCohort Cohort { get; }

        /// <summary>The cohort's bands with the recipes played.</summary>
        public IReadOnlyList<CohortBand> Bands { get; }

        public int RankFor(RecipeEntry recipe) => Cohort.RankFor(recipe);

        /// <summary>True when any recipe of the scenario's cohort has an enhancement slot and a kind of enhancement to put in it.</summary>
        public bool HasEnhanceableRecipes => Cohort.Recipes(Context.Recipes).Any(IsEnhanceable);

        private static bool IsEnhanceable(RecipeEntry recipe) => recipe.EnhancementSlots > 0 && recipe.EnhancementType != RecipeEnhancementType.None;

        /// <summary>
        /// The cohort's recipes that can hold an enhancement, an even sample of each band (the cohort's own bands unless
        /// <paramref name="bands"/> are given), each with the representative enhancement of its kind at the best level it accepts. Empty when the
        /// context has no enhancement items.
        /// </summary>
        public IReadOnlyList<EnhancedBand> EnhancedBands(IReadOnlyList<RecipeBand> bands = null)
        {
            var catalog = Context.Enhancements;
            if (catalog == null)
                return Array.Empty<EnhancedBand>();

            var enhanceable = RecipeCatalog.From(Context.Recipes.Recipes.Where(IsEnhanceable));
            var result = new List<EnhancedBand>();
            foreach (var band in Cohort.Select(enhanceable, Thresholds.RecipesPerBand, bands))
            {
                var recipes = new List<EnhancedRecipe>();
                var dropped = new List<string>();
                foreach (var recipe in band.Recipes)
                {
                    var item = catalog.ForRecipe(recipe.EnhancementType, recipe.Level);
                    if (item == null)
                        dropped.Add($"{recipe.Resref} (level {recipe.Level}, {recipe.EnhancementType})");
                    else
                        recipes.Add(new EnhancedRecipe(recipe, item));
                }

                if (recipes.Count > 0 || dropped.Count > 0)
                    result.Add(new EnhancedBand(band, recipes, dropped));
            }

            return result;
        }

        /// <summary>
        /// The recipes whose engine does not get harder with enhancements, though the scenario says it models them: the progress maximum of an
        /// engine built with the largest enhancement count is compared with a plain one. An unmodeled engine would make enhanced crafts as easy
        /// as plain ones and let the gates pass vacuously.
        /// </summary>
        public IReadOnlyList<string> UnmodeledRecipes(IEnumerable<EnhancedRecipe> recipes, Func<RecipeEntry, int> rankFor, int gearIndex, int largestCount)
        {
            var problems = new List<string>();
            var gear = Gear[gearIndex];
            foreach (var entry in recipes)
            {
                var rank = rankFor(entry.Recipe);
                var plain = CreateEngine(entry.Recipe, rank, gear, null, new SimMinigameRandom(new SimRandom(1)));
                var enhanced = CreateEngine(entry.Recipe, rank, gear, null, new SimMinigameRandom(new SimRandom(1)), entry.Configuration(largestCount));
                if (enhanced.Meters.Progress.Max <= plain.Meters.Progress.Max)
                {
                    problems.Add($"{entry.Recipe.Resref} (level {entry.Recipe.Level}): progress maximum {plain.Meters.Progress.Max} without enhancements " +
                                 $"and {enhanced.Meters.Progress.Max} with {largestCount} x {entry.Item.ProgressPenalty}");
                }
            }

            return problems;
        }

        /// <summary>The solver's effort: the thresholds' override, or the scenario's own.</summary>
        public FixedRotationSolverOptions SolverOptions => Thresholds.SolverOptions ?? Scenario.SolverOptions;

        public static readonly ParallelOptions Parallelism = new() { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount) };

        public CohortCell Adaptive(RecipeEntry recipe, int rank, int gearIndex, CraftStyle style = null) =>
            _adaptive.GetOrAdd((StableId(recipe), rank, gearIndex, style?.Name ?? string.Empty), _ => new Lazy<CohortCell>(() => PlayAdaptive(recipe, rank, gearIndex, style))).Value;

        public LegacyCell Legacy(int level, int rank, int gearIndex) =>
            _legacy.GetOrAdd((level, rank, gearIndex), _ => new Lazy<LegacyCell>(() => PlayLegacy(level, rank, gearIndex))).Value;

        /// <summary>The adaptive player's attempts at a recipe with enhancements slotted: the scenario applies their progress penalty to its engine.</summary>
        public CohortCell AdaptiveEnhanced(RecipeEntry recipe, int rank, int gearIndex, CraftEnhancementConfiguration enhancements) =>
            _adaptiveEnhanced.GetOrAdd(
                (StableId(recipe), rank, gearIndex, enhancements.Count, enhancements.ProgressPenaltyEach),
                _ => new Lazy<CohortCell>(() => PlayAdaptive(recipe, rank, gearIndex, null, enhancements))).Value;

        /// <summary>The old minigame's lookahead player at a recipe level with enhancements slotted.</summary>
        public LegacyEnhancementCell LegacyEnhanced(int level, int rank, int gearIndex, CraftEnhancementConfiguration enhancements) =>
            _legacyEnhanced.GetOrAdd(
                (level, rank, gearIndex, enhancements.Count, enhancements.ProgressPenaltyEach),
                _ => new Lazy<LegacyEnhancementCell>(() => LegacyEnhancementBaseline.PlayCell(
                    level, rank, Gear[gearIndex], gearIndex, LegacyEnhancementBaseline.LookaheadIndex, enhancements, Context.Runs, Context.Seed))).Value;

        /// <summary>Plays the enhanced adaptive cells and their legacy cells in parallel so the gate that reads them finds them done.</summary>
        public void PrepareEnhanced(IEnumerable<(RecipeEntry Recipe, int Rank, int GearIndex, CraftEnhancementConfiguration Enhancements)> cells)
        {
            var list = cells.ToList();
            Parallel.ForEach(list, Parallelism, cell =>
            {
                AdaptiveEnhanced(cell.Recipe, cell.Rank, cell.GearIndex, cell.Enhancements);
                LegacyEnhanced(cell.Recipe.Level, cell.Rank, cell.GearIndex, cell.Enhancements);
            });
        }

        /// <summary>Plays the cells in parallel so the gates that read them find them done.</summary>
        public void PrepareAdaptive(IEnumerable<(RecipeEntry Recipe, int Rank, int GearIndex, CraftStyle Style)> cells) =>
            Parallel.ForEach(cells.ToList(), Parallelism, cell => Adaptive(cell.Recipe, cell.Rank, cell.GearIndex, cell.Style));

        public void PrepareLegacy(IEnumerable<(int Level, int Rank, int GearIndex)> cells) =>
            Parallel.ForEach(cells.Distinct().ToList(), Parallelism, cell => Legacy(cell.Level, cell.Rank, cell.GearIndex));

        /// <summary>Every cohort recipe at its own rank and every gear level: the cells gates 1, 4, 5 and 6 read.</summary>
        public IEnumerable<(RecipeEntry Recipe, int Rank, int GearIndex, CraftStyle Style)> CohortCells() =>
            Bands.SelectMany(band => band.Recipes).SelectMany(recipe =>
                Enumerable.Range(0, Gear.Count).Select(gear => (recipe, RankFor(recipe), gear, (CraftStyle)null)));

        public ICraftMinigameEngine CreateEngine(
            RecipeEntry recipe, int rank, GearLevel gear, CraftStyle style, IMinigameRandom random, CraftEnhancementConfiguration enhancements = null) =>
            Scenario.CreateEngine(new CraftEngineRequest(recipe, rank, gear, style, Context.Seed, random, enhancements));

        /// <summary>A number that names a recipe in a random stream: stable across runs, unlike a string hash.</summary>
        public static int StableId(RecipeEntry recipe) => (int)recipe.Type;

        private CohortCell PlayAdaptive(RecipeEntry recipe, int rank, int gearIndex, CraftStyle style, CraftEnhancementConfiguration enhancements = null)
        {
            var gear = Gear[gearIndex];
            var baseXp = CraftOutcomeRules.ScaleBaseXp(ServerXp.ForRecipe(recipe, rank), Scenario.XpMultiplier);
            var cell = new CohortCell { Recipe = recipe, Rank = rank, Gear = gear.Name, Style = style, Runs = Context.Runs };
            var id = StableId(recipe);
            var enhanced = enhancements is { IsNone: false };

            for (var run = 0; run < Context.Runs; run++)
            {

                // The style is left out of the streams on purpose: two styles of one recipe are played on the same dice (common random
                // numbers), so a difference between them is the styles' and not the luck of two independent samples. Enhanced attempts
                // have their own streams, so gate 7 never moves another gate's dice.
                var random = enhanced
                    ? SimMinigameRandom.ForRun(Context.Seed, SimStreams.AdaptiveEnhanced, id, rank, gearIndex, enhancements.Count, enhancements.ProgressPenaltyEach, run)
                    : SimMinigameRandom.ForRun(Context.Seed, SimStreams.Adaptive, id, rank, gearIndex, run);
                var policySeed = unchecked((int)(enhanced
                    ? SimRandom.ForRun(Context.Seed, SimStreams.AdaptiveEnhancedPolicy, id, rank, gearIndex, enhancements.Count, enhancements.ProgressPenaltyEach, run)
                    : SimRandom.ForRun(Context.Seed, SimStreams.AdaptivePolicy, id, rank, gearIndex, run)).NextUInt64());
                var engine = CreateEngine(recipe, rank, gear, style, random, enhanced ? enhancements : null);
                var result = CraftPlayer.Play(engine, new AdaptivePolicy(Scenario.AdaptiveOptions, policySeed), Thresholds.ActionLimit);

                cell.Actions += result.Actions;
                cell.CpSpent += result.CpSpent;
                if (result.Stalled)
                    cell.Stalled++;

                if (result.Succeeded)
                {
                    cell.Successes++;
                    cell.QualitySum += result.QualityFraction;
                    cell.QualityPercentCounts[Math.Clamp(result.QualityPercent, 0, 100)]++;
                    if (result.ReachedMaxQuality)
                        cell.FullQuality++;

                    cell.XpSum += CraftOutcomeRules.CalculateXp(baseXp, 0, false, (float)result.QualityFraction);
                }
                else
                {
                    cell.XpSum += CraftOutcomeRules.CalculateFailureXp(baseXp, 0);
                }
            }

            return cell;
        }

        private LegacyCell PlayLegacy(int level, int rank, int gearIndex)
        {
            var gear = Gear[gearIndex];
            var context = new LegacyCraftContext(level, rank, gear.Control, gear.Craftsmanship, gear.CpBonus);
            var policy = new RolloutPolicy();
            var qualities = new double[Context.Runs];
            var succeeded = new bool[Context.Runs];
            var successes = 0;
            long actions = 0;

            for (var run = 0; run < Context.Runs; run++)
            {
                var result = LegacyCraftRunner.Play(context, policy, SimRandom.ForRun(Context.Seed, SimStreams.Legacy, level, rank, gearIndex, run));
                actions += result.Actions;
                if (!result.Succeeded)
                    continue;

                successes++;
                succeeded[run] = true;
                qualities[run] = result.QualityFraction;
            }

            return new LegacyCell
            {
                Level = level,
                Rank = rank,
                Gear = gear.Name,
                Runs = Context.Runs,
                Successes = successes,
                Actions = actions,
                Qualities = qualities,
                Succeeded = succeeded
            };
        }
    }
}
