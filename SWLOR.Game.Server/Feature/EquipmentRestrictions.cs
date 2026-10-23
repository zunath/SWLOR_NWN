using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
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

            // The internal appearance carrier never dispatches gameplay equipment hooks.
            if (GetBaseItemType(item) == BaseItem.CompanionPowerGlove)
            {
                var main = GetItemInSlot(InventorySlot.RightHand, creature);
                var left = GetItemInSlot(InventorySlot.LeftHand, creature);
                var authorized = PowerGloveEquipment.IsChanging(creature) &&
                                 PowerGloveEquipment.IsLinkedPair(creature, main, item);
                if (string.IsNullOrWhiteSpace(GetPowerGloveEquipmentError(GetBaseItemType(item), slot,
                        GetIsObjectValid(main) ? GetBaseItemType(main) : null,
                        GetIsObjectValid(left) ? GetBaseItemType(left) : null, authorized)))
                    return;
                EventsPlugin.SkipEvent();
                return;
            }

            var originalBaseItem = GetBaseItemType(item);
            PistolBaseItemCompatibility.Normalize(item);
            var canonicalSlot = PistolBaseItemCompatibility.GetCanonicalInventorySlot(
                originalBaseItem,
                slot);
            if (canonicalSlot != slot)
            {
                EventsPlugin.SkipEvent();
                AssignCommand(creature, () => ActionEquipItem(item, canonicalSlot));
                return;
            }

            var isSwapping = IsItemSwapping(creature, item, slot);
            var isRestrictedActor =
                (GetIsPC(creature) || Droid.IsDroid(creature)) &&
                !GetIsDM(creature) &&
                !GetIsDMPossessed(creature);
            var canUseItem = Item.CanEquip(creature, item, slot);
            // Equipping a worn item into another slot adds its stats again while the engine leaves it where it is,
            // so a worn item must be unequipped first, as rings always had to be. Normalized ammunition still
            // moves between ammunition slots.
            var wornSlot = Item.GetItemSlot(creature, item);
            var isMovingWornItem = wornSlot != InventorySlot.Invalid &&
                                   !(IsAmmunitionSlot(wornSlot) && IsAmmunitionSlot(slot));
            var mainHand = GetItemInSlot(InventorySlot.RightHand, creature);
            var offHand = GetItemInSlot(InventorySlot.LeftHand, creature);
            canUseItem = GetPowerGloveEquipmentError(GetBaseItemType(item), slot,
                GetIsObjectValid(mainHand) ? GetBaseItemType(mainHand) : null,
                GetIsObjectValid(offHand) ? GetBaseItemType(offHand) : null,
                existingError: canUseItem);

            if (isRestrictedActor)
            {
                var rightHand = GetItemInSlot(InventorySlot.RightHand, creature);
                var leftHand = GetItemInSlot(InventorySlot.LeftHand, creature);
                var rightHandType = GetIsObjectValid(rightHand)
                    ? GetBaseItemType(rightHand)
                    : (BaseItem?)null;
                var leftHandType = GetIsObjectValid(leftHand)
                    ? GetBaseItemType(leftHand)
                    : (BaseItem?)null;

                canUseItem = GetPistolEquipmentError(
                    GetBaseItemType(item),
                    slot,
                    rightHandType,
                    leftHandType,
                    canUseItem);
            }

            if (string.IsNullOrWhiteSpace(canUseItem) &&
                !isSwapping &&
                !isMovingWornItem)
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

        /// <summary>One real Power Glove owns both hands; only its generated cosmetic may occupy the left.</summary>
        public static string GetPowerGloveEquipmentError(
            BaseItem itemType,
            InventorySlot slot,
            BaseItem? rightHandType,
            BaseItem? leftHandType,
            bool isAuthorizedCompanion = false,
            string existingError = "")
        {
            if (itemType == BaseItem.CompanionPowerGlove)
                return isAuthorizedCompanion && slot == InventorySlot.LeftHand &&
                       rightHandType == BaseItem.PowerGlove && !leftHandType.HasValue
                    ? string.Empty : "The other Power Glove hand cannot be equipped separately.";
            if (!string.IsNullOrWhiteSpace(existingError)) return existingError;
            if (itemType == BaseItem.PowerGlove && slot != InventorySlot.RightHand)
                return "Power Gloves may only be equipped in the right hand.";
            if (itemType == BaseItem.PowerGlove && leftHandType.HasValue)
                return "Power Gloves require both hands. Remove the left-hand item first.";
            if (slot == InventorySlot.LeftHand && rightHandType == BaseItem.PowerGlove)
                return "Power Gloves already occupy both hands.";
            return string.Empty;
        }

        /// <summary>
        /// Validates pistol hand placement independently of the engine's ranged weapon rules.
        /// Pistols are one-handed so that a shield can occupy the left hand, but players may
        /// not place a normal pistol in that hand. The companion is a separate, simulated profile.
        /// </summary>
        public static string GetPistolEquipmentError(
            BaseItem itemType,
            InventorySlot slot,
            BaseItem? rightHandType,
            BaseItem? leftHandType,
            string existingError = "")
        {
            if (itemType == BaseItem.OffHandPistol)
                return "Legacy off-hand pistols cannot be equipped.";

            var pairError = TwinPistolEquipment.GetPairCompatibilityError(
                slot == InventorySlot.RightHand ? itemType : rightHandType,
                slot == InventorySlot.LeftHand ? itemType : leftHandType);
            if ((slot is InventorySlot.RightHand or InventorySlot.LeftHand) && !string.IsNullOrEmpty(pairError)) return pairError;

            if (!string.IsNullOrWhiteSpace(existingError))
                return existingError;

            if (itemType == BaseItem.CompanionPistol)
                return slot == InventorySlot.LeftHand ? string.Empty : "Companion pistols may only be equipped in the left hand.";

            var isPistol = Item.PistolBaseItemTypes.Contains(itemType);

            if (isPistol && slot != InventorySlot.RightHand)
                return "Pistols may only be equipped in the right hand.";

            if (isPistol &&
                leftHandType.HasValue &&
                leftHandType != BaseItem.CompanionPistol &&
                !Item.ShieldBaseItemTypes.Contains(leftHandType.Value))
            {
                return "Pistols may only be paired with a shield or companion pistol in the left hand.";
            }

            if (slot == InventorySlot.LeftHand &&
                rightHandType.HasValue &&
                Item.PistolBaseItemTypes.Contains(rightHandType.Value) &&
                itemType != BaseItem.CompanionPistol &&
                !Item.ShieldBaseItemTypes.Contains(itemType))
            {
                return "Pistols may only be paired with a shield or companion pistol in the left hand.";
            }

            return string.Empty;
        }

        /// <summary>
        /// Adds a visible legacy warning to the examine description of retired off-hand pistols.
        /// </summary>
        [NWNEventHandler(ScriptName.OnExamineObjectBefore)]
        public static void MarkLegacyOffHandPistolOnExamine()
        {
            MarkLegacyOffHandPistol(StringToObject(EventsPlugin.GetEventData("EXAMINEE_OBJECT_ID")));
        }

        public static void MarkLegacyOffHandPistol(uint item)
        {
            if (GetObjectType(item) != ObjectType.Item ||
                GetBaseItemType(item) != BaseItem.OffHandPistol)
            {
                return;
            }

            SetDescription(item, GetLegacyOffHandPistolDescription(GetDescription(item, true)));
        }

        public static string GetLegacyOffHandPistolDescription(string description)
        {
            const string warning = "Legacy item: this off-hand pistol cannot be equipped.";
            if (description.Contains(warning, StringComparison.OrdinalIgnoreCase))
                return description;

            return string.IsNullOrWhiteSpace(description)
                ? warning
                : $"{description.TrimEnd()}\n{warning}";
        }

        private static bool IsAmmunitionSlot(InventorySlot slot) =>
            slot is InventorySlot.Arrows or InventorySlot.Bullets or InventorySlot.Bolts;

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
                Item.RifleBaseItemTypes.Contains(itemType))
            {
                if (GetIsObjectValid(rightHand) ||
                    GetIsObjectValid(leftHand))
                    return true;
            }
            // Shields & One-Handed Weapons
            else if (Item.ShieldBaseItemTypes.Contains(itemType) ||
                     Item.OneHandedMeleeItemTypes.Contains(itemType) ||
                     Item.ThrowingWeaponBaseItemTypes.Contains(itemType) ||
                     itemType == BaseItem.CompanionPistol ||
                     Item.PistolBaseItemTypes.Contains(itemType))
            {
                if (Item.TwoHandedMeleeItemTypes.Contains(rightHandType) ||
                    Item.TwinBladeBaseItemTypes.Contains(rightHandType) ||
                    Item.SaberstaffBaseItemTypes.Contains(rightHandType) ||
                    Item.RifleBaseItemTypes.Contains(rightHandType))
                {
                    return true;
                }
            }

            return GetIsObjectValid(itemInSlot);
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
            if (PowerGloveEquipment.IsGeneratedCompanion(item)) return;
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
