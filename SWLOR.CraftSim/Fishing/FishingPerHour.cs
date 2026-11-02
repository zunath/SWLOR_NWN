using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Fishing
{
    /// <summary>One band of fish levels: the fish and the XP an angler earns in an hour from today's catch roll, from Quick Reel and from the fight.</summary>
    /// <param name="Band">The band's name, such as "21-30".</param>
    /// <param name="TodayFishLow">Fish per hour at the low end of the Willpower corridor.</param>
    /// <param name="TodayFishMiddle">Fish per hour at the middle of the corridor.</param>
    /// <param name="TodayFishReference">Fish per hour at the Willpower score Quick Reel rolls with.</param>
    /// <param name="TodayFishHigh">Fish per hour at the high end of the corridor.</param>
    /// <param name="QuickReelFish">Fish per hour by Quick Reel.</param>
    /// <param name="FightFish">Fish per hour by the fight, played by the typical player at the modelled seconds per turn.</param>
    /// <param name="TodayXp">XP per hour today.</param>
    /// <param name="QuickReelXp">XP per hour by Quick Reel.</param>
    /// <param name="FightXp">XP per hour by the fight, with the band's multiplier.</param>
    public sealed record FishingPerHourRow(
        string Band,
        double TodayFishLow,
        double TodayFishMiddle,
        double TodayFishReference,
        double TodayFishHigh,
        double QuickReelFish,
        double FightFish,
        double TodayXp,
        double QuickReelXp,
        double FightXp);

    /// <summary>
    /// Fish and XP per hour of fishing, for the XP cohort's bands: today's catch roll against Quick Reel and the fight. Quick Reel takes no fight
    /// time, so it spends the cast and the wait like today's roll; the fight spends them and then plays its turns.
    /// </summary>
    public static class FishingPerHour
    {
        public static IReadOnlyList<FishingPerHourRow> Rows(FishingXpRateCohort cohort, FishingParity parity, FishingParityOptions options, GateContext context)
        {
            var rows = new List<FishingPerHourRow>();
            foreach (var xpCase in cohort.Cases(context))
            {
                var mix = cohort.Mix(xpCase.Group);
                var castHours = options.CastSeconds / 3600.0;

                double Chance(Func<FishingMixEntry, double> percent) => mix.Sum(entry => entry.Weight * percent(entry) / 100.0);

                var low = Chance(entry => FishingCatchModel.CatchChancePercent(entry.Rank, entry.Level, options.CorridorLowScore));
                var middle = Chance(entry => FishingCatchModel.CatchChancePercent(entry.Rank, entry.Level, MiddleScore(options)));
                var reference = Chance(entry => FishingCatchModel.CatchChancePercent(entry.Rank, entry.Level, options.QuickReelWillpowerScore));
                var high = Chance(entry => FishingCatchModel.CatchChancePercent(entry.Rank, entry.Level, options.CorridorHighScore));
                var quickReel = Chance(entry => options.QuickReel(entry.Rank, entry.Level));

                var samples = mix.Select(entry => (Entry: entry, Sample: parity.Catch(entry.Level, entry.Rarity, entry.Rank, entry.RodTier))).ToList();
                var fightChance = samples.Sum(pair => pair.Entry.Weight * pair.Sample.LandedPercent / 100.0);
                var fightHours = castHours + samples.Sum(pair => pair.Entry.Weight * pair.Sample.MeanTurns) * options.SecondsPerTurn / 3600.0;

                var todayXp = mix.Sum(entry => entry.Weight * FishingCatchModel.XpPerBite(entry.Rank, entry.Level)) / castHours;
                var fightXp = cohort.PlayReplacement(xpCase, context).XpPerHour(cohort.ReplacementTiming);

                rows.Add(new FishingPerHourRow(
                    xpCase.Group,
                    low / castHours,
                    middle / castHours,
                    reference / castHours,
                    high / castHours,
                    quickReel / castHours,
                    fightChance / fightHours,
                    todayXp,
                    todayXp,
                    fightXp));
            }

            return rows;
        }

        /// <summary>The Willpower score in the middle of the corridor.</summary>
        public static int MiddleScore(FishingParityOptions options) => (options.CorridorLowScore + options.CorridorHighScore) / 2;

        public static GateTable Table(IReadOnlyList<FishingPerHourRow> rows, FishingParityOptions options)
        {
            var table = new EvidenceTable(
                "Fish-level band",
                $"Today, fish per hour, Willpower {options.CorridorLowScore}",
                $"Today, fish per hour, Willpower {MiddleScore(options)}",
                $"Today, fish per hour, Willpower {options.CorridorHighScore}",
                "Quick Reel, fish per hour",
                $"Quick Reel against today (Willpower {options.QuickReelWillpowerScore})",
                $"Quick Reel against today (Willpower {options.CorridorLowScore}-{options.CorridorHighScore})",
                "Fight, fish per hour",
                $"Fight against today (Willpower {options.QuickReelWillpowerScore})",
                "Today, XP per hour",
                "Quick Reel, XP per hour",
                "Fight, XP per hour",
                "Fight XP against today");

            foreach (var row in rows)
            {
                var span = $"{Number(row.QuickReelFish / row.TodayFishLow, 2)}x-{Number(row.QuickReelFish / row.TodayFishHigh, 2)}x";
                table.Add(
                    row.Band,
                    Number(row.TodayFishLow, 0),
                    Number(row.TodayFishMiddle, 0),
                    Number(row.TodayFishHigh, 0),
                    Number(row.QuickReelFish, 0),
                    Number(row.QuickReelFish / row.TodayFishReference, 2) + "x",
                    span,
                    Number(row.FightFish, 0),
                    Number(row.FightFish / row.TodayFishReference, 2) + "x",
                    Number(row.TodayXp, 0),
                    Number(row.QuickReelXp, 0),
                    Number(row.FightXp, 0),
                    Number(row.FightXp / row.TodayXp, 2) + "x");
            }

            return table.ToGateTable(
                $"Fish and XP per hour by fish-level band, rank equal to the fish's level (at most 50) with the best rod for it: today's catch roll, Quick Reel and the fight " +
                $"({Number(options.CastSeconds, 0)} s for a cast and the wait, {Number(options.SecondsPerTurn, 0)} s for each turn of the fight; unmeasured estimates)");
        }
    }
}
