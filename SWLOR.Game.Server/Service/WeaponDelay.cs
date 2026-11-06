using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The attack delay each weapon category carries, and the Delay item property that stores it. The logic lives in
    /// <see cref="IWeaponDelayService"/>; the event handler stays here and every member forwards to the container's instance.
    /// </summary>
    public static class WeaponDelay
    {
        private static IWeaponDelayService Instance => FacadeAccessor.Resolve<IWeaponDelayService>();

        /// <summary>
        /// Gets the delay value for a weapon by its BaseItemType
        /// </summary>
        /// <param name="baseItemType">The BaseItemType of the weapon</param>
        /// <returns>The delay value, or null if not found</returns>
        public static ItemPropertyAttackDelay? GetWeaponDelay(BaseItem baseItemType) => Instance.GetWeaponDelay(baseItemType);

        /// <summary>
        /// Checks if a weapon has a Delay item property
        /// </summary>
        /// <param name="item">The weapon item to check</param>
        /// <returns>True if the weapon has a Delay property, false otherwise</returns>
        public static bool HasDelayProperty(uint item) => Instance.HasDelayProperty(item);

        /// <summary>
        /// Adds a Delay item property to a weapon
        /// </summary>
        /// <param name="item">The weapon item to add the property to</param>
        /// <param name="delay">The delay value to set.</param>
        public static void AddDelayProperty(uint item, ItemPropertyAttackDelay delay) => Instance.AddDelayProperty(item, delay);

        /// <summary>
        /// Checks if an item is a weapon that should have delay properties
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>True if the item is a weapon that should have delay properties</returns>
        public static bool IsWeapon(uint item) => Instance.IsWeapon(item);

        /// <summary>
        /// Applies delay property to a weapon if it's missing and the weapon has a known delay value
        /// </summary>
        /// <param name="item">The weapon item to process</param>
        public static void ApplyDelayPropertyIfMissing(uint item) => Instance.ApplyDelayPropertyIfMissing(item);

        /// <summary>
        /// When a weapon is equipped, check if it has a Delay item property.
        /// If not, and it's a known weapon type, add the appropriate delay property.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEquip)]
        public static void ApplyWeaponDelayFallback() => Instance.ApplyWeaponDelayFallback();
    }
}
