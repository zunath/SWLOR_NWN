using SWLOR.CraftSim.Gates;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.ScavengeService;
using SWLOR.Game.Server.Service.StatService;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Search
{
    /// <summary>A scavenger's effects on the Search board to play, by Gathering rank, with the name the report gives them.</summary>
    public sealed record SearchProbe(string Name, Func<int, SearchEffects> Effects);

    /// <summary>
    /// Evidence for the Treasure Seeker line on the Search board: what each purchase does to the skilled player's finds per board against
    /// Quick Search (the cap is +15% of its rolls) and to the rare items a site gives, one at a time and in the loadouts the gates run.
    /// The line's value is in the rare finds, so the table prices them: a rare item is a find of a rare entry of the site's loot table,
    /// and a rare supply of 2.0x means twice the rare items a Quick Search gives.
    /// </summary>
    public static class TreasureSeekerPerkStudy
    {
        /// <summary>The Gathering ranks the table averages over (Hard Look levels 3, 4 and 5): a style opens at 20.</summary>
        public static readonly IReadOnlyList<int> Ranks = new[] { 20, 35, 50 };

        public static SearchEffects OfLevels(IReadOnlyDictionary<PerkType, PerkDetail> perks, params (PerkType Perk, int Level)[] levels)
        {
            var totals = new Dictionary<StatType, int>();
            foreach (var (perk, level) in levels)
            {
                foreach (var bonus in perks[perk].PerkLevels[level].StatBonuses)
                {
                    totals.TryGetValue(bonus.Stat, out var current);
                    totals[bonus.Stat] = Stat.AggregateStatAdjustment(bonus.Stat, current, bonus.Calculate(0));
                }
            }

            return SearchEffects.FromStats(stat => totals.GetValueOrDefault(stat));
        }

        public static SearchEffects OfStyle(GatheringStyle style) =>
            SearchEffects.FromStats(stat => GatheringStyles.StatsOf(style).GetValueOrDefault(stat));

        public static IReadOnlyList<SearchProbe> Probes(IReadOnlyDictionary<PerkType, PerkDetail> perks)
        {
            SearchProbe Fixed(string name, SearchEffects effects) => new(name, _ => effects);

            var probes = new List<SearchProbe>
            {
                Fixed("No perks", SearchEffects.None),
                Fixed("Fine Trowel I", OfLevels(perks, (PerkType.FineTrowel, 1))),
                Fixed("Fine Trowel II", OfLevels(perks, (PerkType.FineTrowel, 2))),
                Fixed("Steady Dig", OfLevels(perks, (PerkType.SteadyDig, 1))),
                Fixed("Keen Eye", OfLevels(perks, (PerkType.KeenEye, 1))),
                Fixed("Rich Pickings", OfLevels(perks, (PerkType.RichPickings, 1))),
                Fixed("Jackpot", OfLevels(perks, (PerkType.Jackpot, 1))),
                Fixed("Careful Hands, Search half (style)", OfStyle(GatheringStyle.CarefulHands))
            };

            foreach (var loadout in GatheringLoadout.Perked.Where(loadout => loadout.TreasureSeeker || loadout.Style == GatheringStyle.CarefulHands))
                probes.Add(new SearchProbe(loadout.Name + " (every purchase the rank allows)", rank => loadout.SearchAt(perks, rank)));

            return probes;
        }

        public static IEnumerable<SearchKey> Keys(IReadOnlyList<SearchProbe> probes) =>
            probes.SelectMany(probe => Ranks.Select(rank => new SearchKey(rank, 1, SearchPlayerKind.Skilled, probe.Effects(rank))));

        /// <summary>The rare items per site of a table with the effects, and what Quick Search gives, at a rank.</summary>
        public static (double Quick, double Skilled) RarePerSite(
            LootTable loot, int rank, SearchSample sample, SearchEffects effects, int treasureHunterLevel)
        {
            var tier = CraftRankMilestones.HardLookLevelForRank(rank);
            var share = ScavengeLoot.RareShare(loot, treasureHunterLevel, false);
            var quick = ScavengeQuickSearch.ExpectedRolls(tier) * share;
            var findShare = ScavengeLoot.RareShare(loot, treasureHunterLevel, false, 0, effects.FindWeightBonus);
            var digShare = ScavengeLoot.RareShare(loot, treasureHunterLevel, true, effects.DeeperWeightBonus);
            var jackpotShare = ScavengeLoot.RareShare(loot, treasureHunterLevel, true, effects.DeeperWeightBonus + effects.JackpotWeightBonus);
            var firstFinds = effects.JackpotWeightBonus > 0 ? sample.DeeperBoardShare : 0;
            return (quick, sample.Ordinary * findShare + (sample.Deeper - firstFinds) * digShare + firstFinds * jackpotShare);
        }

        public static GateTable Table(SearchParity parity, SearchParityOptions options, IReadOnlyList<SearchProbe> probes)
        {
            var tables = SearchRareSupply.LoadTables(options.RareSupplyTables);
            var names = options.RareSupplyTables.Where(tables.ContainsKey).ToList();
            var table = new EvidenceTable(
                "Purchase or loadout", "Skilled finds per board", "Added finds", "Skilled / Quick Search (highest rank)", "Dig Deeper finds",
                "Rare items per site / Quick Search (mean of tables)", "Lowest table", "Highest table");
            double baseline = double.NaN;
            foreach (var probe in probes)
            {
                var samples = Ranks.Select(rank => (Rank: rank, Effects: probe.Effects(rank), Sample: parity.Sample(rank, 1, SearchPlayerKind.Skilled, probe.Effects(rank)))).ToList();
                var finds = samples.Average(entry => entry.Sample.Finds);
                var ratio = samples.Max(entry => entry.Sample.Finds / ScavengeQuickSearch.ExpectedRolls(CraftRankMilestones.HardLookLevelForRank(entry.Rank)));
                if (double.IsNaN(baseline))
                    baseline = finds;

                var perTable = names.Select(name =>
                {
                    double quick = 0, skilled = 0;
                    foreach (var entry in samples)
                    {
                        var (q, s) = RarePerSite(tables[name], entry.Rank, entry.Sample, entry.Effects, 0);
                        quick += q;
                        skilled += s;
                    }

                    return quick <= 0 ? double.NaN : skilled / quick;
                }).Where(value => !double.IsNaN(value)).ToList();

                table.Add(
                    probe.Name,
                    Number(finds, 3),
                    probe.Name == "No perks" ? "-" : Number(finds - baseline, 3),
                    Number(ratio, 3) + "x",
                    Number(samples.Average(entry => entry.Sample.Deeper), 3),
                    Number(perTable.Average(), 2) + "x",
                    Number(perTable.Min(), 2) + "x",
                    Number(perTable.Max(), 2) + "x");
            }

            return table.ToGateTable(
                $"The skilled player at site level 1 with each purchase, mean over Gathering ranks {string.Join(", ", Ranks)} ({parity.Runs} boards per case). " +
                $"The cap: finds at most {Number(options.SkilledMaximumRatio, 2)}x of Quick Search's rolls at every rank. Rare supply is over the tables {string.Join(", ", names)}");
        }
    }
}
