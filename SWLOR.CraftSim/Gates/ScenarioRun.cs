using System.Collections.Concurrent;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Legacy.Crafting;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Rng;
using SWLOR.CraftSim.Scenario;
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

    /// <summary>
    /// The plays one evaluation of a scenario shares between its gates: the cohort, the gear levels, and a memo of every adaptive cell
    /// and legacy cell so no gate plays what another already has. Everything is repeatable from the context's seed; cells are played in
    /// parallel and filed by key, so the machine's thread count cannot change a result.
    /// </summary>
    internal sealed class ScenarioRun
    {
        private readonly ConcurrentDictionary<(int Recipe, int Rank, int Gear, string Style), Lazy<CohortCell>> _adaptive = new();
        private readonly ConcurrentDictionary<(int Level, int Rank, int Gear), Lazy<LegacyCell>> _legacy = new();

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

        /// <summary>The solver's effort: the thresholds' override, or the scenario's own.</summary>
        public FixedRotationSolverOptions SolverOptions => Thresholds.SolverOptions ?? Scenario.SolverOptions;

        public static readonly ParallelOptions Parallelism = new() { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount) };

        public CohortCell Adaptive(RecipeEntry recipe, int rank, int gearIndex, CraftStyle style = null) =>
            _adaptive.GetOrAdd((StableId(recipe), rank, gearIndex, style?.Name ?? string.Empty), _ => new Lazy<CohortCell>(() => PlayAdaptive(recipe, rank, gearIndex, style))).Value;

        public LegacyCell Legacy(int level, int rank, int gearIndex) =>
            _legacy.GetOrAdd((level, rank, gearIndex), _ => new Lazy<LegacyCell>(() => PlayLegacy(level, rank, gearIndex))).Value;

        /// <summary>Plays the cells in parallel so the gates that read them find them done.</summary>
        public void PrepareAdaptive(IEnumerable<(RecipeEntry Recipe, int Rank, int GearIndex, CraftStyle Style)> cells) =>
            Parallel.ForEach(cells.ToList(), Parallelism, cell => Adaptive(cell.Recipe, cell.Rank, cell.GearIndex, cell.Style));

        public void PrepareLegacy(IEnumerable<(int Level, int Rank, int GearIndex)> cells) =>
            Parallel.ForEach(cells.Distinct().ToList(), Parallelism, cell => Legacy(cell.Level, cell.Rank, cell.GearIndex));

        /// <summary>Every cohort recipe at its own rank and every gear level: the cells gates 1, 4, 5 and 6 read.</summary>
        public IEnumerable<(RecipeEntry Recipe, int Rank, int GearIndex, CraftStyle Style)> CohortCells() =>
            Bands.SelectMany(band => band.Recipes).SelectMany(recipe =>
                Enumerable.Range(0, Gear.Count).Select(gear => (recipe, RankFor(recipe), gear, (CraftStyle)null)));

        public ICraftMinigameEngine CreateEngine(RecipeEntry recipe, int rank, GearLevel gear, CraftStyle style, IMinigameRandom random) =>
            Scenario.CreateEngine(new CraftEngineRequest(recipe, rank, gear, style, Context.Seed, random));

        /// <summary>A number that names a recipe in a random stream: stable across runs, unlike a string hash.</summary>
        public static int StableId(RecipeEntry recipe) => (int)recipe.Type;

        private CohortCell PlayAdaptive(RecipeEntry recipe, int rank, int gearIndex, CraftStyle style)
        {
            var gear = Gear[gearIndex];
            var baseXp = (int)Math.Round(ServerXp.ForRecipe(recipe, rank) * Scenario.XpMultiplier, MidpointRounding.AwayFromZero);
            var cell = new CohortCell { Recipe = recipe, Rank = rank, Gear = gear.Name, Style = style, Runs = Context.Runs };
            var id = StableId(recipe);

            for (var run = 0; run < Context.Runs; run++)
            {

                // The style is left out of the streams on purpose: two styles of one recipe are played on the same dice (common random
                // numbers), so a difference between them is the styles' and not the luck of two independent samples.
                var random = SimMinigameRandom.ForRun(Context.Seed, SimStreams.Adaptive, id, rank, gearIndex, run);
                var policySeed = unchecked((int)SimRandom.ForRun(Context.Seed, SimStreams.AdaptivePolicy, id, rank, gearIndex, run).NextUInt64());
                var engine = CreateEngine(recipe, rank, gear, style, random);
                var result = CraftPlayer.Play(engine, new AdaptivePolicy(Scenario.AdaptiveOptions, policySeed), Thresholds.ActionLimit);

                cell.Actions += result.Actions;
                cell.CpSpent += result.CpSpent;
                if (result.Stalled)
                    cell.Stalled++;

                if (result.Succeeded)
                {
                    cell.Successes++;
                    cell.QualitySum += result.QualityFraction;
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
