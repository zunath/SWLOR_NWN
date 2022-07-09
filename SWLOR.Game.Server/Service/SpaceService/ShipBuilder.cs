using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public class ShipBuilder
    {
        private readonly Dictionary<string, ShipDetail> _ships = new Dictionary<string, ShipDetail>();
        private ShipDetail _activeShip;

        /// <summary>
        /// Creates a new ship.
        /// </summary>
        /// <param name="itemTag">The tag of the item to associate with the ship detail.</param>
        /// <returns>A ship builder with the configured options.</returns>
        public ShipBuilder Create(string itemTag)
        {
            _activeShip = new ShipDetail();
            _ships[itemTag] = _activeShip;

            return this;
        }

        /// <summary>
        /// Sets the name of the ship currently being built.
        /// </summary>
        /// <param name="name">The name to set.</param>
        /// <returns>A ship builder with the configured options.</returns>
        public ShipBuilder Name(string name)
        {
            _activeShip.Name = name;

            return this;
        }

        /// <summary>
        /// Sets the appearance of the ship.
        /// The player's appearance will be changed to this when they enter space mode.
        /// </summary>
        /// <param name="appearance">The appearance to set.</param>
        /// <returns>A ship builder with the configured options.</returns>
        public ShipBuilder Appearance(AppearanceType appearance)
        {
            _activeShip.Appearance = appearance;

            return this;
        }

        /// <summary>
        /// Sets the resref of the deed item which is used for registering this ship type.
        /// </summary>
        /// <param name="itemResref">The resref of the deed item.</param>
        /// <returns>A ship builder with the configured options.</returns>
        public ShipBuilder ItemResref(string itemResref)
        {
            _activeShip.ItemResref = itemResref;

            return this;
        }

        /// <summary>
        /// Sets the maximum shields of the ship.
        /// </summary>
        /// <param name="maxShield">The value to set.</param>
        /// <returns>A ship builder with the configured options.</returns>
        public ShipBuilder MaxShield(int maxShield)
        {
            _activeShip.MaxShield = maxShield;

            return this;
        }

        /// <summary>
        /// Sets the maximum armor of the ship.
        /// </summary>
        /// <param name="maxArmor">The value to set.</param>
        /// <returns>A ship builder with the configured options.</returns>
        public ShipBuilder MaxArmor(int maxArmor)
        {
            _activeShip.MaxHull = maxArmor;

            return this;
        }

        /// <summary>
        /// Sets the maximum capacitor of the ship.
        /// </summary>
        /// <param name="maxCapacitor">The value to set.</param>
        /// <returns>A ship builder with the configured options.</returns>
        public ShipBuilder MaxCapacitor(int maxCapacitor)
        {
            _activeShip.MaxCapacitor = maxCapacitor;

            return this;
        }

        /// <summary>
        /// Sets the shield recharge rate of the ship (in whole seconds).
        /// </summary>
        /// <param name="shieldRechargeRate">The value to set.</param>
        /// <returns>A ship builder with the configured options.</returns>
        public ShipBuilder ShieldRechargeRate(int shieldRechargeRate)
        {
            _activeShip.ShieldRechargeRate = shieldRechargeRate;

            return this;
        }

        /// <summary>
        /// Sets the number of high power nodes on this ship.
        /// High power nodes are typically used for weaponry and mining lasers.
        /// </summary>
        /// <param name="highPowerNodes">The number of high power nodes to set.</param>
        /// <returns>A ship builder with the configured options.</returns>
        public ShipBuilder HighPowerNodes(int highPowerNodes)
        {
            _activeShip.HighPowerNodes = highPowerNodes;

            return this;
        }

        /// <summary>
        /// Sets the number of low power nodes on this ship.
        /// Low power nodes are typically used for shield boosters, armor reinforcement, etc.
        /// </summary>
        /// <param name="lowPowerNodes">The number of low power nodes to set.</param>
        /// <returns>A ship builder with the configured options.</returns>
        public ShipBuilder LowPowerNodes(int lowPowerNodes)
        {
            _activeShip.LowPowerNodes = lowPowerNodes;

            return this;
        }

        /// <summary>
        /// Indicates a player must have the perk at a specific level in order to use the ship.
        /// </summary>
        /// <param name="perkType">The type of perk to require</param>
        /// <param name="requiredLevel">The minimum level required</param>
        /// <returns>A ship builder with the configured options.</returns>
        public ShipBuilder RequirePerk(PerkType perkType, int requiredLevel)
        {
            if (requiredLevel < 0)
            {
                Log.Write(LogGroup.Error, $"Failed to add required perk to {_activeShip.Name} because requiredLevel cannot be less than zero.");
                return this;
            }
            if (requiredLevel > 100)
            {
                Log.Write(LogGroup.Error, $"Failed to add required perk to {_activeShip.Name} because requiredLevel cannot be greater than 100.");
                return this;
            }

            _activeShip.RequiredPerks[perkType] = requiredLevel;

            return this;
        }

        /// <summary>
        /// Sets the interior layout type used by the associated property on this ship.
        /// </summary>
        /// <param name="layout">The layout to assign.</param>
        /// <returns>A ship builder with the configured options.</returns>
        public ShipBuilder InteriorLayout(PropertyLayoutType layout)
        {
            _activeShip.Layout = layout;
            return this;
        }

        /// <summary>
        /// Returns a built dictionary of ships.
        /// </summary>
        /// <returns>A dictionary of ship details.</returns>
        public Dictionary<string, ShipDetail> Build()
        {
            return _ships;
        }
    }
}
