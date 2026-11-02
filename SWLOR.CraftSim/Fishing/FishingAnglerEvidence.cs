using SWLOR.CraftSim.Gates;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.FishingService;
using SWLOR.Game.Server.Service.PerkService;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Fishing
{
    /// <summary>
    /// The evidence behind the Angler perk line, judged by the fight-bound gate: how close the whole line (the game's own perk definitions,
    /// <see cref="AnglerBuild"/>) lands to the owner's target of about 90% of the fish at the angler's own level, whether the two rank-20
    /// styles are balanced, and how far Trophy Catch moves a rare fish against the ceiling the parity check sets. The unperked skilled fight
    /// keeps the bound of Quick Reel's chance plus 15% (<see cref="FishingParityOptions.FightBoundRelative"/>).
    /// </summary>
    public static class FishingAnglerEvidence
    {
        public static readonly IReadOnlyList<int> Ranks = AnglerPerkStudy.AverageRanks;

        /// <summary>The level of the special fish, which are all the same.</summary>
        public const int RareFishLevel = AnglerPerkStudy.RareFishLevel;

        /// <summary>The rank every rare-fish case is played at, and its rod tier.</summary>
        public const int RareFishRank = AnglerPerkStudy.RareFishRank;

        /// <summary>The fish levels, above the rank, the style comparison plays.</summary>
        public static readonly IReadOnlyList<int> StyleDeltas = new[] { -4, -3, -2, -1, 0, 1, 2 };

        /// <summary>Everything the line grants below the capstone, with the Patient Angler style, at rank 50.</summary>
        public static LineAnglerEffects WholeLinePatient => AnglerBuild.PatientBelowCapstone.EffectsAt(RareFishRank);

        /// <summary>Everything the line grants below the capstone, with the Aggressive Angler style, at rank 50.</summary>
        public static LineAnglerEffects WholeLineAggressive => AnglerBuild.AggressiveBelowCapstone.EffectsAt(RareFishRank);

        /// <summary>The rungs Trophy Catch takes off a rare fish's difficulty.</summary>
        public static int TrophyCatchRungs => AnglerBuild.Alone(PerkType.TrophyCatch, 1).EffectsAt(RareFishRank).RareFishRungReduction;

        /// <summary>The builds the fight-bound gate plays besides the unperked fight: the whole line the rank allows, in each style.</summary>
        public static IReadOnlyList<AnglerBuild> Lines => new[] { AnglerBuild.PatientLine, AnglerBuild.AggressiveLine };

        public static IEnumerable<CatchKey> Keys()
        {
            var defaults = FishingParityOptions.Default;
            foreach (var build in new[] { AnglerBuild.Unperked }.Concat(Lines))
            {
                foreach (var rank in defaults.ReferenceRanks)
                {
                    foreach (var delta in defaults.Deltas.Where(delta => rank - delta >= 0))
                    {
                        yield return new CatchKey(
                            rank - delta, FishRarity.Common, rank, FishingParity.NominalRodTier(rank), LinePlayer.Adaptive, build.EffectsAt(rank));
                    }
                }
            }

            foreach (var key in AnglerPerkStudy.Keys())
                yield return key;

            foreach (var player in new[] { LinePlayer.Adaptive, LinePlayer.RuleOfThumb })
            {
                yield return new CatchKey(RareFishLevel, FishRarity.Rare, RareFishRank, 4, player, LineAnglerEffects.None);
                yield return new CatchKey(RareFishLevel, FishRarity.Common, RareFishRank, 4, player, LineAnglerEffects.None);
                foreach (var (_, effects) in AnglerPerkStudy.RareCases())
                    yield return new CatchKey(RareFishLevel, FishRarity.Rare, RareFishRank, 4, player, effects);
            }
        }

        /// <summary>
        /// How close the whole line lands to the owner's target at the fish's own level: for each style at rank 50, where every purchase is
        /// owned, the adaptive player's catch rate must sit within <see cref="FishingParityOptions.PerkedTolerancePoints"/> of
        /// <see cref="FishingParityOptions.PerkedTargetPercent"/>. The other ranks are shown with the purchases each allows.
        /// </summary>
        public static (GateTable Table, IReadOnlyList<string> Outside, double Margin) PerkedLine(FishingParity parity, FishingParityOptions options)
        {
            var table = new EvidenceTable(
                "Rank", "Quick Reel", "No perks", "Patient Angler line", "Aggressive Angler line", "Target", "Verdict");
            var outside = new List<string>();
            var margin = double.MaxValue;
            var low = options.PerkedTargetPercent - options.PerkedTolerancePoints;
            var high = options.PerkedTargetPercent + options.PerkedTolerancePoints;

            double Landed(int rank, AnglerBuild build) =>
                parity.Catch(rank, FishRarity.Common, rank, FishingParity.NominalRodTier(rank), LinePlayer.Adaptive, build.EffectsAt(rank)).LandedPercent;

            foreach (var rank in options.ReferenceRanks)
            {
                var none = Landed(rank, AnglerBuild.Unperked);
                var patient = Landed(rank, AnglerBuild.PatientLine);
                var aggressive = Landed(rank, AnglerBuild.AggressiveLine);
                var judged = rank == RareFishRank;
                var verdict = "-";
                if (judged)
                {
                    var inside = patient >= low && patient <= high && aggressive >= low && aggressive <= high;
                    margin = Math.Min(Math.Min(patient, aggressive) - low, high - Math.Max(patient, aggressive));
                    verdict = inside ? "Within" : "Outside";
                    if (!inside)
                        outside.Add($"rank {rank} ({Number(patient, 0)}% Patient, {Number(aggressive, 0)}% Aggressive against {Number(low, 0)}-{Number(high, 0)}%)");
                }

                table.Add(
                    rank.ToString(),
                    options.QuickReel(rank, rank) + "%",
                    Number(none, 0) + "%",
                    Number(patient, 0) + "%",
                    Number(aggressive, 0) + "%",
                    judged ? $"{Number(low, 0)}-{Number(high, 0)}%" : "-",
                    verdict);
            }

            return (table.ToGateTable(
                    $"The Angler line at the fish's own level, by rank: the adaptive player with no perks and with every purchase the rank allows ({parity.Runs} fights per case). " +
                    $"The owner's target for the whole line, at rank 50 where it is complete, is about {Number(options.PerkedTargetPercent, 0)}%, judged within {Number(options.PerkedTolerancePoints, 0)} points"),
                outside,
                margin);
        }

        /// <summary>The two rank-20 styles against each other over fish from four levels above the rank to two below it.</summary>
        public static (GateTable Table, double PatientShare, double AggressiveShare, double MeanGap) StyleBalance(FishingParity parity, double tiePoints = 2.0) =>
            AnglerPerkStudy.StyleBalance(parity, tiePoints);

        /// <summary>One case of the rare-fish table: a fish, and the perks the angler brings.</summary>
        /// <param name="Name">What the row says.</param>
        /// <param name="Rarity">The fish's rarity.</param>
        /// <param name="Effects">The angler's perks.</param>
        /// <param name="Gated">True when the fight bound judges the row, false when it is shown for comparison.</param>
        public sealed record RareFishCase(string Name, FishRarity Rarity, LineAnglerEffects Effects, bool Gated);

        /// <summary>The cases the rare-fish table plays: the six special fish at rank 50 with a tier 4 rod, with and without the line and Trophy Catch.</summary>
        public static IReadOnlyList<RareFishCase> RareCases()
        {
            var cases = new List<RareFishCase>
            {
                new("Fight, a common fish of level 52 (for comparison)", FishRarity.Common, LineAnglerEffects.None, false)
            };
            // The old bound judges the fish no perk but Trophy Catch touches. With the line the fight perks reach a rare fish at half strength, which
            // the owner chose in place of a bound (October 2026), so those rows are shown, not judged.
            cases.AddRange(AnglerPerkStudy.RareCases().Select(rareCase => new RareFishCase(
                $"Fight, a rare fish: {rareCase.Name}",
                FishRarity.Rare,
                rareCase.Effects,
                rareCase.Effects.TensionCapBonus == 0 && rareCase.Effects.SetHookDrainBonus == 0 && rareCase.Effects.GiveLineDistanceReduction == 0 && rareCase.Effects.ReelDistanceBonus == 0)));
            return cases;
        }

        /// <summary>
        /// How the special fish fight at rank 50 against Quick Reel and the bound: what each case lands, the bound it is held to, and whether it
        /// is within it. The special fish take the fight perks at half strength and Trophy Catch at full strength.
        /// </summary>
        public static IReadOnlyList<(RareFishCase Case, double Landed, double Bound, bool Within)> RareFishVerdicts(FishingParity parity, FishingParityOptions options = null)
        {
            options ??= FishingParityOptions.Default;
            var bound = options.FightBound(options.QuickReel(RareFishRank, RareFishLevel));
            return RareCases()
                .Select(rareCase =>
                {
                    var landed = parity.Catch(RareFishLevel, rareCase.Rarity, RareFishRank, 4, LinePlayer.Adaptive, rareCase.Effects).LandedPercent;
                    return (rareCase, landed, bound, landed <= bound);
                })
                .ToList();
        }

        /// <summary>How the special fish fight at rank 50 against Quick Reel and today's catch chance, and how far Trophy Catch moves them.</summary>
        public static GateTable RareFish(FishingParity parity, IReadOnlyList<int> scores, FishingParityOptions options = null)
        {
            options ??= FishingParityOptions.Default;
            var quickReel = options.QuickReel(RareFishRank, RareFishLevel);
            var bound = options.FightBound(quickReel);
            var header = new List<string> { "Case" };
            header.AddRange(scores.Select(score => $"Today, Willpower {score}"));
            header.AddRange(new[]
            {
                $"Quick Reel (Willpower {options.QuickReelWillpowerScore})", "Bound", "Fight, adaptive", "Fight, rule of thumb", "Adaptive turns", "Within the bound"
            });
            var table = new EvidenceTable(header.ToArray());

            var todayRow = new List<string> { "Today (level 52 fish, rank 50)" };
            todayRow.AddRange(scores.Select(score => FishingCatchRateGate.Today(RareFishRank - RareFishLevel, score) + "%"));
            todayRow.AddRange(new[] { quickReel + "%", Number(bound, 1) + "%", "-", "-", "-", "-" });
            table.Add(todayRow.ToArray());

            foreach (var verdict in RareFishVerdicts(parity, options))
            {
                var rule = parity.Catch(RareFishLevel, verdict.Case.Rarity, RareFishRank, 4, LinePlayer.RuleOfThumb, verdict.Case.Effects);
                var adaptive = parity.Catch(RareFishLevel, verdict.Case.Rarity, RareFishRank, 4, LinePlayer.Adaptive, verdict.Case.Effects);
                var row = new List<string> { verdict.Case.Name };
                row.AddRange(scores.Select(_ => "-"));
                row.AddRange(new[]
                {
                    "-",
                    "-",
                    Number(adaptive.LandedPercent, 0) + "%",
                    Number(rule.LandedPercent, 0) + "%",
                    Number(adaptive.MeanTurns),
                    !verdict.Case.Gated ? "Not judged" : verdict.Within ? "Yes" : "No: over by " + Number(verdict.Landed - verdict.Bound, 1) + " points"
                });
                table.Add(row.ToArray());
            }

            return table.ToGateTable(
                $"The six special fish (level {RareFishLevel}) at Agriculture rank {RareFishRank} with a tier 4 rod, against Quick Reel's chance and the bound " +
                $"({parity.Runs} fights per case). A rare fish takes the fight perks at half strength, rounded down; Trophy Catch is the only perk at full strength");
        }
    }
}
