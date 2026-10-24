using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>Resolves an item's conditional weapon rating before damage formula and proc bonuses.</summary>
    public static class WeaponDamage
    {
        public const int NaturalSingleWeaponPercent = 20;

        // Spears are two-handed despite their legacy equipment classification.
        // Power Gloves always strike as a pair, like two katars.
        public static bool IsSingleWeaponType(BaseItem type) =>
            Item.OneHandedMeleeItemTypes.Contains(type) && !Item.SpearBaseItemTypes.Contains(type) &&
            !Item.PowerGloveBaseItemTypes.Contains(type) ||
            Item.PistolBaseItemTypes.Contains(type) ||
            Item.ThrowingWeaponBaseItemTypes.Contains(type);

        public static bool ReceivesNaturalSingleWeaponBonus(uint creature) =>
            GetIsPC(creature) && !GetIsDM(creature) ||
            Droid.IsDroid(creature) && GetIsPC(GetMaster(creature)) && !GetIsDM(GetMaster(creature));

        public static int GetNaturalSingleWeaponPercent(uint creature) =>
            ReceivesNaturalSingleWeaponBonus(creature) ? NaturalSingleWeaponPercent : 0;

        public static int GetSingleWeaponPercent(uint creature) =>
            GetNaturalSingleWeaponPercent(creature) + Stat.GetStatAdjustment(creature, StatType.SingleWeaponDamagePercentAdjustment);

        public static int GetEffectiveDMG(uint creature, uint weapon, int? itemDamage = null)
        {
            var damage = itemDamage ?? Item.GetDMG(weapon);
            // GetDMG supplies a synthetic minimum when a real rating is absent.
            // Abilities and previews must preserve that fallback just like native swings.
            if (!GetItemHasItemProperty(weapon, ItemPropertyType.DMG))
                return damage;
            var qualifies = EquipmentPredicates.HasSingleWeapon(creature) &&
                            GetItemInSlot(InventorySlot.RightHand, creature) == weapon;
            return CalculateEffectiveDMG(damage, qualifies ? GetSingleWeaponPercent(creature) : 0);
        }

        /// <summary>
        /// Weapon DMG an ability adds. Unarmed abilities strike with both Power Gloves, so they count the
        /// owned glove's DMG once per hand; auto-attacks still roll one glove per hand.
        /// </summary>
        public static int GetAbilityDMG(uint creature, uint weapon, SkillType abilitySkill)
        {
            var damage = GetEffectiveDMG(creature, weapon);
            return abilitySkill == SkillType.Unarmed && PowerGloveEquipment.IsActivePair(creature) &&
                   GetItemInSlot(InventorySlot.RightHand, creature) == weapon
                ? damage * 2
                : damage;
        }

        /// <summary>Damage type a weapon deals, matching native auto-attack resolution.</summary>
        public static CombatDamageType GetDamageType(uint weapon)
        {
            // A damage type only retypes a real DMG property; the DMG fallback stays Physical.
            if (!GetIsObjectValid(weapon) || !GetItemHasItemProperty(weapon, ItemPropertyType.DMG))
                return CombatDamageType.Physical;

            var damageType = CombatDamageType.Physical;
            for (var ip = GetFirstItemProperty(weapon); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(weapon))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.WeaponDamageType)
                    damageType = ResolveDamageType(damageType, GetItemPropertySubType(ip));
            }

            return damageType;
        }

        public static CombatDamageType ResolveDamageType(CombatDamageType current, int damageTypeId)
        {
            if (damageTypeId > (int)CombatDamageType.Sonic || damageTypeId < (int)CombatDamageType.Physical)
                return current;

            // Only elements retype weapon damage; Force conversion belongs to Imbuement Stance.
            var candidate = (CombatDamageType)damageTypeId;
            if (!candidate.IsElementalDamageType() || current.IsElementalDamageType())
                return current;

            return candidate;
        }

        public static int CalculateEffectiveDMG(int itemDamage, int percent)
        {
            if (itemDamage <= 0 || percent <= 0)
                return itemDamage;
            var bonus = ((long)itemDamage * percent + 99) / 100;
            return (int)Math.Min(int.MaxValue, itemDamage + bonus);
        }

        public static string BuildSingleWeaponDescription(int itemDamage, int naturalPercent, int perkPercent, bool hasItemDamage = true)
        {
            if (!hasItemDamage)
                return $"Weapon DMG: {itemDamage} (fallback; no DMG property).\nSingle Weapon bonuses require a weapon DMG property.";
            var text = $"Weapon DMG: {itemDamage}\nSingle Weapon: +{naturalPercent}% weapon DMG while wielded with an empty off hand.";
            if (perkPercent > 0)
                text += $"\nDoublehand: Additional +{perkPercent}% weapon DMG.";
            return text + $"\nDMG when wielded alone: {CalculateEffectiveDMG(itemDamage, naturalPercent + perkPercent)} (rounded up once)";
        }
    }
}
