using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service.ItemService
{
    /// <summary>
    /// The data-only part of the shared economy classifier (see <c>Item.IsEconomyRestricted</c>): the rules that need no
    /// engine and no state. It stays a stateless static class because the module editor applies it to UTI data offline,
    /// where there is no service container for the <c>Item</c> facade to resolve from. <c>ItemService</c> calls it, so
    /// the server and the editor share the one definition.
    /// </summary>
    public static class EconomyRestriction
    {
        /// <summary>
        /// Data-only form of the shared economy classifier. Builder tools use this overload while
        /// inspecting UTI data that has not been instantiated by the game engine.
        /// </summary>
        public static bool IsRestricted(
            BaseItem baseItem,
            string name,
            bool noEconomy,
            bool hasInventoryIcon)
        {
            return IsRestrictedBaseItem(baseItem) ||
                   noEconomy ||
                   IsRestrictedName(name) ||
                   !hasInventoryIcon;
        }

        /// <summary>
        /// The name-based portion of the classifier, split out so it can be unit
        /// tested without spawning an item. A blank name denotes an internal/unfinished blueprint.
        /// Name prefixes the builders reserve for NPC-only gear are anchored to the start of the item name.
        /// </summary>
        /// <param name="name">The item's display name.</param>
        /// <returns>true if the name marks the item as NPC-only or internal.</returns>
        public static bool IsRestrictedName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return true;

            var trimmed = name.TrimStart();

            foreach (var prefix in new[] { "[NPC]", "(NPC" })
            {
                if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Creature-equipment base item types. Players never trade these; the "stat skins" that carry
        /// a creature's combat stats are <see cref="BaseItem.CreatureItem"/>.
        /// </summary>
        private static bool IsRestrictedBaseItem(BaseItem baseItem)
        {
            return baseItem is BaseItem.Invalid
                or BaseItem.CompanionPowerGlove
                or BaseItem.CreatureSlashWeapon
                or BaseItem.CreaturePierceWeapon
                or BaseItem.CreatureBludgeonWeapon
                or BaseItem.CreatureSlashPierceWeapon
                or BaseItem.CreatureItem;
        }
    }
}
