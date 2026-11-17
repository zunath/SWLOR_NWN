using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// Equipment slots: which items fit in which hand, which sets of items go together, where a slot's items are, and putting them in the
    /// player's hands or away. A part of the ExtraHotbars service.
    /// </summary>
    public class ExtraHotbarsEquipService : IExtraHotbarsEquipService
    {
        // baseitems.2da EquipableSlots bits.
        private const int RightHandSlotBit = 0x10;
        private const int LeftHandSlotBit = 0x20;

        private readonly IItemService _item;
        private readonly ITwinPistolEquipmentService _twinPistolEquipment;
        private readonly ICraftMinigameRoutingService _craftMinigameRouting;
        private readonly IExtraHotbarsStatusService _status;

        public ExtraHotbarsEquipService(
            IItemService item,
            ITwinPistolEquipmentService twinPistolEquipment,
            ICraftMinigameRoutingService craftMinigameRouting,
            IExtraHotbarsStatusService status)
        {
            _item = item;
            _twinPistolEquipment = twinPistolEquipment;
            _craftMinigameRouting = craftMinigameRouting;
            _status = status;
        }

        /// <summary>
        /// Puts the slot's items in their hands, then goes back to attacking whatever the player was fighting.
        /// The usual equip checks still apply and explain any item that cannot be held. A set already in hand is put away.
        /// </summary>
        public void EquipHands(uint player, ExtraHotbarSlot slot)
        {
            var (mainHand, offHand) = FindEquipItems(player, slot);
            foreach (var (equipItem, item) in new[] { (slot.MainHand, mainHand), (slot.OffHand, offHand) })
            {
                if (equipItem != null && !GetIsObjectValid(item))
                {
                    SendMessageToPC(player, ColorToken.Red($"You do not have {equipItem.ItemName}."));
                    return;
                }
            }

            // A craft holds the player's gear, so nothing goes on or comes off either hand while any crafting window is open.
            if (IsAnyCraftWindowOpen(player))
            {
                SendMessageToPC(player, ColorToken.Red("Items cannot be equipped while crafting."));
                return;
            }

            var currentMainHand = GetItemInSlot(InventorySlot.RightHand, player);
            var currentOffHand = GetItemInSlot(InventorySlot.LeftHand, player);
            var (newMainHand, newOffHand) = PlanHands(currentMainHand, currentOffHand, mainHand, offHand, CanPairHands);
            if (currentMainHand == newMainHand && currentOffHand == newOffHand)
            {
                // The set is already in hand, so a second click takes it off, like the native quickbar.
                UnequipHands(player, mainHand, offHand);
                return;
            }

            // A Twin Pistols or Power Glove companion only leaves with its main-hand item, so that item is put away
            // and, if the set keeps it, equipped again.
            var offHandChanges = currentOffHand != newOffHand;
            var mainHandChanges = currentMainHand != newMainHand || (offHandChanges && IsHandCompanion(currentOffHand));

            // Nothing comes off until everything going on is known to be usable.
            foreach (var (changes, item) in new[] { (mainHandChanges, newMainHand), (offHandChanges, newOffHand) })
            {
                var error = changes && GetIsObjectValid(item) ? GetEquipError(player, item) : string.Empty;
                if (!string.IsNullOrEmpty(error))
                {
                    SendMessageToPC(player, ColorToken.Red(error));
                    return;
                }
            }

            var attackTarget = GetAttackTarget(player);
            ClearAllActions();

            // The equip checks silently refuse an item whose hand is still occupied, so everything leaving a hand
            // is put away before anything goes on. The off hand goes first: the engine moves an off-hand weapon into
            // an emptied main hand, so a kept weapon is put away too and goes back after the new main-hand item.
            // A companion stays to leave with its main-hand item.
            var offHandLeaves = GetIsObjectValid(currentOffHand) && !IsHandCompanion(currentOffHand) &&
                                (offHandChanges || (mainHandChanges && GetIsObjectValid(currentMainHand) &&
                                                    CanHoldInMainHand(currentOffHand)));
            if (offHandLeaves && !PutAwayHandItem(player, currentOffHand, InventorySlot.LeftHand))
                return;
            offHandChanges |= offHandLeaves;

            if (mainHandChanges && GetIsObjectValid(currentMainHand) &&
                !PutAwayHandItem(player, currentMainHand, InventorySlot.RightHand))
            {
                // The off hand was already put away, so it goes back into its free hand rather than staying empty.
                mainHandChanges = false;
                newOffHand = offHandLeaves ? currentOffHand : OBJECT_INVALID;
            }

            if (mainHandChanges && GetIsObjectValid(newMainHand))
                ActionEquipItem(newMainHand, InventorySlot.RightHand);
            if (offHandChanges && GetIsObjectValid(newOffHand))
                ActionEquipItem(newOffHand, InventorySlot.LeftHand);

            if (GetIsObjectValid(attackTarget) && !GetIsDead(attackTarget))
                ActionAttack(attackTarget);

            _status.RefreshStatusSoon(player);
        }

        /// <summary>
        /// Puts a worn set's items away. The main hand goes first so a Twin Pistols or Power Glove companion leaves
        /// with it; the off-hand item is then found in whichever hand the engine left it. An item with no room to go
        /// stays held, and the player is told to make room. If the main hand cannot go, the set stays as it is.
        /// </summary>
        private void UnequipHands(uint player, uint mainHand, uint offHand)
        {
            if (GetIsObjectValid(mainHand) && !PutAwayHandItem(player, mainHand, InventorySlot.RightHand))
                return;

            var offHandSlot = GetHandSlot(player, offHand);
            if (offHandSlot != InventorySlot.Invalid)
                PutAwayHandItem(player, offHand, offHandSlot);

            _status.RefreshStatusSoon(player);
        }

        /// <summary>
        /// The hand holding an item, or Invalid. The engine moves an off-hand weapon into an emptied main hand,
        /// so an off-hand item can turn up in either.
        /// </summary>
        private InventorySlot GetHandSlot(uint player, uint item)
        {
            if (!GetIsObjectValid(item))
                return InventorySlot.Invalid;
            if (GetItemInSlot(InventorySlot.LeftHand, player) == item)
                return InventorySlot.LeftHand;
            return GetItemInSlot(InventorySlot.RightHand, player) == item ? InventorySlot.RightHand : InventorySlot.Invalid;
        }

        /// <summary>
        /// The equip checks that do not depend on the other hand, which the set is about to change.
        /// </summary>
        private string GetEquipError(uint player, uint item)
        {
            if (_item.ForceSensitiveWeaponBaseItemTypes.Contains(GetBaseItemType(item)) &&
                !_item.CanEquipForceSensitiveWeapon(player))
                return "Only Force Sensitive characters may equip that item.";

            var itemUseError = _item.GetCreatureItemUseError(player, item,
                _twinPistolEquipment.IsPairItem(item) ? SkillType.TwinPistol : SkillType.Invalid);
            return string.IsNullOrEmpty(itemUseError) ? _item.GetRacialEquipError(player, item) : itemUseError;
        }

        /// <summary>
        /// Moves a held item into the inventory or a carried bag. Unlike a plain unequip, it never drops the item
        /// on the ground when the inventory is full; the item stays held and the player is told to make room.
        /// </summary>
        private bool PutAwayHandItem(uint player, uint item, InventorySlot slot)
        {
            try
            {
                if (TryUnequip(player, item, slot, false))
                    return true;
            }
            catch (InvalidOperationException) when (GetItemInSlot(slot, player) != item && GetItemPossessor(item) == player)
            {
                // Another equipment rule put the item away itself.
                return true;
            }
            catch (InvalidOperationException)
            {
            }

            SendMessageToPC(player, ColorToken.Red($"Free inventory space or make room in a carried bag before putting away {GetName(item)}."));
            return false;
        }

        /// <summary>
        /// What each hand holds after an equipment slot is used. A hand the slot leaves out keeps its item unless
        /// that item is moving to the other hand or cannot be held beside the new one.
        /// </summary>
        public (uint MainHand, uint OffHand) PlanHands(uint currentMainHand, uint currentOffHand,
            uint mainHand, uint offHand, Func<uint, uint, bool> canPairHands)
        {
            var newMainHand = mainHand != OBJECT_INVALID ? mainHand : currentMainHand;
            var newOffHand = offHand != OBJECT_INVALID ? offHand : currentOffHand;

            if (mainHand == OBJECT_INVALID && (newMainHand == newOffHand || !canPairHands(newMainHand, newOffHand)))
                newMainHand = OBJECT_INVALID;
            if (offHand == OBJECT_INVALID && (newOffHand == newMainHand || !canPairHands(newMainHand, newOffHand)))
                newOffHand = OBJECT_INVALID;

            return (newMainHand, newOffHand);
        }

        /// <summary>
        /// Whether every item of an equipment slot is already in its hand.
        /// </summary>
        public bool IsEquipped(uint player, uint mainHand, uint offHand)
        {
            if (!GetIsObjectValid(mainHand) && !GetIsObjectValid(offHand))
                return false;

            return (!GetIsObjectValid(mainHand) || GetItemInSlot(InventorySlot.RightHand, player) == mainHand) &&
                   (!GetIsObjectValid(offHand) || GetItemInSlot(InventorySlot.LeftHand, player) == offHand);
        }

        /// <summary>
        /// Finds an equipment slot's items on the player. The exact items come first, so two identical weapons
        /// stay apart; an item whose original is gone falls back to one with the same resref and name.
        /// </summary>
        public (uint MainHand, uint OffHand) FindEquipItems(uint player, ExtraHotbarSlot slot) =>
            FindEquipItems(slot, GetHandItems(player));

        /// <summary>
        /// Same as above with a hand-item list already built, so several slots can share one inventory scan.
        /// </summary>
        public (uint MainHand, uint OffHand) FindEquipItems(ExtraHotbarSlot slot, List<uint> items)
        {
            var mainHand = FindExactItem(items, slot.MainHand);
            var offHand = FindExactItem(items, slot.OffHand);
            if (!GetIsObjectValid(mainHand))
                mainHand = FindSimilarItem(items, slot.MainHand, offHand);
            if (!GetIsObjectValid(offHand))
                offHand = FindSimilarItem(items, slot.OffHand, mainHand);

            return (mainHand, offHand);
        }

        private uint FindExactItem(List<uint> items, ExtraHotbarEquipItem equipItem)
        {
            if (equipItem == null || string.IsNullOrEmpty(equipItem.ItemId))
                return OBJECT_INVALID;

            return items.FirstOrDefault(item => GetObjectUUID(item) == equipItem.ItemId, OBJECT_INVALID);
        }

        private uint FindSimilarItem(List<uint> items, ExtraHotbarEquipItem equipItem, uint exclude)
        {
            if (equipItem == null)
                return OBJECT_INVALID;

            return items.FirstOrDefault(item => item != exclude &&
                                                GetResRef(item) == equipItem.ItemResref &&
                                                GetName(item) == equipItem.ItemName, OBJECT_INVALID);
        }

        /// <summary>
        /// Items the player can hold in a hand: what they are holding now, then their inventory.
        /// </summary>
        public List<uint> GetHandItems(uint player)
        {
            var items = new List<uint>();
            foreach (var slot in new[] { InventorySlot.RightHand, InventorySlot.LeftHand })
            {
                var held = GetItemInSlot(slot, player);
                if (GetIsObjectValid(held) && (CanHoldInMainHand(held) || CanHoldInOffHand(held)))
                    items.Add(held);
            }

            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            {
                if (CanHoldInMainHand(item) || CanHoldInOffHand(item))
                    items.Add(item);
            }

            return items;
        }

        public bool CanHoldInMainHand(uint item) =>
            (GetEquipableSlots(GetBaseItemType(item)) & RightHandSlotBit) != 0;

        /// <summary>
        /// Companion pistols and gloves come with their main-hand item, so they are never picked on their own.
        /// </summary>
        public bool CanHoldInOffHand(uint item) =>
            GetBaseItemType(item) is not (BaseItem.CompanionPistol or BaseItem.CompanionPowerGlove or BaseItem.OffHandPistol) &&
            (GetEquipableSlots(GetBaseItemType(item)) & LeftHandSlotBit) != 0 &&
            !FillsBothHands(item);

        /// <summary>
        /// Whether two items can be held together. Pistols take a shield in the off hand; Twin Pistols, other ranged
        /// weapons, Power Gloves and weapons too large for the off hand leave no room for an off-hand item.
        /// A generated companion belongs to its main-hand item, which keeps or removes it.
        /// </summary>
        public bool CanPairHands(uint mainHand, uint offHand)
        {
            if (!GetIsObjectValid(mainHand) || !GetIsObjectValid(offHand) || IsHandCompanion(offHand))
                return true;

            if (_twinPistolEquipment.IsPairItem(mainHand))
                return false;

            if (_item.PistolBaseItemTypes.Contains(GetBaseItemType(mainHand)))
                return _item.ShieldBaseItemTypes.Contains(GetBaseItemType(offHand));

            return !FillsBothHands(mainHand);
        }

        private bool FillsBothHands(uint item) =>
            GetWeaponRanged(item) ||
            GetBaseItemType(item) == BaseItem.PowerGlove ||
            (GetEquipableSlots(GetBaseItemType(item)) & (RightHandSlotBit | LeftHandSlotBit)) == RightHandSlotBit;

        private int GetEquipableSlots(BaseItem baseItem)
        {
            var text = Get2DAString("baseitems", "EquipableSlots", (int)baseItem);
            return text.StartsWith("0x") &&
                   int.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var slots)
                ? slots
                : 0;
        }

        /// <summary>
        /// The picker's hand choices after the player clicks an item. Clicking a chosen item takes it back out;
        /// an off-hand-only item such as a shield goes to the off hand; an item goes to the off hand when it fits
        /// beside the chosen main hand; anything else becomes the main hand. A hand that no longer fits is cleared.
        /// </summary>
        public (uint MainHand, uint OffHand) PickHandItem(uint mainHand, uint offHand, uint item,
            bool canMainHand, bool canOffHand, Func<uint, uint, bool> canPairHands)
        {
            if (item == mainHand)
                return (OBJECT_INVALID, offHand);
            if (item == offHand)
                return (mainHand, OBJECT_INVALID);

            if (!canMainHand)
                return canOffHand ? (canPairHands(mainHand, item) ? mainHand : OBJECT_INVALID, item) : (mainHand, offHand);

            if (mainHand != OBJECT_INVALID && canOffHand && canPairHands(mainHand, item))
                return (mainHand, item);

            return (item, canPairHands(item, offHand) ? offHand : OBJECT_INVALID);
        }

        public bool IsHandCompanion(uint item) =>
            GetIsObjectValid(item) &&
            GetBaseItemType(item) is BaseItem.CompanionPistol or BaseItem.CompanionPowerGlove;

        protected virtual bool IsAnyCraftWindowOpen(uint player) => _craftMinigameRouting.IsAnyCraftWindowOpen(player);

        protected virtual bool TryUnequip(uint player, uint item, InventorySlot slot, bool allowRecoveryBag) =>
            PlayerEquipmentStorage.TryUnequip(player, item, slot, allowRecoveryBag: allowRecoveryBag);
    }
}
