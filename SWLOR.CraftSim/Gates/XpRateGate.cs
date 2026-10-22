using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Gates
{
    /// <summary>One row of an XP-rate comparison: a recipe band and gear tier, a node tier, a site level or a fish-level band.</summary>
    /// <param name="Group">The grouping the plan's cohort uses, such as the recipe band "21-30".</param>
    /// <param name="Label">The variant within the group, such as the gear tier "mid gear".</param>
    public sealed record XpRateCase(string Group, string Label);

    /// <summary>The time a loop costs the player: seconds for each action they take, plus fixed seconds for each run of the loop.</summary>
    /// <param name="SecondsPerAction">An estimate until the client evidence measures it, separately for each loop.</param>
    /// <param name="OverheadSecondsPerRun">Time a run costs besides its actions, such as a cast and the wait for a bite.</param>
    public readonly record struct XpLoopTiming(double SecondsPerAction, double OverheadSecondsPerRun = 0);

    /// <summary>What many runs of one loop yielded: the XP awarded and the player actions taken.</summary>
    /// <param name="TotalXp">XP over all the runs, with failures at their own XP.</param>
    /// <param name="TotalActions">Player actions over all the runs.</param>
    /// <param name="Runs">How many runs of the loop were played (crafts, nodes, sites or fights).</param>
    public readonly record struct XpLoopSample(double TotalXp, double TotalActions, int Runs)
    {
        public double XpPerRun => Runs == 0 ? 0 : TotalXp / Runs;
        public double ActionsPerRun => Runs == 0 ? 0 : TotalActions / Runs;

        public double SecondsPerRun(XpLoopTiming timing) => ActionsPerRun * timing.SecondsPerAction + timing.OverheadSecondsPerRun;

        /// <summary>XP per hour of play.</summary>
        public double XpPerHour(XpLoopTiming timing)
        {
            var seconds = SecondsPerRun(timing);
            return seconds <= 0 ? 0 : XpPerRun / seconds * 3600;
        }
    }

    /// <summary>
    /// A cohort the XP-rate gate compares: the legacy loop and its replacement played over the same cases. The item-craft cohort of
    /// the plan is <see cref="CraftXpRateCohort"/>. The gathering and fishing workstreams implement this for their activities, each
    /// pairing the legacy loop with the new one at the parity attribute medians (<see cref="GateContext.Attributes"/>):
    /// harvesting per node tier with rank equal to the vein level (XP per item), scavenging per site level with rank equal to 10 x the site
    /// level (XP per attempt, a quarter on failure), and fishing per fish-level band with rank equal to the fish level capped at 50 (XP per bite).
    /// Refining has no minigame and research XP is new, so neither is a cohort. An implementation must be safe to call from several threads.
    /// </summary>
    public interface IXpRateCohort
    {
        /// <summary>The cohort's name in the report, such as "Item crafts" or "Harvesting".</summary>
        string Name { get; }

        /// <summary>What one run is, for the report: "craft", "node", "site" or "fight".</summary>
        string RunName { get; }

        /// <summary>The player's time in the legacy loop.</summary>
        XpLoopTiming LegacyTiming { get; }

        /// <summary>The player's time in the replacement loop.</summary>
        XpLoopTiming ReplacementTiming { get; }

        IReadOnlyList<XpRateCase> Cases(GateContext context);

        /// <summary>Plays the legacy loop for the case with the context's runs and seed.</summary>
        XpLoopSample PlayLegacy(XpRateCase xpCase, GateContext context);

        /// <summary>Plays the replacement loop for the case with the context's runs and seed.</summary>
        XpLoopSample PlayReplacement(XpRateCase xpCase, GateContext context);
    }

    /// <summary>
    /// Gate 6, XP rate: at matched rank and gear, XP per hour of play in each minigame is at least today's
    /// (<see cref="GateThresholds.XpRateMinimumRatio"/>), in every case of the cohort on its own. Time is actions taken times the loop's
    /// seconds per action, so a loop that falls short shows the XP multiplier it would need; the plan scales XP up for that rather than
    /// shortening a loop below its length target.
    /// </summary>
    public sealed class XpRateGate : IBalanceGate
    {
        private readonly IXpRateCohort _cohort;
        private readonly GateThresholds _thresholds;

        public XpRateGate(IXpRateCohort cohort, GateThresholds thresholds = null)
        {
            _cohort = cohort ?? throw new ArgumentNullException(nameof(cohort));
            _thresholds = thresholds ?? GateThresholds.Default;
        }

        public string Name => GateNames.XpRate;

        public GateResult Evaluate(GateContext context)
        {
            var cases = _cohort.Cases(context);
            if (cases.Count == 0)
                return GateResult.NotRun(Name, $"The {_cohort.Name} cohort has no cases.");

            var legacyTiming = _cohort.LegacyTiming;
            var replacementTiming = _cohort.ReplacementTiming;
            var table = new EvidenceTable(
                "Group", "Variant",
                $"Legacy XP per {_cohort.RunName}", $"Legacy actions per {_cohort.RunName}", "Legacy XP per hour",
                $"New XP per {_cohort.RunName}", $"New actions per {_cohort.RunName}", "New XP per hour",
                "Ratio", "XP multiplier needed", "Verdict");

            var failures = new List<string>();
            var lowest = double.MaxValue;
            var lowestCase = string.Empty;
            foreach (var xpCase in cases)
            {
                var legacy = _cohort.PlayLegacy(xpCase, context);
                var replacement = _cohort.PlayReplacement(xpCase, context);
                var legacyRate = legacy.XpPerHour(legacyTiming);
                var newRate = replacement.XpPerHour(replacementTiming);
                var ratio = legacyRate <= 0 ? double.PositiveInfinity : newRate / legacyRate;
                var passed = ratio >= _thresholds.XpRateMinimumRatio;
                var label = $"{xpCase.Group} / {xpCase.Label}";
                if (!passed)
                    failures.Add($"{label} ({Number(ratio, 2)}x)");

                if (ratio < lowest)
                {
                    lowest = ratio;
                    lowestCase = label;
                }

                table.Add(
                    xpCase.Group,
                    xpCase.Label,
                    Number(legacy.XpPerRun, 0),
                    Number(legacy.ActionsPerRun),
                    Number(legacyRate, 0),
                    Number(replacement.XpPerRun, 0),
                    Number(replacement.ActionsPerRun),
                    Number(newRate, 0),
                    double.IsInfinity(ratio) ? "n/a" : Number(ratio, 2),
                    passed || newRate <= 0 ? "-" : Number(legacyRate / newRate, 2) + "x",
                    passed ? "Passed" : "Failed");
            }

            var tables = new[]
            {
                table.ToGateTable(
                    $"{_cohort.Name}: XP per hour against the legacy loop ({Number(legacyTiming.SecondsPerAction)} s per action legacy, " +
                    $"{Number(replacementTiming.SecondsPerAction)} s per action new; unmeasured estimates)")
            };

            return failures.Count == 0
                ? new GateResult(Name, GateVerdict.Passed,
                    $"{_cohort.Name}: XP per hour is at least {Number(_thresholds.XpRateMinimumRatio, 2)}x the legacy rate in every case (lowest {Number(lowest, 2)}x in {lowestCase}).",
                    tables)
                : new GateResult(Name, GateVerdict.Failed,
                    $"{_cohort.Name}: XP per hour falls below {Number(_thresholds.XpRateMinimumRatio, 2)}x the legacy rate in {failures.Count} case(s): {string.Join("; ", failures)}.",
                    tables);
        }
    }
}
