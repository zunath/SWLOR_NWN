using System;
using System.Linq;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// How far from a kill a player still earns its XP (plan section 4, "XP rules", and section 16, bug 10: the range was
    /// 40 m everywhere, shorter than the longest space weapon).
    /// </summary>
    public static class ShipKillXpRules
    {
        /// <summary>The ground range: a player farther than this from the kill earns none of its XP.</summary>
        public const float GroundRange = 40f;

        private static readonly Lazy<float> _longestWeaponRange = new(() =>
            RevampModuleCatalog.Steps
                .Where(x => x.Row == ShipModulePowerType.High)
                .Select(x => (float)x.Range)
                .DefaultIfEmpty(GroundRange)
                .Max());

        /// <summary>The longest range of any High-row module (weapons and extractors).</summary>
        public static float LongestWeaponRange => _longestWeaponRange.Value;

        /// <summary>The kill XP range: in space it covers the longest weapon range, so every shooter is credited.</summary>
        public static float GetRange(bool inSpace)
        {
            return inSpace ? Math.Max(GroundRange, LongestWeaponRange) : GroundRange;
        }
    }
}
