using System.Collections.Concurrent;
using System.Text;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Report;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Scenario.Experiment
{
    /// <summary>
    /// A search for the Methodical and Intuitive numbers that pass the style-balance thresholds both for the styles alone and for the
    /// styles on top of the whole line. Each style is played once per case (alone and with the line) and every pairing is judged from
    /// those results, so the whole space costs one pass over its styles and not one per pair. The pair closest to the approved numbers
    /// that passes both is the answer; when none passes, the report ranks the pairs by how few comparisons they fail.
    /// </summary>
    public static class ResearcherStyleSearch
    {
        /// <summary>A style's trial bonus and Test chance change, as the perk definitions give them.</summary>
        public sealed record StyleNumbers(int Trials, int Chance)
        {
            public string Describe(string name) =>
                $"{name} {(Trials > 0 ? "+" : string.Empty)}{Trials} trial{(Math.Abs(Trials) == 1 ? string.Empty : "s")}, {(Chance > 0 ? "+" : string.Empty)}{Chance} points";
        }

        /// <summary>One pairing: how far it is from the approved numbers, how many comparisons it fails, and what it does.</summary>
        public sealed record PairResult(
            StyleNumbers Methodical,
            StyleNumbers Intuitive,
            int Distance,
            int AloneFailed,
            int LineFailed,
            double AloneFixedGap,
            double AloneContestedGap,
            double LineFixedGap,
            double LineContestedGap,
            double MethodicalActions,
            double IntuitiveActions)
        {
            public int Failed => AloneFailed + LineFailed;

            public bool PassesBoth => Failed == 0;
        }

        /// <summary>The numbers the owner approved and the plan gave: Methodical +2 trials and -10 points, Intuitive -1 trial and +20 points.</summary>
        public static readonly StyleNumbers ApprovedMethodical = new(2, -10);

        public static readonly StyleNumbers ApprovedIntuitive = new(-1, 20);

        /// <summary>A trial counts as this many points of Test chance when measuring how far a pair is from the approved numbers.</summary>
        public const int PointsPerTrial = 5;

        public static readonly IReadOnlyList<int> MethodicalTrials = new[] { 2, 3 };

        public static readonly IReadOnlyList<int> MethodicalChances = Enumerable.Range(5, 16).Select(points => -points).ToArray();

        public static readonly IReadOnlyList<int> IntuitiveTrials = new[] { -1, -2 };

        public static readonly IReadOnlyList<int> IntuitiveChances = Enumerable.Range(10, 16).ToArray();

        /// <summary>How far a pair is from the approved numbers: points of Test chance, and <see cref="PointsPerTrial"/> for each trial.</summary>
        public static int Distance(StyleNumbers methodical, StyleNumbers intuitive) =>
            Math.Abs(methodical.Chance - ApprovedMethodical.Chance) + PointsPerTrial * Math.Abs(methodical.Trials - ApprovedMethodical.Trials) +
            Math.Abs(intuitive.Chance - ApprovedIntuitive.Chance) + PointsPerTrial * Math.Abs(intuitive.Trials - ApprovedIntuitive.Trials);

        /// <summary>Every pairing of the search space, judged on the same cases and dice, in no particular order.</summary>
        public static IReadOnlyList<PairResult> Search(
            ResearcherPerkStudyOptions options,
            IReadOnlyList<StyleNumbers> methodicalStyles = null,
            IReadOnlyList<StyleNumbers> intuitiveStyles = null)
        {
            var gear = options.Gear ?? throw new ArgumentException("The search needs gear levels.", nameof(options));
            methodicalStyles ??= MethodicalTrials.SelectMany(trials => MethodicalChances.Select(chance => new StyleNumbers(trials, chance))).ToList();
            intuitiveStyles ??= IntuitiveTrials.SelectMany(trials => IntuitiveChances.Select(chance => new StyleNumbers(trials, chance))).ToList();

            // The cases, grouped as the comparisons are: a level kind at a gear level is one comparison.
            var groups = new List<List<ExperimentCase>>();
            foreach (var level in new[] { ResearcherPerkStudy.FixedLevel, ResearcherPerkStudy.ContestedLevel })
            {
                foreach (var gearLevel in gear)
                    groups.Add(ResearcherPerkStudy.StyleCases(level, gearLevel).ToList());
            }

            var cases = groups.SelectMany(group => group).ToList();
            var groupLevels = groups.Select(group => group[0].ResearchLevel).ToList();

            // Prime anything the first play builds lazily before the plays run side by side.
            ResearcherPerkStudy.LineWith(50, 0, 0);

            var played = new ConcurrentDictionary<StyleNumbers, StylePlay>();
            Parallel.ForEach(methodicalStyles.Concat(intuitiveStyles).Distinct(), style => played[style] = Play(style, cases, options));

            var results = new List<PairResult>();
            foreach (var methodical in methodicalStyles)
            {
                foreach (var intuitive in intuitiveStyles)
                {
                    var first = played[methodical];
                    var second = played[intuitive];
                    var alone = Compare(first.AloneScore, second.AloneScore, groups, groupLevels);
                    var line = Compare(first.LineScore, second.LineScore, groups, groupLevels);
                    results.Add(new PairResult(
                        methodical, intuitive, Distance(methodical, intuitive), alone.Failed, line.Failed,
                        alone.FixedGap, alone.ContestedGap, line.FixedGap, line.ContestedGap,
                        first.AloneActions.Average(), second.AloneActions.Average()));
                }
            }

            return results;
        }

        public static string Build(ResearcherPerkStudyOptions options, string command)
        {
            var results = Search(options);
            var passing = results.Where(result => result.PassesBoth).OrderBy(result => result.Distance).ThenBy(result => result.Methodical.Trials)
                .ThenBy(result => result.Intuitive.Trials).ToList();

            var text = new StringBuilder();
            text.AppendLine("# Crafting revamp: the Researcher styles search");
            text.AppendLine();
            text.AppendLine($"Command: `{command}`");
            text.AppendLine();
            AppendInputs(text, options, results.Count);

            text.AppendLine("## Result");
            text.AppendLine();
            text.AppendLine($"- Pairs that pass the styles alone: {results.Count(result => result.AloneFailed == 0)} of {results.Count}.");
            text.AppendLine($"- Pairs that pass the full line: {results.Count(result => result.LineFailed == 0)} of {results.Count}.");
            text.AppendLine($"- Pairs that pass both: {passing.Count} of {results.Count}.");
            if (passing.Count > 0)
            {
                var best = passing[0];
                text.AppendLine($"- Closest to the approved numbers: {best.Methodical.Describe("Methodical")} against {best.Intuitive.Describe("Intuitive")} (distance {best.Distance}).");
            }
            else
            {
                var best = results.OrderBy(result => result.Failed).ThenBy(result => result.Distance).First();
                text.AppendLine($"- No pair passes both. Fewest comparisons failed: {best.Failed} of 12 ({best.AloneFailed} alone, {best.LineFailed} with the line), by {best.Methodical.Describe("Methodical")} against {best.Intuitive.Describe("Intuitive")} (distance {best.Distance}).");
            }

            text.AppendLine();
            text.AppendLine(passing.Count > 0 ? "## The closest pairs that pass both" : "## The best pairs (none passes both)");
            text.AppendLine();
            var ranked = passing.Count > 0
                ? passing
                : results.OrderBy(result => result.Failed).ThenBy(result => result.Distance).ToList();
            text.AppendLine(Table(ranked.Take(20)));

            text.AppendLine("## Reference pairs");
            text.AppendLine();
            text.AppendLine("The approved numbers, and the numbers in force before this search (Methodical -15), against the same Intuitive.");
            text.AppendLine();
            text.AppendLine(Table(results
                .Where(result => result.Intuitive == ApprovedIntuitive && result.Methodical.Trials == 2 && new[] { -10, -12, -15 }.Contains(result.Methodical.Chance))
                .OrderBy(result => result.Methodical.Chance)));

            AppendDetail(text, options, "The implemented pair, case by case", ImplementedMethodical, ImplementedIntuitive);
            var leader = passing.Count > 0 ? passing[0] : results.OrderBy(result => result.Failed).ThenBy(result => result.Distance).First();
            if (leader.Methodical != ImplementedMethodical || leader.Intuitive != ImplementedIntuitive)
                AppendDetail(text, options, "The leading pair, case by case", leader.Methodical, leader.Intuitive);

            text.AppendLine("## Limitations");
            text.AppendLine();
            text.AppendLine("- The same score analog and thresholds as `researcher-perks`: the generic gates have no scoring rule for an experiment, and each comparison has only six cases, so a share is a multiple of one twelfth and one case decides a comparison. A pair that passes here at a few hundred experiments per case can miss at another seed; run `researcher-perks` on the chosen numbers.");
            text.AppendLine("- Length is reported (mean actions of each style alone) and not ranked: Methodical +3 trials gives experiments longer than the owner's one-or-two-turns allowance.");
            return text.ToString();
        }

        /// <summary>The numbers in force when the search was written: Methodical +2 trials and -15 points, Intuitive -1 trial and +20 points.</summary>
        public static readonly StyleNumbers ImplementedMethodical = new(2, -15);

        public static readonly StyleNumbers ImplementedIntuitive = new(-1, 20);

        private static void AppendDetail(StringBuilder text, ResearcherPerkStudyOptions options, string heading, StyleNumbers methodical, StyleNumbers intuitive)
        {
            var gear = options.Gear;
            var middle = gear.Count > 1 ? gear[1] : gear[0];
            text.AppendLine($"## {heading}");
            text.AppendLine();
            text.AppendLine($"{methodical.Describe("Methodical")} against {intuitive.Describe("Intuitive")}, at {middle.Name}. A score is out of 100 (the time cut as a share of the cap at a fixed level, the reduction won as a share of today's largest where evidence decides); a case within {Number(ResearcherPerkStudy.TieMargin)} point is a tie. \"Line\" is each style on top of every other purchase the rank allows.");
            text.AppendLine();
            var table = new MarkdownTable(
                "Level kind", "Tier", "Rank", "Alone: Methodical", "Alone: Intuitive", "Alone: leads", "Line: Methodical", "Line: Intuitive", "Line: leads");
            foreach (var level in new[] { ResearcherPerkStudy.FixedLevel, ResearcherPerkStudy.ContestedLevel })
            {
                foreach (var experimentCase in ResearcherPerkStudy.StyleCases(level, middle))
                {
                    var m = Play(methodical, new[] { experimentCase }, options);
                    var i = Play(intuitive, new[] { experimentCase }, options);
                    table.Add(
                        level == ResearcherPerkStudy.FixedLevel ? "Fixed levels (time cut)" : "Evidence decides (reduction)",
                        experimentCase.Tier.ToString(), experimentCase.Rank.ToString(),
                        Number(m.AloneScore[0]), Number(i.AloneScore[0]), Leads(m.AloneScore[0] - i.AloneScore[0]),
                        Number(m.LineScore[0]), Number(i.LineScore[0]), Leads(m.LineScore[0] - i.LineScore[0]));
                }
            }

            text.AppendLine(table.ToString());
        }

        private static string Leads(double difference) =>
            Math.Abs(difference) <= ResearcherPerkStudy.TieMargin ? "tie" : difference > 0 ? "Methodical" : "Intuitive";

        private static string Table(IEnumerable<PairResult> rows)
        {
            var table = new MarkdownTable(
                "Methodical", "Intuitive", "Distance", "Alone: failed of 6", "Full line: failed of 6", "Alone: fixed gap", "Alone: evidence gap",
                "Line: fixed gap", "Line: evidence gap", "Methodical actions", "Intuitive actions");
            foreach (var row in rows)
            {
                table.Add(
                    row.Methodical.Describe("Methodical"), row.Intuitive.Describe("Intuitive"), row.Distance.ToString(), row.AloneFailed.ToString(),
                    row.LineFailed.ToString(), ResearcherPerkStudy.SignedNumber(row.AloneFixedGap), ResearcherPerkStudy.SignedNumber(row.AloneContestedGap),
                    ResearcherPerkStudy.SignedNumber(row.LineFixedGap), ResearcherPerkStudy.SignedNumber(row.LineContestedGap),
                    Number(row.MethodicalActions), Number(row.IntuitiveActions));
            }

            return table.ToString();
        }

        private static void AppendInputs(StringBuilder text, ResearcherPerkStudyOptions options, int pairs)
        {
            text.AppendLine("## Inputs");
            text.AppendLine();
            text.AppendLine($"- Seed {options.Seed}; {options.Runs} experiments per case and style, every style on the same dice, as `researcher-perks`.");
            text.AppendLine($"- Methodical: {string.Join(" or ", MethodicalTrials.Select(trials => $"+{trials}"))} trials and {MethodicalChances.Min()} to {MethodicalChances.Max()} points of Test chance. Intuitive: {IntuitiveChances.Min()} to {IntuitiveChances.Max()} points of Test chance and {string.Join(" or ", IntuitiveTrials)} trials. {pairs} pairs.");
            text.AppendLine($"- A pair passes when, for the styles alone and again for each style on top of every other purchase of the line, all six comparisons (two level kinds by three gear levels, six cases each) are inside the style-balance gate's thresholds: each style the better choice on at least {Percent(GateThresholds.Default.StyleMinimumWinShare)} of cases (a tie within {Number(ResearcherPerkStudy.TieMargin)} point counts half) and a mean score gap of at most {Number(GateThresholds.Default.StyleMaximumMeanDifferencePoints, 0)} points.");
            text.AppendLine($"- Distance from the approved numbers (Methodical +2 trials and -10 points, Intuitive -1 trial and +20 points): the points of Test chance each style moves, plus {PointsPerTrial} points for each trial.");
            text.AppendLine();
        }

        // ---- plays and comparisons ------------------------------------------------------------------------------------------

        private sealed record StylePlay(double[] AloneScore, double[] LineScore, double[] AloneActions);

        private static StylePlay Play(StyleNumbers style, IReadOnlyList<ExperimentCase> cases, ResearcherPerkStudyOptions options)
        {
            var aloneScore = new double[cases.Count];
            var lineScore = new double[cases.Count];
            var aloneActions = new double[cases.Count];
            for (var i = 0; i < cases.Count; i++)
            {
                var experimentCase = cases[i];
                var alone = ResearcherPerkStudy.Play(
                    experimentCase,
                    ExperimentTuning.Default with { Trials = ExperimentRules.Trials + style.Trials, TestChanceBonusPercent = style.Chance },
                    ResearcherPerkStudy.Focused, options.Runs, options.Seed);
                var line = ResearcherPerkStudy.Play(
                    experimentCase, ResearcherPerkStudy.LineWith(experimentCase.Rank, style.Trials, style.Chance), ResearcherPerkStudy.Focused,
                    options.Runs, options.Seed);
                aloneScore[i] = ResearcherPerkStudy.Score(experimentCase, alone);
                lineScore[i] = ResearcherPerkStudy.Score(experimentCase, line);
                aloneActions[i] = alone.Actions;
            }

            return new StylePlay(aloneScore, lineScore, aloneActions);
        }

        private static (int Failed, double FixedGap, double ContestedGap) Compare(
            double[] first, double[] second, IReadOnlyList<List<ExperimentCase>> groups, IReadOnlyList<int> groupLevels)
        {
            var failed = 0;
            double fixedGap = 0, contestedGap = 0;
            var fixedCases = 0;
            var contestedCases = 0;
            var offset = 0;
            for (var group = 0; group < groups.Count; group++)
            {
                var count = groups[group].Count;
                double firstWins = 0, secondWins = 0, gap = 0;
                for (var i = offset; i < offset + count; i++)
                {
                    var difference = first[i] - second[i];
                    gap += difference;
                    if (Math.Abs(difference) <= ResearcherPerkStudy.TieMargin)
                    {
                        firstWins += 0.5;
                        secondWins += 0.5;
                    }
                    else if (difference > 0)
                    {
                        firstWins++;
                    }
                    else
                    {
                        secondWins++;
                    }
                }

                var passed = firstWins / count >= GateThresholds.Default.StyleMinimumWinShare &&
                             secondWins / count >= GateThresholds.Default.StyleMinimumWinShare &&
                             Math.Abs(gap / count) <= GateThresholds.Default.StyleMaximumMeanDifferencePoints;
                if (!passed)
                    failed++;

                if (groupLevels[group] == ResearcherPerkStudy.FixedLevel)
                {
                    fixedGap += gap;
                    fixedCases += count;
                }
                else
                {
                    contestedGap += gap;
                    contestedCases += count;
                }

                offset += count;
            }

            return (failed, fixedCases == 0 ? 0 : fixedGap / fixedCases, contestedCases == 0 ? 0 : contestedGap / contestedCases);
        }
    }
}
