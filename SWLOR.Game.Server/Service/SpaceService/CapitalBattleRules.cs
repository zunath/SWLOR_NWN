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

        /// <summary>When the next battle group may arrive after one ends or is skipped.</summary>
        public static DateTime NextStart(DateTime now) => now.AddHours(IncursionTuning.CapitalBattleIntervalHours);

        /// <summary>A running battle ends when its ships are all destroyed, or withdraws when its time is up.</summary>
        public static CapitalBattleStep GetStep(int shipsAlive, DateTime now, DateTime endsAt) =>
            shipsAlive <= 0 ? CapitalBattleStep.Destroyed
            : now >= endsAt ? CapitalBattleStep.Withdraw
            : CapitalBattleStep.Running;
    }
}
