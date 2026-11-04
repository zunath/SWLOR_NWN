using System.Text;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Report;
using SWLOR.CraftSim.Rng;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;
using SWLOR.Game.Server.Service.CraftService.Research;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Scenario.Experiment
{
    /// <summary>What the research report plays and how.</summary>
    public sealed class ResearchReportOptions
    {
        /// <summary>Experiments per case for the focused player.</summary>
        public int Runs { get; init; } = 400;

        /// <summary>Experiments per case for the adaptive player, which searches and is slower.</summary>
        public int AdaptiveRuns { get; init; } = 40;

        public int Seed { get; init; } = 1;

        /// <summary>Seconds per action in the window, an estimate until client evidence measures it.</summary>
        public double SecondsPerAction { get; init; } = 6;

        /// <summary>Seconds per job besides the experiment's actions: choosing the recipe and blueprint, confirming, and completing it.</summary>
        public double JobOverheadSeconds { get; init; } = 30;

        /// <summary>The Fabrication gear levels (none, mid, best) to play.</summary>
        public IReadOnlyList<GearLevel> Gear { get; init; }

        /// <summary>The XP chart: XP for an activity whose level is the given amount above the researcher's rank.</summary>
        public Func<int, int> ChartXp { get; init; } = ServerXp.ForDelta;
    }

    /// <summary>
    /// Builds the research Experiment's evidence report: whether it takes 5-8 turns, what its bonuses pay against today's dice and how
    /// that changes the supply of licensed runs and reductions, the time it cuts, and the Fabrication XP per real hour it and its jobs
    /// give. Every figure is repeatable from the seed.
    /// </summary>
    public static class ResearchReport
    {
        private static readonly int[] FixedTierRanks = { 10, 20, 30, 40, 50 };

        public static string Build(ResearchReportOptions options, string command)
        {
            var gear = options.Gear ?? throw new ArgumentException("The report needs gear levels.", nameof(options));
            var text = new StringBuilder();

            text.AppendLine("# Crafting revamp: the research Experiment");
            text.AppendLine();
            text.AppendLine($"Command: `{command}`");
            text.AppendLine();
            AppendInputs(text, options, gear);
            AppendLength(text, options, gear);
            AppendMagnitude(text, options, gear);
            AppendTimeCut(text, options, gear);
            AppendXp(text, options, gear);
            AppendLimitations(text);
            return text.ToString();
        }

        private static void AppendInputs(StringBuilder text, ResearchReportOptions options, IReadOnlyList<GearLevel> gear)
        {
            text.AppendLine("## Inputs");
            text.AppendLine();
            text.AppendLine($"- Seed {options.Seed}; {options.Runs} experiments per case for the focused player and {options.AdaptiveRuns} for the adaptive player. Each experiment's dice come from its own stream of the seed.");
            text.AppendLine($"- Starting values (`ExperimentRules`): {ExperimentRules.Trials} trials; Test chance {ExperimentRules.BaseTestChancePercent}% plus one point per Fabrication rank past the recipe tier's required rank, up to {ExperimentRules.RankChanceBonusMaximum}; Refine Method +{ExperimentRules.RefineChanceBonusPercent} points for the next Test at {ExperimentRules.RefineCp} CP; Analyze {ExperimentRules.TimeCutPercentPerEvidence}% of the job's time per piece of spare evidence, capped at {ExperimentRules.TimeCutCapPercent}%; evidence pays today's dice minimum at 1 and maximum at {ResearchOutcomeRules.EvidenceForMaximum}.");
            text.AppendLine($"- CP is gear CP plus 0.75 per rank with no rank passive (the Experiment's only CP sink is Refine Method). Gear: {string.Join("; ", gear.Select(level => $"{level.Name} = {level.CpBonus} CP"))}.");
            text.AppendLine("- Focused player: at a random level every Test goes to one chosen candidate; at a fixed level every Test banks evidence and the last trial analyzes it; it refines its method before every Test it can afford.");
            text.AppendLine("- Adaptive player: `AdaptivePolicy` over the previews (depth 3), valuing evidence only.");
            text.AppendLine($"- Time: {Number(options.SecondsPerAction)} s per action (an estimate until the client evidence measures it) plus {Number(options.JobOverheadSeconds)} s per job for choosing the recipe and completing it.");
            text.AppendLine();
        }

        private static void AppendLength(StringBuilder text, ResearchReportOptions options, IReadOnlyList<GearLevel> gear)
        {
            var target = LengthTarget.ResearchExperiment;
            text.AppendLine("## Length");
            text.AppendLine();
            text.AppendLine($"Target: {target}. Actions are the player's turns: Tests, Analyzes and Refine Methods (Conclude would only shorten it). Research level 1 stands for the fixed levels and 3 for the random levels (3, 5 and 8), which are played both as a reduction decided by evidence and with licensed runs rolled (a fixed-style experiment).");
            text.AppendLine();

            var table = new MarkdownTable("Level kind", "Tier", "Rank", "Gear", "CP", "Focused: mean actions", "Focused: Refines", "Adaptive: mean actions", "Verdict");
            var failures = new List<string>();
            foreach (var (level, runs) in new[] { (1, 0), (3, 0), (3, 2) })
            {
                foreach (var (tier, rank) in TierRanks())
                {
                    foreach (var gearLevel in gear)
                    {
                        var experimentCase = new ExperimentCase(level, tier, rank, gearLevel, runs);
                        var focused = ExperimentStudy.Play(experimentCase, () => new ExperimentFocusedPolicy(ResearchCandidate.CreditReduction), ResearchCandidate.CreditReduction, options.Runs, options.Seed);
                        var adaptive = PlayAdaptive(experimentCase, options);
                        var worst = Math.Max(focused.MeanActions, adaptive);
                        var passed = target.Contains(focused.MeanActions) && target.Contains(adaptive);
                        if (!passed)
                            failures.Add($"{Kind(level, runs)} tier {tier} rank {rank} {gearLevel.Name} ({Number(focused.MeanActions)} / {Number(adaptive)})");

                        table.Add(
                            Kind(level, runs), tier.ToString(), rank.ToString(), gearLevel.Name, MaxCp(experimentCase).ToString(),
                            Number(focused.MeanActions), Number(focused.MeanRefines, 2), Number(adaptive), passed ? "Passed" : $"Outside ({Number(worst)})");
                    }
                }
            }

            text.AppendLine(table.ToString());
            text.AppendLine(failures.Count == 0
                ? $"Verdict: every case takes {target} for both players."
                : $"Verdict: outside {target}: {string.Join("; ", failures)}.");
            text.AppendLine();
        }

        private static void AppendMagnitude(StringBuilder text, ResearchReportOptions options, IReadOnlyList<GearLevel> gear)
        {
            text.AppendLine("## Reduction size against today's dice (random levels 3, 5 and 8)");
            text.AppendLine();
            text.AppendLine("Owner decision (October 2026): licensed runs stay random. When the experiment opens, the level rolls whether it grants licensed runs at today's weight (300 of 1100, or 310 of 1150 with an enhancement slot) and today's d3; the experiment then earns only the time cut, XP and the extra-bonus chance. Otherwise the level grants a reduction and evidence decides between credit reduction and time reduction and sizes the winner, never above today's d10. \"Mean size\" counts an experiment with no evidence as 0 (it falls back to today's roll between the two); \"Mean size with evidence\" leaves those out.");
            text.AppendLine();

            var table = new MarkdownTable(
                "Candidate", "Tier", "Rank", "Gear", "Refine", "Test chance", "Mean evidence", "No evidence", "Mean size", "Mean size with evidence",
                "Today: dice average", "Experiment / dice average", "Reaches the maximum");
            var summary = new List<(ResearchCandidate Candidate, double Ratio)>();
            foreach (var candidate in new[] { ResearchCandidate.CreditReduction, ResearchCandidate.TimeReduction })
            {
                foreach (var (tier, rank) in MagnitudeCases())
                {
                    foreach (var level in gear)
                    {
                        foreach (var refine in new[] { true, false })
                        {
                            if (!refine && level.Tier != gear[0].Tier)
                                continue;

                            var experimentCase = new ExperimentCase(3, tier, rank, level);
                            var sample = ExperimentStudy.Play(
                                experimentCase, () => new ExperimentFocusedPolicy(candidate, refine), candidate, options.Runs, options.Seed);
                            var dice = ResearchGrantRules.AverageDice(candidate);
                            var ratio = sample.MeanMagnitudeWithEvidence / dice;
                            if (refine)
                                summary.Add((candidate, ratio));

                            table.Add(
                                ResearchGrantRules.CandidateName(candidate, 3), tier.ToString(), rank.ToString(), level.Name, refine ? "yes" : "no",
                                $"{experimentCase.CreateSetup().BaseTestChancePercent}%", Number(sample.MeanTargetEvidence, 2),
                                Percent(sample.NoEvidenceShare, 1), Number(sample.MeanTargetMagnitude, 2), Number(sample.MeanMagnitudeWithEvidence, 2),
                                Number(dice), Number(ratio, 2) + "x", Percent(sample.MaximumShare));
                        }
                    }
                }
            }

            text.AppendLine(table.ToString());
            AppendSupply(text, summary);
        }

        private static void AppendSupply(StringBuilder text, List<(ResearchCandidate Candidate, double Ratio)> summary)
        {
            text.AppendLine("### Expected supply per random level");
            text.AppendLine();
            text.AppendLine("Today one random level adds, on average, the weighted mix below. Under the Experiment the licensed runs are still rolled (same odds, same d3), so their supply is exactly today's. The rest of the time the level grants a reduction, and the player picks the kind with their evidence: a player who always wants one kind gets that kind's size whenever the level is a reduction, and none of the other kind. The \"Experiment\" column is the chance the level is a reduction times that kind's mean size across the focused cases above (with Refine Method, evidence gathered).");
            text.AppendLine();

            var runsShare = ResearchGrantRules.TodayWeightShare(ResearchCandidate.LicensedRuns, false);
            var runsToday = runsShare * ResearchGrantRules.AverageDice(ResearchCandidate.LicensedRuns);
            var table = new MarkdownTable(
                "Kind", "Today: expected per random level", "Experiment: expected per random level", "Change in that kind's supply", "Dice maximum");
            table.Add(
                "Licensed Runs (rolled)", Number(runsToday, 3) + " runs", Number(runsToday, 3) + " runs", "1.0x (unchanged)",
                ResearchGrantRules.LicensedRunsMaximum + " runs");
            foreach (var candidate in new[] { ResearchCandidate.CreditReduction, ResearchCandidate.TimeReduction })
            {
                var today = ResearchGrantRules.TodayWeightShare(candidate, false) * ResearchGrantRules.AverageDice(candidate);
                var ratios = summary.Where(entry => entry.Candidate == candidate).Select(entry => entry.Ratio).ToList();
                var meanSize = ratios.Average() * ResearchGrantRules.AverageDice(candidate);
                var experiment = (1 - runsShare) * meanSize;
                table.Add(
                    ResearchGrantRules.CandidateName(candidate, 3) + " (if always chosen)",
                    Number(today, 2) + "%",
                    Number(experiment, 2) + "%",
                    $"{Number(experiment / today, 1)}x ({Signed((int)Math.Round((experiment / today - 1) * 100))}%)",
                    ResearchGrantRules.ReductionMaximum + "%");
            }

            text.AppendLine(table.ToString());
            text.AppendLine($"Licensed runs: today a random level rolls runs {Percent(runsShare, 1)} of the time for 1-3 (mean 2), so {Number(runsToday, 3)} runs per random level on average; the Experiment rolls the same chance and die when it opens, so the expected licensed runs per blueprint are identical (a test enumerates every roll of both). The power ceiling is unchanged for every kind: one bonus per random level and never above the dice maximum. The reduction supply rises because the player chooses the kind and the evidence sizes it near the die's average instead of a one-in-two draw; it falls back toward today's if the player mixes kinds across levels 3, 5 and 8.");
            text.AppendLine();
        }

        private static void AppendTimeCut(StringBuilder text, ResearchReportOptions options, IReadOnlyList<GearLevel> gear)
        {
            text.AppendLine("## Time cut (fixed levels)");
            text.AppendLine();
            text.AppendLine($"All of a fixed level's evidence is spare. The focused player Tests and Analyzes with the last trial. The cap is {ExperimentRules.TimeCutCapPercent}% of the job.");
            text.AppendLine();

            var table = new MarkdownTable("Tier", "Rank", "Gear", "Test chance", "Mean evidence analyzed", "Mean time cut", "Reaches the cap");
            foreach (var (tier, rank) in TierRanks())
            {
                foreach (var level in gear)
                {
                    var experimentCase = new ExperimentCase(1, tier, rank, level);
                    var sample = ExperimentStudy.Play(
                        experimentCase, () => new ExperimentFocusedPolicy(ResearchCandidate.FixedGrant), ResearchCandidate.FixedGrant, options.Runs, options.Seed);
                    var capShare = CapShare(experimentCase, options);
                    table.Add(
                        tier.ToString(), rank.ToString(), level.Name, $"{experimentCase.CreateSetup().BaseTestChancePercent}%",
                        Number(sample.MeanTimeCutPercent / ExperimentRules.TimeCutPercentPerEvidence, 2), Number(sample.MeanTimeCutPercent) + "%", Percent(capShare));
                }
            }

            text.AppendLine(table.ToString());
        }

        private static double CapShare(ExperimentCase experimentCase, ResearchReportOptions options)
        {
            var setup = experimentCase.CreateSetup();
            var atCap = 0;
            for (var run = 0; run < options.Runs; run++)
            {
                var random = SimMinigameRandom.ForRun(
                    options.Seed, ExperimentStudy.StudyStream, 7, experimentCase.Tier, experimentCase.Rank, experimentCase.Gear.Tier, run);
                var engine = new ExperimentEngine(random, setup);
                CraftPlayer.Play(engine, new ExperimentFocusedPolicy(ResearchCandidate.FixedGrant));
                if (engine.TimeCutPercent >= setup.TimeCutCap)
                    atCap++;
            }

            return atCap / (double)options.Runs;
        }

        private static void AppendXp(StringBuilder text, ResearchReportOptions options, IReadOnlyList<GearLevel> gear)
        {
            text.AppendLine("## Fabrication XP per real hour");
            text.AppendLine();
            text.AppendLine(
                $"XP is the experiment ({ResearchXpRules.ExperimentXpPercent}% of the chart XP at full effort) plus completion ({ResearchXpRules.CompletionXpPercent}%), each raised {ResearchXpRules.LevelBonusPercentPerLevel}% per research level reached; the chart XP is `Skill.GetDeltaXP(recipe level - Fabrication rank)`, 0 once the recipe is 5 or more ranks below the researcher. " +
                "The values are named constants in `ResearchXpRules`, in one place, so a ceiling can be applied there.");
            text.AppendLine();
            text.AppendLine(
                "Each row is the best case for a tier: the researcher is at the tier's first rank and researches the highest-level recipe of the tier, which is where the chart pays most. " +
                "XP per hour assumes every job slot is kept busy (one at ranks 10-24, two from 25, three from 50), each job restarted the moment it completes, with the experiment's mean time cut. A job's timer starts before its experiment opens, so the experiment runs inside the job's length; " +
                "the researcher still plays only one experiment at a time, so a round takes at least the active time per job (the experiment's actions plus the overhead) times the concurrent jobs.");
            text.AppendLine();

            var middle = gear.Count > 1 ? gear[1] : gear[0];
            var rows = new List<ResearchXpRow>();
            var table = new MarkdownTable(
                "Tier", "Rank", "Recipe level", "Research level", "Concurrent jobs", "Chart XP", "Experiment XP", "Completion XP", "Job length (after cut)", "Active time per job", "XP per real hour");
            foreach (var rank in FixedTierRanks)
            {
                var tier = CraftRankMilestones.ResearchTierForRank(rank);
                var recipeLevel = rank == 50 ? 52 : rank - 1;
                var concurrent = CraftRankMilestones.MaxConcurrentResearchJobsForRank(rank);
                foreach (var researchLevel in new[] { 1, 5, 10 })
                {
                    var experimentCase = new ExperimentCase(researchLevel, tier, rank, middle);
                    var sample = ExperimentStudy.Play(
                        experimentCase,
                        () => new ExperimentFocusedPolicy(ResearchCandidate.CreditReduction),
                        ResearchCandidate.CreditReduction,
                        options.Runs,
                        options.Seed);
                    var row = ExperimentStudy.XpRow(
                        rank, recipeLevel, researchLevel, concurrent, sample.MeanActions, sample.MeanTimeCutPercent,
                        sample.MeanTrialsUsed * 100.0 / ExperimentRules.Trials, options.SecondsPerAction, options.JobOverheadSeconds, options.ChartXp);
                    rows.Add(row);
                    table.Add(
                        tier.ToString(), rank.ToString(), recipeLevel.ToString(), researchLevel.ToString(), concurrent.ToString(), row.BaseXp.ToString(),
                        row.ExperimentXp.ToString(), row.CompletionXp.ToString(), Duration(row.JobSeconds), Duration(row.ActiveSeconds),
                        Number(row.XpPerHour, 0));
                }
            }

            text.AppendLine(table.ToString());

            var craft = CraftingXpPerHour(options.ChartXp, options.SecondsPerAction);
            text.AppendLine("### Against at-level Fabrication crafting");
            text.AppendLine();
            text.AppendLine(
                $"At level (recipe level equal to rank), chart XP is {options.ChartXp(0)}. A craft of 15 actions (the middle of the 10-20 target) at {Number(options.SecondsPerAction)} s each takes {Number(15 * options.SecondsPerAction)} s and pays " +
                $"{Number(craft.Low, 0)} XP per hour at 0% quality, {Number(craft.Middle, 0)} at 50% and {Number(craft.High, 0)} at 100% (`CraftOutcomeRules.CalculateXp`, no blueprint, not a first craft).");
            text.AppendLine();
            var peak = rows.OrderByDescending(row => row.XpPerHour).First();
            text.AppendLine(
                $"The highest research row above is {Number(peak.XpPerHour, 0)} XP per hour (tier {CraftRankMilestones.ResearchTierForRank(peak.Rank)}, rank {peak.Rank}, recipe level {peak.RecipeLevel}, research level {peak.ResearchLevel}, {peak.ConcurrentJobs} concurrent job(s)), " +
                $"{Number(peak.XpPerHour / craft.Low * 100, 0)}% of the 0%-quality at-level crafting rate and {Number(peak.XpPerHour / craft.Middle * 100, 0)}% of the 50%-quality rate. " +
                "Research XP falls with research level because the job length grows with the square of the level while the XP grows by 10% per level.");
            text.AppendLine();
        }

        private static (double Low, double Middle, double High) CraftingXpPerHour(Func<int, int> chartXp, double secondsPerAction)
        {
            var baseXp = chartXp(0);
            return (Rate(baseXp, 0f), Rate(baseXp, 0.5f), Rate(baseXp, 1f));

            double Rate(int xp, float quality) =>
                CraftOutcomeRules.CalculateXp(xp, 0, false, quality) / (15 * secondsPerAction) * 3600;
        }

        private static void AppendLimitations(StringBuilder text)
        {
            text.AppendLine("## Limitations");
            text.AppendLine();
            text.AppendLine("- Seconds per action and the per-job overhead are estimates; the client evidence replaces them. XP per hour scales directly with them.");
            text.AppendLine("- The focused player is a stated strategy, not a search for the best play. The adaptive player values evidence only. A real player who mixes candidates or skips Refine Method will land between the rows.");
            text.AppendLine("- Tier 1-5 rank cases use the lowest rank that can research the tier and rank 50. Ranks between those add up to 15 points of Test chance (one per rank past the tier's required rank), which raises evidence a little and leaves the length unchanged.");
            text.AppendLine("- The recipe cost modifier (0.2 on some food) lengthens a job by up to that share and is not modeled; blueprint time reductions (up to 30%) shorten it and are not modeled.");
            text.AppendLine("- Perks are not modeled; the Researcher line is a draft (`fabrication-researcher-perk-line-draft.md`).");
            text.AppendLine("- No client run: the window is untested in the client.");
        }

        private static double PlayAdaptive(ExperimentCase experimentCase, ResearchReportOptions options)
        {
            var setup = experimentCase.CreateSetup();
            double actions = 0;
            for (var run = 0; run < options.AdaptiveRuns; run++)
            {
                var random = SimMinigameRandom.ForRun(
                    options.Seed, SimStreams.Adaptive, experimentCase.ResearchLevel, experimentCase.Tier, experimentCase.Rank, experimentCase.Gear.Tier, run);
                var policySeed = unchecked((int)SimRandom.ForRun(
                    options.Seed, SimStreams.AdaptivePolicy, experimentCase.ResearchLevel, experimentCase.Tier, experimentCase.Rank, run).NextUInt64());
                var engine = new ExperimentEngine(random, setup);
                actions += CraftPlayer.Play(engine, new AdaptivePolicy(AdaptivePolicyOptions.Default, policySeed)).Actions;
            }

            return actions / options.AdaptiveRuns;
        }

        /// <summary>Each tier at the lowest rank that can research it, then the tiers below 5 again at rank 50.</summary>
        private static IEnumerable<(int Tier, int Rank)> TierRanks()
        {
            for (var tier = 1; tier <= 5; tier++)
                yield return (tier, CraftRankMilestones.RequiredFabricationRankForResearchTier(tier));

            yield return (1, 50);
            yield return (3, 50);
        }

        private static IEnumerable<(int Tier, int Rank)> MagnitudeCases()
        {
            yield return (1, 10);
            yield return (3, 30);
            yield return (5, 50);
            yield return (1, 50);
        }

        private static int MaxCp(ExperimentCase experimentCase) => experimentCase.CreateSetup().MaxCp;

        private static string Kind(int level, int licensedRuns) =>
            level == 1 ? "Fixed (1)" : licensedRuns > 0 ? "Random (3), runs rolled" : "Random (3), reductions";

        private static string Duration(double seconds)
        {
            var span = TimeSpan.FromSeconds(seconds);
            return span.TotalHours >= 1
                ? $"{Number(span.TotalHours)} h"
                : span.TotalMinutes >= 1 ? $"{Number(span.TotalMinutes)} min" : $"{Number(span.TotalSeconds, 0)} s";
        }
    }
}
