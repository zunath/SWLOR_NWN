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
            "Patient Angler (Give Line lets out 2 less Distance)",
            new LineAnglerEffects(GiveLineDistanceReduction: 2));

        public static readonly AnglerPerkVariant Aggressive = new(
            "Aggressive Angler (Reel pulls 2 more Distance and adds 2 more Tension)",
            new LineAnglerEffects(ReelDistanceBonus: 2, ReelTensionBonus: 2));

        /// <summary>Everything the line grants below the capstone, with the Patient Angler style.</summary>
        public static readonly LineAnglerEffects WholeLinePatient =
            new(TensionCapBonus: 3, SetHookDrainBonus: 4, HoldDrainBonus: 1, ForecastDepth: 2, GiveLineDistanceReduction: 2);

        /// <summary>Everything the line grants below the capstone, with the Aggressive Angler style.</summary>
        public static readonly LineAnglerEffects WholeLineAggressive =
            new(TensionCapBonus: 3, SetHookDrainBonus: 4, HoldDrainBonus: 1, ForecastDepth: 2, ReelDistanceBonus: 2, ReelTensionBonus: 2);

        public static readonly IReadOnlyList<AnglerPerkVariant> Variants = new[]
        {
            new AnglerPerkVariant("Strong Line I (+1 Tension cap)", new LineAnglerEffects(TensionCapBonus: 1)),
            new AnglerPerkVariant("Strong Line II (+2 Tension cap)", new LineAnglerEffects(TensionCapBonus: 2)),
            new AnglerPerkVariant("Strong Line III (+3 Tension cap)", new LineAnglerEffects(TensionCapBonus: 3)),
            new AnglerPerkVariant("Hard Set I (Set Hook drains 2 more Stamina)", new LineAnglerEffects(SetHookDrainBonus: 2)),
            new AnglerPerkVariant("Hard Set II (Set Hook drains 3 more Stamina)", new LineAnglerEffects(SetHookDrainBonus: 3)),
            new AnglerPerkVariant("Hard Set III (Set Hook drains 4 more Stamina)", new LineAnglerEffects(SetHookDrainBonus: 4)),
            new AnglerPerkVariant("Steady Hold I (Hold drains 1 more Stamina)", new LineAnglerEffects(HoldDrainBonus: 1)),
            new AnglerPerkVariant("Steady Hold II (Hold drains 2 more Stamina)", new LineAnglerEffects(HoldDrainBonus: 2)),
            new AnglerPerkVariant("Fish Sense (sees the next two moves)", new LineAnglerEffects(ForecastDepth: 2)),
            Patient,
            Aggressive,
            new AnglerPerkVariant("Whole line below the capstone, Patient Angler", WholeLinePatient),
            new AnglerPerkVariant("Whole line below the capstone, Aggressive Angler", WholeLineAggressive)
        };

        /// <summary>The rung reductions Trophy Catch is tried at.</summary>
        public static readonly IReadOnlyList<int> TrophyReductions = new[] { 2, 4, 6, 8, 12 };

        /// <summary>The level of the special fish, which are all the same.</summary>
        public const int RareFishLevel = 52;

        public static IEnumerable<CatchKey> Keys()
        {
            foreach (var rank in Ranks)
            {
                foreach (var above in FishAboveRank)
                {
                    var tier = FishingParity.NominalRodTier(rank);
                    yield return new CatchKey(rank + above, FishRarity.Common, rank, tier, LinePlayer.Adaptive, LineAnglerEffects.None);
                    foreach (var variant in Variants)
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
        public static GateTable PerkMagnitudes(FishingParity parity)
        {
            var table = new EvidenceTable(
                "Perk",
                "Landed, fish at the rank's level",
                "Landed, fish 3 levels above the rank",
                "Added points, mean");

            double Mean(LineAnglerEffects effects, int above) => Ranks.Average(rank =>
                parity.Catch(rank + above, FishRarity.Common, rank, FishingParity.NominalRodTier(rank), LinePlayer.Adaptive, effects).LandedPercent);

            var baseAtLevel = Mean(LineAnglerEffects.None, 0);
            var baseHarder = Mean(LineAnglerEffects.None, 3);
            table.Add("No perks", Number(baseAtLevel, 1) + "%", Number(baseHarder, 1) + "%", "-");
            foreach (var variant in Variants)
            {
                var atLevel = Mean(variant.Effects, 0);
                var harder = Mean(variant.Effects, 3);
                table.Add(
                    variant.Name,
                    Number(atLevel, 1) + "%",
                    Number(harder, 1) + "%",
                    Number(((atLevel - baseAtLevel) + (harder - baseHarder)) / 2, 1));
            }

            return table.ToGateTable(
                $"Angler perk magnitudes: the adaptive player's catch rate at ranks {string.Join(", ", Ranks)}, averaged over the ranks ({parity.Runs} fights per case)");
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

        /// <summary>How the special fish fight at rank 50, and how far Trophy Catch moves them against today's catch chance.</summary>
        public static GateTable RareFish(FishingParity parity, IReadOnlyList<int> scores)
        {
            var header = new List<string> { "Case" };
            header.AddRange(scores.Select(score => $"Today, Willpower {score}"));
            header.AddRange(new[] { "Fight, adaptive", "Fight, rule of thumb", "Adaptive turns" });
            var table = new EvidenceTable(header.ToArray());

            void AddToday(string name)
            {
                var row = new List<string> { name };
                row.AddRange(scores.Select(score => FishingCatchRateGate.Today(50 - RareFishLevel, score) + "%"));
                row.AddRange(new[] { "-", "-", "-" });
                table.Add(row.ToArray());
            }

            void AddFight(string name, FishRarity rarity, LineAnglerEffects effects)
            {
                var adaptive = parity.Catch(RareFishLevel, rarity, 50, 4, LinePlayer.Adaptive, effects);
                var rule = parity.Catch(RareFishLevel, rarity, 50, 4, LinePlayer.RuleOfThumb, effects);
                var row = new List<string> { name };
                row.AddRange(scores.Select(_ => "-"));
                row.AddRange(new[] { Number(adaptive.LandedPercent, 0) + "%", Number(rule.LandedPercent, 0) + "%", Number(adaptive.MeanTurns) });
                table.Add(row.ToArray());
            }

            AddToday("Today (level 52 fish, rank 50)");
            AddFight("Fight, a common fish of level 52", FishRarity.Common, LineAnglerEffects.None);
            AddFight("Fight, a rare fish (chained moves)", FishRarity.Rare, LineAnglerEffects.None);
            foreach (var reduction in TrophyReductions)
                AddFight($"Fight, a rare fish with Trophy Catch (-{reduction} rungs) and no other perk", FishRarity.Rare, new LineAnglerEffects(RareFishRungReduction: reduction));

            AddFight("Fight, a rare fish with the whole line below the capstone (Patient Angler)", FishRarity.Rare, WholeLinePatient);
            AddFight("Fight, a rare fish with the whole line below the capstone (Aggressive Angler)", FishRarity.Rare, WholeLineAggressive);
            foreach (var reduction in TrophyReductions)
            {
                AddFight(
                    $"Fight, a rare fish with the whole line (Patient Angler) and Trophy Catch (-{reduction} rungs)",
                    FishRarity.Rare,
                    WholeLinePatient with { RareFishRungReduction = reduction });
            }

            return table.ToGateTable(
                $"The six special fish (level {RareFishLevel}) at Agriculture rank 50 with a tier 4 rod, against today's catch chance ({parity.Runs} fights per case)");
        }
    }
}
