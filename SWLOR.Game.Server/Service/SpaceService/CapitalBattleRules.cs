using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>The capital battle event's decisions (plan section 11, encounter types): where, which ships, and when it ends.</summary>
    public static class CapitalBattleRules
    {
        /// <summary>One of the open tier 5-6 orbits, or null when there are none.</summary>
        public static IncursionArea PickArea(Func<int, int> roll)
        {
            var areas = IncursionTuning.CapitalBattleAreas;
            return areas.Count == 0 ? null : areas[Math.Clamp(roll(areas.Count), 0, areas.Count - 1)];
        }

        /// <summary>The battle group: the incursion command group of the orbit's tier (command ship first, then its escorts).</summary>
        public static List<(string Tag, string Hull)> Ships(IncursionArea area, IncursionInvader invader) =>
            IncursionRules.CommandShips(area.Tier, invader);

        /// <summary>
        /// What destroying the battle group pays one contributor (W11p): credits per point at the orbit's tier, and
        /// <see cref="IncursionTuning.CapitalBattlePulls"/> pull(s) from the tier's elite table with at least
        /// <see cref="IncursionTuning.CapitalBattleMinimumPoints"/> points. Nothing for a contributor with no points.
        /// </summary>
        public static IncursionPayout BuildPayout(IncursionContribution contribution, IncursionArea area)
        {
            var payout = new IncursionPayout { Points = contribution?.TotalPoints() ?? 0, RegionName = area.Name };
            if (contribution == null || payout.Points <= 0)
                return payout;

            var credits = 0.0;
            foreach (var (tier, points) in contribution.Points)
                credits += points * IncursionTuning.CapitalBattleCreditsPerPoint[Math.Clamp(tier, 1, 6)];

            payout.Credits = (int)Math.Round(credits, MidpointRounding.AwayFromZero);
            if (payout.Points >= IncursionTuning.CapitalBattleMinimumPoints)
                payout.Pulls[IncursionRules.EliteTable(area.Tier)] = IncursionTuning.CapitalBattlePulls;

            return payout;
        }

        /// <summary>Keeps a payout for a player who is offline, adding to one already waiting from an earlier battle.</summary>
        public static void AddPending(Dictionary<string, IncursionPayout> pending, string playerId, IncursionPayout payout)
        {
            if (!pending.TryGetValue(playerId, out var waiting))
            {
                pending[playerId] = payout;
                return;
            }

            waiting.Credits += payout.Credits;
            waiting.Points += payout.Points;
            waiting.RegionName = payout.RegionName;
            waiting.Items.AddRange(payout.Items);
            foreach (var (table, pulls) in payout.Pulls)
            {
                waiting.Pulls.TryGetValue(table, out var current);
                waiting.Pulls[table] = current + pulls;
            }
        }

        /// <summary>When the next battle group may arrive after one ends or is skipped.</summary>
        /// <summary>The end of a battle or incursion broadcast: "1 pilot shares the spoils." or "3 pilots share the spoils."</summary>
        public static string DescribeSpoilsShare(int paid) =>
            paid == 1 ? "1 pilot shares the spoils." : $"{paid} pilots share the spoils.";

        public static DateTime NextStart(DateTime now) => now.AddHours(IncursionTuning.CapitalBattleIntervalHours);

        /// <summary>A running battle ends when its ships are all destroyed, or withdraws when its time is up.</summary>
        public static CapitalBattleStep GetStep(int shipsAlive, DateTime now, DateTime endsAt) =>
            shipsAlive <= 0 ? CapitalBattleStep.Destroyed
            : now >= endsAt ? CapitalBattleStep.Withdraw
            : CapitalBattleStep.Running;
    }
}
