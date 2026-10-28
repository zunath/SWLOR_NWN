using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service.PerkService
{
    public static class EquipmentPredicates
    {
        public static bool HasSingleWeapon(uint creature)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, creature);
            var offHand = GetItemInSlot(InventorySlot.LeftHand, creature);
            return GetIsObjectValid(weapon) && WeaponDamage.IsSingleWeaponType(GetBaseItemType(weapon)) &&
                   !GetIsObjectValid(offHand);
        }

        public static bool HasMainHandLightsaber(uint creature)
        {
            return HasItemInSlot(creature, InventorySlot.RightHand, Item.LightsaberBaseItemTypes);
        }

        public static bool HasMainHandStaff(uint creature)
        {
            return HasItemInSlot(creature, InventorySlot.RightHand, Item.StaffBaseItemTypes);
        }

        public static bool HasMainHandSpear(uint creature)
        {
            return HasItemInSlot(creature, InventorySlot.RightHand, Item.SpearBaseItemTypes);
        }

        public static bool HasPistol(uint creature)
        {
            return HasItemInSlot(creature, InventorySlot.RightHand, Item.PistolBaseItemTypes) ||
                   HasItemInSlot(creature, InventorySlot.LeftHand, Item.PistolBaseItemTypes);
        }

        public static bool HasTwinPistol(uint creature) => TwinPistolEquipment.IsActivePair(creature);

        public static bool HasRifle(uint creature)
        {
            return HasItemInSlot(creature, InventorySlot.RightHand, Item.RifleBaseItemTypes);
        }

        public static bool HasThrowing(uint creature)
        {
            return HasItemInSlot(creature, InventorySlot.RightHand, Item.ThrowingWeaponBaseItemTypes);
        }

        public static bool HasMainHandKatar(uint creature)
        {
            return HasItemInSlot(creature, InventorySlot.RightHand, Item.KatarBaseItemTypes);
        }

        /// <summary>One owned Power Glove with its linked cosmetic companion.</summary>
        public static bool HasUnarmedGrip(uint creature)
        {
            return PowerGloveEquipment.IsActivePair(creature);
        }

        public static bool HasUnarmedGrip(BaseItem? mainHand, BaseItem? offHand) =>
            mainHand == BaseItem.PowerGlove && offHand == BaseItem.CompanionPowerGlove;

        public static bool HasDualWieldKatar(uint creature)
        {
            return HasItemInSlot(creature, InventorySlot.RightHand, Item.KatarBaseItemTypes) &&
                   HasItemInSlot(creature, InventorySlot.LeftHand, Item.KatarBaseItemTypes);
        }

        public static bool HasDualWield(uint creature)
        {
            if (HasTwinPistol(creature)) return true;
            var mainHand = GetItemInSlot(InventorySlot.RightHand, creature);
            var offHand = GetOffhandAttackWeapon(creature);
            return GetIsObjectValid(mainHand) && Item.IsAttackWeaponType(GetBaseItemType(mainHand)) &&
                   GetIsObjectValid(offHand) && Item.IsAttackWeaponType(GetBaseItemType(offHand));
        }

        /// <summary>
        /// Returns the second striking end of a double weapon, the real glove of a Power Glove pair, or a separate
        /// left-hand item.
        /// </summary>
        public static uint GetOffhandAttackWeapon(uint creature)
        {
            var mainHand = GetItemInSlot(InventorySlot.RightHand, creature);
            if (GetIsObjectValid(mainHand) && Item.IsDoubleWeaponType(GetBaseItemType(mainHand))) return mainHand;
            // The cosmetic left glove has no stats; the owned glove supplies both hands' rolls.
            if (PowerGloveEquipment.IsActivePair(creature)) return mainHand;
            var offHand = GetItemInSlot(InventorySlot.LeftHand, creature);
            // The native scheduler must never resolve the companion's inert carrier as a melee roll.
            return GetIsObjectValid(offHand) && GetBaseItemType(offHand) == BaseItem.CompanionPistol
                ? OBJECT_INVALID : offHand;
        }

        public static bool HasMainHandVibroknife(uint creature)
        {
            return HasItemInSlot(creature, InventorySlot.RightHand, Item.VibroknifeBaseItemTypes);
        }

        public static bool HasMainHandVibroblade(uint creature)
        {
            return HasItemInSlot(creature, InventorySlot.RightHand, Item.VibrobladeBaseItemTypes);
        }

        public static bool HasMainHandSaberstaff(uint creature)
        {
            return HasItemInSlot(creature, InventorySlot.RightHand, Item.SaberstaffBaseItemTypes);
        }

        public static bool HasMainHandTwinBlade(uint creature)
        {
            return HasItemInSlot(creature, InventorySlot.RightHand, Item.TwinBladeBaseItemTypes);
        }

        public static bool HasMainHandHeavyVibroblade(uint creature)
        {
            return HasItemInSlot(creature, InventorySlot.RightHand, Item.HeavyVibrobladeBaseItemTypes);
        }

        public static bool HasOffHandShield(uint creature)
        {
            return HasItemInSlot(creature, InventorySlot.LeftHand, Item.ShieldBaseItemTypes);
        }

        private static bool HasItemInSlot(
            uint creature,
            InventorySlot slot,
            IReadOnlyCollection<BaseItem> baseItemTypes)
        {
            var item = GetItemInSlot(slot, creature);
            return Item.IsBaseItemType(item, baseItemTypes);
        }
    }
}
