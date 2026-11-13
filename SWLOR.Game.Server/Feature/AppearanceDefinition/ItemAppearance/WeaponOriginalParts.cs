using System.Linq;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    /// <summary>
    /// A weapon that comes with a part missing from its type's shared appearance editor list, such as a rare
    /// drop's unique blade, keeps that part as its own. The editor offers it on that weapon only, so the player
    /// can restyle the weapon and switch back later while other weapons never see the part.
    /// Part IDs are the editor's color * 100 + model.
    /// </summary>
    public static class WeaponOriginalParts
    {
        private const string VariablePrefix = "APPEARANCE_EDITOR_ORIGINAL_WEAPON_PART_";

        /// <summary>The weapon's own part for one slot, remembered on the item the first time the editor sees it.</summary>
        public static int Remember(uint item, AppearanceWeapon part, int current, int[] shared)
        {
            var variable = VariablePrefix + (int)part;
            var recorded = GetLocalInt(item, variable);
            var original = Resolve(recorded, current, shared);
            if (original != recorded)
                SetLocalInt(item, variable, original);
            return original;
        }

        /// <summary>
        /// The part a weapon keeps as its own: the one already recorded, otherwise its current part when the
        /// shared list does not offer it, otherwise none (0).
        /// </summary>
        public static int Resolve(int recorded, int current, int[] shared)
        {
            if (recorded > 0)
                return recorded;
            return current > 0 && !shared.Contains(current) ? current : 0;
        }

        /// <summary>The parts one weapon may use in a slot: the shared list plus its own part, in ID order.</summary>
        public static int[] GetOptions(int[] shared, int original) =>
            original <= 0 || shared.Contains(original)
                ? shared
                : shared.Append(original).OrderBy(id => id).ToArray();
    }
}
