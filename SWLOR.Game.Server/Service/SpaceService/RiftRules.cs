using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The rift network (plan section 23): each Wild Space system keeps about two rifts open to a known orbit or another
    /// Wild Space system. A rift lives 4-24 hours, its size class caps the hulls through it, every passage spends its
    /// mass, and a collapse warns the ships inside first. Pure rules; numbers come from <see cref="WildSpaceTuning"/>.
    /// </summary>
    public static class RiftRules
    {
        /// <summary>Picks a size class by weight. <paramref name="next"/> returns 0 to its argument minus 1.</summary>
        public static RiftSizeClass RollSize(Func<int, int> next)
        {
            var classes = WildSpaceTuning.SizeClasses.OrderBy(x => (int)x.Key).ToList();
            var pick = next(classes.Sum(x => x.Value.Weight));
            foreach (var (size, detail) in classes)
            {
                if (pick < detail.Weight)
                    return size;
                pick -= detail.Weight;
            }

            return classes[^1].Key;
        }

        /// <summary>A rift's lifetime in seconds, uniform over the tuning's hours, to the minute.</summary>
        public static int RollLifetimeSeconds(Func<int, int> next)
        {
            var minimum = WildSpaceTuning.MinimumLifetimeHours * 60;
            var maximum = WildSpaceTuning.MaximumLifetimeHours * 60;
            return (minimum + next(maximum - minimum + 1)) * 60;
        }

        /// <summary>A new rift with its size class's full mass budget.</summary>
        public static RiftConnection Create(string id, string wildSpaceArea, string farArea, RiftSizeClass size, DateTime now, int lifetimeSeconds)
        {
            return new RiftConnection
            {
                Id = id,
                AreaA = wildSpaceArea,
                AreaB = farArea,
                Size = size,
                OpenedAt = now,
                CollapsesAt = now.AddSeconds(lifetimeSeconds),
                MassRemaining = MassBudget(size),
            };
        }

        public static int MassBudget(RiftSizeClass size) =>
            WildSpaceTuning.SizeClasses.TryGetValue(size, out var detail) ? detail.MassBudget : 0;

        /// <summary>The largest hull a size class lets through. Only Large rifts take Capital hulls.</summary>
        public static ShipSize MaxHull(RiftSizeClass size) =>
            WildSpaceTuning.SizeClasses.TryGetValue(size, out var detail) ? detail.MaxHull : ShipSize.Invalid;

        public static bool Admits(RiftSizeClass size, ShipSize hull) =>
            hull != ShipSize.Invalid && (int)hull <= (int)MaxHull(size);

        /// <summary>The mass one passage of a hull spends.</summary>
        public static int PassageMass(ShipSize hull) =>
            WildSpaceTuning.PassageMass.TryGetValue(hull, out var mass) ? mass : WildSpaceTuning.PassageMass[ShipSize.Capital];

        /// <summary>Whether a rift is still open: not collapsed and before its collapse time.</summary>
        public static bool IsOpen(RiftConnection rift, DateTime now) =>
            rift != null && !rift.Collapsed && now < rift.CollapsesAt;

        /// <summary>Why a hull cannot cross a rift right now, or None.</summary>
        public static RiftPassageRefusal GetPassageRefusal(RiftConnection rift, ShipSize hull, DateTime now)
        {
            if (!IsOpen(rift, now))
                return RiftPassageRefusal.Collapsed;
            if (!Admits(rift.Size, hull))
                return RiftPassageRefusal.HullTooLarge;
            if (rift.MassRemaining < PassageMass(hull))
                return RiftPassageRefusal.MassSpent;
            return RiftPassageRefusal.None;
        }

        /// <summary>
        /// Spends a passage's mass. A rift whose budget is spent starts to collapse: it closes after the warning time
        /// (or at its own end, if sooner), and the warning goes out on the next network check.
        /// </summary>
        public static void SpendPassage(RiftConnection rift, ShipSize hull, DateTime now)
        {
            rift.MassRemaining = Math.Max(0, rift.MassRemaining - PassageMass(hull));
            if (rift.MassRemaining >= PassageMass(ShipSize.Small))
                return;

            var collapse = now.AddSeconds(WildSpaceTuning.WarningSeconds);
            if (collapse < rift.CollapsesAt)
                rift.CollapsesAt = collapse;
        }

        public static bool IsWarningDue(RiftConnection rift, DateTime now) =>
            !rift.Collapsed && !rift.WarningSent && now >= rift.CollapsesAt.AddSeconds(-WildSpaceTuning.WarningSeconds);

        public static bool IsCollapseDue(RiftConnection rift, DateTime now) =>
            !rift.Collapsed && now >= rift.CollapsesAt;

        /// <summary>The open rifts touching an area.</summary>
        public static int OpenConnections(string areaResref, IEnumerable<RiftConnection> rifts, DateTime now) =>
            rifts.Count(x => x.Touches(areaResref) && IsOpen(x, now));

        /// <summary>
        /// Whether a ship of this hull in an area still has a way out: an open rift touching the area that takes the
        /// hull and has the mass for its passage.
        /// </summary>
        public static bool HasExit(string areaResref, ShipSize hull, IEnumerable<RiftConnection> rifts, DateTime now) =>
            rifts.Any(x => x.Touches(areaResref) && GetPassageRefusal(x, hull, now) == RiftPassageRefusal.None);

        /// <summary>
        /// Where a new rift from a Wild Space system leads: a random known orbit, or (the rest of the time) another
        /// Wild Space system still short of its connections. With none short, an orbit.
        /// </summary>
        public static string PickDestination(string fromArea, IReadOnlyDictionary<string, int> openBySystem, Func<int, int> next)
        {
            var orbits = WildSpaceTuning.KnownOrbits;
            if (next(100) < WildSpaceTuning.OrbitDestinationPercent)
                return orbits[next(orbits.Length)];

            var candidates = WildSpaceTuning.Systems
                .Select(x => x.AreaResref)
                .Where(x => x != fromArea && (!openBySystem.TryGetValue(x, out var open) || open < WildSpaceTuning.ConnectionsPerSystem))
                .ToList();
            return candidates.Count > 0 ? candidates[next(candidates.Count)] : orbits[next(orbits.Length)];
        }

        /// <summary>
        /// The Wild Space systems that need a new rift now: fewer open connections than the target, counting those
        /// already promised. Each system's count includes rifts from other Wild Space systems that reach it.
        /// </summary>
        public static List<string> SystemsShort(IEnumerable<RiftConnection> rifts, IReadOnlyDictionary<string, int> pending, DateTime now)
        {
            var list = rifts.ToList();
            return WildSpaceTuning.Systems
                .Select(x => x.AreaResref)
                .Where(x => OpenConnections(x, list, now) + (pending.TryGetValue(x, out var p) ? p : 0) < WildSpaceTuning.ConnectionsPerSystem)
                .ToList();
        }

        /// <summary>
        /// When a short system's new rift opens: at once while the network is being seeded (a fresh start, after a
        /// restart), otherwise <see cref="WildSpaceTuning.ReplacementSeconds"/> after the collapse that left it short.
        /// </summary>
        public static DateTime ReplacementTime(bool seeded, DateTime now) =>
            seeded ? now.AddSeconds(WildSpaceTuning.ReplacementSeconds) : now;

        /// <summary>A ship's hull after being thrown out of a collapsing system: the tuning's share of max hull lost, never below 1.</summary>
        public static int HullAfterEjection(int hull, int maxHull)
        {
            var damage = (int)Math.Ceiling(Math.Max(0, maxHull) * WildSpaceTuning.EjectHullPercent / 100.0);
            return Math.Max(1, hull - damage);
        }

        /// <summary>The Scanner tab's rift detail at the threat level: its size, the largest hull and where it leads.</summary>
        public static string Describe(RiftConnection rift, string destinationName) =>
            $"{rift.Size} rift (up to {MaxHull(rift.Size)} hulls) to {destinationName}";

        /// <summary>The message for a refusal.</summary>
        public static string Message(RiftPassageRefusal refusal) => refusal switch
        {
            RiftPassageRefusal.Collapsed => "The rift has collapsed.",
            RiftPassageRefusal.HullTooLarge => "Your hull is too large for this rift.",
            RiftPassageRefusal.MassSpent => "The rift is too unstable to carry your hull's mass.",
            _ => string.Empty,
        };
    }
}
