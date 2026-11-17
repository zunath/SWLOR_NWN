using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum.Item;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Carries an item's custom colours across a model change. Static callers reach the container's instance
    /// through <c>GameServices.TintMapCarry</c>.
    /// </summary>
    public interface ITintMapCarryService
    {
        /// <summary>Reads the colours each material of an item layer holds before its model changes.</summary>
        TintMapItemColorCarry CaptureItemCustomColors(uint item, IReadOnlyList<TintMapMaterialSelection> selections);

        /// <summary>Links a replacement item into the pending carry of the item it replaces.</summary>
        void LinkPendingItemColorCarryReplacement(uint sourceItem, uint replacementItem);

        /// <summary>
        /// Queues a captured carry for the replacement item. It runs after a short delay, or at once when
        /// <paramref name="applyImmediately"/> is set.
        /// </summary>
        void QueueItemCustomColorCarry(
            uint creature,
            uint sourceItem,
            uint item,
            uint player,
            InventorySlot slot,
            AppearanceArmor armorPart,
            TintMapItemColorCarry carry,
            bool applyImmediately = false);

        /// <summary>
        /// Gives every worn material that lost its colour the colour of the equivalent material the item still
        /// stores, so a part swap keeps its dye.
        /// </summary>
        void CarryStoredEquipmentCustomColors(uint creature);
    }
}
