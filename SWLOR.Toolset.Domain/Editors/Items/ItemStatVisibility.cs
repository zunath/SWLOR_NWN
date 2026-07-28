namespace SWLOR.Toolset.Domain.Editors.Items
{
    /// <summary>
    /// Which <see cref="ItemStatGroup"/>s a family shows up front versus tucked away as
    /// secondary. Miscellaneous has no primary groups of its own - its stats are unlocked by
    /// <see cref="ItemRoleCatalog.GroupsUnlockedBy"/> once a role is chosen.
    /// </summary>
    public static class ItemStatVisibility
    {
        private static readonly ItemStatGroup[] WeaponPrimary =
        {
            ItemStatGroup.Combat, ItemStatGroup.Vitals, ItemStatGroup.Utility
        };

        private static readonly ItemStatGroup[] WornArmorPrimary =
        {
            ItemStatGroup.Defense, ItemStatGroup.Resistance, ItemStatGroup.Vitals, ItemStatGroup.Combat,
            ItemStatGroup.Utility
        };

        private static readonly ItemStatGroup[] AccessoryOrToolPrimary =
        {
            ItemStatGroup.Defense, ItemStatGroup.Resistance, ItemStatGroup.Vitals, ItemStatGroup.Combat,
            ItemStatGroup.Crafting, ItemStatGroup.Utility
        };

        private static readonly ItemStatGroup[] EssencePrimary =
        {
            ItemStatGroup.Defense, ItemStatGroup.Resistance, ItemStatGroup.Vitals, ItemStatGroup.Combat,
            ItemStatGroup.Utility
        };

        private static readonly ItemStatGroup[] NpcPrimary = { ItemStatGroup.Npc };

        /// <summary>Non-weapon families exclude the weapon-only rows from Combat.</summary>
        private static readonly int[] WeaponOnlyCombatProperties = { 93, 98, 103, 134 }; // DMG, Delay, DamageStat, WeaponDamageType

        public static IReadOnlyList<ItemStatGroup> PrimaryGroups(ItemFamily family) => family switch
        {
            ItemFamily.MeleeWeapon or ItemFamily.RangedWeapon or ItemFamily.Lightsaber => WeaponPrimary,
            ItemFamily.Armor or ItemFamily.Helmet or ItemFamily.Cape or ItemFamily.Shield => WornArmorPrimary,
            ItemFamily.Accessory or ItemFamily.Tool => AccessoryOrToolPrimary,
            ItemFamily.CreatureItem => NpcPrimary,
            ItemFamily.Essence => EssencePrimary,
            _ => Array.Empty<ItemStatGroup>()
        };

        public static IReadOnlyList<ItemStatGroup> SecondaryGroups(ItemFamily family)
        {
            var primary = PrimaryGroups(family);
            return Enum.GetValues<ItemStatGroup>()
                .Where(group => !primary.Contains(group))
                .ToList();
        }

        /// <summary>
        /// The Combat group's stats for <paramref name="family"/>: weapons see every Combat stat;
        /// everything else drops the weapon-only rows (DMG, Delay, DamageStat, WeaponDamageType).
        /// </summary>
        public static IReadOnlyList<ItemStatDefinition> CombatStatsFor(ItemFamily family)
        {
            var combat = ItemStatCatalog.ByGroup(ItemStatGroup.Combat);
            if (family is ItemFamily.MeleeWeapon or ItemFamily.RangedWeapon or ItemFamily.Lightsaber)
                return combat;

            return combat.Where(stat => !WeaponOnlyCombatProperties.Contains(stat.PropertyId)).ToList();
        }
    }
}
