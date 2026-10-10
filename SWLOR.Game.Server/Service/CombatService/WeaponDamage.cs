using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>Resolves an item's conditional weapon rating before damage formula and proc bonuses.</summary>
    public static class WeaponDamage
    {
        public const int NaturalSingleWeaponPercent = 20;

        public static bool IsSingleWeaponType(BaseItem type) =>
            Item.OneHandedMeleeItemTypes.Contains(type) || Item.PistolBaseItemTypes.Contains(type) ||
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
