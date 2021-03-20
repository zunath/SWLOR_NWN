using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public class ShipModuleBuilder
    {
        private readonly Dictionary<string, ShipModuleDetail> _shipModules = new Dictionary<string, ShipModuleDetail>();
        private ShipModuleDetail _activeShipModule;

        /// <summary>
        /// Creates a new ship module.
        /// </summary>
        /// <param name="itemTag">The tag of the item associated with this ship module.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        public ShipModuleBuilder Create(string itemTag)
        {
            _activeShipModule = new ShipModuleDetail();
            _shipModules[itemTag] = _activeShipModule;

            return this;
        }

        /// <summary>
        /// Sets the name of the active ship module we're building.
        /// </summary>
        /// <param name="name">The name to set.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        public ShipModuleBuilder Name(string name)
        {
            _activeShipModule.Name = name;

            return this;
        }

        /// <summary>
        /// Sets the short name of the active ship module we're building.
        /// Short names should generally be no longer than 14 characters.
        /// </summary>
        /// <param name="shortName">The short name to set.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        public ShipModuleBuilder ShortName(string shortName)
        {
            if (shortName.Length > 14)
            {
                Log.Write(LogGroup.Space, $"Ship module with short name {shortName} is longer than 14 characters. Short names should be no more than 14 characters so they display on the UI properly.", true);
            }

            _activeShipModule.ShortName = shortName;

            return this;
        }

        /// <summary>
        /// Sets the description of the active ship module we're building.
        /// This description is shown when the ship module is examined.
        /// </summary>
        /// <param name="description">The description to set.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        public ShipModuleBuilder Description(string description)
        {
            _activeShipModule.Description = description;

            return this;
        }

        /// <summary>
        /// Sets the power type of the active ship module we're building.
        /// </summary>
        /// <param name="powerType">The power type to set.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        public ShipModuleBuilder PowerType(ShipModulePowerType powerType)
        {
            _activeShipModule.PowerType = powerType;

            return this;
        }

        /// <summary>
        /// Indicates this ship module can be actively used.
        /// </summary>
        /// <returns>A ship module builder with the configured options.</returns>
        public ShipModuleBuilder IsActiveModule()
        {
            _activeShipModule.IsPassive = false;

            return this;
        }

        /// <summary>
        /// Sets the action to run when this module's recast timer is calculated.
        /// </summary>
        /// <param name="action">The action to take.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        public ShipModuleBuilder Recast(ShipModuleCalculateRecastDelegate action)
        {
            _activeShipModule.CalculateRecastAction = action;

            return this;
        }

        /// <summary>
        /// Sets the number of seconds it takes to recast the module.
        /// </summary>
        /// <param name="seconds">The number of seconds to apply to the recast when used.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        public ShipModuleBuilder Recast(float seconds)
        {
            _activeShipModule.CalculateRecastAction = (player, ship) => seconds;

            return this;
        }

        /// <summary>
        /// Sets the action to run when this module's capacitor usage is calculated.
        /// </summary>
        /// <param name="action">The action to take.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        public ShipModuleBuilder Capacitor(ShipModuleCalculateCapacitorDelegate action)
        {
            _activeShipModule.CalculateCapacitorAction = action;

            return this;
        }

        /// <summary>
        /// Sets the amount of capacitor required to use the module.
        /// </summary>
        /// <param name="capacitor">The amount of capacitor to drain from the ship when this module is used.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        public ShipModuleBuilder Capacitor(int capacitor)
        {
            _activeShipModule.CalculateCapacitorAction = (player, ship) => capacitor;

            return this;
        }

        /// <summary>
        /// Sets the action to run when this module is equipped to a ship.
        /// </summary>
        /// <param name="action">The action to take.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        public ShipModuleBuilder EquippedAction(ShipModuleEquippedDelegate action)
        {
            _activeShipModule.ModuleEquippedAction = action;

            return this;
        }

        /// <summary>
        /// Sets the action to run when this module is unequipped from a ship.
        /// </summary>
        /// <param name="action">The action to take.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        public ShipModuleBuilder UnequippedAction(ShipModuleUnequippedDelegate action)
        {
            _activeShipModule.ModuleUnequippedAction = action;

            return this;
        }

        /// <summary>
        /// Sets the action to run when this module is used in space by the player.
        /// </summary>
        /// <param name="action">The action to take.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        public ShipModuleBuilder ActivatedAction(ShipModuleActivatedDelegate action)
        {
            _activeShipModule.ModuleActivatedAction = action;

            return this;
        }

        /// <summary>
        /// Indicates a player must have the perk at a specific level in order to equip and use it.
        /// </summary>
        /// <param name="perkType">The type of perk to require.</param>
        /// <param name="requiredLevel">The required level of the perk.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        public ShipModuleBuilder RequirePerk(PerkType perkType, int requiredLevel)
        {
            if (requiredLevel < 0)
            {
                Log.Write(LogGroup.Error, $"Failed to add required perk to {_activeShipModule.Name} because requiredLevel cannot be less than zero.");
                return this;
            }
            if (requiredLevel > 100)
            {
                Log.Write(LogGroup.Error, $"Failed to add required perk to {_activeShipModule.Name} because requiredLevel cannot be greater than 100.");
                return this;
            }

            _activeShipModule.RequiredPerks[perkType] = requiredLevel;

            return this;
        }

        /// <summary>
        /// Builds a dictionary of ship module details.
        /// </summary>
        /// <returns>A dictionary of ship module details.</returns>
        public Dictionary<string, ShipModuleDetail> Build()
        {
            return _shipModules;
        }
    }
}
