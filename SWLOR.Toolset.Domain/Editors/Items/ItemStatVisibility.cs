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

        /// <summary>
        /// Creature items are the NPC stat surface: a creature "stat skin" is where an NPC's
        /// statline is actually authored, and a creature weapon carries its own DMG and delay. They
        /// get the NPC rows plus the full combat statline - corpus-verified, creature blueprints
        /// carry FP, STM, DMG, defenses, attack, evasion and resistances.
        /// </summary>
        private static readonly ItemStatGroup[] NpcPrimary =
        {
            ItemStatGroup.Npc, ItemStatGroup.Combat, ItemStatGroup.Vitals, ItemStatGroup.Defense,
            ItemStatGroup.Resistance, ItemStatGroup.Utility
        };

        /// <summary>Non-weapon families exclude the weapon-only rows from Combat.</summary>
        private static readonly int[] WeaponOnlyCombatProperties = { 93, 98, 103, 134 }; // DMG, Delay, DamageStat, WeaponDamageType

        /// <summary>
        /// Combat properties a worn piece never carries. EnhancementLevel (104) belongs to the
        /// enhancement modules that slot INTO gear, not to the gear itself - corpus-verified: of
        /// its 812 entries not one sits on an armor, helmet, cape or shield.
        /// </summary>
        private static readonly int[] NotWornProperties = { 104 };

        /// <summary>
        /// Families that swing something. The creature families are included because base items
        /// 69-72 are literally creature weapons and their skins are where an NPC's damage and delay
        /// are authored - leaving them out meant a new creature weapon had nowhere to set DMG.
        /// </summary>
        public static bool CarriesWeaponCombatStats(ItemFamily family) =>
            family is ItemFamily.MeleeWeapon or ItemFamily.RangedWeapon or ItemFamily.Lightsaber
                or ItemFamily.CreatureItem;

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
        /// The Combat group's stats for <paramref name="family"/>: anything that swings something
        /// (see <see cref="CarriesWeaponCombatStats"/>) sees every Combat stat; everything else
        /// drops the weapon-only rows (DMG, Delay, DamageStat, WeaponDamageType).
        /// </summary>
        /// <param name="storedProperties">
        /// What this particular blueprint already carries. A filtered-out row that the item
        /// nonetheless HAS is shown anyway - hiding it would leave a real stored value invisible
        /// and uneditable, which is worse than showing a row the family does not usually want.
        /// </param>
        public static IReadOnlyList<ItemStatDefinition> CombatStatsFor(
            ItemFamily family, IReadOnlySet<int>? storedProperties = null)
        {
            var combat = ItemStatCatalog.ByGroup(ItemStatGroup.Combat);
            if (CarriesWeaponCombatStats(family))
                return combat;

            bool IsStored(int propertyId) => storedProperties?.Contains(propertyId) == true;

            return combat
                .Where(stat => IsStored(stat.PropertyId) || !WeaponOnlyCombatProperties.Contains(stat.PropertyId))
                .Where(stat => IsStored(stat.PropertyId) || !IsWorn(family) || !NotWornProperties.Contains(stat.PropertyId))
                .ToList();
        }

        /// <summary>
        /// The multi-entry/exclusive property lists a family's group shows. Weapon Damage Type is
        /// an exclusive CHOICE rather than a numeric row, so it never passed through
        /// <see cref="CombatStatsFor"/>'s filter - which is how a damage-type picker came to sit on
        /// armor despite being in the weapon-only set all along.
        /// </summary>
        /// <remarks>
        /// Only WORN gear drops it. An Essence is an enhancement module that grants a damage type
        /// to whatever it slots into, and 23 corpus essences carry property 134 - filtering it off
        /// every non-weapon family made those uneditable, which is a worse bug than the one that
        /// filter was added to fix.
        /// </remarks>
        public static IReadOnlyList<ItemMultiEntryDefinition> MultiEntryFor(
            ItemFamily family, ItemStatGroup group, IReadOnlySet<int>? storedProperties = null)
        {
            var definitions = ItemMultiEntryCatalog.All.Where(definition => definition.Context == group);
            if (!IsWorn(family))
                return definitions.ToList();

            return definitions
                .Where(definition => storedProperties?.Contains(definition.PropertyId) == true ||
                                     !WeaponOnlyCombatProperties.Contains(definition.PropertyId))
                .ToList();
        }
    }
}
