using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.SpaceObjectDefinition;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// What the DM tools' Space tab can spawn: the faction tiers and the Rift Raiders of Wild Space, the ship roles of
    /// each (the trade risk ships too: pirate raiders, convoy haulers and escorts) and where a squad's ships stand around
    /// the DM. Pure data and arithmetic.
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
            "T5.5 Rift Raiders",
        };

        /// <summary>The creature tag prefix of each faction: t1-t6, and rr for the Rift Raiders.</summary>
        private static readonly string[] _prefixes = { "t1", "t2", "t3", "t4", "t5", "t6", "rr" };

        /// <summary>
        /// The Rift Raiders are pirates through and through: their interceptor, fighter and gunship answer the pirate
        /// roles too.
        /// </summary>
        private static readonly Dictionary<string, string> _riftRaiderHulls = new()
        {
            ["pirint"] = "interceptor",
            ["pirftr"] = "fighter",
            ["pirgun"] = "gunship",
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
            ("Carrier (Boss)", "carrier"),
            ("Capital (Boss)", "capital"),
            ("Pirate Interceptor", "pirint"),
            ("Pirate Fighter", "pirftr"),
            ("Pirate Gunship", "pirgun"),
            ("Convoy Hauler (with escorts)", "convoy"),
            ("Convoy Escort", "escort"),
        };

        private static readonly HashSet<string> _presetTags = RevampNpcShipCatalog.Ships.Select(x => x.CreatureTag).ToHashSet();

        /// <summary>
        /// The creature tag of a faction's ship in a role, or empty when it has no such ship (the Imperial tier has no
        /// generic capital; its capitals are the Sith ones; the Rift Raiders fly no haulers, platforms or capitals).
        /// </summary>
        public static string CreatureTag(int factionIndex, int roleIndex)
        {
            if (factionIndex < 0 || factionIndex >= Factions.Count || roleIndex < 0 || roleIndex >= Roles.Count)
                return string.Empty;

            var prefix = _prefixes[factionIndex];
            var hull = Roles[roleIndex].Hull;
            if (prefix == "rr" && _riftRaiderHulls.TryGetValue(hull, out var raiderHull))
                hull = raiderHull;

            var tag = prefix + hull;
            return _presetTags.Contains(tag) ? tag : string.Empty;
        }

        /// <summary>The faction tier (1-6) a convoy of the picked faction flies at, or 0 for the Rift Raiders, who run none.</summary>
        public static int ConvoyTier(int factionIndex) =>
            factionIndex >= 0 && factionIndex < _prefixes.Length && _prefixes[factionIndex] != "rr" ? factionIndex + 1 : 0;

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