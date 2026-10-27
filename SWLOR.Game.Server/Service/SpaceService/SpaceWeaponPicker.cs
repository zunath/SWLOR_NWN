using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// A fitted weapon module as a space ability sees it when it picks one to fire.
    /// </summary>
    public class SpaceWeaponCandidate
    {
        /// <summary>The hotbar slot number (1-30).</summary>
        public int Slot { get; set; }

        /// <summary>The module's rank. The highest rank fires.</summary>
        public int Rank { get; set; }

        public SpaceWeaponFamily Family { get; set; }

        /// <summary>How far the module reaches, in meters.</summary>
        public float Range { get; set; }

        /// <summary>
        /// The module may fire now: its seat is the Helm or is crewed, and the module's own checks (ammo, target
        /// size) pass for the target.
        /// </summary>
        public bool Usable { get; set; }

        /// <summary>The fitted module the candidate stands for, for the caller's use.</summary>
        public object Source { get; set; }
    }

    /// <summary>
    /// Which weapon a space ability fires: the highest-ranked usable module of the requested family that reaches
    /// the target.
    /// </summary>
    public static class SpaceWeaponPicker
    {
        /// <summary>Whether a weapon family passes a filter.</summary>
        public static bool Matches(SpaceWeaponFilter filter, SpaceWeaponFamily family)
        {
            return filter switch
            {
                SpaceWeaponFilter.Energy => family == SpaceWeaponFamily.Energy,
                SpaceWeaponFilter.Launcher => family == SpaceWeaponFamily.Launcher,
                _ => true,
            };
        }

        /// <summary>
        /// A module in a Helm slot always works. A module in a crew station works only while the seat is crewed.
        /// A slot with no tag (an older ship) counts as the Helm.
        /// </summary>
        public static bool IsSeatUsable(ShipSlotTag? tag, bool seatCrewed)
        {
            return !tag.HasValue || !ShipStatCalculator.IsStation(tag.Value) || seatCrewed;
        }

        /// <summary>
        /// The usable module of the filter's family with the highest rank whose range reaches
        /// <paramref name="distance"/>. Equal ranks go to the lower slot number. Null when none qualifies.
        /// </summary>
        public static SpaceWeaponCandidate Pick(IEnumerable<SpaceWeaponCandidate> candidates, SpaceWeaponFilter filter, float distance)
        {
            return candidates
                .Where(x => x.Usable && Matches(filter, x.Family) && x.Range >= distance)
                .OrderByDescending(x => x.Rank)
                .ThenBy(x => x.Slot)
                .FirstOrDefault();
        }

        /// <summary>The longest range among usable modules of the filter's family; 0 when there are none.</summary>
        public static float LongestRange(IEnumerable<SpaceWeaponCandidate> candidates, SpaceWeaponFilter filter)
        {
            return candidates
                .Where(x => x.Usable && Matches(filter, x.Family))
                .Select(x => x.Range)
                .DefaultIfEmpty(0f)
                .Max();
        }
    }
}
