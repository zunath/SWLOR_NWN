using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The pure rules of space auras (see <see cref="SpaceAuras"/>).
    /// </summary>
    public static class SpaceAuraRules
    {
        public const int MaximumAuras = 4;

        /// <summary>
        /// How many auras a commander can hold at once: one, plus one per 5 SOC above 10, up to four. This is the
        /// ground rule (Ability.GetMaxNumberOfAuras), so a space pilot's auras cost the same Social.
        /// </summary>
        public static int MaxAuras(int social)
        {
            return Math.Clamp(1 + (social - 10) / 5, 1, MaximumAuras);
        }

        /// <summary>
        /// Splits an aura's current recipients and the ships that should hold it now into the ships to give the aura
        /// to and the ships to take it from.
        /// </summary>
        public static (List<uint> Add, List<uint> Remove) Reconcile(IEnumerable<uint> current, IEnumerable<uint> desired)
        {
            var currentSet = current.ToHashSet();
            var desiredSet = desired.ToHashSet();
            return (
                desiredSet.Where(x => !currentSet.Contains(x)).ToList(),
                currentSet.Where(x => !desiredSet.Contains(x)).ToList());
        }

        /// <summary>
        /// The recipients that no longer hold this commander's copy of the aura, for example because a stronger
        /// commander's copy replaced it. They leave the recipient list so the next refresh gives them the aura again
        /// once nothing stronger holds them, instead of the ship losing the buff when the stronger aura ends.
        /// </summary>
        public static List<uint> LostCopies(IEnumerable<uint> recipients, Func<uint, bool> holdsOwnCopy)
        {
            return recipients.Where(ship => !holdsOwnCopy(ship)).ToList();
        }

        /// <summary>
        /// Whether a recipient already carries a copy of the aura from a commander at least as strong (as much SOC),
        /// which keeps two commanders' copies from replacing each other every refresh.
        /// </summary>
        public static bool HasEqualOrStrongerCopy(int sourceSocial, IEnumerable<int> otherSourceSocials)
        {
            return otherSourceSocials.Any(other => other >= sourceSocial);
        }
    }
}
