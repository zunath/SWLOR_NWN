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

        /// <summary>
        /// Combat properties a worn piece never carries. EnhancementLevel (104) belongs to the
        /// enhancement modules that slot INTO gear, not to the gear itself - corpus-verified: of
        /// its 812 entries not one sits on an armor, helmet, cape or shield.
        /// </summary>
        private static readonly int[] NotWornProperties = { 104 };

        private static bool IsWorn(ItemFamily family) =>
            family is ItemFamily.Armor or ItemFamily.Helmet or ItemFamily.Cape or ItemFamily.Shield;

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

            return combat
                .Where(stat => !WeaponOnlyCombatProperties.Contains(stat.PropertyId))
                .Where(stat => !IsWorn(family) || !NotWornProperties.Contains(stat.PropertyId))
                .ToList();
        }

        /// <summary>
        /// The multi-entry/exclusive property lists a family's group shows. Weapon Damage Type is
        /// an exclusive CHOICE rather than a numeric row, so it never passed through
        /// <see cref="CombatStatsFor"/>'s filter - which is how a damage-type picker came to sit on
        /// armor despite being in the weapon-only set all along.
        /// </summary>
        public static IReadOnlyList<ItemMultiEntryDefinition> MultiEntryFor(
            ItemFamily family, ItemStatGroup group)
        {
            var definitions = ItemMultiEntryCatalog.All.Where(definition => definition.Context == group);
            if (family is ItemFamily.MeleeWeapon or ItemFamily.RangedWeapon or ItemFamily.Lightsaber)
                return definitions.ToList();

            return definitions
                .Where(definition => !WeaponOnlyCombatProperties.Contains(definition.PropertyId))
                .ToList();
        }
    }
}
