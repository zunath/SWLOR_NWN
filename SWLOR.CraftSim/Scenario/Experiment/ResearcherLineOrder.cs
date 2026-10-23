using System.Collections.Concurrent;
using System.Text;
using SWLOR.CraftSim.Report;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;
using SWLOR.Game.Server.Service.PerkService;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Scenario.Experiment
{
    /// <summary>
    /// Which order of the Researcher line's shared purchases keeps Methodical and Intuitive balanced while every purchase does something
    /// the moment it unlocks. A purchase that spends CP (Steady Hands, Second Opinion, Peer Review) does nothing for a researcher with
    /// no gear until their rank alone pays for it, so those sit at the ranks where it does; the rest fill the early ranks. Each
    /// arrangement is built from the same per-purchase effects the perk definitions have, with no definition edited, and judged with
    /// the style-balance thresholds of <see cref="ResearcherPerkStudy"/>.
    /// </summary>
    public static class ResearcherLineOrder
    {
        /// <summary>The ranks of the shared purchases (the styles are at 20 and Breakthrough at 50).</summary>
        public static readonly IReadOnlyList<int> SlotRanks = new[] { 5, 10, 15, 25, 30, 35, 40, 45 };

        /// <summary>One order of the shared purchases over <see cref="SlotRanks"/>; a perk named twice is its first rank, then its second.</summary>
        public sealed record Arrangement(IReadOnlyList<PerkType> Slots)
        {
            public override string ToString() => string.Join(", ", SlotRanks.Select((rank, index) => $"{rank} {Short(Slots, index)}"));

            public bool Equals(Arrangement other) => other != null && Slots.SequenceEqual(other.Slots);

            public override int GetHashCode() => Slots.Aggregate(17, (hash, perk) => hash * 31 + (int)perk);
        }

        /// <summary>How much the order has to guarantee about a researcher whose gear gives no CP.</summary>
        public enum Rule
        {
            /// <summary>No rule: every order.</summary>
            None,

            /// <summary>Second Opinion sits at a rank where a Refine Method can be paid for.</summary>
            SecondOpinion,

            /// <summary>Steady Hands, Second Opinion and Peer Review all sit at ranks where the CP they spend can be paid for.</summary>
            EveryPurchase
        }

        /// <summary>One arrangement judged.</summary>
        public sealed record Result(
            Arrangement Arrangement, bool EveryPurchaseUseful, int Failed, double FixedGap, double ContestedGap, double LargestGap);

        /// <summary>The order the perk definitions have (the first order in a report, for reference).</summary>
        public static Arrangement Current { get; } = new(new[]
        {
            PerkType.FieldNotes, PerkType.EfficientAnalysis, PerkType.FieldNotes, PerkType.BroaderHypotheses, PerkType.EfficientAnalysis,
            PerkType.SteadyHands, PerkType.PeerReview, PerkType.SecondOpinion
        });

        private static string Short(IReadOnlyList<PerkType> slots, int index)
        {
            var perk = slots[index];
            var level = slots.Take(index).Count(earlier => earlier == perk) + 1;
            return perk switch
            {
                PerkType.FieldNotes => level == 1 ? "FN I" : "FN II",
                PerkType.EfficientAnalysis => level == 1 ? "EA I" : "EA II",
                PerkType.BroaderHypotheses => "BH",
                PerkType.SteadyHands => "SH",
                PerkType.SecondOpinion => "SO",
                PerkType.PeerReview => "PR",
                _ => perk.ToString()
            };
        }

        /// <summary>
        /// The numbers the Experiment reads for a researcher of this rank with this style and the shared purchases in this order,
        /// as <see cref="ExperimentTuning.FromStatTotals"/> would from the perk definitions' effects.
        /// </summary>
        public static ExperimentTuning TuningAt(Arrangement arrangement, bool methodical, int rank, int fieldNotesPoints = 10)
        {
            var trials = methodical ? 2 : -1;
            var chance = methodical ? -15 : 20;
            var refineChance = 0;
            var refineCpReduction = 0;
            var timeCut = 0;
            var peerReview = false;
            var fieldNotes = 0;
            var analysis = 0;
            for (var index = 0; index < SlotRanks.Count; index++)
            {
                if (SlotRanks[index] > rank)
                    continue;

                switch (arrangement.Slots[index])
                {
                    case PerkType.FieldNotes:
                        fieldNotes++;
                        chance += fieldNotesPoints;
                        break;
                    case PerkType.EfficientAnalysis:
                        analysis++;
                        timeCut = analysis == 1 ? 1 : 2;
                        break;
                    case PerkType.BroaderHypotheses:
                        trials++;
                        break;
                    case PerkType.SteadyHands:
                        refineCpReduction = 5;
                        break;
                    case PerkType.SecondOpinion:
                        refineChance = 5;
                        break;
                    case PerkType.PeerReview:
                        peerReview = true;
                        break;
                }
            }

            return new ExperimentTuning(
                Math.Max(ExperimentRules.MinimumTrials, ExperimentRules.Trials + trials),
                chance,
                ExperimentRules.RefineChanceBonusPercent + refineChance,
                Math.Max(ExperimentRules.MinimumRefineCp, ExperimentRules.RefineCp - refineCpReduction),
                ExperimentRules.TimeCutPercentPerEvidence + timeCut,
                ExperimentRules.TimeCutCapPercent,
                peerReview,
                rank >= 50);
        }

        /// <summary>
        /// Whether the order meets a rule for a researcher whose gear gives no CP (<see cref="ExperimentRules.CpPerRank"/> a rank): Steady
        /// Hands needs the CP of a cheaper Refine Method, Second Opinion the CP of a Refine Method, and Peer Review its own CP.
        /// </summary>
        public static bool Meets(Arrangement arrangement, Rule rule)
        {
            if (rule == Rule.None)
                return true;

            var steadyHands = false;
            for (var index = 0; index < SlotRanks.Count; index++)
            {
                var cp = ExperimentRules.CpPerRank * SlotRanks[index];
                var refineCost = steadyHands ? ExperimentRules.RefineCp - 5 : ExperimentRules.RefineCp;
                switch (arrangement.Slots[index])
                {
                    case PerkType.SteadyHands:
                        steadyHands = true;
                        if (rule == Rule.EveryPurchase && cp < ExperimentRules.RefineCp - 5)
                            return false;
                        break;
                    case PerkType.SecondOpinion:
                        if (cp < refineCost)
                            return false;
                        break;
                    case PerkType.PeerReview:
                        if (rule == Rule.EveryPurchase && cp < ExperimentRules.PeerReviewCp)
                            return false;
                        break;
                }
            }

            return true;
        }

        /// <summary>Every order of the shared purchases that meets the rule, with the current order first.</summary>
        public static IReadOnlyList<Arrangement> Candidates(Rule rule)
        {
            var results = new List<Arrangement> { Current };
            var seen = new HashSet<Arrangement> { Current };
            var all = Permutations(new[]
            {
                PerkType.FieldNotes, PerkType.FieldNotes, PerkType.BroaderHypotheses, PerkType.EfficientAnalysis, PerkType.EfficientAnalysis,
                PerkType.SteadyHands, PerkType.SecondOpinion, PerkType.PeerReview
            });
            foreach (var slots in all)
            {
                var arrangement = new Arrangement(slots);
                if (Meets(arrangement, rule) && seen.Add(arrangement))
                    results.Add(arrangement);
            }

            return results;
        }

        public static IReadOnlyList<Result> Search(
            ResearcherPerkStudyOptions options, IReadOnlyList<Arrangement> arrangements = null, int fieldNotesPoints = 10)
        {
            var gear = options.Gear ?? throw new ArgumentException("The search needs gear levels.", nameof(options));
            arrangements ??= Candidates(Rule.EveryPurchase);
            ResearcherPerkStudy.LineWith(50, 0, 0);

            // Arrangements share many tunings at a case; each (case, tuning) is played once.
            var cache = new ConcurrentDictionary<(ExperimentCase, ExperimentTuning), Lazy<ResearcherPerkStudy.Sample>>();
            ResearcherPerkStudy.Sample Play(ExperimentCase experimentCase, ExperimentTuning tuning) =>
                cache.GetOrAdd((experimentCase, tuning), key => new Lazy<ResearcherPerkStudy.Sample>(() =>
                    ResearcherPerkStudy.Play(key.Item1, key.Item2, ResearcherPerkStudy.Focused, options.Runs, options.Seed))).Value;

            var results = new ConcurrentBag<Result>();
            Parallel.ForEach(arrangements, arrangement =>
            {
                var rows = ResearcherPerkStudy.CompareStyles(
                    options, gear, rank => TuningAt(arrangement, true, rank, fieldNotesPoints), rank => TuningAt(arrangement, false, rank, fieldNotesPoints), Play);
                var fixedGap = rows.Where(row => row.Level == ResearcherPerkStudy.FixedLevel).Average(row => row.Gap);
                var contestedGap = rows.Where(row => row.Level == ResearcherPerkStudy.ContestedLevel).Average(row => row.Gap);
                results.Add(new Result(
                    arrangement, Meets(arrangement, Rule.EveryPurchase), rows.Count(row => !row.Passed), fixedGap, contestedGap,
                    Math.Max(Math.Abs(fixedGap), Math.Abs(contestedGap))));
            });

            return results.ToList();
        }

        public static string Build(ResearcherPerkStudyOptions options, string command)
        {
            var text = new StringBuilder();
            text.AppendLine("# Crafting revamp: the order of the Researcher line's shared purchases");
            text.AppendLine();
            text.AppendLine($"Command: `{command}`");
            text.AppendLine();
            text.AppendLine("## Inputs");
            text.AppendLine();
            text.AppendLine($"- Seed {options.Seed}; {options.Runs} experiments per case and build, every build on the same dice, as `researcher-perks`. The styles are Methodical +2 trials and -15 points, Intuitive -1 trial and +20 points; the effects of the shared purchases are the perk definitions' (Steady Hands -5 CP, Second Opinion +5, Peer Review, Efficient Analysis +1 then +2 points of time cut per evidence, Broader Hypotheses +1 trial, Field Notes as each setting says), and Breakthrough is the capstone at 50. Prices stay with the ranks.");
            text.AppendLine($"- A purchase that spends CP does nothing for a researcher whose gear gives no CP until their rank alone pays for it (CP is {ExperimentRules.CpPerRank} a rank): Steady Hands needs {ExperimentRules.RefineCp - 5} CP, Second Opinion the CP of a Refine Method ({ExperimentRules.RefineCp}, or {ExperimentRules.RefineCp - 5} with Steady Hands owned) and Peer Review {ExperimentRules.PeerReviewCp}. \"Second Opinion affordable\" requires only that one; \"every CP purchase affordable\" requires all three, which puts them at ranks 35, 40 and 45 and the five purchases that work without CP (Field Notes twice, Efficient Analysis twice, Broader Hypotheses) at 5, 10, 15, 25 and 30 in some order.");
            text.AppendLine("- \"Failed\" counts the six comparisons (two level kinds by three gear levels) of the styles on the whole line outside the style-balance gate's thresholds; the gaps are the mean score gap over the level kind (positive favors Methodical). A passing order fails none.");
            text.AppendLine();

            var settings = new[]
            {
                (Rule.EveryPurchase, 6), (Rule.EveryPurchase, 8), (Rule.EveryPurchase, 10), (Rule.SecondOpinion, 6), (Rule.None, 6), (Rule.None, 10)
            };
            var summary = new MarkdownTable("Rule", "Field Notes points per purchase", "Orders", "Passing", "Fewest failed");
            var sections = new StringBuilder();
            foreach (var (rule, points) in settings)
            {
                var results = Search(options, Candidates(rule), points);
                var ranked = results.OrderBy(result => result.Failed).ThenBy(result => result.LargestGap).ToList();
                summary.Add(
                    RuleName(rule), points.ToString(), results.Count.ToString(), results.Count(result => result.Failed == 0).ToString(),
                    ranked[0].Failed.ToString());
                sections.AppendLine($"## {RuleName(rule)}, Field Notes +{points} per purchase: the best orders");
                sections.AppendLine();
                sections.AppendLine(Table(ranked.Take(10)));
            }

            text.AppendLine("## Summary");
            text.AppendLine();
            text.AppendLine(summary.ToString());
            text.Append(sections);
            return text.ToString();
        }

        private static string RuleName(Rule rule) => rule switch
        {
            Rule.None => "No rule (every order)",
            Rule.SecondOpinion => "Second Opinion affordable",
            _ => "Every CP purchase affordable"
        };

        private static string Table(IEnumerable<Result> rows)
        {
            var table = new MarkdownTable("Order (rank and purchase)", "Every purchase useful at its rank", "Failed of 6", "Fixed levels: mean gap", "Evidence decides: mean gap");
            foreach (var row in rows)
            {
                table.Add(
                    row.Arrangement.ToString(), row.EveryPurchaseUseful ? "yes" : "no", row.Failed.ToString(), ResearcherPerkStudy.SignedNumber(row.FixedGap),
                    ResearcherPerkStudy.SignedNumber(row.ContestedGap));
            }

            return table.ToString();
        }

        /// <summary>Every distinct ordering of a list that may repeat items.</summary>
        private static IEnumerable<PerkType[]> Permutations(PerkType[] items)
        {
            var results = new HashSet<string>();
            var output = new List<PerkType[]>();

            void Recurse(List<PerkType> remaining, List<PerkType> chosen)
            {
                if (remaining.Count == 0)
                {
                    var key = string.Join(",", chosen);
                    if (results.Add(key))
                        output.Add(chosen.ToArray());
                    return;
                }

                for (var i = 0; i < remaining.Count; i++)
                {
                    var rest = remaining.ToList();
                    rest.RemoveAt(i);
                    Recurse(rest, chosen.Append(remaining[i]).ToList());
                }
            }

            Recurse(items.ToList(), new List<PerkType>());
            return output;
        }
    }
}
