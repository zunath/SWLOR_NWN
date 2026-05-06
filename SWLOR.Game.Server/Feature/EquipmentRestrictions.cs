using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature
{
    public static class EquipmentRestrictions
    {
        /// <summary>
        /// When an item is equipped, check the custom rules to see if the item can be equipped by the player.
        /// If not able to be used, an error message will be sent and item will not be equipped.
        /// </summary>
        [NWNEventHandler(ScriptName.OnItemEquipValidateBefore)]
        public static void ValidateItemEquip()
        {
            var creature = OBJECT_SELF;
            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            var slot = (InventorySlot)Convert.ToInt32(EventsPlugin.GetEventData("SLOT"));

            var isSwapping = IsItemSwapping(creature, item, slot);
            var canUseItem = Item.CanEquip(creature, item);
            var isRingSwappingPositions = IsRingSwappingPositions(creature, item, slot);

            if (string.IsNullOrWhiteSpace(canUseItem) &&
                !isSwapping &&
                !isRingSwappingPositions)
            {
                EventsPlugin.PushEventData("ITEM", ObjectToString(item));
                EventsPlugin.PushEventData("SLOT", Convert.ToString((int)slot));
                EventsPlugin.SignalEvent("SWLOR_ITEM_EQUIP_VALID_BEFORE", creature);
                return;
            }

            if (!string.IsNullOrWhiteSpace(canUseItem))
            {
                var messageTarget = creature;
                if (Droid.IsDroid(creature))
                {
                    messageTarget = GetMaster(creature);
                }
                SendMessageToPC(messageTarget, ColorToken.Red(canUseItem));
            }
            
            EventsPlugin.SkipEvent();
        }

        private static bool IsItemSwapping(uint creature, uint item, InventorySlot slot)
        {
            var itemInSlot = GetItemInSlot(slot, creature);
            var itemType = GetBaseItemType(item);
            var rightHand = GetItemInSlot(InventorySlot.RightHand, creature);
            var rightHandType = GetBaseItemType(rightHand);
            var leftHand = GetItemInSlot(InventorySlot.LeftHand, creature);
            var leftHandType = GetBaseItemType(leftHand);

            // Two-handed weapons
            if (Item.TwoHandedMeleeItemTypes.Contains(itemType) || 
                Item.TwinBladeBaseItemTypes.Contains(itemType) || 
                Item.SaberstaffBaseItemTypes.Contains(itemType) ||
                Item.RifleBaseItemTypes.Contains(itemType) ||
                Item.PistolBaseItemTypes.Contains(itemType))
            {
                if (GetIsObjectValid(rightHand) ||
                    GetIsObjectValid(leftHand))
                    return true;
            }
            // Shields & One-Handed Weapons
            else if (Item.ShieldBaseItemTypes.Contains(itemType) || 
                     Item.OneHandedMeleeItemTypes.Contains(itemType) || 
                     Item.ThrowingWeaponBaseItemTypes.Contains(itemType))
            {
                if (Item.TwoHandedMeleeItemTypes.Contains(rightHandType) || 
                    Item.TwinBladeBaseItemTypes.Contains(rightHandType) || 
                    Item.SaberstaffBaseItemTypes.Contains(rightHandType) ||
                    Item.RifleBaseItemTypes.Contains(rightHandType) ||
                    Item.PistolBaseItemTypes.Contains(rightHandType))
                {
                    return true;
                }
            }

            return GetIsObjectValid(itemInSlot);
        }

        private static bool IsRingSwappingPositions(uint creature, uint item, InventorySlot slot)
        {
            var currentRightSlot = GetItemInSlot(InventorySlot.RightRing, creature);
            var currentLeftSlot = GetItemInSlot(InventorySlot.LeftRing, creature);

            if (currentRightSlot == item && slot == InventorySlot.LeftRing)
                return true;

            if (currentLeftSlot == item && slot == InventorySlot.RightRing)
                return true;

            return false;
        }


        /// <summary>
        /// When an item is equipped, if any of a player's perks has an Equipped Trigger, run those actions now.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSWLORItemEquipValidBefore)]
        public static void ApplyEquipTriggers()
        {
            var player = OBJECT_SELF;
            if (GetIsDM(player)) return;

            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            var slot = (InventorySlot)Convert.ToInt32(EventsPlugin.GetEventData("SLOT"));

            // The unequip event doesn't fire if an item is being swapped out. 
            // If there's an item in the slot, run the unequip triggers first.
            var existingItemInSlot = GetItemInSlot(slot, player);
            if (GetIsObjectValid(existingItemInSlot))
            {
                RunUnequipTriggers(player, existingItemInSlot);
            }

            foreach (var (perkType, actionList) in Perk.GetAllEquipTriggers())
            {
                var playerPerkLevel = Perk.GetPerkLevel(player, perkType);
                if (playerPerkLevel <= 0) continue;

                foreach (var action in actionList)
                {
                    action(player, item, slot, perkType, playerPerkLevel);
                }
            }
        }

        private static void RunUnequipTriggers(uint player, uint item)
        {
            var slot = Item.GetItemSlot(player, item);

            foreach (var (perkType, actionList) in Perk.GetAllUnequipTriggers())
            {
                var playerPerkLevel = Perk.GetPerkLevel(player, perkType);
                if (playerPerkLevel <= 0) continue;

                foreach (var action in actionList)
                {
                    action(player, item, slot, perkType, playerPerkLevel);
                }
            }
        }

        /// <summary>
        /// When an item is unequipped, if any of a player's perks has an Unequipped Trigger, run those actions now.
        /// </summary>
        [NWNEventHandler(ScriptName.OnItemUnequipBefore)]
        public static void ApplyUnequipTriggers()
        {
            var player = OBJECT_SELF;
            if (GetIsDM(player)) return;

            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            RunUnequipTriggers(player, item);
        }
    }
}
