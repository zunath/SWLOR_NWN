using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Gates;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Harvesting
{
    /// <summary>A gatherer's effects on the Prospecting session to play, by Gathering rank, with the name the report gives them.</summary>
    public sealed record ProspectorProbe(string Name, Func<int, ProspectingEffects> Effects);

    /// <summary>
    /// Evidence for the Prospector and Treasure Seeker lines on the Prospecting session: what each purchase does to the skilled session's
    /// items, gems, charges, time and length, one at a time and in the loadouts the gates run, over the module's node tiers. The cap
    /// checks are the economy-parity gate's (<see cref="HarvestingYieldGate"/>): a session of any plan stays within +15% of today's items
    /// and gems and spends at most 1.5 times today's charges per node.
    /// </summary>
    public static class ProspectorPerkStudy
    {
        /// <summary>The Gathering ranks the one-at-a-time table plays: a style opens at 20, the rank-50 perks at 50.</summary>
        public static readonly IReadOnlyList<int> Ranks = new[] { 20, 30, 40, 50 };

        /// <summary>The effects of one level of a perk, whatever the rank (the study isolates the effect from the schedule).</summary>
        public static ProspectingEffects OfLevel(IReadOnlyDictionary<PerkType, PerkDetail> perks, PerkType perk, int level) =>
            FromLevels(perks, (perk, level));

        /// <summary>The effects of several perk levels together.</summary>
        public static ProspectingEffects FromLevels(IReadOnlyDictionary<PerkType, PerkDetail> perks, params (PerkType Perk, int Level)[] levels)
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

            return ProspectingEffects.FromStats(stat => totals.GetValueOrDefault(stat));
        }

        /// <summary>A style's effects: what its status effect carries.</summary>
        public static ProspectingEffects OfStyle(GatheringStyle style) =>
            ProspectingEffects.FromStats(stat => GatheringStyles.StatsOf(style).GetValueOrDefault(stat));

        /// <summary>The purchases one at a time, as the draft quotes them, then the loadouts the gates run.</summary>
        public static IReadOnlyList<ProspectorProbe> Probes(IReadOnlyDictionary<PerkType, PerkDetail> perks)
        {
            ProspectorProbe Fixed(string name, ProspectingEffects effects) => new(name, _ => effects);

            var probes = new List<ProspectorProbe>
            {
                Fixed("No perks", ProspectingEffects.None),
                Fixed("Efficient Strikes I", OfLevel(perks, PerkType.EfficientStrikes, 1)),
                Fixed("Efficient Strikes II", OfLevel(perks, PerkType.EfficientStrikes, 2)),
                Fixed("Rich Veins I", OfLevel(perks, PerkType.RichVeins, 1)),
                Fixed("Rich Veins II", OfLevel(perks, PerkType.RichVeins, 2)),
                Fixed("Core Sample", OfLevel(perks, PerkType.CoreSample, 1)),
                Fixed("Mother Lode", OfLevel(perks, PerkType.MotherLode, 1)),
                Fixed("Strip Mining (style)", OfStyle(GatheringStyle.StripMining)),
                Fixed("Sharp Chisel I", OfLevel(perks, PerkType.SharpChisel, 1)),
                Fixed("Sharp Chisel II", OfLevel(perks, PerkType.SharpChisel, 2)),
                Fixed("Deep Scan", OfLevel(perks, PerkType.DeepScan, 1))
            };

            foreach (var loadout in GatheringLoadout.Perked)
                probes.Add(new ProspectorProbe(loadout.Name + " (every purchase the rank allows)", rank => loadout.ProspectingAt(perks, rank)));

            return probes;
        }

        /// <summary>What one probe came to over the study's cells.</summary>
        public sealed record Row(
            string Name,
            double SkilledItems,
            double CeilingItems,
            string CeilingCase,
            double CeilingGems,
            string CeilingGemsCase,
            double SkilledCharges,
            double SkilledSeconds,
            double SkilledActions,
            double Collapsed,
            double SkilledGems);

        /// <summary>Plays every probe at the study's cells and returns its row.</summary>
        public static IReadOnlyList<Row> Play(
            IReadOnlyList<HarvestNodeType> nodes,
            IReadOnlyDictionary<PerkType, PerkDetail> perks,
            int runs,
            int seed,
            IReadOnlyList<ProspectorProbe> probes,
            IReadOnlyList<int> ranks,
            IReadOnlyList<int> tiers)
        {
            var rows = new List<Row>();
            foreach (var probe in probes)
            {
                var options = HarvestingParityOptions.Default with { Ranks = ranks, EffectsOverride = probe.Effects, Perks = perks };
                var parity = new HarvestingParity(runs, seed, nodes, options);
                parity.Prepare();
                var today = HarvestingParity.TodayItemsPerNode;
                double items = 0, seconds = 0, actions = 0, collapsed = 0, gems = 0, charges = 0;
                double ceiling = 0, ceilingGems = 0;
                string ceilingCase = string.Empty, ceilingGemsCase = string.Empty;
                var cells = 0;
                var gemCells = 0;
                foreach (var tier in parity.Tiers.Where(tier => tiers.Contains(tier)))
                {
                    foreach (var rank in parity.RanksFor(tier))
                    {
                        var skilled = parity.Skilled(tier, rank);
                        var ceilingMean = parity.Ceiling(tier, rank).Mean.Items / today;
                        if (ceilingMean > ceiling)
                        {
                            ceiling = ceilingMean;
                            ceilingCase = $"tier {tier} / rank {rank}";
                        }

                        var todayGems = parity.TodayGemsPerNode(tier);
                        if (todayGems > 0)
                        {
                            var (_, gemRatio) = parity.GemCeiling(tier, rank);
                            gems += skilled.Mean.Gems / todayGems;
                            gemCells++;
                            if (gemRatio > ceilingGems)
                            {
                                ceilingGems = gemRatio;
                                ceilingGemsCase = $"tier {tier} / rank {rank}";
                            }
                        }

                        items += skilled.Mean.Items / today;
                        charges = Math.Max(charges, skilled.Mean.Charges / today);
                        seconds += skilled.Mean.Seconds;
                        actions += skilled.Mean.Actions;
                        collapsed += skilled.Mean.Collapsed;
                        cells++;
                    }
                }

                rows.Add(new Row(
                    probe.Name, items / cells, ceiling, ceilingCase, ceilingGems, ceilingGemsCase, charges, seconds / cells, actions / cells,
                    collapsed / cells, gemCells == 0 ? 0 : gems / gemCells));
            }

            return rows;
        }

        /// <summary>The one-at-a-time table: each probe against no perks.</summary>
        public static GateTable Table(IReadOnlyList<Row> rows, int runs, IReadOnlyList<int> ranks, IReadOnlyList<int> tiers)
        {
            var baseline = rows.First();
            var table = new EvidenceTable(
                "Purchase or loadout", "Skilled items / today", "Highest of any plan / today", "Where", "Highest gems / today", "Skilled charges / today (worst case)",
                "Skilled seconds per node", "Skilled actions per node", "Nodes collapsed");
            foreach (var row in rows)
            {
                table.Add(
                    row.Name,
                    Number(row.SkilledItems, 3),
                    Number(row.CeilingItems, 3),
                    row.CeilingCase,
                    Number(row.CeilingGems, 3),
                    Number(row.SkilledCharges, 2),
                    $"{Number(row.SkilledSeconds, 1)} ({Number(row.SkilledSeconds / baseline.SkilledSeconds, 2)}x)",
                    Number(row.SkilledActions, 2),
                    Percent(row.Collapsed, 1));
            }

            return table.ToGateTable(
                $"The skilled session with each purchase, over node tiers {string.Join(", ", tiers)} and Gathering ranks {string.Join(", ", ranks)} ({runs} nodes per plan and case). " +
                "The cap: items and gems at most 1.15x of today's, charges at most 1.5x.");
        }
    }
}
