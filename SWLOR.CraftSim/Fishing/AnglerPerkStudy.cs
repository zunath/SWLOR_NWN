using System.Text;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Report;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.FishingService;
using SWLOR.Game.Server.Service.PerkService;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Fishing
{
    /// <summary>
    /// The evidence behind the Angler perk line: what each purchase adds to the adaptive player's catch rate, the whole line at every rank
    /// against Quick Reel and the unperked fight, the two styles against each other, and the special rare fish with and without Trophy
    /// Catch. The numbers are the game's own perk definitions played at the rank each case names.
    /// </summary>
    public static class AnglerPerkStudy
    {
        public static readonly IReadOnlyList<int> LineRanks = new[] { 5, 10, 15, 20, 25, 30, 35, 40, 45, 50 };

        /// <summary>The ranks the average at the fish's own level is taken over: the rank of the style, the middle and the top.</summary>
        public static readonly IReadOnlyList<int> AverageRanks = new[] { 20, 35, 50 };

        /// <summary>Rank minus fish level: from a fish 6 levels above the rank to one 4 below it.</summary>
        public static readonly IReadOnlyList<int> Deltas = new[] { -6, -4, -3, -2, -1, 0, 1, 2, 4 };

        /// <summary>The rank each purchase is played at on its own: the top, where every purchase can be owned.</summary>
        public const int PurchaseRank = 50;

        public const int RareFishLevel = 52;
        public const int RareFishRank = 50;

        /// <summary>Every case the study plays, so the tables only read.</summary>
        public static IEnumerable<CatchKey> Keys()
        {
            foreach (var rank in LineRanks)
            {
                foreach (var build in AllBuilds())
                {
                    foreach (var delta in Deltas.Where(delta => rank - delta >= 0))
                        yield return Key(rank - delta, rank, build.EffectsAt(rank));
                }
            }

            foreach (var (_, perk, level) in Purchases())
            {
                foreach (var above in new[] { 0, 3 })
                    yield return Key(PurchaseRank + above, PurchaseRank, AnglerBuild.Alone(perk, level).EffectsAt(PurchaseRank));
            }

            foreach (var (_, effects) in RareCases())
            {
                yield return new CatchKey(RareFishLevel, FishRarity.Rare, RareFishRank, 4, LinePlayer.Adaptive, effects);
                yield return new CatchKey(RareFishLevel, FishRarity.Common, RareFishRank, 4, LinePlayer.Adaptive, effects);
            }

            foreach (var (_, perk, level) in Purchases())
                yield return new CatchKey(RareFishLevel, FishRarity.Rare, RareFishRank, 4, LinePlayer.Adaptive, AnglerBuild.Alone(perk, level).EffectsAt(RareFishRank));
        }

        private static CatchKey Key(int fishLevel, int rank, LineAnglerEffects effects) =>
            new(fishLevel, FishRarity.Common, rank, FishingParity.NominalRodTier(rank), LinePlayer.Adaptive, effects);

        private static IEnumerable<AnglerBuild> AllBuilds() => new[] { AnglerBuild.Unperked, AnglerBuild.PatientLine, AnglerBuild.AggressiveLine };

        /// <summary>Every purchase of the line, as (name, perk, level), in rank order.</summary>
        public static IReadOnlyList<(string Name, PerkType Perk, int Level)> Purchases() =>
            AnglerBuild.Line
                .SelectMany(pair => pair.Value.PerkLevels.Select(level => (Perk: pair.Key, Level: level.Key, Detail: pair.Value, Rank: Rank(level.Value))))
                .OrderBy(item => item.Rank)
                .ThenBy(item => (int)item.Perk)
                .Select(item => (AnglerBuild.Alone(item.Perk, item.Level).Name, item.Perk, item.Level))
                .ToList();

        private static int Rank(PerkLevel level) =>
            level.Requirements.OfType<PerkRequirementSkill>().Single().RequiredRank;

        public static IReadOnlyList<(string Name, LineAnglerEffects Effects)> RareCases() => new[]
        {
            ("No perks", AnglerBuild.Unperked.EffectsAt(RareFishRank)),
            ("Patient Angler and the line below the capstone (half strength)", AnglerBuild.PatientBelowCapstone.EffectsAt(RareFishRank)),
            ("Aggressive Angler and the line below the capstone (half strength)", AnglerBuild.AggressiveBelowCapstone.EffectsAt(RareFishRank)),
            ("Trophy Catch alone", AnglerBuild.Alone(PerkType.TrophyCatch, 1).EffectsAt(RareFishRank)),
            ("Patient Angler, the whole line with Trophy Catch", AnglerBuild.PatientLine.EffectsAt(RareFishRank)),
            ("Aggressive Angler, the whole line with Trophy Catch", AnglerBuild.AggressiveLine.EffectsAt(RareFishRank))
        };

        public static string Build(FishingParity parity, FishingParityOptions fight, string command)
        {
            parity.Prepare(Keys());

            var text = new StringBuilder();
            text.AppendLine("# Crafting revamp: the Angler perk line");
            text.AppendLine();
            text.AppendLine($"- Command: `{command}`");
            text.AppendLine($"- Seed {parity.Seed}; {parity.Runs} fights per case, each with its own dice. The adaptive player ({LineFights.AdaptiveOptions.Describe()}) stands for the skilled fight.");
            text.AppendLine("- Every number is the game's own perk definition (`AgricultureAnglerPerkDefinition`) played at the rank the row names: an angler owns the purchases that rank allows, at the highest level it allows. Quick Reel is today's roll at Willpower " + fight.QuickReelWillpowerScore + ".");
            text.AppendLine($"- The unperked fight is bounded to {Number(fight.FightBoundRelative * 100, 0)}% above Quick Reel plus {Number(fight.FightBoundSlackPoints, 0)} points. The whole line is held to the owner's target instead: about {Number(fight.PerkedTargetPercent, 0)}% of the fish at the angler's own level at rank 50, where it is complete (judged within {Number(fight.PerkedTolerancePoints, 0)} points).");
            text.AppendLine();

            Section(text, PurchaseTable(parity, fight));
            Section(text, AtLevelTable(parity, fight));
            Section(text, ByDeltaTable(parity, fight, 20));
            Section(text, ByDeltaTable(parity, fight, 35));
            Section(text, ByDeltaTable(parity, fight, 50));
            Section(text, StyleTable(parity));
            Section(text, RareTable(parity, fight));
            return text.ToString();
        }

        private static string Render(GateTable table)
        {
            var markdown = new MarkdownTable(table.Header.ToArray());
            foreach (var row in table.Rows)
                markdown.Add(row.ToArray());

            return markdown.ToString();
        }

        private static void Section(StringBuilder text, GateTable table)
        {
            text.AppendLine("## " + table.Title);
            text.AppendLine();
            text.AppendLine(Render(table));
            text.AppendLine();
        }

        private static double Landed(FishingParity parity, int fishLevel, int rank, LineAnglerEffects effects) =>
            parity.Catch(fishLevel, FishRarity.Common, rank, FishingParity.NominalRodTier(rank), LinePlayer.Adaptive, effects).LandedPercent;

        private static double Turns(FishingParity parity, int fishLevel, int rank, LineAnglerEffects effects) =>
            parity.Catch(fishLevel, FishRarity.Common, rank, FishingParity.NominalRodTier(rank), LinePlayer.Adaptive, effects).MeanTurns;

        public static GateTable PurchaseTable(FishingParity parity, FishingParityOptions options)
        {
            var table = new EvidenceTable("Purchase (alone)", "Effect read by the Line", "Fish at the rank's level", "Fish 3 levels above", "Added points (mean of both)");
            double Mean(LineAnglerEffects effects, int above) => Landed(parity, PurchaseRank + above, PurchaseRank, effects);
            var baseAt = Mean(LineAnglerEffects.None, 0);
            var baseAbove = Mean(LineAnglerEffects.None, 3);
            table.Add("No perks", "-", Number(baseAt, 1) + "%", Number(baseAbove, 1) + "%", "-");
            foreach (var (name, perk, level) in Purchases())
            {
                double At(int above) => Landed(parity, PurchaseRank + above, PurchaseRank, AnglerBuild.Alone(perk, level).EffectsAt(PurchaseRank));
                var effects = AnglerBuild.Alone(perk, level).EffectsAt(PurchaseRank);
                var at = At(0);
                var above = At(3);
                table.Add(name, Describe(effects), Number(at, 1) + "%", Number(above, 1) + "%", Number(((at - baseAt) + (above - baseAbove)) / 2, 1));
            }

            return table.ToGateTable(
                $"Each purchase alone: the adaptive player's catch rate at rank {PurchaseRank}, owning only that purchase at the level named, against a fish of the angler's own level and one 3 levels above");
        }

        public static GateTable AtLevelTable(FishingParity parity, FishingParityOptions options)
        {
            var table = new EvidenceTable("Rank", "Quick Reel", "Bound (no perks)", "No perks", "Patient Angler line", "Aggressive Angler line", "Target");
            foreach (var rank in LineRanks)
            {
                var quick = options.QuickReel(rank, rank);
                table.Add(
                    rank.ToString(),
                    quick + "%",
                    Number(options.FightBound(quick), 1) + "%",
                    Number(Landed(parity, rank, rank, AnglerBuild.Unperked.EffectsAt(rank)), 1) + "%",
                    Number(Landed(parity, rank, rank, AnglerBuild.PatientLine.EffectsAt(rank)), 1) + "%",
                    Number(Landed(parity, rank, rank, AnglerBuild.AggressiveLine.EffectsAt(rank)), 1) + "%",
                    rank == RareFishRank ? Number(options.PerkedTargetPercent, 0) + "%" : "-");
            }

            return table.ToGateTable("The fish at the angler's own level, by rank: the adaptive player with no perks and with the whole line the rank allows");
        }

        private static GateTable ByDeltaTable(FishingParity parity, FishingParityOptions options, int rank)
        {
            var table = new EvidenceTable("Rank - fish level", "Quick Reel", "No perks", "Patient Angler line", "Aggressive Angler line", "Turns, no perks", "Turns, Patient", "Turns, Aggressive");
            foreach (var delta in Deltas.Where(delta => rank - delta >= 0))
            {
                var level = rank - delta;
                table.Add(
                    Signed(delta),
                    options.QuickReel(rank, level) + "%",
                    Number(Landed(parity, level, rank, AnglerBuild.Unperked.EffectsAt(rank)), 1) + "%",
                    Number(Landed(parity, level, rank, AnglerBuild.PatientLine.EffectsAt(rank)), 1) + "%",
                    Number(Landed(parity, level, rank, AnglerBuild.AggressiveLine.EffectsAt(rank)), 1) + "%",
                    Number(Turns(parity, level, rank, AnglerBuild.Unperked.EffectsAt(rank))),
                    Number(Turns(parity, level, rank, AnglerBuild.PatientLine.EffectsAt(rank))),
                    Number(Turns(parity, level, rank, AnglerBuild.AggressiveLine.EffectsAt(rank))));
            }

            return table.ToGateTable($"Rank {rank}: catch rate and fight length by the fish's level against the rank");
        }

        /// <summary>The two styles with the line each rank allows, over fish from four levels above the rank to two below it.</summary>
        public static (GateTable Table, double PatientShare, double AggressiveShare, double MeanGap) StyleBalance(FishingParity parity, double tiePoints = 2.0)
        {
            var table = new EvidenceTable("Rank", "Rank - fish level", "Patient Angler (full line)", "Aggressive Angler (full line)", "Better");
            double patientWins = 0, aggressiveWins = 0, gap = 0;
            var cases = 0;
            foreach (var rank in AverageRanks)
            {
                foreach (var delta in FishingAnglerEvidence.StyleDeltas)
                {
                    var patient = Landed(parity, rank - delta, rank, AnglerBuild.PatientLine.EffectsAt(rank));
                    var aggressive = Landed(parity, rank - delta, rank, AnglerBuild.AggressiveLine.EffectsAt(rank));
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

                    table.Add(rank.ToString(), Signed(delta), Number(patient, 0) + "%", Number(aggressive, 0) + "%", better);
                }
            }

            return (table.ToGateTable($"Style balance with the whole line (ties within {Number(tiePoints, 1)} points count half to each)"), patientWins / cases, aggressiveWins / cases, gap / cases);
        }

        private static GateTable StyleTable(FishingParity parity)
        {
            var style = StyleBalance(parity);
            var table = new EvidenceTable("Measure", "Value");
            table.Add("Patient Angler is the better style", Percent(style.PatientShare));
            table.Add("Aggressive Angler is the better style", Percent(style.AggressiveShare));
            table.Add("Mean gap, Patient less Aggressive (points)", Number(style.MeanGap));
            var rows = style.Table.Rows;
            foreach (var row in rows)
                table.Add($"Rank {row[0]}, {row[1]}", $"{row[2]} / {row[3]} ({row[4]})");

            return table.ToGateTable("The two styles over the full line");
        }

        private static GateTable RareTable(FishingParity parity, FishingParityOptions options)
        {
            var quick = options.QuickReel(RareFishRank, RareFishLevel);
            var table = new EvidenceTable("Case (rank 50, tier 4 rod, level 52)", "Rare fish landed", "Common fish of the same level", "Bound", "Within the bound");
            foreach (var (name, effects) in RareCases())
            {
                var rare = parity.Catch(RareFishLevel, FishRarity.Rare, RareFishRank, 4, LinePlayer.Adaptive, effects).LandedPercent;
                var common = parity.Catch(RareFishLevel, FishRarity.Common, RareFishRank, 4, LinePlayer.Adaptive, effects).LandedPercent;
                var bound = options.FightBound(quick);
                var judged = effects.TensionCapBonus == 0 && effects.SetHookDrainBonus == 0 && effects.GiveLineDistanceReduction == 0 && effects.ReelDistanceBonus == 0;
                table.Add(
                    name,
                    Number(rare, 0) + "%",
                    Number(common, 0) + "%",
                    Number(bound, 1) + "%",
                    !judged ? "Not judged (fight perks at half strength)" : rare <= bound ? "Yes" : "No: over by " + Number(rare - bound, 1) + " points");
            }

            foreach (var (name, perk, level) in Purchases())
            {
                var effects = AnglerBuild.Alone(perk, level).EffectsAt(RareFishRank);
                var rare = parity.Catch(RareFishLevel, FishRarity.Rare, RareFishRank, 4, LinePlayer.Adaptive, effects).LandedPercent;
                table.Add($"{name} alone", Number(rare, 0) + "%", "-", Number(options.FightBound(quick), 1) + "%", "-");
            }

            return table.ToGateTable(
                $"The six special fish: a rare fish takes the fight perks at half strength, rounded down, and Trophy Catch at full strength ({parity.Runs} fights per case)");
        }

        private static string Describe(LineAnglerEffects effects)
        {
            var parts = new List<string>();
            if (effects.TensionCapBonus != 0)
                parts.Add($"cap +{effects.TensionCapBonus}");
            if (effects.SetHookDrainBonus != 0)
                parts.Add($"Set Hook drain +{effects.SetHookDrainBonus}");
            if (effects.ForecastDepth > 1)
                parts.Add($"two moves ahead for {effects.ForecastTurns} turn(s)");
            if (effects.GiveLineDistanceReduction != 0)
                parts.Add($"Give Line -{effects.GiveLineDistanceReduction}");
            if (effects.ReelDistanceBonus != 0)
                parts.Add($"Reel +{effects.ReelDistanceBonus}");
            if (effects.RareFishRungReduction != 0)
                parts.Add($"rare fish -{effects.RareFishRungReduction} rungs");

            return parts.Count == 0 ? "-" : string.Join(", ", parts);
        }
    }
}
