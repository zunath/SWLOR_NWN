using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public class ShipModuleBuilder
    {
        private readonly Dictionary<ShipModuleType, ShipModuleDetail> _shipModules = new Dictionary<ShipModuleType, ShipModuleDetail>();
        private ShipModuleDetail _activeShipModule;

        /// <summary>
        /// Creates a new ship module.
        /// </summary>
        /// <param name="type">The type of ship module to create.</param>
        /// <returns>A ship module builder with the configured options.</returns>
        public ShipModuleBuilder Create(ShipModuleType type)
        {
            _activeShipModule = new ShipModuleDetail();
            _shipModules[type] = _activeShipModule;

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
        /// Indicates this ship module can be actively used.
        /// </summary>
        /// <returns>A ship module builder with the configured options.</returns>
        public ShipModuleBuilder IsActiveModule()
        {
            _activeShipModule.IsPassive = false;

            return this;
        }

        /// <summary>
        /// Builds a dictionary of ship module details.
        /// </summary>
        /// <returns>A dictionary of ship module details.</returns>
        public Dictionary<ShipModuleType, ShipModuleDetail> Build()
        {
            return _shipModules;
        }
    }
}
