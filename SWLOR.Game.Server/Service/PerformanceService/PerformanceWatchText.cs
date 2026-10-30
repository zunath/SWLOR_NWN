using System.Collections.Generic;
using System.Globalization;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>The wording of the Watching window. Pure, so it is tested without the engine.</summary>
    public static class PerformanceWatchText
    {
        public static string Header(int performerCount, double troupeBonus)
        {
            var noun = performerCount == 1 ? "performer" : "performers";
            var percent = (int)Math.Round(troupeBonus * 100.0, MidpointRounding.AwayFromZero);
            return percent > 0
                ? $"{performerCount} {noun} in range - Troupe +{percent}%"
                : $"{performerCount} {noun} in range";
        }

        /// <summary>The banked time against the cap, how fast it banks, and how long to stay to fill it.</summary>
        public static string Banked(
            double remainingMinutes,
            int capMinutes,
            double ratePerMinute,
            bool isPaying,
            int warmupMinutesLeft)
        {
            var total = $"{WholeMinutes(remainingMinutes)} / {capMinutes} min";
            if (!isPaying)
            {
                var noun = warmupMinutesLeft == 1 ? "minute" : "minutes";
                return $"{total}\nWarming up: watch {Math.Max(1, warmupMinutesLeft)} more {noun} and Inspired starts banking.";
            }

            var rate = FormatNumber(ratePerMinute);
            var stay = StayMinutes(capMinutes, remainingMinutes, ratePerMinute);
            var note = stay > 0
                ? $"Stay about {stay} more {(stay == 1 ? "minute" : "minutes")} to fill it. Your timer is paused while you watch."
                : "Your Inspired is full. Your timer is paused while you watch.";
            return $"{total}  (+{rate} min per minute)\n{note}";
        }

        /// <summary>Whole minutes of watching it takes to bank the rest of the cap; 0 when it is full or nothing banks.</summary>
        public static int StayMinutes(int capMinutes, double remainingMinutes, double ratePerMinute)
        {
            var missing = capMinutes - Math.Max(0.0, remainingMinutes);
            if (missing <= 0.0 || ratePerMinute <= 0.0)
                return 0;

            return (int)Math.Ceiling(missing / ratePerMinute - 1e-9);
        }

        public static float BankedFraction(double remainingMinutes, int capMinutes)
        {
            return capMinutes <= 0 ? 0f : (float)Math.Clamp(remainingMinutes / capMinutes, 0.0, 1.0);
        }

        public static string Debt(int xpDebt, int clearPercent, int clearedLastMinute)
        {
            if (xpDebt < 0)
                return "XP debt unavailable";
            if (xpDebt == 0)
                return "No XP debt";

            var debt = xpDebt.ToString("N0", CultureInfo.InvariantCulture);
            if (clearPercent <= 0)
                return $"{debt}  (nothing you watch clears debt)";

            var cleared = clearedLastMinute.ToString("N0", CultureInfo.InvariantCulture);
            return $"{debt}  (-{clearPercent}% per minute, {cleared} cleared last minute)";
        }

        /// <summary>The skill XP and rider lines the watcher leaves with: the Inspired they would hold.</summary>
        public static string LeaveWith(InspiredEffectData data)
        {
            var items = new List<string>();
            if (data.OutOfCombatMovementSpeedPercent > 0)
                items.Add($"+{data.OutOfCombatMovementSpeedPercent}% move speed out of combat");
            AddAttribute(items, data.Might, "MGT");
            AddAttribute(items, data.Agility, "AGI");
            AddAttribute(items, data.Vitality, "VIT");
            AddAttribute(items, data.Perception, "PER");
            AddAttribute(items, data.Willpower, "WIL");
            AddAttribute(items, data.Social, "SOC");
            if (data.CraftBonus > 0)
                items.Add($"+{data.CraftBonus} Control, +{data.CraftBonus} Craftsmanship");
            if (data.FishingCatchChancePercent > 0)
                items.Add($"+{data.FishingCatchChancePercent}% fishing catch chance");
            if (data.Requiem)
                items.Add("Requiem");
            if (data.StandingOvation)
                items.Add("Standing Ovation");

            var experience = $"+{data.XPBonusPercent}% skill XP";
            return items.Count == 0 ? experience : $"{experience}\n{string.Join("; ", items)}";
        }

        public static string RowDetail(string routineOrSetText, PerformanceQuality quality)
        {
            return $"{routineOrSetText} - {quality}";
        }

        private static void AddAttribute(List<string> items, int value, string label)
        {
            if (value > 0)
                items.Add($"+{value} {label}");
        }

        private static int WholeMinutes(double minutes)
        {
            return (int)Math.Floor(Math.Max(0.0, minutes) + 1e-9);
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("0.#", CultureInfo.InvariantCulture);
        }
    }
}
