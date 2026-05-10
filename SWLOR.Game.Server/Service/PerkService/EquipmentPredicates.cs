using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service.PerkService
{
    public static class EquipmentPredicates
    {
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

        public static bool HasMainHandVibroknife(uint creature)
        {
            return HasItemInSlot(creature, InventorySlot.RightHand, Item.VibroknifeBaseItemTypes);
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
