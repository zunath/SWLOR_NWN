using System.Text;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Report;
using SWLOR.CraftSim.Rng;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;
using SWLOR.Game.Server.Service.CraftService.Research;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Scenario.Experiment
{
    /// <summary>What the Researcher study plays and how.</summary>
    public sealed class ResearcherPerkStudyOptions
    {
        /// <summary>Experiments per case and build.</summary>
        public int Runs { get; init; } = 400;

        public int Seed { get; init; } = 1;

        /// <summary>Seconds per action in the research window, an estimate until client evidence measures it.</summary>
        public double SecondsPerAction { get; init; } = 6;

        /// <summary>Seconds per action in the Fabricator window, which sets the crafting rate the research ceiling is measured against.</summary>
        public double CraftSecondsPerAction { get; init; } = 8;

        /// <summary>Seconds per job besides the experiment's actions.</summary>
        public double JobOverheadSeconds { get; init; } = 30;

        /// <summary>The Fabrication gear levels (none, mid, best).</summary>
        public IReadOnlyList<GearLevel> Gear { get; init; }

        public Func<int, int> ChartXp { get; init; } = ServerXp.ForDelta;
    }

    /// <summary>
    /// The Researcher line's own evidence: what each purchase does to the experiment's length, evidence, reduction size and time cut,
    /// the two styles compared with the style-balance gate's thresholds (the generic gates have no scoring rule for an experiment),
    /// what Peer Review is worth to a player who has not decided, and Fabrication XP per real hour with the line, against the unperked
    /// researcher and the at-level crafting rate it must stay under.
    /// </summary>
    public static class ResearcherPerkStudy
    {
        /// <summary>The stream that separates the study's dice from every gate's and from the research report's.</summary>
        public const int StudyStream = 137;

        internal const int FixedLevel = 1;
        internal const int ContestedLevel = 3;
        internal const double TieMargin = 0.5;

        /// <summary>The trials a style adds and the points of Test chance it gives (negative for a cost), as candidates a retune could try.</summary>
        private static readonly (string Name, int Trials, int Chance)[] MethodicalCandidates =
        {
            ("Methodical +2 trials, -15 points", 2, -15),
            ("Methodical +2 trials, -14 points", 2, -14),
            ("Methodical +2 trials, -13 points", 2, -13),
            ("Methodical +2 trials, -12 points", 2, -12),
            ("Methodical +2 trials, -11 points", 2, -11),
            ("Methodical +2 trials, -10 points (the plan's number)", 2, -10),
            ("Methodical +2 trials, -5 points", 2, -5),
            ("Methodical +3 trials, -10 points", 3, -10)
        };

        /// <summary>The line's other purchases with no style, so a candidate style can be added to it.</summary>
        private static readonly ResearcherBuild LineWithoutAStyle = new(
            "Line without a style",
            ResearcherBuild.AllPerks.Where(perk => perk != PerkType.Methodical && perk != PerkType.Intuitive));

        private static readonly (string Name, int Trials, int Chance)[] IntuitiveCandidates =
        {
            ("Intuitive -1 trial, +20 points", -1, 20),
            ("Intuitive -1 trial, +15 points", -1, 15),
            ("Intuitive -1 trial, +25 points", -1, 25)
        };

        /// <summary>What many experiments of one build at one case came to.</summary>
        public sealed record Sample(
            double Actions, double TrialsUsed, double Trials, double TimeCut, double Magnitude, double AtMaximum, double AtCap, double NoEvidence);

        /// <summary>One comparison of two styles: a level kind and a gear level over every rank and tier the study plays.</summary>
        private sealed record StyleRow(
            int Level, string Gear, int Cases, double FirstWins, double SecondWins, double Gap, double FirstActions, double SecondActions, bool Passed);

        public static string Build(ResearcherPerkStudyOptions options, string command)
        {
            var gear = options.Gear ?? throw new ArgumentException("The study needs gear levels.", nameof(options));
            var text = new StringBuilder();
            text.AppendLine("# Crafting revamp: the Fabrication Researcher line");
            text.AppendLine();
            text.AppendLine($"Command: `{command}`");
            text.AppendLine();
            AppendInputs(text, options, gear);
            AppendStyleBalance(text, options, gear);
            AppendStyleMagnitudes(text, options, gear);
            AppendEachPerk(text, options, gear);
            AppendPeerReview(text, options, gear);
            AppendXp(text, options, gear);
            AppendLimitations(text);
            return text.ToString();
        }

        /// <summary>Plays <paramref name="runs"/> experiments of a case for a researcher with these numbers.</summary>
        public static Sample Play(ExperimentCase experimentCase, ExperimentTuning tuning, Func<ExperimentSetup, ICraftPolicy> policy, int runs, int seed)
        {
            if (runs < 1)
                throw new ArgumentOutOfRangeException(nameof(runs));

            var setup = experimentCase.CreateSetup(tuning);
            double actions = 0, trialsUsed = 0, cut = 0, magnitude = 0;
            var atMaximum = 0;
            var atCap = 0;
            var noEvidence = 0;
            for (var run = 0; run < runs; run++)
            {
                // The build is left out of the stream on purpose: two builds of one case are played on the same dice, and each trial's
                // roll is the same in both however many actions came before it (a Breakthrough Test takes no roll).
                ExperimentEngine engine = null;
                var random = new TrialPairedRandom(
                    () => engine?.TrialsUsed ?? 0,
                    trial => SimMinigameRandom.ForRun(
                        seed, StudyStream, experimentCase.ResearchLevel, experimentCase.Tier, experimentCase.Rank, experimentCase.Gear.Tier,
                        experimentCase.LicensedRunsGrant, run, trial));
                engine = new ExperimentEngine(random, setup);
                var result = CraftPlayer.Play(engine, policy(setup));

                actions += result.Actions;
                trialsUsed += engine.TrialsUsed;
                cut += engine.TimeCutPercent;
                if (engine.TimeCutPercent >= setup.TimeCutCap)
                    atCap++;

                if (!setup.IsContested)
                    continue;

                var (winner, size) = ResearchOutcomeRules.Resolve(
                    experimentCase.ResearchLevel, experimentCase.LicensedRunsGrant, engine.Snapshot().Evidence, random);
                if (winner == ResearchCandidate.None)
                {
                    noEvidence++;
                    continue;
                }

                magnitude += size;
                if (size >= ResearchGrantRules.ReductionMaximum)
                    atMaximum++;
            }

            return new Sample(
                actions / runs, trialsUsed / runs, setup.Trials, cut / runs, magnitude / runs, atMaximum / (double)runs, atCap / (double)runs,
                noEvidence / (double)runs);
        }

        internal static Func<ExperimentSetup, ICraftPolicy> Focused { get; } =
            _ => new ExperimentPerkedPolicy(ResearchCandidate.CreditReduction);

        private static Func<ExperimentSetup, ICraftPolicy> Undecided { get; } =
            _ => new ExperimentPerkedPolicy(ResearchCandidate.CreditReduction, spread: true);

        /// <summary>
        /// A case's score from 0 to 100: where evidence decides between the reductions, the size of the reduction won against today's
        /// largest (10%); at a fixed level, the time cut against its cap (25%). Both are what the experiment is for at that level.
        /// </summary>
        public static double Score(ExperimentCase experimentCase, Sample sample) =>
            experimentCase.ResearchLevel == FixedLevel
                ? sample.TimeCut / ExperimentRules.TimeCutCapPercent * 100
                : sample.Magnitude / ResearchGrantRules.ReductionMaximum * 100;

        private static void AppendInputs(StringBuilder text, ResearcherPerkStudyOptions options, IReadOnlyList<GearLevel> gear)
        {
            text.AppendLine("## Inputs");
            text.AppendLine();
            text.AppendLine($"- Seed {options.Seed}; {options.Runs} experiments per case and build, every build on the same dice (the build is left out of each experiment's stream), so a difference between two builds is theirs and not the luck of two samples.");
            text.AppendLine($"- Every build takes its numbers from the perk definitions (`FabricationResearcherPerkDefinition`) at the researcher's rank; a perk with two ranks gives the highest rank that rank allows. Base values (`ExperimentRules`): {ExperimentRules.Trials} trials, Test chance {ExperimentRules.BaseTestChancePercent}% plus up to {ExperimentRules.RankChanceBonusMaximum} points for rank, Refine Method {ExperimentRules.RefineChanceBonusPercent} points for {ExperimentRules.RefineCp} CP, Analyze {ExperimentRules.TimeCutPercentPerEvidence}% per piece of spare evidence up to {ExperimentRules.TimeCutCapPercent}%, Peer Review {ExperimentRules.PeerReviewCp} CP.");
            text.AppendLine($"- Gear: {string.Join("; ", gear.Select(level => $"{level.Name} = {level.CpBonus} CP"))}. CP is gear CP plus 0.75 per rank.");
            text.AppendLine($"- Cases: research level {FixedLevel} stands for the fixed levels and licensed-run rolls (every Test banks evidence and the last trial Analyzes it); level {ContestedLevel} stands for the levels where evidence decides between a credit and a time reduction. Researchers at ranks 20, 30, 40 and 50 research the tier their rank opens, at that tier's first rank, and rank 50 also researches tiers 1 and 3 (up to 15 more points of Test chance).");
            text.AppendLine("- Focused player: refines before every Test it can afford, puts every Test on one reduction (credit), Analyzes with the last trial at a fixed level, and with Breakthrough spends it first. Undecided player (Peer Review only): tests both reductions for half the trials, then backs the leader and, with Peer Review, pools the evidence behind it into the leader, keeping the CP it costs back from Refine Method.");
            text.AppendLine($"- Score (0-100): where evidence decides, the reduction won as a share of today's largest ({ResearchGrantRules.ReductionMaximum}%), a run with no evidence counting 0; at a fixed level, the time cut as a share of the cap ({ExperimentRules.TimeCutCapPercent}%). Style balance uses the style-balance gate's thresholds: each style the better choice on at least {Percent(GateThresholds.Default.StyleMinimumWinShare)} of cases, a mean gap of at most {Number(GateThresholds.Default.StyleMaximumMeanDifferencePoints, 0)} points, ties within {Number(TieMargin)} point count half.");
            text.AppendLine();
        }

        // ---- style balance --------------------------------------------------------------------------------------------

        internal static IEnumerable<ExperimentCase> StyleCases(int level, GearLevel gear)
        {
            foreach (var (tier, rank) in new[] { (2, 20), (3, 30), (4, 40), (5, 50), (1, 50), (3, 50) })
                yield return new ExperimentCase(level, tier, rank, gear);
        }

        private static List<StyleRow> CompareStyles(
            ResearcherPerkStudyOptions options, IReadOnlyList<GearLevel> gear, Func<int, ExperimentTuning> first, Func<int, ExperimentTuning> second)
        {
            var rows = new List<StyleRow>();
            foreach (var level in new[] { FixedLevel, ContestedLevel })
            {
                foreach (var gearLevel in gear)
                {
                    double firstWins = 0, secondWins = 0, gap = 0, firstActions = 0, secondActions = 0;
                    var cases = StyleCases(level, gearLevel).ToList();
                    foreach (var experimentCase in cases)
                    {
                        var a = Play(experimentCase, first(experimentCase.Rank), Focused, options.Runs, options.Seed);
                        var b = Play(experimentCase, second(experimentCase.Rank), Focused, options.Runs, options.Seed);
                        var difference = Score(experimentCase, a) - Score(experimentCase, b);
                        gap += difference;
                        firstActions += a.Actions;
                        secondActions += b.Actions;
                        if (Math.Abs(difference) <= TieMargin)
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

                    var count = (double)cases.Count;
                    var passed = firstWins / count >= GateThresholds.Default.StyleMinimumWinShare &&
                                 secondWins / count >= GateThresholds.Default.StyleMinimumWinShare &&
                                 Math.Abs(gap / count) <= GateThresholds.Default.StyleMaximumMeanDifferencePoints;
                    rows.Add(new StyleRow(level, gearLevel.Name, cases.Count, firstWins / count, secondWins / count, gap / count, firstActions / count, secondActions / count, passed));
                }
            }

            return rows;
        }

        private static void AppendStyleBalance(StringBuilder text, ResearcherPerkStudyOptions options, IReadOnlyList<GearLevel> gear)
        {
            text.AppendLine("## Style balance: Methodical against Intuitive");
            text.AppendLine();
            text.AppendLine("The generic gates have no scoring rule for an experiment (it has no quality meter), so this applies gate 2's thresholds to the score above, over every case. \"Alone\" owns the style and nothing else; \"full line\" owns every other purchase the rank allows too. The length columns show what each style costs in turns, which the score does not count.");
            text.AppendLine();

            var table = new MarkdownTable(
                "Pair", "Level kind", "Gear", "Cases", "Methodical better", "Intuitive better", "Mean score gap (Methodical - Intuitive)",
                "Methodical mean actions", "Intuitive mean actions", "Verdict");
            var failures = new List<string>();
            foreach (var (first, second) in new[]
                     {
                         (ResearcherBuild.MethodicalAlone, ResearcherBuild.IntuitiveAlone),
                         (ResearcherBuild.MethodicalLine, ResearcherBuild.IntuitiveLine)
                     })
            {
                foreach (var row in CompareStyles(options, gear, first.TuningAt, second.TuningAt))
                {
                    var label = $"{first.Name} vs {second.Name} / {Kind(row.Level)} / {row.Gear}";
                    if (!row.Passed)
                        failures.Add(label);

                    table.Add(
                        $"{first.Name} vs {second.Name}", Kind(row.Level), row.Gear, row.Cases.ToString(), Percent(row.FirstWins), Percent(row.SecondWins),
                        SignedNumber(row.Gap), Number(row.FirstActions), Number(row.SecondActions), row.Passed ? "Passed" : "Failed");
                }
            }

            text.AppendLine(table.ToString());
            text.AppendLine(failures.Count == 0
                ? "Verdict: every style is the better choice on at least the required share of cases and no pair differs by more than the allowed mean gap."
                : $"Verdict: out of balance in {failures.Count} of {table.RowCount} comparison(s): {string.Join("; ", failures)}.");
            text.AppendLine();
        }

        /// <summary>The numbers of a researcher with every purchase but the styles, at this rank, plus a style's trials and Test chance.</summary>
        internal static ExperimentTuning LineWith(int rank, int trials, int chance)
        {
            var line = LineWithoutAStyle.TuningAt(rank);
            return line with { Trials = line.Trials + trials, TestChanceBonusPercent = line.TestChanceBonusPercent + chance };
        }

        private static void AppendStyleMagnitudes(StringBuilder text, ResearcherPerkStudyOptions options, IReadOnlyList<GearLevel> gear)
        {
            text.AppendLine("## The styles' magnitudes");
            text.AppendLine();
            text.AppendLine("Each row pairs a Methodical and an Intuitive candidate over the same 36 cases as the table above (two level kinds, three gear levels, six rank and tier combinations): the styles alone, and each style on top of every other purchase of the line (the full line). \"Failed\" counts the six comparisons (two level kinds by three gear levels) that miss gate 2's thresholds; the gaps are the mean score gap over all cases of the level kind (positive favors Methodical), for the styles alone.");
            text.AppendLine();

            var table = new MarkdownTable(
                "Methodical", "Intuitive", "Alone: failed of 6", "Full line: failed of 6", "Fixed levels: mean gap", "Evidence decides: mean gap",
                "Full line fixed levels: mean gap", "Full line evidence decides: mean gap", "Methodical mean actions", "Intuitive mean actions");
            foreach (var methodical in MethodicalCandidates)
            {
                foreach (var intuitive in IntuitiveCandidates)
                {
                    var rows = CompareStyles(
                        options,
                        gear,
                        _ => ExperimentTuning.Default with { Trials = ExperimentRules.Trials + methodical.Trials, TestChanceBonusPercent = methodical.Chance },
                        _ => ExperimentTuning.Default with { Trials = ExperimentRules.Trials + intuitive.Trials, TestChanceBonusPercent = intuitive.Chance });
                    var lineRows = CompareStyles(
                        options,
                        gear,
                        rank => LineWith(rank, methodical.Trials, methodical.Chance),
                        rank => LineWith(rank, intuitive.Trials, intuitive.Chance));
                    table.Add(
                        methodical.Name, intuitive.Name, rows.Count(row => !row.Passed).ToString(), lineRows.Count(row => !row.Passed).ToString(),
                        SignedNumber(rows.Where(row => row.Level == FixedLevel).Average(row => row.Gap)),
                        SignedNumber(rows.Where(row => row.Level == ContestedLevel).Average(row => row.Gap)),
                        SignedNumber(lineRows.Where(row => row.Level == FixedLevel).Average(row => row.Gap)),
                        SignedNumber(lineRows.Where(row => row.Level == ContestedLevel).Average(row => row.Gap)),
                        Number(rows.Average(row => row.FirstActions)), Number(rows.Average(row => row.SecondActions)));
                }
            }

            text.AppendLine(table.ToString());
        }

        // ---- each perk ------------------------------------------------------------------------------------------------------

        private static void AppendEachPerk(StringBuilder text, ResearcherPerkStudyOptions options, IReadOnlyList<GearLevel> gear)
        {
            var middle = gear.Count > 1 ? gear[1] : gear[0];
            text.AppendLine("## Each purchase alone, and the whole line (rank 50, tier 5, " + middle.Name + ")");
            text.AppendLine();
            text.AppendLine("Each row is a rank-50 researcher who owns the one perk named (at its highest rank), or the whole line with that style, against the unperked researcher on the same dice. \"Reaches the maximum\" is the share of experiments whose reduction reached today's largest; \"Reaches the cap\" the share that cut the full 25% of the job.");
            text.AppendLine();

            var builds = new List<ResearcherBuild> { ResearcherBuild.Unperked };
            builds.AddRange(ResearcherBuild.AllPerks.Select(ResearcherBuild.Alone));
            builds.Add(ResearcherBuild.MethodicalLine);
            builds.Add(ResearcherBuild.IntuitiveLine);

            var table = new MarkdownTable(
                "Build", "Trials", "Test chance", "Reductions: mean actions", "Mean reduction size", "No evidence", "Reaches the maximum",
                "Fixed: mean actions", "Mean time cut", "Reaches the cap");
            foreach (var build in builds)
            {
                var tuning = build.TuningAt(50);
                var contested = Play(new ExperimentCase(ContestedLevel, 5, 50, middle), tuning, Focused, options.Runs, options.Seed);
                var fixedLevel = Play(new ExperimentCase(FixedLevel, 5, 50, middle), tuning, Focused, options.Runs, options.Seed);
                var chance = new ExperimentCase(ContestedLevel, 5, 50, middle).CreateSetup(tuning).BaseTestChancePercent;
                table.Add(
                    build.Name, tuning.Trials.ToString(), chance + "%", Number(contested.Actions), Number(contested.Magnitude, 2) + "%",
                    Percent(contested.NoEvidence, 1), Percent(contested.AtMaximum), Number(fixedLevel.Actions), Number(fixedLevel.TimeCut) + "%",
                    Percent(fixedLevel.AtCap));
            }

            text.AppendLine(table.ToString());
        }

        private static void AppendPeerReview(StringBuilder text, ResearcherPerkStudyOptions options, IReadOnlyList<GearLevel> gear)
        {
            text.AppendLine("## Peer Review for a player who has not decided");
            text.AppendLine();
            text.AppendLine("The focused player never has evidence behind the leader, so Peer Review has nothing to move for them. A player who tests both reductions first and then backs the leader does: Peer Review pools the evidence behind the leader into it. Rank 50, tier 5, where evidence decides. The perk costs 25 CP, so it competes with Refine Method for CP: a no-gear researcher has 37 CP and cannot afford both.");
            text.AppendLine();

            var table = new MarkdownTable("Build", "Gear", "Mean actions", "Mean reduction size", "No evidence", "Reaches the maximum");
            foreach (var gearLevel in gear)
            {
                foreach (var build in new[] { ResearcherBuild.Unperked, ResearcherBuild.Alone(PerkType.PeerReview) })
                {
                    var sample = Play(new ExperimentCase(ContestedLevel, 5, 50, gearLevel), build.TuningAt(50), Undecided, options.Runs, options.Seed);
                    table.Add(build.Name, gearLevel.Name, Number(sample.Actions), Number(sample.Magnitude, 2) + "%", Percent(sample.NoEvidence, 1), Percent(sample.AtMaximum));
                }
            }

            text.AppendLine(table.ToString());
        }

        // ---- XP per hour ---------------------------------------------------------------------------------------------------

        private static void AppendXp(StringBuilder text, ResearcherPerkStudyOptions options, IReadOnlyList<GearLevel> gear)
        {
            var middle = gear.Count > 1 ? gear[1] : gear[0];
            text.AppendLine("## Fabrication XP per real hour with the line");
            text.AppendLine();
            text.AppendLine($"Rows follow the research report: the researcher is at the tier's first rank and researches the highest-level recipe of the tier, in {middle.Name}, every job slot kept busy, and research level 1 (the highest-paying level: the job length grows with the square of the level while the XP grows by 10% per level). The experiment's XP is the share of its trials used, so a researcher with more trials who plays them all earns the same XP: perks reach research XP only through the job's length, which the time cut shortens by at most {ExperimentRules.TimeCutCapPercent}% whatever the perks, and through the active time per job, which only matters when it exceeds the job's length. \"At the cap\" recomputes the row with the full {ExperimentRules.TimeCutCapPercent}% cut and the shortest experiment, the most any build can reach.");
            text.AppendLine();

            var builds = new[] { ResearcherBuild.Unperked, ResearcherBuild.MethodicalLine, ResearcherBuild.IntuitiveLine };
            var table = new MarkdownTable(
                "Rank", "Tier", "Recipe level", "Jobs", "Unperked XP per hour", "Methodical (full line)", "Intuitive (full line)", "Most any build can reach (the cap)",
                "At-level Fabricator crafting XP per hour (0% quality)", "Most / crafting");
            (double Unperked, double Perked, double Cap) peak = (0, 0, 0);
            foreach (var rank in new[] { 10, 20, 30, 40, 50 })
            {
                var tier = CraftRankMilestones.ResearchTierForRank(rank);
                var recipeLevel = rank == 50 ? 52 : rank - 1;
                var concurrent = CraftRankMilestones.MaxConcurrentResearchJobsForRank(rank);
                var rates = builds.Select(build => XpRow(options, middle, build, rank, tier, recipeLevel, concurrent, cut: null).XpPerHour).ToList();
                var fewestTrials = builds.Min(build => build.TuningAt(rank).Trials);
                var atCap = XpRow(options, middle, ResearcherBuild.Unperked, rank, tier, recipeLevel, concurrent, ExperimentRules.TimeCutCapPercent, fewestTrials).XpPerHour;
                var craft = CraftingXpPerHour(options, rank);
                peak = (Math.Max(peak.Unperked, rates[0]), Math.Max(peak.Perked, Math.Max(rates[1], rates[2])), Math.Max(peak.Cap, atCap));
                table.Add(
                    rank.ToString(), tier.ToString(), recipeLevel.ToString(), concurrent.ToString(), Number(rates[0], 0), Number(rates[1], 0), Number(rates[2], 0),
                    Number(atCap, 0), Number(craft, 0), Percent(atCap / craft));
            }

            text.AppendLine(table.ToString());
            text.AppendLine($"The highest unperked row is {Number(peak.Unperked, 0)} XP per hour; the highest with the line is {Number(peak.Perked, 0)} ({Number(peak.Perked / peak.Unperked, 3)}x), and no build can exceed {Number(peak.Cap, 0)} ({Number(peak.Cap / peak.Unperked, 2)}x the unperked peak) because the time cut's cap does not move. The crafting column is a Fabricator craft of 15 actions (the middle of the 10-20 target) at {Number(options.CraftSecondsPerAction)} s each with the Fabricator's XP multiplier for the rank's level band (`CraftMinigameXp`), no blueprint, not a first craft, at 0% quality, the lowest crafting rate; chart XP at level is {options.ChartXp(0)}.");
            text.AppendLine();
        }

        private static ResearchXpRow XpRow(
            ResearcherPerkStudyOptions options, GearLevel gear, ResearcherBuild build, int rank, int tier, int recipeLevel, int concurrent, double? cut,
            double? actionsOverride = null)
        {
            var experimentCase = new ExperimentCase(1, tier, rank, gear);
            var sample = Play(experimentCase, build.TuningAt(rank), Focused, options.Runs, options.Seed);
            return ExperimentStudy.XpRow(
                rank, recipeLevel, 1, concurrent, actionsOverride ?? sample.Actions, cut ?? sample.TimeCut, sample.TrialsUsed * 100.0 / sample.Trials,
                options.SecondsPerAction, options.JobOverheadSeconds, options.ChartXp);
        }

        private static double CraftingXpPerHour(ResearcherPerkStudyOptions options, int rank)
        {
            var baseXp = CraftMinigameXp.ScaleBaseXp(options.ChartXp(0), CraftMinigameXp.MultiplierFor(CraftMinigameType.Fabricator, rank));
            return CraftOutcomeRules.CalculateXp(baseXp, 0, false, 0f) / (15 * options.CraftSecondsPerAction) * 3600;
        }

        private static void AppendLimitations(StringBuilder text)
        {
            text.AppendLine("## Limitations");
            text.AppendLine();
            text.AppendLine("- The focused and undecided players are stated strategies, not searches for the best play. A real player who mixes reductions or skips Refine Method lands between the rows.");
            text.AppendLine("- Seconds per action and the per-job overhead are estimates; the client evidence replaces them. XP per hour scales directly with them.");
            text.AppendLine("- The score weighs a reduction's size and a time cut against their maximums; it does not value a time cut against a credit or time reduction, or a shorter sitting against a longer one. Each Methodical turn is about six seconds, so a style's length costs far less than a shortened job saves.");
            text.AppendLine("- The recipe cost modifier (0.2 on some food) and blueprint time reductions are not modeled, as in the research report.");
            text.AppendLine("- No client run: the window is untested in the client.");
        }

        private static string Kind(int level) => level == FixedLevel ? "Fixed levels (time cut)" : "Evidence decides (reduction)";

        internal static string SignedNumber(double value) => (value > 0 ? "+" : string.Empty) + Number(value);
    }
}
