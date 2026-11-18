using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The astromech player species.
    /// </summary>
    public static class Astromech
    {
        /// <summary>The astromech voice set: its row in soundset.2da.</summary>
        public const int SoundSetId = 954;

        /// <summary>The portrait a new astromech starts with: the resref without its size letter.</summary>
        public const string DefaultPortrait = "po_astro01_";

        /// <summary>The stat-less plating a new astromech wears, as others wear Traveler's Clothes.</summary>
        public const string StarterPlatingResref = "am_plat_start";

        public const string HeadScaleMessage = "An astromech's dome is seated on its barrel and keeps its size.";

        public static bool IsAstromech(uint creature)
        {
            return GetRacialType(creature) == RacialType.Astromech;
        }
    }
}
