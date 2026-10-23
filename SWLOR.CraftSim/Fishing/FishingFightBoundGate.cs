using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.FishingService;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Fishing
{
    /// <summary>
    /// The bound on the fight (owner decision, October 2026): fighting a bite is optional, keeps its multiplied XP for its time and may catch
    /// better than Quick Reel, but a skilled fight's expected catch rate stays within <see cref="FishingParityOptions.FightBoundRelative"/> of
    /// Quick Reel's for the same fish and angler. The adaptive player stands for the skilled fight. The gate holds it to the bound for the
    /// angler with no perks, for each rank over the real pools, for the Angler line the perk draft proposes (both styles) and for the special
    /// rare fish with and without Trophy Catch.
    /// </summary>
    public sealed class FishingFightBoundGate : IBalanceGate
    {
        public const string GateName = "Fight bound";

        private readonly FishingParity _parity;
        private readonly FishingParityOptions _options;

        public FishingFightBoundGate(FishingParity parity, FishingParityOptions options = null)
        {
            _parity = parity ?? throw new ArgumentNullException(nameof(parity));
            _options = options ?? FishingParityOptions.Default;
        }

        public string Name => GateName;

        public GateResult Evaluate(GateContext context)
        {
            var pools = FishingCatchModel.Pools();
            Prepare(pools);

            var unperked = ByDelta();
            var pooled = ByRank(pools);
            var line = AnglerLine();
            var rare = FishingAnglerEvidence.RareFishVerdicts(_parity, _options).Where(verdict => verdict.Case.Gated).ToList();

            var over = unperked.Over.Concat(pooled.Over).Concat(line.Over)
                .Concat(rare.Where(verdict => !verdict.Within).Select(verdict =>
                    $"{verdict.Case.Name} ({Number(verdict.Landed, 0)}% against {Number(verdict.Bound, 1)}%)"))
                .ToList();
            var tables = new[] { unperked.Table, pooled.Table, line.Table, FishingAnglerEvidence.RareFish(_parity, new[] { _options.CorridorLowScore, _options.CorridorHighScore }, _options) };

            var rule = $"{Number(_options.FightBoundRelative * 100, 0)}% above Quick Reel's chance plus {Number(_options.FightBoundSlackPoints, 0)} points";
            if (over.Count == 0)
            {
                return new GateResult(
                    Name,
                    GateVerdict.Passed,
                    $"The skilled (adaptive) fight lands at most {rule} for the same fish: with no perks at every rank above the fish's level and over the real pools, " +
                    $"with the whole Angler line in either style, and on the special rare fish with and without Trophy Catch (furthest below the bound by {Number(Math.Min(unperked.Margin, Math.Min(pooled.Margin, line.Margin)), 1)} points in the tables).",
                    tables);
            }

            return new GateResult(
                Name,
                GateVerdict.Failed,
                $"The skilled (adaptive) fight lands more than {rule} in {over.Count} row(s): {string.Join("; ", over)}.",
                tables);
        }

        private void Prepare(IReadOnlyList<FishingPool> pools)
        {
            var keys = new List<CatchKey>();
            foreach (var effects in new[] { LineAnglerEffects.None, FishingAnglerEvidence.WholeLinePatient, FishingAnglerEvidence.WholeLineAggressive })
            {
                foreach (var rank in _options.ReferenceRanks)
                {
                    foreach (var delta in _options.Deltas.Where(delta => rank - delta >= 0))
                        keys.Add(new CatchKey(rank - delta, FishRarity.Common, rank, FishingParity.NominalRodTier(rank), LinePlayer.Adaptive, effects));
                }
            }

            foreach (var rank in _options.ReferenceRanks)
            {
                foreach (var delta in _options.Deltas.Where(delta => rank - delta >= 0))
                    keys.Add(new CatchKey(rank - delta, FishRarity.Common, rank, FishingParity.NominalRodTier(rank), LinePlayer.RuleOfThumb, LineAnglerEffects.None));
            }

            foreach (var rank in _options.PooledRanks)
            {
                foreach (var pool in FishingCatchRateGate.Reachable(pools, rank))
                {
                    foreach (var entry in pool.Entries)
                        keys.Add(new CatchKey(entry.Level, FishingCatchRateGate.RarityOf(entry.Fish), rank, FishingRodTier.Tier(pool.Rod), LinePlayer.Adaptive, LineAnglerEffects.None));
                }
            }

            keys.AddRange(FishingAnglerEvidence.Keys());
            _parity.Prepare(keys);
        }

        /// <summary>The adaptive player's mean landed percent against fish <paramref name="delta"/> levels below the rank, over the reference ranks.</summary>
        private double Adaptive(int delta, LineAnglerEffects effects, LinePlayer player = LinePlayer.Adaptive) =>
            _options.ReferenceRanks
                .Where(rank => rank - delta >= 0)
                .Average(rank => _parity.Catch(rank - delta, FishRarity.Common, rank, FishingParity.NominalRodTier(rank), player, effects).LandedPercent);

        private (GateTable Table, IReadOnlyList<string> Over, double Margin) ByDelta()
        {
            var table = new EvidenceTable(
                "Rank - fish level", $"Quick Reel (Willpower {_options.QuickReelWillpowerScore})", "Bound", "Fight, adaptive", "Adaptive, by rank", "Fight, rule of thumb", "Adaptive turns", "Verdict");
            var over = new List<string>();
            var margin = double.MaxValue;
            foreach (var delta in _options.Deltas)
            {
                var ranks = _options.ReferenceRanks.Where(rank => rank - delta >= 0).ToList();
                if (ranks.Count == 0)
                    continue;

                var quickReel = _options.QuickReel(delta, 0);
                var bound = _options.FightBound(quickReel);
                var adaptive = Adaptive(delta, LineAnglerEffects.None);
                var byRank = ranks.Select(rank => _parity.Catch(rank - delta, FishRarity.Common, rank, FishingParity.NominalRodTier(rank)).LandedPercent).ToList();
                var casual = Adaptive(delta, LineAnglerEffects.None, LinePlayer.RuleOfThumb);
                var turns = ranks.Average(rank => _parity.Catch(rank - delta, FishRarity.Common, rank, FishingParity.NominalRodTier(rank)).MeanTurns);
                var within = adaptive <= bound;
                margin = Math.Min(margin, bound - adaptive);
                if (!within)
                    over.Add($"{Signed(delta)} with no perks ({Number(adaptive, 0)}% against {Number(bound, 1)}%)");

                table.Add(
                    Signed(delta),
                    quickReel + "%",
                    Number(bound, 1) + "%",
                    Number(adaptive, 0) + "%",
                    $"{Number(byRank.Min(), 0)}-{Number(byRank.Max(), 0)}%",
                    Number(casual, 0) + "%",
                    Number(turns),
                    within ? "Within" : "Over");
            }

            return (table.ToGateTable(
                    $"No perks: landed per bite by rank above the fish's level, at ranks {string.Join(", ", _options.ReferenceRanks)} with the best rod each can use " +
                    $"({_parity.Runs} fights per fish). The bound is Quick Reel's chance plus {Number(_options.FightBoundRelative * 100, 0)}% plus {Number(_options.FightBoundSlackPoints, 0)} points"),
                over,
                margin);
        }

        private (GateTable Table, IReadOnlyList<string> Over, double Margin) ByRank(IReadOnlyList<FishingPool> pools)
        {
            var table = new EvidenceTable(
                "Agriculture rank", $"Quick Reel (Willpower {_options.QuickReelWillpowerScore})", "Bound", "Fight, adaptive", "Adaptive turns", "Pools", "Verdict");
            var over = new List<string>();
            var margin = double.MaxValue;
            foreach (var rank in _options.PooledRanks)
            {
                var reachable = FishingCatchRateGate.Reachable(pools, rank).ToList();
                if (reachable.Count == 0)
                    continue;

                double Fight(Func<LineCatchSample, double> read) => reachable.Average(pool => FishingCatchRateGate.Weighted(pool, entry =>
                    read(_parity.Catch(entry.Level, FishingCatchRateGate.RarityOf(entry.Fish), rank, FishingRodTier.Tier(pool.Rod)))));

                var quickReel = reachable.Average(pool => FishingCatchRateGate.Weighted(pool, entry => _options.QuickReel(rank, entry.Level)));
                var bound = _options.FightBound(quickReel);
                var adaptive = Fight(sample => sample.LandedPercent);
                var within = adaptive <= bound;
                margin = Math.Min(margin, bound - adaptive);
                if (!within)
                    over.Add($"rank {rank} over the pools ({Number(adaptive, 0)}% against {Number(bound, 1)}%)");

                table.Add(
                    rank.ToString(),
                    Number(quickReel, 0) + "%",
                    Number(bound, 1) + "%",
                    Number(adaptive, 0) + "%",
                    Number(Fight(sample => sample.MeanTurns)),
                    reachable.Count.ToString(),
                    within ? "Within" : "Over");
            }

            return (table.ToGateTable(
                    "No perks: landed per bite by Agriculture rank, over every rod and bait pool the angler can use (pools weighted equally, fish within a pool by frequency)"),
                over,
                margin);
        }

        private (GateTable Table, IReadOnlyList<string> Over, double Margin) AnglerLine()
        {
            var table = new EvidenceTable(
                "Rank - fish level", $"Quick Reel (Willpower {_options.QuickReelWillpowerScore})", "Bound", "No perks", "Whole line, Patient Angler", "Whole line, Aggressive Angler", "Verdict");
            var over = new List<string>();
            var margin = double.MaxValue;
            foreach (var delta in _options.Deltas)
            {
                if (_options.ReferenceRanks.All(rank => rank - delta < 0))
                    continue;

                var quickReel = _options.QuickReel(delta, 0);
                var bound = _options.FightBound(quickReel);
                var none = Adaptive(delta, LineAnglerEffects.None);
                var patient = Adaptive(delta, FishingAnglerEvidence.WholeLinePatient);
                var aggressive = Adaptive(delta, FishingAnglerEvidence.WholeLineAggressive);
                var worst = Math.Max(patient, aggressive);
                var within = worst <= bound;
                margin = Math.Min(margin, bound - worst);
                if (!within)
                    over.Add($"{Signed(delta)} with the whole line ({Number(worst, 0)}% against {Number(bound, 1)}%)");

                table.Add(
                    Signed(delta),
                    quickReel + "%",
                    Number(bound, 1) + "%",
                    Number(none, 0) + "%",
                    Number(patient, 0) + "%",
                    Number(aggressive, 0) + "%",
                    within ? "Within" : "Over");
            }

            return (table.ToGateTable(
                    "The Angler line below the capstone (Strong Line I, Hard Set I and a style; the perk draft): landed per bite by rank above the fish's level, " +
                    $"at ranks {string.Join(", ", _options.ReferenceRanks)} ({_parity.Runs} fights per fish)"),
                over,
                margin);
        }
    }
}
