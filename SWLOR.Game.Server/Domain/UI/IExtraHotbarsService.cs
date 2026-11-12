using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.UI
{
    /// <summary>
    /// Extra click-to-use hotbars shown beside the native quickbar. One member per public member of the old static
    /// <c>Service.ExtraHotbars</c>.
    /// </summary>
    public interface IExtraHotbarsService
    {
        GuiWindowType GetWindowType(int barIndex);

        int GetBarIndex(GuiWindowType windowType);

        bool IsUnlocked(uint player);

        /// <summary>
        /// Returns the player's bars, adding any that are missing so every bar has a full set of slots.
        /// </summary>
        List<ExtraHotbar> GetBars(Player dbPlayer);

        ExtraHotbar GetBar(uint player, int barIndex);

        void OpenBarsOnEnter();

        void LockBarsOnEnter();

        void LockBarsOnExit();

        /// <summary>The work of <see cref="LockBarsOnExit"/> for one player (a hot reload's quiescence step).</summary>
        void LockBarsFor(uint player);

        /// <summary>
        /// Opens the bars the player has turned on and closes the rest.
        /// </summary>
        void ApplyVisibility(uint player);

        void SetUnlocked(uint player, bool isUnlocked);

        /// <summary>
        /// Changes one bar's layout settings and redraws it.
        /// </summary>
        void UpdateBar(uint player, int barIndex, System.Action<ExtraHotbar> change);

        void AssignAbility(uint player, int barIndex, int slotIndex, FeatType feat);

        void AssignItem(uint player, int barIndex, int slotIndex, uint item);

        /// <summary>
        /// Puts a set of hand items on a slot. Either hand may be left out, which leaves that hand alone when used.
        /// </summary>
        void AssignEquipment(uint player, int barIndex, int slotIndex, uint mainHand, uint offHand);

        void AssignChatCommand(uint player, int barIndex, int slotIndex, string command);

        void AssignActionMode(uint player, int barIndex, int slotIndex, ActionMode mode);

        void ClearSlot(uint player, int barIndex, int slotIndex);

        /// <summary>
        /// Runs whatever the slot holds.
        /// </summary>
        void UseSlot(uint player, ExtraHotbarSlot slot);

        /// <summary>
        /// The abilities a player can put on a bar: every registered active ability they know.
        /// </summary>
        List<FeatType> GetKnownAbilities(uint player);

        string GetAbilityIcon(FeatType feat);

        /// <summary>
        /// The combat modes the player can turn on. Detect is open to everyone and Stealth needs the Stealth perk;
        /// every other native mode needs a feat players cannot get, and Parry has no effect in SWLOR combat.
        /// </summary>
        List<ActionMode> GetAvailableActionModes(uint player);

        string GetActionModeName(ActionMode mode);

        string GetActionModeIcon(ActionMode mode);

        /// <summary>
        /// What each hand holds after an equipment slot is used. A hand the slot leaves out keeps its item unless
        /// that item is moving to the other hand or cannot be held beside the new one.
        /// </summary>
        (uint MainHand, uint OffHand) PlanHands(uint currentMainHand, uint currentOffHand,
            uint mainHand, uint offHand, Func<uint, uint, bool> canPairHands);

        /// <summary>
        /// Whether every item of an equipment slot is already in its hand.
        /// </summary>
        bool IsEquipped(uint player, uint mainHand, uint offHand);

        /// <summary>
        /// Finds an equipment slot's items on the player. The exact items come first, so two identical weapons
        /// stay apart; an item whose original is gone falls back to one with the same resref and name.
        /// </summary>
        (uint MainHand, uint OffHand) FindEquipItems(uint player, ExtraHotbarSlot slot);

        /// <summary>
        /// Same as above with a hand-item list already built, so several slots can share one inventory scan.
        /// </summary>
        (uint MainHand, uint OffHand) FindEquipItems(ExtraHotbarSlot slot, List<uint> items);

        /// <summary>
        /// Items the player can hold in a hand: what they are holding now, then their inventory.
        /// </summary>
        List<uint> GetHandItems(uint player);

        bool CanHoldInMainHand(uint item);

        /// <summary>
        /// Companion pistols and gloves come with their main-hand item, so they are never picked on their own.
        /// </summary>
        bool CanHoldInOffHand(uint item);

        /// <summary>
        /// Whether two items can be held together. Pistols take a shield in the off hand; Twin Pistols, other ranged
        /// weapons, Power Gloves and weapons too large for the off hand leave no room for an off-hand item.
        /// A generated companion belongs to its main-hand item, which keeps or removes it.
        /// </summary>
        bool CanPairHands(uint mainHand, uint offHand);

        /// <summary>
        /// The picker's hand choices after the player clicks an item. Clicking a chosen item takes it back out;
        /// an off-hand-only item such as a shield goes to the off hand; an item goes to the off hand when it fits
        /// beside the chosen main hand; anything else becomes the main hand. A hand that no longer fits is cleared.
        /// </summary>
        (uint MainHand, uint OffHand) PickHandItem(uint mainHand, uint offHand, uint item,
            bool canMainHand, bool canOffHand, Func<uint, uint, bool> canPairHands);

        /// <summary>
        /// Usable items in the player's inventory, one per resref.
        /// </summary>
        List<uint> GetUsableItems(uint player);

        uint FindItem(uint player, string resref);

        int CountItems(uint player, string resref);

        void RefreshOnAcquire();

        void RefreshOnUnacquire();

        void RefreshOnEquip();

        void RefreshOnUnequip();

        void RefreshOnHeartbeat();
    }
}
