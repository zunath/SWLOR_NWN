using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Sensor Sweep, the Probe Launcher's pulse mode (plan section 24): it lists every ship within range on the Scanner
    /// tab, cloaked ships included (plan section 32), on its own cooldown.
    /// </summary>
    public static class SensorSweepRules
    {
        /// <summary>The sweep range in meters of a Probe Launcher of this rank.</summary>
        public static int Range(int rank) => ScanningTuning.SweepRangeBase + ScanningTuning.SweepRangePerRank * Math.Max(0, rank);

        /// <summary>When a ship that last swept at <paramref name="lastSweep"/> can sweep again.</summary>
        public static DateTime ReadyAt(DateTime? lastSweep) =>
            lastSweep?.AddSeconds(ScanningTuning.SweepCooldownSeconds) ?? DateTime.MinValue;

        public static bool IsReady(DateTime? lastSweep, DateTime now) => now >= ReadyAt(lastSweep);

        /// <summary>Whole seconds until the next sweep, 0 when ready.</summary>
        public static int SecondsLeft(DateTime? lastSweep, DateTime now) =>
            lastSweep == null ? 0 : Math.Max(0, (int)Math.Ceiling((ReadyAt(lastSweep) - now).TotalSeconds));

        /// <summary>Whether a sweep lists a ship: within range, cloaked or not.</summary>
        public static bool Includes(float distance, int range, bool cloaked) => distance <= range;

        /// <summary>Whether a sweep's list is still shown.</summary>
        public static bool IsResultFresh(DateTime sweptAt, DateTime now) =>
            now < sweptAt.AddSeconds(ScanningTuning.SweepResultSeconds);

        /// <summary>The contacts a sweep lists: nearest first, at most <see cref="ScanningTuning.SweepMaximumShips"/>.</summary>
        public static List<SensorSweepContact> Order(IEnumerable<SensorSweepContact> contacts) =>
            contacts.OrderBy(x => x.Distance).ThenBy(x => x.Name, StringComparer.Ordinal).Take(ScanningTuning.SweepMaximumShips).ToList();

        /// <summary>A contact's line on the Scanner tab.</summary>
        public static string Describe(SensorSweepContact contact) =>
            $"{contact.Name} ({contact.Size}, {contact.Distance:0} m)" +
            (contact.Hostile ? ", hostile" : string.Empty) +
            (contact.Cloaked ? ", cloaked" : string.Empty);
    }
}
