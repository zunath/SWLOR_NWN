using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.FishingService;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Fishing
{
    /// <summary>A perk magnitude the Angler line proposes, as the adjustments the engine would read.</summary>
    public sealed record AnglerPerkVariant(string Name, LineAnglerEffects Effects);

    /// <summary>
    /// The evidence behind the Angler perk-line draft: how much each proposed magnitude adds to the adaptive player's catch rate, whether the
    /// two rank-20 styles are balanced, and how far Trophy Catch moves a rare fish against the ceiling the parity check sets. The magnitudes
    /// are starting values; the draft quotes these tables.
    /// </summary>
    public static class FishingAnglerEvidence
    {
        public static readonly IReadOnlyList<int> Ranks = new[] { 20, 35, 50 };

        /// <summary>The fish a table plays: this many levels above the angler's rank (0 is at level, 3 is a harder fish).</summary>
        public static readonly IReadOnlyList<int> FishAboveRank = new[] { 0, 3 };

        public static readonly AnglerPerkVariant Patient = new(
            "Patient Angler (Give Line lets out 1 less Distance)",
            new LineAnglerEffects(GiveLineDistanceReduction: 1));

        public static readonly AnglerPerkVariant Aggressive = new(
            "Aggressive Angler (Reel pulls the fish in 1 more Distance)",
            new LineAnglerEffects(ReelDistanceBonus: 1));

        /// <summary>Everything the line grants below the capstone, with the Patient Angler style.</summary>
        public static readonly LineAnglerEffects WholeLinePatient =
            new(TensionCapBonus: 1, SetHookDrainBonus: 1, GiveLineDistanceReduction: 1);

        /// <summary>Everything the line grants below the capstone, with the Aggressive Angler style.</summary>
        public static readonly LineAnglerEffects WholeLineAggressive =
            new(TensionCapBonus: 1, SetHookDrainBonus: 1, ReelDistanceBonus: 1);

        /// <summary>The rungs Trophy Catch takes off a rare fish's difficulty.</summary>
        public const int TrophyCatchRungs = 4;

        /// <summary>The purchases the line has, one at a time and together.</summary>
        public static readonly IReadOnlyList<AnglerPerkVariant> Variants = new[]
        {
            new AnglerPerkVariant("Strong Line I (+1 Tension cap)", new LineAnglerEffects(TensionCapBonus: 1)),
            new AnglerPerkVariant("Hard Set I (Set Hook drains 1 more Stamina)", new LineAnglerEffects(SetHookDrainBonus: 1)),
            Patient,
            Aggressive,
            new AnglerPerkVariant("Whole line below the capstone, Patient Angler", WholeLinePatient),
            new AnglerPerkVariant("Whole line below the capstone, Aggressive Angler", WholeLineAggressive)
        };

        /// <summary>
        /// The draft's first whole line, kept to show how far over the bound it went. Each of its purchases alone is within the bound; it is the
        /// sum that is not, which is why the line was cut.
        /// </summary>
        public static readonly IReadOnlyList<AnglerPerkVariant> RejectedVariants = new[]
        {
            new AnglerPerkVariant(
                "Rejected: the first draft's whole line (Strong Line III, Hard Set III, Steady Hold, Fish Sense, Patient Angler -2)",
                new LineAnglerEffects(TensionCapBonus: 3, SetHookDrainBonus: 4, HoldDrainBonus: 1, ForecastDepth: 2, GiveLineDistanceReduction: 2))
        };

        /// <summary>
        /// Purchases the line could take if the owner fills its open rows, each added to the whole line, with what that does to the
        /// room under the bound. The bound has no room for them with the unperked fight where it is.
        /// </summary>
        public static readonly IReadOnlyList<AnglerPerkVariant> OpenLines = new[]
        {
            new AnglerPerkVariant("Whole line (Patient Angler)", WholeLinePatient),
            new AnglerPerkVariant("Whole line (Patient Angler) with Strong Line II (+2 cap in all)", WholeLinePatient with { TensionCapBonus = 2 }),
            new AnglerPerkVariant("Whole line (Patient Angler) with Hard Set II (+2 drain in all)", WholeLinePatient with { SetHookDrainBonus = 2 }),
            new AnglerPerkVariant(
                "Whole line (Patient Angler) with Strong Line II and Hard Set II",
                WholeLinePatient with { TensionCapBonus = 2, SetHookDrainBonus = 2 }),
            new AnglerPerkVariant(
                "Whole line (Patient Angler) with Fish Sense (two moves ahead on the first turn)",
                WholeLinePatient with { ForecastDepth = 2 }),
            new AnglerPerkVariant("Whole line (Aggressive Angler)", WholeLineAggressive),
            new AnglerPerkVariant("Whole line (Aggressive Angler) with Strong Line II (+2 cap in all)", WholeLineAggressive with { TensionCapBonus = 2 }),
            new AnglerPerkVariant(
                "Whole line (Aggressive Angler) with Fish Sense (two moves ahead on the first turn)",
                WholeLineAggressive with { ForecastDepth = 2 }),
            new AnglerPerkVariant("Fish Sense alone (two moves ahead on the first turn)", new LineAnglerEffects(ForecastDepth: 2)),
            new AnglerPerkVariant("Strong Line II alone (+2 Tension cap)", new LineAnglerEffects(TensionCapBonus: 2)),
            new AnglerPerkVariant("Hard Set II alone (Set Hook drains 2 more Stamina)", new LineAnglerEffects(SetHookDrainBonus: 2))
        };

        /// <summary>The rung reductions Trophy Catch is tried at.</summary>
        public static readonly IReadOnlyList<int> TrophyReductions = new[] { 2, TrophyCatchRungs, 6, 8 };

        /// <summary>The level of the special fish, which are all the same.</summary>
        public const int RareFishLevel = 52;

        public static IEnumerable<CatchKey> Keys()
        {
            var defaults = FishingParityOptions.Default;
            foreach (var line in OpenLines)
            {
                foreach (var rank in defaults.ReferenceRanks)
                {
                    foreach (var delta in defaults.Deltas.Where(delta => rank - delta >= 0))
                        yield return new CatchKey(rank - delta, FishRarity.Common, rank, FishingParity.NominalRodTier(rank), LinePlayer.Adaptive, line.Effects);
                }
            }

            foreach (var rank in Ranks)
            {
                foreach (var above in FishAboveRank)
                {
                    var tier = FishingParity.NominalRodTier(rank);
                    yield return new CatchKey(rank + above, FishRarity.Common, rank, tier, LinePlayer.Adaptive, LineAnglerEffects.None);
                    foreach (var variant in Variants.Concat(RejectedVariants))
                        yield return new CatchKey(rank + above, FishRarity.Common, rank, tier, LinePlayer.Adaptive, variant.Effects);
                }

                foreach (var delta in StyleDeltas)
                {
                    yield return new CatchKey(rank - delta, FishRarity.Common, rank, FishingParity.NominalRodTier(rank), LinePlayer.Adaptive, Patient.Effects);
                    yield return new CatchKey(rank - delta, FishRarity.Common, rank, FishingParity.NominalRodTier(rank), LinePlayer.Adaptive, Aggressive.Effects);
                }
            }

            foreach (var player in new[] { LinePlayer.Adaptive, LinePlayer.RuleOfThumb })
            {
                yield return new CatchKey(RareFishLevel, FishRarity.Rare, 50, 4, player, LineAnglerEffects.None);
                yield return new CatchKey(RareFishLevel, FishRarity.Common, 50, 4, player, LineAnglerEffects.None);
                yield return new CatchKey(RareFishLevel, FishRarity.Rare, 50, 4, player, WholeLinePatient);
                yield return new CatchKey(RareFishLevel, FishRarity.Rare, 50, 4, player, WholeLineAggressive);
                yield return new CatchKey(RareFishLevel, FishRarity.Rare, 50, 4, player, WholeLineAggressive with { RareFishRungReduction = TrophyCatchRungs });
                foreach (var reduction in TrophyReductions)
                {
                    yield return new CatchKey(RareFishLevel, FishRarity.Rare, 50, 4, player, new LineAnglerEffects(RareFishRungReduction: reduction));
                    yield return new CatchKey(RareFishLevel, FishRarity.Rare, 50, 4, player, WholeLinePatient with { RareFishRungReduction = reduction });
                }
            }
        }

        /// <summary>The fish levels, above the rank, the style comparison plays.</summary>
        public static readonly IReadOnlyList<int> StyleDeltas = new[] { -4, -3, -2, -1, 0, 1, 2 };

        /// <summary>What each proposed perk adds to the adaptive player's catch rate against fish at level and three levels above the rank.</summary>
        public static GateTable PerkMagnitudes(FishingParity parity, FishingParityOptions options = null)
        {
            options ??= FishingParityOptions.Default;
            var boundAtLevel = options.FightBound(options.QuickReel(0, 0));
            var boundHarder = options.FightBound(options.QuickReel(0, FishAboveRank[1]));
            var table = new EvidenceTable(
                "Perk",
                "Landed, fish at the rank's level",
                "Landed, fish 3 levels above the rank",
                "Added points, mean",
                "Within the bound");

            double Mean(LineAnglerEffects effects, int above) => Ranks.Average(rank =>
                parity.Catch(rank + above, FishRarity.Common, rank, FishingParity.NominalRodTier(rank), LinePlayer.Adaptive, effects).LandedPercent);

            string Verdict(double atLevel, double harder) =>
                atLevel <= boundAtLevel && harder <= boundHarder
                    ? "Yes"
                    : "No: over by " + Number(Math.Max(atLevel - boundAtLevel, harder - boundHarder), 1) + " points";

            var baseAtLevel = Mean(LineAnglerEffects.None, 0);
            var baseHarder = Mean(LineAnglerEffects.None, 3);
            table.Add("No perks", Number(baseAtLevel, 1) + "%", Number(baseHarder, 1) + "%", "-", Verdict(baseAtLevel, baseHarder));
            foreach (var variant in Variants.Concat(RejectedVariants))
            {
                var atLevel = Mean(variant.Effects, 0);
                var harder = Mean(variant.Effects, 3);
                table.Add(
                    variant.Name,
                    Number(atLevel, 1) + "%",
                    Number(harder, 1) + "%",
                    Number(((atLevel - baseAtLevel) + (harder - baseHarder)) / 2, 1),
                    Verdict(atLevel, harder));
            }

            return table.ToGateTable(
                $"Angler perk magnitudes: the adaptive player's catch rate at ranks {string.Join(", ", Ranks)}, averaged over the ranks " +
                $"({parity.Runs} fights per case). The bound is Quick Reel's chance plus {Number(options.FightBoundRelative * 100, 0)}% plus " +
                $"{Number(options.FightBoundSlackPoints, 0)} points: {Number(boundAtLevel, 1)}% at the rank's level and {Number(boundHarder, 1)}% for fish 3 levels above");
        }

        /// <summary>
        /// What each purchase the line could still take would do to the room under the bound: the adaptive player's catch rate at the fish's
        /// own level, the fewest points left under the bound over every rank above the fish's level, the row that is tightest and whether
        /// the whole stays within the bound.
        /// </summary>
        public static (GateTable Table, IReadOnlyList<(string Name, bool Within)> Verdicts) OpenPurchases(FishingParity parity, FishingParityOptions options = null)
        {
            options ??= FishingParityOptions.Default;
            var table = new EvidenceTable(
                "Candidate for an open row", "Landed, fish at the rank's level", "Added at the rank's level, against the whole line (Patient Angler)",
                "Points left under the bound, tightest row", "Tightest row (rank - fish level)", "Within the bound");
            var verdicts = new List<(string Name, bool Within)>();

            double Landed(int delta, LineAnglerEffects effects) => options.ReferenceRanks
                .Where(rank => rank - delta >= 0)
                .Average(rank => parity.Catch(rank - delta, FishRarity.Common, rank, FishingParity.NominalRodTier(rank), LinePlayer.Adaptive, effects).LandedPercent);

            var baseline = Landed(0, WholeLinePatient);
            foreach (var line in OpenLines)
            {
                var tightest = double.MaxValue;
                var tightestDelta = 0;
                foreach (var delta in options.Deltas.Where(delta => options.ReferenceRanks.Any(rank => rank - delta >= 0)))
                {
                    var margin = options.FightBound(options.QuickReel(delta, 0)) - Landed(delta, line.Effects);
                    if (margin < tightest)
                    {
                        tightest = margin;
                        tightestDelta = delta;
                    }
                }

                var atLevel = Landed(0, line.Effects);
                var within = tightest >= 0;
                verdicts.Add((line.Name, within));
                table.Add(
                    line.Name,
                    Number(atLevel, 1) + "%",
                    Signed((int)Math.Round(atLevel - baseline)),
                    Number(tightest, 1),
                    Signed(tightestDelta),
                    within ? "Yes" : "No: over by " + Number(-tightest, 1) + " points");
            }

            return (table.ToGateTable(
                    "Open rows of the Angler line: each candidate added to the whole line (or alone), judged against the bound at every rank above the fish's level " +
                    $"from {Signed(options.LowestDelta)} to {Signed(options.HighestDelta)} ({parity.Runs} fights per fish, ranks {string.Join(", ", options.ReferenceRanks)})"),
                verdicts);
        }

        /// <summary>The two rank-20 styles against each other over fish from four levels above the rank to two below it.</summary>
        public static (GateTable Table, double PatientShare, double AggressiveShare, double MeanGap) StyleBalance(FishingParity parity, double tiePoints = 2.0)
        {
            var table = new EvidenceTable("Rank", "Fish levels above the rank", "Patient Angler", "Aggressive Angler", "Better");
            double patientWins = 0, aggressiveWins = 0, gap = 0;
            var cases = 0;
            foreach (var rank in Ranks)
            {
                foreach (var delta in StyleDeltas)
                {
                    var tier = FishingParity.NominalRodTier(rank);
                    var patient = parity.Catch(rank - delta, FishRarity.Common, rank, tier, LinePlayer.Adaptive, Patient.Effects).LandedPercent;
                    var aggressive = parity.Catch(rank - delta, FishRarity.Common, rank, tier, LinePlayer.Adaptive, Aggressive.Effects).LandedPercent;
                    var diff = patient - aggressive;
                    gap += diff;
                    cases++;
                    string better;
                    if (Math.Abs(diff) <= tiePoints)
                    {
                        patientWins += 0.5;
                        aggressiveWins += 0.5;
                        better = "Tie";
                    }
                    else if (diff > 0)
                    {
                        patientWins++;
                        better = "Patient";
                    }
                    else
                    {
                        aggressiveWins++;
                        better = "Aggressive";
                    }

                    table.Add(rank.ToString(), Signed(-delta), Number(patient, 0) + "%", Number(aggressive, 0) + "%", better);
                }
            }

            var title = $"Style balance: Patient Angler against Aggressive Angler (ties within {Number(tiePoints, 1)} points count half to each)";
            return (table.ToGateTable(title), patientWins / cases, aggressiveWins / cases, gap / cases);
        }

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
                new("Fight, a common fish of level 52 (for comparison)", FishRarity.Common, LineAnglerEffects.None, false),
                new("Fight, a rare fish (chained moves)", FishRarity.Rare, LineAnglerEffects.None, true)
            };
            cases.AddRange(TrophyReductions.Select(reduction => new RareFishCase(
                $"Fight, a rare fish with Trophy Catch (-{reduction} rungs) and no other perk",
                FishRarity.Rare,
                new LineAnglerEffects(RareFishRungReduction: reduction),
                reduction == TrophyCatchRungs)));
            cases.Add(new("Fight, a rare fish with the whole line below the capstone (Patient Angler)", FishRarity.Rare, WholeLinePatient, true));
            cases.Add(new("Fight, a rare fish with the whole line below the capstone (Aggressive Angler)", FishRarity.Rare, WholeLineAggressive, true));
            cases.Add(new(
                $"Fight, a rare fish with the whole line (Patient Angler) and Trophy Catch (-{TrophyCatchRungs} rungs)",
                FishRarity.Rare,
                WholeLinePatient with { RareFishRungReduction = TrophyCatchRungs },
                true));
            cases.Add(new(
                $"Fight, a rare fish with the whole line (Aggressive Angler) and Trophy Catch (-{TrophyCatchRungs} rungs)",
                FishRarity.Rare,
                WholeLineAggressive with { RareFishRungReduction = TrophyCatchRungs },
                true));
            return cases;
        }

        /// <summary>The rank every rare-fish case is played at, and its rod tier.</summary>
        public const int RareFishRank = 50;

        /// <summary>
        /// How the special fish fight at rank 50 against Quick Reel and the bound: what each case lands, the bound it is held to, and whether it
        /// is within it. The special fish take the line at half strength and Trophy Catch at full strength.
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
                $"({parity.Runs} fights per case). A rare fish takes the fight perks at half strength; Trophy Catch is the only perk at full strength");
        }
    }
}
