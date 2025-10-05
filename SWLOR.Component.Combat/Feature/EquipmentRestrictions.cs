using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Domain.Associate.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.NWNX;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Combat.Feature
{
    public class EquipmentRestrictions
    {
        private readonly IDatabaseService _db;
        private readonly IItemService _itemService;
        private readonly IPerkService _perkService;
        private readonly IDroidService _droidService;
        private readonly IGuiService _guiService;
        private readonly IEventsPluginService _eventsPlugin;

        public EquipmentRestrictions(
            IDatabaseService db, 
            IItemService itemService, 
            IPerkService perkService, 
            IDroidService droidService, 
            IGuiService guiService,
            IEventsPluginService eventsPlugin)
        {
            _db = db;
            _itemService = itemService;
            _perkService = perkService;
            _droidService = droidService;
            _guiService = guiService;
            _eventsPlugin = eventsPlugin;
        }
        
        /// <summary>
        /// When an item is equipped, check the custom rules to see if the item can be equipped by the player.
        /// If not able to be used, an error message will be sent and item will not be equipped.
        /// </summary>
        [ScriptHandler<OnItemEquipValidateBefore>]
        public void ValidateItemEquip()
        {
            var creature = OBJECT_SELF;
            var item = StringToObject(_eventsPlugin.GetEventData("ITEM"));
            var slot = (InventorySlotType)Convert.ToInt32(_eventsPlugin.GetEventData("SLOT"));

            var isSwapping = IsItemSwapping(creature, item, slot);
            var canUseItem = CanItemBeUsed(creature, item);
            var canDualWield = ValidateDualWield(item, slot);
            var isRingSwappingPositions = IsRingSwappingPositions(creature, item, slot);

            if (string.IsNullOrWhiteSpace(canUseItem) &&
                canDualWield && 
                !isSwapping &&
                !isRingSwappingPositions)
            {
                _eventsPlugin.PushEventData("ITEM", ObjectToString(item));
                _eventsPlugin.PushEventData("SLOT", Convert.ToString((int)slot));
                _eventsPlugin.SignalEvent("SWLOR_ITEM_EQUIP_VALID_BEFORE", creature);
                return;
            }

            if (!string.IsNullOrWhiteSpace(canUseItem))
            {
                var messageTarget = creature;
                if (_droidService.IsDroid(creature))
                {
                    messageTarget = GetMaster(creature);
                }
                SendMessageToPC(messageTarget, ColorToken.Red(canUseItem));
            }
            
            _eventsPlugin.SkipEvent();
        }

        private bool IsItemSwapping(uint creature, uint item, InventorySlotType slot)
        {
            var itemInSlot = GetItemInSlot(slot, creature);
            var itemType = GetBaseItemType(item);
            var rightHand = GetItemInSlot(InventorySlotType.RightHand, creature);
            var rightHandType = GetBaseItemType(rightHand);
            var leftHand = GetItemInSlot(InventorySlotType.LeftHand, creature);
            var leftHandType = GetBaseItemType(leftHand);

            // Two-handed weapons
            if (_itemService.TwoHandedMeleeItemTypes.Contains(itemType) || 
                _itemService.TwinBladeBaseItemTypes.Contains(itemType) || 
                _itemService.SaberstaffBaseItemTypes.Contains(itemType) ||
                _itemService.RifleBaseItemTypes.Contains(itemType) ||
                _itemService.PistolBaseItemTypes.Contains(itemType))
            {
                if (GetIsObjectValid(rightHand) ||
                    GetIsObjectValid(leftHand))
                    return true;
            }
            // Shields & One-Handed Weapons
            else if (_itemService.ShieldBaseItemTypes.Contains(itemType) || 
                     _itemService.OneHandedMeleeItemTypes.Contains(itemType) || 
                     _itemService.ThrowingWeaponBaseItemTypes.Contains(itemType))
            {
                if (_itemService.TwoHandedMeleeItemTypes.Contains(rightHandType) || 
                    _itemService.TwinBladeBaseItemTypes.Contains(rightHandType) || 
                    _itemService.SaberstaffBaseItemTypes.Contains(rightHandType) ||
                    _itemService.RifleBaseItemTypes.Contains(rightHandType) ||
                    _itemService.PistolBaseItemTypes.Contains(rightHandType))
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
        private bool ValidateDualWield(uint item, InventorySlotType slot)
        {
            var creature = OBJECT_SELF;

            // Not equipping to the left hand, or there's nothing equipped in the right hand.
            if (slot != InventorySlotType.LeftHand) return true;
            if (!GetIsObjectValid(GetItemInSlot(InventorySlotType.RightHand, creature))) return true;
            
            var baseItem = GetBaseItemType(item);
            var dualWieldWeapons = new[]
            {
                BaseItemType.ShortSword,
                BaseItemType.Longsword,
                BaseItemType.BattleAxe,
                BaseItemType.BastardSword,
                BaseItemType.LightFlail,
                BaseItemType.LightMace,
                BaseItemType.Dagger,
                BaseItemType.Club,
                BaseItemType.HandAxe,
                BaseItemType.Kama,
                BaseItemType.Katana,
                BaseItemType.Kukri,
                BaseItemType.Rapier,
                BaseItemType.Scimitar,
                BaseItemType.Sickle,
                BaseItemType.Lightsaber,
                BaseItemType.Electroblade
            };
            if (!dualWieldWeapons.Contains(baseItem)) return true;

            var dualWieldLevel = _perkService.GetPerkLevel(creature, PerkType.DualWield);
            if (dualWieldLevel <= 0)
            {
                SendMessageToPC(creature, ColorToken.Red("Equipping two weapons requires the Dual Wield perk."));
                _eventsPlugin.SkipEvent();

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
        private string CanItemBeUsed(uint creature, uint item)
        {
            var isPlayer = GetIsPC(creature);
            var isDroid = _droidService.IsDroid(creature);

            if ((!isPlayer && !isDroid) || GetIsDM(creature) || GetIsDMPossessed(creature)) 
                return string.Empty;

            if (_guiService.IsWindowOpen(creature, GuiWindowType.Craft))
            {
                return "Items cannot be equipped while crafting.";
            }

            var itemType = GetBaseItemType(item);

            // Droids may only equip items in specific slots if the item has the Use Limitation Race: Droid item property.
            // They are unable to equip any items in these slots if this item property is missing.
            // Non-Droids may not equip any items which have this item property.
            var race = GetRacialType(creature);
            var needsDroidLimitation = race == RacialType.Droid && _itemService.DroidBaseItemTypes.Contains(itemType);
            var itemHasDroidIP = false;
            Dictionary<PerkType, int> creaturePerks;

            if (isPlayer)
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);
                creaturePerks = dbPlayer.Perks;
            }
            // Droids
            else
            {
                var controller = _droidService.GetControllerItem(creature);
                var droidDetails = _droidService.LoadDroidItemPropertyDetails(controller);
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
                        var perkName = _perkService.GetPerkDetails(perkType).Name;
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

        private bool IsRingSwappingPositions(uint creature, uint item, InventorySlotType slot)
        {
            var currentRightSlot = GetItemInSlot(InventorySlotType.RightRing, creature);
            var currentLeftSlot = GetItemInSlot(InventorySlotType.LeftRing, creature);

            if (currentRightSlot == item && slot == InventorySlotType.LeftRing)
                return true;

            if (currentLeftSlot == item && slot == InventorySlotType.RightRing)
                return true;

            return false;
        }


        /// <summary>
        /// When an item is equipped, if any of a player's perks has an Equipped Trigger, run those actions now.
        /// </summary>
        [ScriptHandler<OnSWLORItemEquipValidBefore>]
        public void ApplyEquipTriggers()
        {
            var player = OBJECT_SELF;
            if (GetIsDM(player)) return;

            var item = StringToObject(_eventsPlugin.GetEventData("ITEM"));
            var slot = (InventorySlotType)Convert.ToInt32(_eventsPlugin.GetEventData("SLOT"));

            // The unequip event doesn't fire if an item is being swapped out. 
            // If there's an item in the slot, run the unequip triggers first.
            var existingItemInSlot = GetItemInSlot(slot, player);
            if (GetIsObjectValid(existingItemInSlot))
            {
                RunUnequipTriggers(player, existingItemInSlot);
            }

            foreach (var (perkType, actionList) in _perkService.GetAllEquipTriggers())
            {
                var playerPerkLevel = _perkService.GetPerkLevel(player, perkType);
                if (playerPerkLevel <= 0) continue;

                foreach (var action in actionList)
                {
                    action(player, item, slot, perkType, playerPerkLevel);
                }
            }
        }

        private void RunUnequipTriggers(uint player, uint item)
        {
            var slot = _itemService.GetItemSlot(player, item);

            foreach (var (perkType, actionList) in _perkService.GetAllUnequipTriggers())
            {
                var playerPerkLevel = _perkService.GetPerkLevel(player, perkType);
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
        [ScriptHandler<OnItemUnequipBefore>]
        public void ApplyUnequipTriggers()
        {
            var player = OBJECT_SELF;
            if (GetIsDM(player)) return;

            var item = StringToObject(_eventsPlugin.GetEventData("ITEM"));
            RunUnequipTriggers(player, item);
        }
    }
}
