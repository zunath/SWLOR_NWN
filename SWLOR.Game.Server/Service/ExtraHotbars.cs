using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Service.ExtraHotbarService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Extra click-to-use hotbars shown beside the native quickbar. Slots hold abilities, inventory items or chat commands.
    /// The static facade every caller uses: the logic is <see cref="ExtraHotbarsService"/>, reached through the container.
    /// </summary>
    public static class ExtraHotbars
    {
        private static IExtraHotbarsService Instance => FacadeAccessor.Resolve<IExtraHotbarsService>();

        public const int BarCount = Domain.UI.ExtraHotbarsService.BarCount;
        public const int MaxSlots = Domain.UI.ExtraHotbarsService.MaxSlots;

        public static GuiWindowType GetWindowType(int barIndex)
        {
            return Instance.GetWindowType(barIndex);
        }

        public static int GetBarIndex(GuiWindowType windowType)
        {
            return Instance.GetBarIndex(windowType);
        }

        public static bool IsUnlocked(uint player)
        {
            return Instance.IsUnlocked(player);
        }

        /// <summary>
        /// Returns the player's bars, adding any that are missing so every bar has a full set of slots.
        /// </summary>
        public static List<ExtraHotbar> GetBars(Player dbPlayer)
        {
            return Instance.GetBars(dbPlayer);
        }

        public static ExtraHotbar GetBar(uint player, int barIndex)
        {
            return Instance.GetBar(player, barIndex);
        }

        // Area entry follows every login, after the player's windows exist.
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void OpenBarsOnEnter()
        {
            Instance.OpenBarsOnEnter();
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void LockBarsOnEnter()
        {
            Instance.LockBarsOnEnter();
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void LockBarsOnExit()
        {
            Instance.LockBarsOnExit();
        }

        /// <summary>
        /// Opens the bars the player has turned on and closes the rest.
        /// </summary>
        public static void ApplyVisibility(uint player)
        {
            Instance.ApplyVisibility(player);
        }

        public static void SetUnlocked(uint player, bool isUnlocked)
        {
            Instance.SetUnlocked(player, isUnlocked);
        }

        /// <summary>
        /// Changes one bar's layout settings and redraws it.
        /// </summary>
        public static void UpdateBar(uint player, int barIndex, System.Action<ExtraHotbar> change)
        {
            Instance.UpdateBar(player, barIndex, change);
        }

        public static void AssignAbility(uint player, int barIndex, int slotIndex, FeatType feat)
        {
            Instance.AssignAbility(player, barIndex, slotIndex, feat);
        }

        public static void AssignItem(uint player, int barIndex, int slotIndex, uint item)
        {
            Instance.AssignItem(player, barIndex, slotIndex, item);
        }

        /// <summary>
        /// Puts a set of hand items on a slot. Either hand may be left out, which leaves that hand alone when used.
        /// </summary>
        public static void AssignEquipment(uint player, int barIndex, int slotIndex, uint mainHand, uint offHand)
        {
            Instance.AssignEquipment(player, barIndex, slotIndex, mainHand, offHand);
        }

        public static void AssignChatCommand(uint player, int barIndex, int slotIndex, string command)
        {
            Instance.AssignChatCommand(player, barIndex, slotIndex, command);
        }

        public static void AssignActionMode(uint player, int barIndex, int slotIndex, ActionMode mode)
        {
            Instance.AssignActionMode(player, barIndex, slotIndex, mode);
        }

        public static void ClearSlot(uint player, int barIndex, int slotIndex)
        {
            Instance.ClearSlot(player, barIndex, slotIndex);
        }

        /// <summary>
        /// Runs whatever the slot holds.
        /// </summary>
        public static void UseSlot(uint player, ExtraHotbarSlot slot)
        {
            Instance.UseSlot(player, slot);
        }

        /// <summary>
        /// The abilities a player can put on a bar: every registered active ability they know.
        /// </summary>
        public static List<FeatType> GetKnownAbilities(uint player)
        {
            return Instance.GetKnownAbilities(player);
        }

        public static string GetAbilityIcon(FeatType feat)
        {
            return Instance.GetAbilityIcon(feat);
        }

        /// <summary>
        /// The combat modes the player can turn on. Detect is open to everyone and Stealth needs the Stealth perk;
        /// every other native mode needs a feat players cannot get, and Parry has no effect in SWLOR combat.
        /// </summary>
        public static List<ActionMode> GetAvailableActionModes(uint player)
        {
            return Instance.GetAvailableActionModes(player);
        }

        public static string GetActionModeName(ActionMode mode)
        {
            return Instance.GetActionModeName(mode);
        }

        public static string GetActionModeIcon(ActionMode mode)
        {
            return Instance.GetActionModeIcon(mode);
        }

        /// <summary>
        /// What each hand holds after an equipment slot is used. A hand the slot leaves out keeps its item unless
        /// that item is moving to the other hand or cannot be held beside the new one.
        /// </summary>
        public static (uint MainHand, uint OffHand) PlanHands(uint currentMainHand, uint currentOffHand, uint mainHand, uint offHand, Func<uint, uint, bool> canPairHands)
        {
            return Instance.PlanHands(currentMainHand, currentOffHand, mainHand, offHand, canPairHands);
        }

        /// <summary>
        /// Whether every item of an equipment slot is already in its hand.
        /// </summary>
        public static bool IsEquipped(uint player, uint mainHand, uint offHand)
        {
            return Instance.IsEquipped(player, mainHand, offHand);
        }

        /// <summary>
        /// Finds an equipment slot's items on the player. The exact items come first, so two identical weapons
        /// stay apart; an item whose original is gone falls back to one with the same resref and name.
        /// </summary>
        public static (uint MainHand, uint OffHand) FindEquipItems(uint player, ExtraHotbarSlot slot)
        {
            return Instance.FindEquipItems(player, slot);
        }

        /// <summary>
        /// Same as above with a hand-item list already built, so several slots can share one inventory scan.
        /// </summary>
        public static (uint MainHand, uint OffHand) FindEquipItems(ExtraHotbarSlot slot, List<uint> items)
        {
            return Instance.FindEquipItems(slot, items);
        }

        /// <summary>
        /// Items the player can hold in a hand: what they are holding now, then their inventory.
        /// </summary>
        public static List<uint> GetHandItems(uint player)
        {
            return Instance.GetHandItems(player);
        }

        public static bool CanHoldInMainHand(uint item)
        {
            return Instance.CanHoldInMainHand(item);
        }

        /// <summary>
        /// Companion pistols and gloves come with their main-hand item, so they are never picked on their own.
        /// </summary>
        public static bool CanHoldInOffHand(uint item)
        {
            return Instance.CanHoldInOffHand(item);
        }

        /// <summary>
        /// Whether two items can be held together. Pistols take a shield in the off hand; Twin Pistols, other ranged
        /// weapons, Power Gloves and weapons too large for the off hand leave no room for an off-hand item.
        /// A generated companion belongs to its main-hand item, which keeps or removes it.
        /// </summary>
        public static bool CanPairHands(uint mainHand, uint offHand)
        {
            return Instance.CanPairHands(mainHand, offHand);
        }

        /// <summary>
        /// The picker's hand choices after the player clicks an item. Clicking a chosen item takes it back out;
        /// an off-hand-only item such as a shield goes to the off hand; an item goes to the off hand when it fits
        /// beside the chosen main hand; anything else becomes the main hand. A hand that no longer fits is cleared.
        /// </summary>
        public static (uint MainHand, uint OffHand) PickHandItem(uint mainHand, uint offHand, uint item, bool canMainHand, bool canOffHand, Func<uint, uint, bool> canPairHands)
        {
            return Instance.PickHandItem(mainHand, offHand, item, canMainHand, canOffHand, canPairHands);
        }

        /// <summary>
        /// Usable items in the player's inventory, one per resref.
        /// </summary>
        public static List<uint> GetUsableItems(uint player)
        {
            return Instance.GetUsableItems(player);
        }

        public static uint FindItem(uint player, string resref)
        {
            return Instance.FindItem(player, resref);
        }

        public static int CountItems(uint player, string resref)
        {
            return Instance.CountItems(player, resref);
        }

        [NWNEventHandler(ScriptName.OnModuleAcquire)]
        public static void RefreshOnAcquire()
        {
            Instance.RefreshOnAcquire();
        }

        [NWNEventHandler(ScriptName.OnModuleUnacquire)]
        public static void RefreshOnUnacquire()
        {
            Instance.RefreshOnUnacquire();
        }

        [NWNEventHandler(ScriptName.OnModuleEquip)]
        public static void RefreshOnEquip()
        {
            Instance.RefreshOnEquip();
        }

        [NWNEventHandler(ScriptName.OnModuleUnequip)]
        public static void RefreshOnUnequip()
        {
            Instance.RefreshOnUnequip();
        }

        // Scripted item uses lower stack sizes and attacks drop stealth without an event, so slots also catch up each heartbeat.
        [NWNEventHandler(ScriptName.OnPlayerHeartbeat)]
        public static void RefreshOnHeartbeat()
        {
            Instance.RefreshOnHeartbeat();
        }
    }
}
