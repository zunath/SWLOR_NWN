using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PerkService;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Feature
{
    public static class EquipmentRestrictions
    {
        /// <summary>
        /// When an item is equipped, check the custom rules to see if the item can be equipped by the player.
        /// If not able to be used, an error message will be sent and item will not be equipped.
        /// </summary>
        [NWNEventHandler("item_eqpval_bef")]
        public static void ValidateItemEquip()
        {
            var creature = OBJECT_SELF;
            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            var slot = (InventorySlot)Convert.ToInt32(EventsPlugin.GetEventData("SLOT"));

            var isSwapping = IsItemSwapping(creature, item, slot);
            var canUseItem = CanItemBeUsed(creature, item);
            var canDualWield = ValidateDualWield(item, slot);
            var isRingSwappingPositions = IsRingSwappingPositions(creature, item, slot);

            if (string.IsNullOrWhiteSpace(canUseItem) &&
                canDualWield && 
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

        /// <summary>
        /// When an item is equipped, check if the item is going to be dual wielded. If it is, ensure player has
        /// at least level 1 of the Dual Wield perk. If they don't, skip the equip event with an error message.
        /// </summary>
        private static bool ValidateDualWield(uint item, InventorySlot slot)
        {
            var creature = OBJECT_SELF;

            // Not equipping to the left hand, or there's nothing equipped in the right hand.
            if (slot != InventorySlot.LeftHand) return true;
            if (!GetIsObjectValid(GetItemInSlot(InventorySlot.RightHand, creature))) return true;
            
            var baseItem = GetBaseItemType(item);
            var dualWieldWeapons = new[]
            {
                BaseItem.ShortSword,
                BaseItem.Longsword,
                BaseItem.BattleAxe,
                BaseItem.BastardSword,
                BaseItem.LightFlail,
                BaseItem.LightMace,
                BaseItem.Dagger,
                BaseItem.Club,
                BaseItem.HandAxe,
                BaseItem.Kama,
                BaseItem.Katana,
                BaseItem.Kukri,
                BaseItem.Rapier,
                BaseItem.Scimitar,
                BaseItem.Sickle,
                BaseItem.Lightsaber,
                BaseItem.Electroblade
            };
            if (!dualWieldWeapons.Contains(baseItem)) return true;

            var dualWieldLevel = Perk.GetPerkLevel(creature, PerkType.DualWield);
            if (dualWieldLevel <= 0)
            {
                SendMessageToPC(creature, ColorToken.Red("Equipping two weapons requires the Dual Wield perk."));
                EventsPlugin.SkipEvent();

                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if an item can be used by a creature. Non-PCs and DMs automatically can wear all items.
        /// If a player is missing a required perk, an error message will be returned.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="item">The item to check.</param>
        /// <returns>An empty string if successful or an error message if failed</returns>
        private static string CanItemBeUsed(uint creature, uint item)
        {
            var isPlayer = GetIsPC(creature);
            var isDroid = Droid.IsDroid(creature);

            if ((!isPlayer && !isDroid) || GetIsDM(creature) || GetIsDMPossessed(creature)) 
                return string.Empty;

            if (Gui.IsWindowOpen(creature, GuiWindowType.Craft))
            {
                return "Items cannot be equipped while crafting.";
            }

            var itemType = GetBaseItemType(item);

            // Droids may only equip items in specific slots if the item has the Use Limitation Race: Droid item property.
            // They are unable to equip any items in these slots if this item property is missing.
            // Non-Droids may not equip any items which have this item property.
            var race = GetRacialType(creature);
            var needsDroidLimitation = race == RacialType.Droid && Item.DroidBaseItemTypes.Contains(itemType);
            var itemHasDroidIP = false;
            Dictionary<PerkType, int> creaturePerks;

            if (isPlayer)
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);
                creaturePerks = dbPlayer.Perks;
            }
            // Droids
            else
            {
                var controller = Droid.GetControllerItem(creature);
                var droidDetails = Droid.LoadDroidItemPropertyDetails(controller);
                creaturePerks = droidDetails.Perks;
            }

            // Check for required perk levels.
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var type = GetItemPropertyType(ip);

                // Check perk requirements
                if (type == ItemPropertyType.UseLimitationPerk)
                {
                    var perkType = (PerkType)GetItemPropertySubType(ip);
                    if (perkType == PerkType.Invalid) continue;

                    var requiredLevel = GetItemPropertyCostTableValue(ip);
                    var perkLevel = creaturePerks.ContainsKey(perkType) ? creaturePerks[perkType] : 0;

                    if (perkLevel < requiredLevel)
                    {
                        var perkName = Perk.GetPerkDetails(perkType).Name;
                        return $"This item requires '{perkName}' level {requiredLevel} to use.";
                    }
                }
                else if (type == ItemPropertyType.UseLimitationRacialType)
                {
                    var limitationRace = (RacialType)GetItemPropertySubType(ip);

                    if (limitationRace == RacialType.Droid)
                    {
                        // Has the use limitation but is not a droid, return error.
                        if (race != RacialType.Droid)
                            return $"This item may only be equipped by Droids.";

                        // Has the use limitation but item does not have droid limitation
                        if (!needsDroidLimitation)
                            continue;

                        itemHasDroidIP = true;
                    }
                }
            }

            if (needsDroidLimitation && !itemHasDroidIP)
                return "Droids may not equip that item.";

            return string.Empty;
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
        [NWNEventHandler("item_eqp_bef")]
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
        [NWNEventHandler("item_uneqp_bef")]
        public static void ApplyUnequipTriggers()
        {
            var player = OBJECT_SELF;
            if (GetIsDM(player)) return;

            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            RunUnequipTriggers(player, item);
        }
    }
}
