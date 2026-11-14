using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Decision rules for the Ion Web, Interdictor, Jammer and Target Painter (W11k, plan Appendix B): their hull bonuses,
    /// how long each cycle's effect lasts and how two of a kind on one ship combine (the stronger counts). Pure logic
    /// with no engine calls.
    /// </summary>
    public static class ShipElectronicWarfareRules
    {
        public const string InterdictedMessage = "An Interdictor holds your ship.";
        public const string WebImmuneMessage = "That ship's hull shrugs off Ion Webs.";

        /// <summary>How long one cycle's web, jam, paint or hold lasts: the cycle plus a little.</summary>
        public static double HoldSeconds(double cycleSeconds) =>
            Math.Max(0.0, cycleSeconds) + ShipElectronicWarfareTuning.HoldPaddingSeconds;

        /// <summary>An Ion Web's slow in percent on this hull: the Interceptor's strength bonus, capped.</summary>
        public static int WebPercent(int basePercent, ShipRole role)
        {
            var percent = role == ShipRole.Interceptor
                ? (int)Math.Round(basePercent * (100 + ShipElectronicWarfareTuning.InterceptorStrengthPercent) / 100.0, MidpointRounding.AwayFromZero)
                : basePercent;
            return Math.Clamp(percent, 0, ShipElectronicWarfareTuning.MaximumWebPercent);
        }

        /// <summary>An Ion Web's range on this hull: the Interceptor's +50% and the Republic Scout's +20% add together.</summary>
        public static float WebRange(int baseRange, ShipRole role, string hullTag)
        {
            var percent = (role == ShipRole.Interceptor ? ShipElectronicWarfareTuning.InterceptorRangePercent : 0) +
                          (hullTag == ShipElectronicWarfareTuning.RepublicScoutTag ? ShipElectronicWarfareTuning.RepublicScoutWebRangePercent : 0);
            return baseRange * (100 + percent) / 100f;
        }

        /// <summary>An Interdictor's range on this hull: the Interceptor's +50%.</summary>
        public static float InterdictorRange(int baseRange, ShipRole role) =>
            role == ShipRole.Interceptor
                ? baseRange * (100 + ShipElectronicWarfareTuning.InterceptorRangePercent) / 100f
                : baseRange;

        /// <summary>Whether a hull ignores Ion Webs (the Barracuda's signature).</summary>
        public static bool IsWebImmune(string hullTag) => hullTag == ShipElectronicWarfareTuning.BarracudaTag;

        /// <summary>Holds a ship for one more cycle. A longer hold already running is kept.</summary>
        public static void Interdict(ShipStatus status, double seconds, DateTime now)
        {
            var until = now.AddSeconds(seconds);
            if (until > status.InterdictedUntil)
                status.InterdictedUntil = until;
        }

        /// <summary>
        /// Jams a ship for one more cycle: its Accuracy drops by <paramref name="penalty"/> (a negative number). Each
        /// strength keeps its own expiry, so while jams or E-War Array penalties overlap the strongest active one counts,
        /// and a weaker one renewing never keeps a stronger one alive.
        /// </summary>
        public static void Jam(ShipStatus status, int penalty, double seconds, DateTime now)
        {
            status.AccuracyPenalties ??= new Dictionary<int, DateTime>();
            ShipTimedStrengthRules.Refresh(status.AccuracyPenalties, penalty, now.AddSeconds(seconds), now);
        }

        /// <summary>
        /// Paints a ship for one more cycle: it takes <paramref name="percent"/> more damage. The strongest active paint
        /// counts, and a weaker one renewing never keeps a stronger one alive.
        /// </summary>
        public static void Paint(ShipStatus status, int percent, double seconds, DateTime now)
        {
            status.Paints ??= new Dictionary<int, DateTime>();
            ShipTimedStrengthRules.Refresh(status.Paints, percent, now.AddSeconds(seconds), now);
        }

        /// <summary>
        /// Webs a ship for one more cycle at <paramref name="percent"/> slow. The strongest active web slows it, a weaker
        /// one never cuts a stronger one short, and a weaker one renewing never keeps a stronger one alive.
        /// </summary>
        public static void Web(ShipStatus status, int percent, double seconds, DateTime now)
        {
            status.IonWebs ??= new Dictionary<int, DateTime>();
            ShipTimedStrengthRules.Refresh(status.IonWebs, percent, now.AddSeconds(seconds), now);
        }

        /// <summary>Whether the module type debuffs an enemy ship it targets (it needs one, and PvP rules apply).</summary>
        public static bool IsHostile(ShipModuleType type) =>
            type is ShipModuleType.IonWeb or ShipModuleType.Interdictor or ShipModuleType.Jammer or ShipModuleType.TargetPainter;
    }
}
