using System;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using Player = SWLOR.Game.Server.Entity.Player;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public static class EquipmentRestrictions
    {
        /// <summary>
        /// When an item's usage is validated, check the custom rules to see if the item can be used by the player.
        /// If not able to be used, item will appear red in inventory.
        /// </summary>
        [NWNEventHandler("item_valid_bef")]
        public static void ValidateItemUse()
        {
            var creature = OBJECT_SELF;
            var item = StringToObject(Events.GetEventData("ITEM_OBJECT_ID"));

            Events.SetEventResult(string.IsNullOrWhiteSpace(CanItemBeUsed(creature, item)) ? "1" : "0");
            Events.SkipEvent();
        }

        /// <summary>
        /// When an item is equipped, check the custom rules to see if the item can be equipped by the player.
        /// If not able to be used, an error message will be sent and item will not be equipped.
        /// </summary>
        [NWNEventHandler("item_eqpval_bef")]
        public static void ValidateItemEquip()
        {
            var creature = OBJECT_SELF;
            var item = StringToObject(Events.GetEventData("ITEM"));
            var slot = (InventorySlot)Convert.ToInt32(Events.GetEventData("SLOT"));

            var canUseItem = CanItemBeUsed(creature, item);
            var canDualWield = ValidateDualWield(item, slot);

            if (string.IsNullOrWhiteSpace(canUseItem) &&
                canDualWield)
            {
                Events.PushEventData("ITEM", ObjectToString(item));
                Events.PushEventData("SLOT", Convert.ToString((int)slot));
                Events.SignalEvent("SWLOR_ITEM_EQUIP_VALID_BEFORE", creature);
                return;
            }

            SendMessageToPC(creature, ColorToken.Red(canUseItem));
            Events.SkipEvent();
        }

        /// <summary>
        /// When an item is equipped, check if the item is going to be dual wielded. If it is, ensure player has
        /// at least level 1 of the Dual Wield perk. If they don't, skip the equip event with an error message.
        /// </summary>
        public static bool ValidateDualWield(uint item, InventorySlot slot)
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
                BaseItem.Sickle
            };
            if (!dualWieldWeapons.Contains(baseItem)) return true;

            var dualWieldLevel = Perk.GetEffectivePerkLevel(creature, PerkType.DualWield);
            if (dualWieldLevel <= 0)
            {
                SendMessageToPC(creature, ColorToken.Red("Equipping two weapons requires the Dual Wield perk."));
                Events.SkipEvent();

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
            if (!GetIsPC(creature) || GetIsDM(creature)) return string.Empty;

            var playerId = GetObjectUUID(creature);
            var dbPlayer = DB.Get<Player>(playerId);

            // Check for required perk levels.
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var type = GetItemPropertyType(ip);
                if (type != ItemPropertyType.UseLimitationPerk) continue;

                var perkType = (PerkType)GetItemPropertySubType(ip);
                if (perkType == PerkType.Invalid) continue;

                var requiredLevel = GetItemPropertyCostTableValue(ip);
                var perkLevel = dbPlayer.Perks.ContainsKey(perkType) ? dbPlayer.Perks[perkType] : 0;

                if (perkLevel < requiredLevel)
                {
                    var perkName = Perk.GetPerkDetails(perkType).Name;
                    return $"This item requires '{perkName}' level {requiredLevel} to use.";
                }
            }

            return string.Empty;
        }


        /// <summary>
        /// When an item is equipped, if any of a player's perks has an Equipped Trigger, run those actions now.
        /// </summary>
        [NWNEventHandler("item_eqp_bef")]
        public static void ApplyEquipTriggers()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var item = StringToObject(Events.GetEventData("ITEM"));
            var slot = (InventorySlot)Convert.ToInt32(Events.GetEventData("SLOT"));
            
            // The unequip event doesn't fire if an item is being swapped out. 
            // If there's an item in the slot, run the unequip triggers first.
            var existingItemInSlot = GetItemInSlot(slot, player);
            if (GetIsObjectValid(existingItemInSlot))
            {
                RunUnequipTriggers(player, existingItemInSlot);
            }

            foreach (var (perkType, actionList) in Perk.GetAllEquipTriggers())
            {
                var playerPerkLevel = Perk.GetEffectivePerkLevel(player, perkType);
                if (playerPerkLevel <= 0) continue;

                foreach (var action in actionList)
                {
                    action(player, item, slot, perkType, playerPerkLevel);
                }
            }
        }

        private static void RunUnequipTriggers(uint player, uint item)
        {
            var slot = InventorySlot.Invalid;

            if (GetItemInSlot(InventorySlot.Head, player) == item) slot = InventorySlot.Head;
            if (GetItemInSlot(InventorySlot.Chest, player) == item) slot = InventorySlot.Chest;
            if (GetItemInSlot(InventorySlot.Boots, player) == item) slot = InventorySlot.Boots;
            if (GetItemInSlot(InventorySlot.Arms, player) == item) slot = InventorySlot.Arms;
            if (GetItemInSlot(InventorySlot.RightHand, player) == item) slot = InventorySlot.RightHand;
            if (GetItemInSlot(InventorySlot.LeftHand, player) == item) slot = InventorySlot.LeftHand;
            if (GetItemInSlot(InventorySlot.Cloak, player) == item) slot = InventorySlot.Cloak;
            if (GetItemInSlot(InventorySlot.LeftRing, player) == item) slot = InventorySlot.LeftRing;
            if (GetItemInSlot(InventorySlot.RightRing, player) == item) slot = InventorySlot.RightRing;
            if (GetItemInSlot(InventorySlot.Neck, player) == item) slot = InventorySlot.Neck;
            if (GetItemInSlot(InventorySlot.Belt, player) == item) slot = InventorySlot.Belt;
            if (GetItemInSlot(InventorySlot.Arrows, player) == item) slot = InventorySlot.Arrows;
            if (GetItemInSlot(InventorySlot.Bullets, player) == item) slot = InventorySlot.Bullets;
            if (GetItemInSlot(InventorySlot.Bolts, player) == item) slot = InventorySlot.Bolts;
            if (GetItemInSlot(InventorySlot.CreatureLeft, player) == item) slot = InventorySlot.CreatureLeft;
            if (GetItemInSlot(InventorySlot.CreatureRight, player) == item) slot = InventorySlot.CreatureRight;
            if (GetItemInSlot(InventorySlot.CreatureBite, player) == item) slot = InventorySlot.CreatureBite;
            if (GetItemInSlot(InventorySlot.CreatureArmor, player) == item) slot = InventorySlot.CreatureArmor;

            foreach (var (perkType, actionList) in Perk.GetAllUnequipTriggers())
            {
                var playerPerkLevel = Perk.GetEffectivePerkLevel(player, perkType);
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
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var item = StringToObject(Events.GetEventData("ITEM"));
            RunUnequipTriggers(player, item);
        }

    }
}
