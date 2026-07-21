using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// Base item 11 is attached to the bow hand by the client even when its 2DA wield type is
    /// changed to sling. While a shield is equipped, temporarily use a custom sling-attached
    /// base item with otherwise identical pistol data so both models occupy the correct hands.
    /// </summary>
    public static class PistolShieldAttachment
    {
        [NWNEventHandler(ScriptName.OnModuleEquip)]
        public static void OnEquip()
        {
            var creature = GetPCItemLastEquippedBy();
            if (!CanSynchronize(creature))
                return;

            Synchronize(creature);
        }

        [NWNEventHandler(ScriptName.OnModuleUnequip)]
        public static void OnUnequip()
        {
            var creature = GetPCItemLastUnequippedBy();
            if (!CanSynchronize(creature))
                return;

            var unequippedItem = GetPCItemLastUnequipped();
            if (GetIsObjectValid(unequippedItem) &&
                GetBaseItemType(unequippedItem) == BaseItem.PistolWithShield)
            {
                ItemPlugin.SetBaseItemType(unequippedItem, BaseItem.Pistol);
            }

            Synchronize(creature);
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void OnClientEnter()
        {
            var creature = GetEnteringObject();
            if (!CanSynchronize(creature))
                return;

            // The visual form should only persist while equipped. Repair any item which was saved
            // in inventory during an interrupted unequip or server shutdown.
            for (var item = GetFirstItemInInventory(creature);
                 GetIsObjectValid(item);
                 item = GetNextItemInInventory(creature))
            {
                if (GetBaseItemType(item) == BaseItem.PistolWithShield)
                {
                    ItemPlugin.SetBaseItemType(item, BaseItem.Pistol);
                }
            }

            Synchronize(creature);
        }

        public static BaseItem GetDesiredBaseItem(BaseItem currentBaseItem, bool hasShield)
        {
            if (currentBaseItem == BaseItem.Pistol && hasShield)
                return BaseItem.PistolWithShield;

            if (currentBaseItem == BaseItem.PistolWithShield && !hasShield)
                return BaseItem.Pistol;

            return currentBaseItem;
        }

        private static bool CanSynchronize(uint creature)
        {
            return GetIsObjectValid(creature) &&
                   (GetIsPC(creature) || Droid.IsDroid(creature)) &&
                   !GetIsDM(creature) &&
                   !GetIsDMPossessed(creature);
        }

        private static void Synchronize(uint creature)
        {
            var pistol = GetItemInSlot(InventorySlot.RightHand, creature);
            if (!GetIsObjectValid(pistol))
                return;

            var currentBaseItem = GetBaseItemType(pistol);
            if (currentBaseItem != BaseItem.Pistol &&
                currentBaseItem != BaseItem.PistolWithShield)
            {
                return;
            }

            var leftHand = GetItemInSlot(InventorySlot.LeftHand, creature);
            var hasShield = GetIsObjectValid(leftHand) &&
                            Item.ShieldBaseItemTypes.Contains(GetBaseItemType(leftHand));
            var desiredBaseItem = GetDesiredBaseItem(currentBaseItem, hasShield);

            if (desiredBaseItem == currentBaseItem)
                return;

            ItemPlugin.SetBaseItemType(pistol, desiredBaseItem);
            RefreshEquippedItemAppearance(creature);
        }

        private static void RefreshEquippedItemAppearance(uint creature)
        {
            var creaturePointer = NWNXUtils.GetGameObject(creature);
            if (creaturePointer == nint.Zero)
                return;

            var nativeCreature = CNWSCreature.FromPointer(creaturePointer);
            nativeCreature?.UpdateAppearanceForEquippedItems();
        }
    }
}
