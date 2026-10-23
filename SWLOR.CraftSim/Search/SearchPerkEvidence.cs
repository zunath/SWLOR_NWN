using SWLOR.CraftSim.Gates;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.ScavengeService;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Search
{
    /// <summary>
    /// Evidence, not a gate: what each Search-board effect of the proposed Treasure Seeker purchases adds, one at a time and all together,
    /// for the perk draft (<c>design/testing/crafting-revamp/gathering-treasure-seeker-perk-line-draft.md</c>) to quote. Each row is the mean
    /// over the ranks in <see cref="Ranks"/> of the skilled player's finds per board, against Quick Search's rolls at the same rank.
    /// </summary>
    public static class SearchPerkEvidence
    {
        /// <summary>The ranks the evidence averages over: Hard Look levels 3, 4 and 5.</summary>
        public static readonly IReadOnlyList<int> Ranks = new[] { 20, 35, 50 };

        /// <summary>The style: a worse Search for a better chance that a Dig Deeper find is a rare one.</summary>
        public static readonly SearchEffects CarefulHands = new(SearchChanceDelta: -3, DeeperWeightBonus: 3);

        /// <summary>A scavenger's other perks with the style on: its worse Search and its rarer dig finds on top of theirs.</summary>
        public static SearchEffects WithCarefulHands(SearchEffects others) => others with
        {
            SearchChanceDelta = others.SearchChanceDelta + CarefulHands.SearchChanceDelta,
            DeeperWeightBonus = others.DeeperWeightBonus + CarefulHands.DeeperWeightBonus
        };

        /// <summary>The effects the draft proposes, one at a time, with the purchase each belongs to.</summary>
        public static IReadOnlyList<(string Name, SearchEffects Effects)> Probes(SearchParityOptions options) => new (string, SearchEffects)[]
        {
            ("No perks", SearchEffects.None),
            ("Keen Eye (+1 point on every Search)", new SearchEffects(SearchChanceDelta: 1)),
            ("Steady Dig (15% less dig risk)", new SearchEffects(DigRiskPercent: 85)),
            ("Fine Trowel I (rare entries weigh 1 more in a dig)", new SearchEffects(DeeperWeightBonus: 1)),
            ("Fine Trowel II (rare entries weigh 2 more in a dig)", new SearchEffects(DeeperWeightBonus: 2)),
            ("Careful Hands (style: -3 search chance, rare entries weigh 3 more in a dig)", CarefulHands),
            ("Whole line below the capstone, no style", options.TreasureSeekerLine),
            ("Whole line with Careful Hands", WithCarefulHands(options.TreasureSeekerLine))
        };

        public static IEnumerable<SearchKey> Keys(SearchParityOptions options) =>
            Probes(options).SelectMany(probe => Ranks.Select(rank => new SearchKey(rank, 1, SearchPlayerKind.Skilled, probe.Effects)));

        public static GateTable PerkMagnitudes(SearchParity parity, SearchParityOptions options)
        {
            var table = new EvidenceTable(
                "Purchase", "Skilled finds per board", "Dig Deeper finds per board", "Sifts", "Dig Deeper attempts", "Added finds against no perks", "Skilled / Quick Search");
            double baseline = double.NaN;
            foreach (var (name, effects) in Probes(options))
            {
                var samples = Ranks.Select(rank => (Rank: rank, Sample: parity.Sample(rank, 1, SearchPlayerKind.Skilled, effects))).ToList();
                var finds = samples.Average(entry => entry.Sample.Finds);
                var rare = samples.Average(entry => entry.Sample.Deeper);
                var sifts = samples.Average(entry => entry.Sample.SiftsPerBoard);
                var digs = samples.Average(entry => entry.Sample.DigsPerBoard);
                var ratio = samples.Average(entry => entry.Sample.Finds / ScavengeQuickSearch.ExpectedRolls(CraftRankMilestones.HardLookLevelForRank(entry.Rank)));
                if (double.IsNaN(baseline))
                    baseline = finds;

                table.Add(name, Number(finds, 3), Number(rare, 3), Number(sifts, 2), Number(digs, 2), name == "No perks" ? "-" : Number(finds - baseline, 3), Number(ratio, 2) + "x");
            }

            return table.ToGateTable($"Skilled player, perks one at a time (mean over Gathering ranks {string.Join(", ", Ranks)})");
        }
    }
}
