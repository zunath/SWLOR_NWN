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
    /// The hand rules of the equipment slots, and equipping a slot. A part of the ExtraHotbars service.
    /// </summary>
    public interface IExtraHotbarsEquipService
    {
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

        /// <summary>Whether the item is a companion pistol or glove, which leaves with its main-hand item.</summary>
        bool IsHandCompanion(uint item);

        /// <summary>
        /// Puts the slot's items in their hands, then goes back to attacking whatever the player was fighting.
        /// The usual equip checks still apply and explain any item that cannot be held. A set already in hand is put away.
        /// </summary>
        void EquipHands(uint player, ExtraHotbarSlot slot);
    }
}
