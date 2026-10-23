using System.Text;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Report;
using SWLOR.CraftSim.Rng;
using SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Scenario.Fabricator
{
    /// <summary>What the Builder study plays and how.</summary>
    public sealed class FabricatorBuilderStudyOptions
    {
        /// <summary>Attempts per recipe, rank, gear tier and build.</summary>
        public int Runs { get; init; } = 60;

        public int Seed { get; init; } = 1;

        /// <summary>The recipes sampled from each at-level band.</summary>
        public int RecipesPerBand { get; init; } = 8;
    }

    /// <summary>
    /// The Builder line's own evidence, beside the generic gates: what each purchase does to the adaptive player's actions, CP and
    /// quality at rank 50, the two styles compared on quality and on actions, and what the whole line does at every rank. The
    /// generic style-balance gate looks at quality only, and nearly every craft reaches full quality, so this shows the difference
    /// that gate cannot: how much work and CP each style takes to get there.
    /// </summary>
    public static class FabricatorBuilderStudy
    {
        private const double QualityTieMargin = 0.5;
        private const double ActionsTieMargin = 0.25;

        private sealed record Summary(double Quality, double FullQuality, double Success, double Actions, double Cp, double CpAvailable);

        public static string Build(RecipeCatalog recipes, CraftingGearCatalog gear, FabricatorBuilderStudyOptions options, string command)
        {
            var scenario = new FabricatorScenario();
            var thresholds = GateThresholds.Default with { RecipesPerBand = options.RecipesPerBand };
            var context = new GateContext(recipes, gear, options.Runs, options.Seed, AttributeSweep.Default);
            var run = new ScenarioRun(scenario, thresholds, context);

            var text = new StringBuilder();
            text.AppendLine("# Crafting revamp: the Fabrication Builder line");
            text.AppendLine();
            text.AppendLine($"Command: `{command}`");
            text.AppendLine();
            AppendInputs(text, run, options);
            AppendRankFifty(text, run);
            AppendThroughTheRanks(text, run);
            AppendStyleMagnitudes(text, run);
            AppendLimitations(text);
            return text.ToString();
        }

        private static void AppendInputs(StringBuilder text, ScenarioRun run, FabricatorBuilderStudyOptions options)
        {
            text.AppendLine("## Inputs");
            text.AppendLine();
            text.AppendLine($"- Seed {options.Seed}; {options.Runs} attempts per recipe, gear tier and build; up to {options.RecipesPerBand} recipes sampled from each at-level band (`GateThresholds.RecipesPerBand`).");
            text.AppendLine($"- Gear: {string.Join("; ", run.Gear.Select(level => $"{level.Name} (Control {level.Control}, Craftsmanship {level.Craftsmanship}, CP {level.CpBonus})"))}.");
            text.AppendLine("- The adaptive player (`AdaptivePolicy`, depth 3, `FabricatorLeafHeuristic`) plays every build on the same dice, so a difference between two builds is theirs and not the luck of two samples.");
            text.AppendLine("- Every build takes its numbers from the perk definitions (`FabricationBuilderPerkDefinition`) at the rank it is played at; a perk with two ranks gives the highest rank that rank allows.");
            text.AppendLine("- CP spent is what the craft used of the CP available (gear plus 0.75 per rank, plus 31 from rank 25).");
            text.AppendLine();
        }

        // ---- rank 50: each purchase alone, the styles and the whole line ------------------------------------------------

        private static void AppendRankFifty(StringBuilder text, ScenarioRun run)
        {
            const int rank = 50;
            var band = run.Bands.Last();
            var recipes = band.Recipes;
            var builds = new List<(string Name, CraftStyle Style)> { ("Unperked", null) };
            foreach (var perk in FabricatorBuild.AllPerks)
                builds.Add((FabricatorBuild.Alone(perk).Name, FabricatorBuild.Alone(perk).AsStyle()));
            builds.Add((FabricatorBuild.ProductionRunLine.Name, FabricatorBuild.ProductionRunLine.AsStyle()));
            builds.Add((FabricatorBuild.FineToleranceLine.Name, FabricatorBuild.FineToleranceLine.AsStyle()));

            Prepare(run, recipes, _ => rank, builds.Select(build => build.Style));

            text.AppendLine("## Rank 50: each purchase alone, and the whole line");
            text.AppendLine();
            text.AppendLine($"A rank-50 crafter works recipes of levels {band.Band.Name} ({recipes.Count} played, of {band.ActiveRecipes}). Each row owns the one perk named (at its highest rank), or the whole line with that style. \"Actions\" is the mean actions per craft; the change is against the unperked crafter at the same gear. A negative change is a shorter craft.");
            text.AppendLine();

            var table = new MarkdownTable("Build", "Gear", "Mean quality", "Full quality", "Items made", "Mean actions", "Actions change", "CP spent", "CP available");
            for (var gearIndex = 0; gearIndex < run.Gear.Count; gearIndex++)
            {
                var baseline = Summarize(run, recipes, _ => rank, gearIndex, null);
                foreach (var (name, style) in builds)
                {
                    var summary = Summarize(run, recipes, _ => rank, gearIndex, style);
                    table.Add(
                        name, run.Gear[gearIndex].Name, Number(summary.Quality), Percent(summary.FullQuality), Percent(summary.Success),
                        Number(summary.Actions), style == null ? "-" : SignedNumber(summary.Actions - baseline.Actions),
                        Number(summary.Cp), Number(summary.CpAvailable, 0));
                }
            }

            text.AppendLine(table.ToString());

            text.AppendLine("### The two styles: which is better, on quality and on actions");
            text.AppendLine();
            text.AppendLine($"Each recipe is a tie within {Number(QualityTieMargin)} quality point or {Number(ActionsTieMargin, 2)} action, and a tie gives each style half a recipe. The style-balance gate (gate 2) reads the quality columns only.");
            text.AppendLine();
            var pairs = new MarkdownTable(
                "Pair", "Gear", "Recipes", "First better quality", "Second better quality", "Mean quality gap", "First fewer actions", "Second fewer actions", "Mean actions gap (first - second)");
            foreach (var (first, second) in new[]
                     {
                         (FabricatorBuild.ProductionRunAlone, FabricatorBuild.FineToleranceAlone),
                         (FabricatorBuild.ProductionRunLine, FabricatorBuild.FineToleranceLine)
                     })
            {
                for (var gearIndex = 0; gearIndex < run.Gear.Count; gearIndex++)
                {
                    double qualityFirst = 0, qualitySecond = 0, actionsFirst = 0, actionsSecond = 0, qualityGap = 0, actionsGap = 0;
                    foreach (var recipe in recipes)
                    {
                        var a = run.Adaptive(recipe, rank, gearIndex, first.AsStyle());
                        var b = run.Adaptive(recipe, rank, gearIndex, second.AsStyle());
                        var quality = (a.MeanScore - b.MeanScore) * 100;
                        qualityGap += quality;
                        Split(quality, QualityTieMargin, ref qualityFirst, ref qualitySecond);
                        var actions = a.ActionsPerAttempt - b.ActionsPerAttempt;
                        actionsGap += actions;
                        Split(-actions, ActionsTieMargin, ref actionsFirst, ref actionsSecond);
                    }

                    var count = (double)recipes.Count;
                    pairs.Add(
                        $"{first.Name} vs {second.Name}", run.Gear[gearIndex].Name, recipes.Count.ToString(), Percent(qualityFirst / count),
                        Percent(qualitySecond / count), SignedNumber(qualityGap / count), Percent(actionsFirst / count), Percent(actionsSecond / count),
                        SignedNumber(actionsGap / count, 2));
                }
            }

            text.AppendLine(pairs.ToString());
        }

        // ---- the line through the ranks ----------------------------------------------------------------------------------

        private static void AppendThroughTheRanks(StringBuilder text, ScenarioRun run)
        {
            var builds = new List<(string Name, CraftStyle Style)>
            {
                ("Unperked", null),
                (FabricatorBuild.ProductionRunLine.Name, FabricatorBuild.ProductionRunLine.AsStyle()),
                (FabricatorBuild.FineToleranceLine.Name, FabricatorBuild.FineToleranceLine.AsStyle())
            };

            Prepare(run, run.Bands.SelectMany(band => band.Recipes), run.RankFor, builds.Select(build => build.Style));

            text.AppendLine("## The whole line through the ranks (at-level crafts)");
            text.AppendLine();
            text.AppendLine("Every recipe is crafted at its own level's rank (the plan's item-craft cohort), so a band's crafter owns the purchases that rank allows: from rank 5 up, a style from rank 20, Dual Feed from rank 25, the capstone at 50.");
            text.AppendLine();

            var table = new MarkdownTable("Band", "Gear", "Build", "Mean quality", "Full quality", "Mean actions", "Actions change", "CP spent", "CP available");
            foreach (var band in run.Bands)
            {
                for (var gearIndex = 0; gearIndex < run.Gear.Count; gearIndex++)
                {
                    var baseline = Summarize(run, band.Recipes, run.RankFor, gearIndex, null);
                    foreach (var (name, style) in builds)
                    {
                        var summary = Summarize(run, band.Recipes, run.RankFor, gearIndex, style);
                        table.Add(
                            band.Band.Name, run.Gear[gearIndex].Name, name, Number(summary.Quality), Percent(summary.FullQuality), Number(summary.Actions),
                            style == null ? "-" : SignedNumber(summary.Actions - baseline.Actions), Number(summary.Cp), Number(summary.CpAvailable, 0));
                    }
                }
            }

            text.AppendLine(table.ToString());
        }

        // ---- the styles' magnitudes -----------------------------------------------------------------------------------------

        /// <summary>
        /// A candidate pair of styles: Production Run's Run bonus and Precision Pass penalty, and Fine Tolerance's Precision Pass bonus and
        /// Run penalty, in percent. A mirror gives Fine Tolerance the same sizes the other way round.
        /// </summary>
        private sealed record StyleCandidate(string Name, int RunUp, int PrecisionDown, int PrecisionUp, int RunDown)
        {
            public static StyleCandidate Mirror(int up, int down, string note = "") =>
                new($"Mirror +{up}% / {down}%{note}", up, down, up, down);
        }

        private static readonly StyleCandidate[] StyleCandidates =
        {
            StyleCandidate.Mirror(25, -25, " (approved)"),
            StyleCandidate.Mirror(40, -10),
            StyleCandidate.Mirror(50, -15),
            StyleCandidate.Mirror(60, -10),
            new("Production Run +60% / -10%, Fine Tolerance +25% / -25%", 60, -10, 25, -25)
        };

        private static void AppendStyleMagnitudes(StringBuilder text, ScenarioRun run)
        {
            var bands = run.Bands.Where(band => band.Band.First >= 21).ToList();
            var recipes = bands.SelectMany(band => band.Recipes).ToList();
            CraftStyle unperked = null;
            var pairs = StyleCandidates.Select(candidate => (
                candidate.Name,
                Production: new CraftStyle($"Production Run {candidate.Name}", new FabricatorPerkStats(RunPowerPercent: candidate.RunUp, PrecisionPowerPercent: candidate.PrecisionDown)),
                Fine: new CraftStyle($"Fine Tolerance {candidate.Name}", new FabricatorPerkStats(RunPowerPercent: candidate.RunDown, PrecisionPowerPercent: candidate.PrecisionUp)))).ToList();

            Prepare(run, recipes, run.RankFor, pairs.SelectMany(pair => new[] { pair.Production, pair.Fine }).Append(unperked));

            text.AppendLine("## The styles' magnitudes");
            text.AppendLine();
            text.AppendLine("Each candidate gives Production Run a Run bonus and a Precision Pass penalty, and Fine Tolerance a Precision Pass bonus and a Run penalty (a mirror uses the same sizes both ways), the style alone, at-level crafts of levels 21 to 53 (the rank equal to the level). The first is the approved pair; the others are what a retune could try. \"Actions\" is the change against the unperked crafter at the same gear; a negative change is a shorter craft. Gate 2 reads quality only, which every candidate keeps near the maximum, so the actions columns are where the styles differ.");
            text.AppendLine();

            var table = new MarkdownTable(
                "Candidate", "Gear", "Production Run quality", "Production Run actions", "Fine Tolerance quality", "Fine Tolerance actions", "Fine Tolerance - Production Run actions");
            for (var gearIndex = 0; gearIndex < run.Gear.Count; gearIndex++)
            {
                var baseline = Summarize(run, recipes, run.RankFor, gearIndex, unperked);
                foreach (var pair in pairs)
                {
                    var production = Summarize(run, recipes, run.RankFor, gearIndex, pair.Production);
                    var fine = Summarize(run, recipes, run.RankFor, gearIndex, pair.Fine);
                    table.Add(
                        pair.Name, run.Gear[gearIndex].Name,
                        Number(production.Quality), SignedNumber(production.Actions - baseline.Actions),
                        Number(fine.Quality), SignedNumber(fine.Actions - baseline.Actions), SignedNumber(fine.Actions - production.Actions));
                }
            }

            text.AppendLine(table.ToString());
        }

        private static void AppendLimitations(StringBuilder text)
        {
            text.AppendLine("## Limitations");
            text.AppendLine();
            text.AppendLine("- Every figure is the adaptive player's, a bounded-depth search over the previews that does not plan the whole craft; a better player would use fewer actions with any build.");
            text.AppendLine("- Rank 50 plays recipes of levels 41-53 at rank 50 with every perk the rank allows. The through-the-ranks table plays one recipe level at the rank equal to it.");
            text.AppendLine("- Quality is the quality meter's share at the end, a broken or abandoned piece counting 0. Most crafts at mid and best gear reach full quality, so a style's cost shows in actions and CP.");
            text.AppendLine("- No client run; seconds per action are the simulator's estimate.");
        }

        // ---- helpers ------------------------------------------------------------------------------------------------------

        private static void Prepare(ScenarioRun run, IEnumerable<RecipeEntry> recipes, Func<RecipeEntry, int> rankFor, IEnumerable<CraftStyle> styles)
        {
            var list = recipes.ToList();
            var all = styles.ToList();
            run.PrepareAdaptive(list.SelectMany(recipe => Enumerable.Range(0, run.Gear.Count)
                .SelectMany(gear => all.Select(style => (recipe, rankFor(recipe), gear, style)))));
        }

        private static Summary Summarize(ScenarioRun run, IEnumerable<RecipeEntry> recipes, Func<RecipeEntry, int> rankFor, int gearIndex, CraftStyle style)
        {
            var cells = recipes.Select(recipe => (Cell: run.Adaptive(recipe, rankFor(recipe), gearIndex, style), Recipe: recipe)).ToList();
            var attempts = (double)cells.Sum(item => item.Cell.Runs);
            var cpAvailable = cells.Average(item => (double)run.CreateEngine(
                item.Recipe, rankFor(item.Recipe), run.Gear[gearIndex], style, new SimMinigameRandom(new SimRandom(1))).Meters.Cp.Max);
            return new Summary(
                cells.Sum(item => item.Cell.QualitySum) / attempts * 100,
                cells.Sum(item => item.Cell.FullQuality) / attempts,
                cells.Sum(item => item.Cell.Successes) / attempts,
                cells.Sum(item => item.Cell.Actions) / attempts,
                cells.Sum(item => item.Cell.CpSpent) / attempts,
                cpAvailable);
        }

        /// <summary>Gives the recipe to whichever side leads by more than the tie margin, or half to each.</summary>
        private static void Split(double firstLead, double margin, ref double first, ref double second)
        {
            if (Math.Abs(firstLead) <= margin)
            {
                first += 0.5;
                second += 0.5;
            }
            else if (firstLead > 0)
            {
                first++;
            }
            else
            {
                second++;
            }
        }

        private static string SignedNumber(double value, int decimals = 1) =>
            (value > 0 ? "+" : string.Empty) + Number(value, decimals);
    }
}
