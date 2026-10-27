using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.SpaceObjectDefinition;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// What the DM tools' Space tab can spawn: the faction tiers, the ship roles of each tier and where a squad's
    /// ships stand around the DM. Pure data and arithmetic.
    /// </summary>
    public static class SpaceSquadCatalog
    {
        public const int MaxSquadSize = 6;

        /// <summary>The ships stand on a ring this far from the DM, so they spawn clear of each other.</summary>
        public const float SpawnRingMeters = 12f;

        public static readonly IReadOnlyList<string> Factions = new[]
        {
            "T1 Septra Khanate",
            "T2 Krakenborne",
            "T3 Snowdrift Syndicate",
            "T4 Clan Zel Arun",
            "T5 Red Hand",
            "T6 Imperial",
        };

        /// <summary>The roles, with the hull type that names a tier's creature (t3 + "sniper").</summary>
        public static readonly IReadOnlyList<(string Label, string Hull)> Roles = new[]
        {
            ("Fighter", "fighter"),
            ("Interceptor", "interceptor"),
            ("Bomber", "bomber"),
            ("Sniper", "sniper"),
            ("Electronic Warfare", "ewar"),
            ("Gunship (tank)", "gunship"),
            ("Logistics", "logistics"),
            ("Minelayer", "minelayer"),
            ("Cargo Hauler", "cargo"),
            ("Defense Platform (Elite)", "platform"),
            ("Carrier (Elite)", "carrier"),
            ("Capital (Boss)", "capital"),
        };

        private static readonly HashSet<string> _presetTags = RevampNpcShipCatalog.Ships.Select(x => x.CreatureTag).ToHashSet();

        /// <summary>
        /// The creature tag of a faction tier's ship in a role, or empty when that tier has no such ship (the
        /// Imperial tier has no generic capital; its capitals are the Sith ones).
        /// </summary>
        public static string CreatureTag(int factionIndex, int roleIndex)
        {
            if (factionIndex < 0 || factionIndex >= Factions.Count || roleIndex < 0 || roleIndex >= Roles.Count)
                return string.Empty;

            var tag = $"t{factionIndex + 1}{Roles[roleIndex].Hull}";
            return _presetTags.Contains(tag) ? tag : string.Empty;
        }

        /// <summary>A squad size from the count picker's index: 1 to <see cref="MaxSquadSize"/>.</summary>
        public static int SquadSize(int countIndex)
        {
            return Math.Clamp(countIndex + 1, 1, MaxSquadSize);
        }

        /// <summary>Where ship <paramref name="index"/> of a squad stands, relative to the DM, evenly around a ring.</summary>
        public static (float X, float Y) Offset(int index, int count)
        {
            var angle = 2.0 * Math.PI * index / Math.Max(1, count);
            return ((float)(Math.Cos(angle) * SpawnRingMeters), (float)(Math.Sin(angle) * SpawnRingMeters));
        }
    }
}