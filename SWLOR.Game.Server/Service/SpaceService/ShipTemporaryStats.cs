using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public sealed record ShipTemporaryAdjustment(StatType Stat, double Amount, DateTime ExpiresAt, string Family);
    public sealed record ShipControlWindow(DateTime StartedAt, int Applications);
    public sealed record ShipRecoveryReceipt(ShipResource Resource, double Amount, DateTime At);

    public sealed record ShipTemporarySources(IReadOnlyDictionary<StatType, double> Positive, IReadOnlyDictionary<StatType, double> Negative)
    {
        public double Net(StatType stat) => Positive.GetValueOrDefault(stat) - Negative.GetValueOrDefault(stat);
    }

    public static class ShipTemporaryStats
    {
        public static ShipTemporarySources Sources(ShipStatus status, DateTime now)
        {
            status.TemporaryAdjustments.RemoveAll(x => x.ExpiresAt <= now);
            var positive = new Dictionary<StatType, double>(); var negative = new Dictionary<StatType, double>();
            foreach (var group in status.TemporaryAdjustments.GroupBy(x => x.Stat))
            {
                if (Stat.GetStatTypeCategory(group.Key) == StatTypeCategory.BeneficialWhenNegative)
                {
                    positive[group.Key] = group.Where(x => x.Amount > 0).Sum(x => x.Amount);
                    negative[group.Key] = group.Where(x => x.Amount < 0).Select(x => -x.Amount).DefaultIfEmpty(0).Max();
                }
                else
                {
                    positive[group.Key] = group.Where(x => x.Amount > 0).Select(x => x.Amount).DefaultIfEmpty(0).Max();
                    negative[group.Key] = group.Where(x => x.Amount < 0).Sum(x => -x.Amount);
                }
            }
            return new(positive, negative);
        }

        public static IReadOnlyDictionary<StatType, double> Current(ShipStatus status, DateTime now)
        {
            var sources = Sources(status, now);
            return sources.Positive.Keys.Union(sources.Negative.Keys).ToDictionary(stat => stat, sources.Net);
        }

        public static void Add(ShipStatus status, StatType stat, double amount, double seconds, string family, DateTime now)
        {
            if (!ShipFittedStats.StatUnits.ContainsKey(stat) || !double.IsFinite(amount) || !double.IsFinite(seconds) || seconds < 0 || string.IsNullOrEmpty(family))
                throw new ArgumentException("Expected a finite declared ship adjustment and effect family.");
            if (seconds == 0) return;
            // A control family has one strongest source. Refreshing weaker interference cannot add a second penalty.
            var previous = status.TemporaryAdjustments.Where(x => x.Stat == stat && x.Family == family && x.ExpiresAt > now).ToArray();
            if (previous.Any(x => Math.Abs(x.Amount) > Math.Abs(amount))) return;
            status.TemporaryAdjustments.RemoveAll(x => x.Stat == stat && x.Family == family);
            status.TemporaryAdjustments.Add(new(stat, amount, now.AddSeconds(seconds), family));
        }

        public static double ControlDuration(ShipStatus target, string family, double duration, bool hard, DateTime now)
        {
            if (!double.IsFinite(duration) || duration < 0 || string.IsNullOrEmpty(family)) throw new ArgumentOutOfRangeException(nameof(duration));
            if (hard) duration = Math.Min(3, duration);
            if (!target.ControlWindows.TryGetValue(family, out var window) || now >= window.StartedAt.AddSeconds(20))
                window = new(now, 0);
            var multiplier = window.Applications == 0 ? 1 : window.Applications == 1 ? .5 : 0;
            target.ControlWindows[family] = window with { Applications = Math.Min(3, window.Applications + 1) };
            return duration * multiplier;
        }

        public static double ExternalRecoveryAllowance(ShipStatus target, ShipResource resource, double baseMaximum, double requested, DateTime now)
        {
            if (resource == ShipResource.Capacitor || !double.IsFinite(baseMaximum) || baseMaximum < 0 || !double.IsFinite(requested) || requested < 0)
                throw new ArgumentOutOfRangeException(nameof(requested));
            target.ExternalRecoveryReceipts.RemoveAll(x => x.At <= now.AddSeconds(-30));
            var used = target.ExternalRecoveryReceipts.Where(x => x.Resource == resource).Sum(x => x.Amount);
            return Math.Min(requested, Math.Max(0, baseMaximum * .4 - used));
        }
    }
}
