using System;

namespace SWLOR.Game.Server.Domain.Items
{
    /// <summary>
    /// Lets equipment owners reserve safe removal before any shared equipment hooks run. The static facade
    /// <c>Service.EquipmentUnequip</c> forwards to the container's instance of this interface and keeps the event handler.
    /// </summary>
    public interface IEquipmentUnequipService
    {
        /// <summary>
        /// Registers the guard an equipment owner uses to take over the removal of its items. Registering the same key
        /// again replaces that owner's previous guard.
        /// </summary>
        /// <param name="key">The owner of the guard.</param>
        /// <param name="guard">Answers true when it handled the removal of the item from the creature.</param>
        void RegisterGuard(string key, Func<uint, uint, bool> guard);

        /// <summary>
        /// Asks each registered guard, in registration order, until one handles the removal.
        /// </summary>
        /// <param name="creature">The creature removing the item.</param>
        /// <param name="item">The item being removed.</param>
        /// <returns>true when a guard handled the removal, false otherwise.</returns>
        bool TryHandle(uint creature, uint item);

        /// <summary>
        /// Before an item is unequipped, lets the guards handle it. A handled request skips the event; any other
        /// request goes on to the shared unequip scripts.
        /// </summary>
        void BeforeUnequip();
    }
}
