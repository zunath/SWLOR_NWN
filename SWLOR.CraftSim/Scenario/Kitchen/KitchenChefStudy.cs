using System.Text;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Report;
using SWLOR.CraftSim.Rng;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;
using SWLOR.Game.Server.Service.PerkService;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Scenario.Kitchen
{
    /// <summary>
    /// The Chef line's own evidence, beside the generic gates: what each purchase does to the adaptive player's actions, CP and quality at
    /// rank 50, the two styles compared on quality and on actions, what the whole line does at every rank, and the styles' magnitudes tried
    /// against gate 2's rule.
    /// </summary>
    public static class KitchenChefStudy
    {
        private const double QualityTieMargin = 0.5;
        private const int StyleMinimumRank = 20;
        private const double ActionsTieMargin = 0.25;

        private sealed record Summary(double Quality, double FullQuality, double Success, double Actions, double Cp, double CpAvailable);

        public static string Build(RecipeCatalog recipes, CraftingGearCatalog gear, KitchenChefStudyOptions options, string command)
        {
            var scenario = new KitchenScenario();
            var thresholds = GateThresholds.Default with { RecipesPerBand = options.RecipesPerBand };
            var context = new GateContext(recipes, gear, options.Runs, options.Seed, AttributeSweep.Default);
            var run = new ScenarioRun(scenario, thresholds, context);

            var text = new StringBuilder();
            text.AppendLine("# Crafting revamp: the Agriculture Chef line");
            text.AppendLine();
            text.AppendLine($"Command: `{command}`");
            text.AppendLine();
            AppendInputs(text, run, options);
            if (!options.StylesOnly)
            {
                AppendRankFifty(text, run);
                AppendThroughTheRanks(text, run);
            }

            AppendStyleMagnitudes(text, run, options);
            AppendLimitations(text);
            return text.ToString();
        }

        private static void AppendInputs(StringBuilder text, ScenarioRun run, KitchenChefStudyOptions options)
        {
            text.AppendLine("## Inputs");
            text.AppendLine();
            text.AppendLine($"- Seed {options.Seed}; {options.Runs} attempts per recipe, gear tier and build; up to {options.RecipesPerBand} recipes sampled from each at-level band (`GateThresholds.RecipesPerBand`).");
            text.AppendLine($"- Gear: {string.Join("; ", run.Gear.Select(level => $"{level.Name} (Control {level.Control}, Craftsmanship {level.Craftsmanship}, CP {level.CpBonus})"))}.");
            text.AppendLine("- The adaptive player (`AdaptivePolicy`, depth 3, `KitchenLeafHeuristic`) plays every build on the same dice, so a difference between two builds is theirs and not the luck of two samples.");
            text.AppendLine("- Every build takes its numbers from the perk definitions (`AgricultureChefPerkDefinition`) at the rank it is played at; a perk with two ranks gives the highest rank that rank allows. Seasoned Palate is a switch the cook makes for each dish: rows say whether it is on.");
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
            foreach (var perk in KitchenBuild.AllPerks)
            {
                builds.Add((KitchenBuild.Alone(perk).Name, KitchenBuild.Alone(perk).AsStyle()));
                if (perk == PerkType.SeasonedPalate)
                    builds.Add((KitchenBuild.Alone(perk, seasonedPalateOn: true).Name, KitchenBuild.Alone(perk, seasonedPalateOn: true).AsStyle()));
            }

            foreach (var line in new[] { KitchenBuild.SlowCookingLine, KitchenBuild.FlashCookingLine, KitchenBuild.SlowCookingLineSeasoned, KitchenBuild.FlashCookingLineSeasoned })
                builds.Add((line.Name, line.AsStyle()));

            Prepare(run, recipes, _ => rank, builds.Select(build => build.Style));

            text.AppendLine("## Rank 50: each purchase alone, and the whole line");
            text.AppendLine();
            text.AppendLine($"A rank-50 cook works recipes of levels {band.Band.Name} ({recipes.Count} played, of {band.ActiveRecipes}). Each row owns the one perk named (at its highest rank), or the whole line with that style. \"Actions\" is the mean actions per craft; the change is against the unperked cook at the same gear. A negative change is a shorter craft.");
            text.AppendLine();

            var table = new MarkdownTable("Build", "Gear", "Mean quality", "Quality change", "Full quality", "Items made", "Mean actions", "Actions change", "CP spent", "CP available");
            for (var gearIndex = 0; gearIndex < run.Gear.Count; gearIndex++)
            {
                var baseline = Summarize(run, recipes, _ => rank, gearIndex, null);
                foreach (var (name, style) in builds)
                {
                    var summary = Summarize(run, recipes, _ => rank, gearIndex, style);
                    table.Add(
                        name, run.Gear[gearIndex].Name, Number(summary.Quality), style == null ? "-" : SignedNumber(summary.Quality - baseline.Quality), Percent(summary.FullQuality),
                        Percent(summary.Success), Number(summary.Actions), style == null ? "-" : SignedNumber(summary.Actions - baseline.Actions),
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
                         (KitchenBuild.SlowCookingAlone, KitchenBuild.FlashCookingAlone),
                         (KitchenBuild.SlowCookingLine, KitchenBuild.FlashCookingLine),
                         (KitchenBuild.SlowCookingLineSeasoned, KitchenBuild.FlashCookingLineSeasoned)
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
                (KitchenBuild.SlowCookingLine.Name, KitchenBuild.SlowCookingLine.AsStyle()),
                (KitchenBuild.FlashCookingLine.Name, KitchenBuild.FlashCookingLine.AsStyle())
            };

            Prepare(run, run.Bands.SelectMany(band => band.Recipes), run.RankFor, builds.Select(build => build.Style));

            text.AppendLine("## The whole line through the ranks (at-level crafts)");
            text.AppendLine();
            text.AppendLine("Every recipe is crafted at its own level's rank (the plan's item-craft cohort), so a band's cook owns the purchases that rank allows: from rank 5 up, a style from rank 20, Reduce from rank 25, the capstone at 50. Seasoned Palate is off.");
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
        /// A candidate pair of styles: Slow Cooking's Low heat Doneness bonus and Medium heat drift reduction, and Flash Cooking's High heat
        /// drift reduction. Each is tried alone and with the rest of the line the rank allows.
        /// </summary>
        public sealed record StyleCandidate(string Name, int SlowLowDoneness, int SlowMediumDrift, int FlashHighDrift);

        /// <summary>The styles the perk definitions give, then the alternatives the retune tries.</summary>
        public static IReadOnlyList<StyleCandidate> StyleCandidates { get; } = new[]
        {
            Candidate(80, 25, 15),
        };

        public static StyleCandidate Candidate(int slowLow, int slowMedium, int flashHigh) =>
            new($"Slow +{slowLow}% Low / -{slowMedium}% Medium drift, Flash -{flashHigh}% High drift", slowLow, slowMedium, flashHigh);

        private static CraftStyle Style(string name, bool slow, bool withLine, StyleCandidate candidate) =>
            new(name, (Func<int, KitchenPerkStats>)(rank =>
            {
                if (rank < StyleMinimumRank)
                    return withLine ? KitchenBuild.SlowCookingLine.StatsAt(rank) : KitchenPerkStats.None;

                var baseStats = withLine
                    ? KitchenBuild.SlowCookingLine.StatsAt(rank) with { LowHeatDonenessPercent = 0, MediumDriftReductionPercent = 0 }
                    : KitchenPerkStats.None;
                return slow
                    ? baseStats with { LowHeatDonenessPercent = candidate.SlowLowDoneness, MediumDriftReductionPercent = candidate.SlowMediumDrift }
                    : baseStats with { HighDriftReductionPercent = candidate.FlashHighDrift };
            }));

        private static void AppendStyleMagnitudes(StringBuilder text, ScenarioRun run, KitchenChefStudyOptions options)
        {
            // The style-balance gate's recipes: every sampled recipe played at rank 20 or more.
            var recipes = run.Bands.SelectMany(band => band.Recipes).Where(recipe => run.RankFor(recipe) >= StyleMinimumRank).ToList();
            var gears = options.MidGearOnly ? new[] { 1 } : Enumerable.Range(0, run.Gear.Count).ToArray();
            CraftStyle unperked = null;
            var pairs = new List<(string Name, string Context, CraftStyle Slow, CraftStyle Flash)>();
            foreach (var candidate in options.Candidates ?? StyleCandidates)
            {
                foreach (var withLine in new[] { false, true })
                {
                    var label = withLine ? "full line" : "alone";
                    pairs.Add((candidate.Name, label, Style($"{candidate.Name} Slow {label}", true, withLine, candidate), Style($"{candidate.Name} Flash {label}", false, withLine, candidate)));
                }
            }

            Prepare(run, recipes, run.RankFor, pairs.SelectMany(pair => new[] { pair.Slow, pair.Flash }).Append(unperked), gears);

            text.AppendLine("## The styles' magnitudes");
            text.AppendLine();
            text.AppendLine($"Each candidate gives Slow Cooking a Low heat Simmer bonus and a Medium heat drift reduction, and Flash Cooking a High heat drift reduction, alone and with the rest of the line the rank allows (Seasoned Palate off), at-level crafts of levels {StyleMinimumRank} to 53 (the rank equal to the level), the style-balance gate's recipes. The \"better\" columns and the verdict are gate 2's rule (a tie within {Number(QualityTieMargin)} point gives each style half a recipe; each style must lead on at least {Percent(GateThresholds.Default.StyleMinimumWinShare)} and the mean gap must not pass {Number(GateThresholds.Default.StyleMaximumMeanDifferencePoints, 0)} points). \"Actions\" is the change against the unperked cook at the same gear; a negative change is a shorter craft.");
            text.AppendLine();

            var table = new MarkdownTable(
                "Candidate", "With", "Gear", "Slow Cooking quality", "Slow Cooking actions", "Flash Cooking quality", "Flash Cooking actions",
                "Slow better", "Flash better", "Mean quality gap (Slow - Flash)", "Gate 2");
            foreach (var gearIndex in gears)
            {
                var baseline = Summarize(run, recipes, run.RankFor, gearIndex, unperked);
                foreach (var pair in pairs)
                {
                    var slow = Summarize(run, recipes, run.RankFor, gearIndex, pair.Slow);
                    var flash = Summarize(run, recipes, run.RankFor, gearIndex, pair.Flash);
                    double slowBetter = 0, flashBetter = 0, gap = 0;
                    foreach (var recipe in recipes)
                    {
                        var difference = (run.Adaptive(recipe, run.RankFor(recipe), gearIndex, pair.Slow).MeanScore
                                          - run.Adaptive(recipe, run.RankFor(recipe), gearIndex, pair.Flash).MeanScore) * 100;
                        gap += difference;
                        Split(difference, QualityTieMargin, ref slowBetter, ref flashBetter);
                    }

                    var count = (double)recipes.Count;
                    var passes = slowBetter / count >= GateThresholds.Default.StyleMinimumWinShare &&
                                 flashBetter / count >= GateThresholds.Default.StyleMinimumWinShare &&
                                 Math.Abs(gap / count) <= GateThresholds.Default.StyleMaximumMeanDifferencePoints;
                    table.Add(
                        pair.Name, pair.Context, run.Gear[gearIndex].Name,
                        Number(slow.Quality), SignedNumber(slow.Actions - baseline.Actions),
                        Number(flash.Quality), SignedNumber(flash.Actions - baseline.Actions),
                        Percent(slowBetter / count), Percent(flashBetter / count), SignedNumber(gap / count), passes ? "Passes" : "Fails");
                }
            }

            text.AppendLine(table.ToString());
        }

        private static void AppendLimitations(StringBuilder text)
        {
            text.AppendLine("## Limitations");
            text.AppendLine();
            text.AppendLine("- Every figure is the adaptive player's, a bounded-depth search over the previews that does not plan the whole dish; a better player would use fewer actions with any build.");
            text.AppendLine("- Rank 50 plays recipes of levels 41-53 at rank 50 with every perk the rank allows. The through-the-ranks table plays one recipe level at the rank equal to it.");
            text.AppendLine("- Quality is the quality meter's share at the end, a ruined dish counting 0.");
            text.AppendLine("- Seasoned Palate is a choice the cook makes for each dish; the study plays it on for every dish or off for every dish, never choosing per dish.");
            text.AppendLine("- No client run; seconds per action are the simulator's estimate.");
        }

        // ---- helpers ------------------------------------------------------------------------------------------------------

        private static void Prepare(
            ScenarioRun run, IEnumerable<RecipeEntry> recipes, Func<RecipeEntry, int> rankFor, IEnumerable<CraftStyle> styles, IReadOnlyList<int> gears = null)
        {
            var list = recipes.ToList();
            var all = styles.ToList();
            var gearIndexes = gears ?? Enumerable.Range(0, run.Gear.Count).ToList();
            run.PrepareAdaptive(list.SelectMany(recipe => gearIndexes
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
