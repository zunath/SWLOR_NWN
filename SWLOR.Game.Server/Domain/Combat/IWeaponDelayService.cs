using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>The attack delay each weapon category carries, and the Delay item property that stores it.</summary>
    public interface IWeaponDelayService
    {
        /// <summary>Gets the delay value for a weapon by its BaseItemType.</summary>
        /// <param name="baseItemType">The BaseItemType of the weapon</param>
        /// <returns>The delay value, or null if not found</returns>
        ItemPropertyAttackDelay? GetWeaponDelay(BaseItem baseItemType);

        /// <summary>Checks if a weapon has a Delay item property.</summary>
        /// <param name="item">The weapon item to check</param>
        /// <returns>True if the weapon has a Delay property, false otherwise</returns>
        bool HasDelayProperty(uint item);

        /// <summary>Adds a Delay item property to a weapon.</summary>
        /// <param name="item">The weapon item to add the property to</param>
        /// <param name="delay">The delay value to set.</param>
        void AddDelayProperty(uint item, ItemPropertyAttackDelay delay);

        /// <summary>Checks if an item is a weapon that should have delay properties.</summary>
        /// <param name="item">The item to check</param>
        /// <returns>True if the item is a weapon that should have delay properties</returns>
        bool IsWeapon(uint item);

        /// <summary>Applies delay property to a weapon if it's missing and the weapon has a known delay value.</summary>
        /// <param name="item">The weapon item to process</param>
        void ApplyDelayPropertyIfMissing(uint item);

        /// <summary>
        /// When a weapon is equipped, check if it has a Delay item property.
        /// If not, and it's a known weapon type, add the appropriate delay property.
        /// </summary>
        void ApplyWeaponDelayFallback();
    }
}
