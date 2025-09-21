using System.Collections.Generic;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public class SpaceObjectBuilder
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, SpaceObjectDetail> _spaceObjects = new();
        private SpaceObjectDetail _activeSpaceObject;
        private string _creatureTag;

        public SpaceObjectBuilder(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Creates a new space object.
        /// </summary>
        /// <param name="objectTag">The tag of the object to associate with this space object detail.</param>
        /// <returns>A ship enemy builder with the configured options.</returns>
        public SpaceObjectBuilder Create(string objectTag)
        {
            _creatureTag = objectTag;
            _activeSpaceObject = new SpaceObjectDetail();
            _spaceObjects[objectTag] = _activeSpaceObject;

            return this;
        }

        /// <summary>
        /// Sets the item tag of the ship being used by the enemy.
        /// </summary>
        /// <param name="itemTag"></param>
        /// <returns></returns>
        public SpaceObjectBuilder ItemTag(string itemTag)
        {
            _activeSpaceObject.ShipItemTag = itemTag;

            return this;
        }

        /// <summary>
        /// Adds the specified ship module to the enemy's loadout.
        /// </summary>
        /// <param name="shipModuleItemTag">Item tag of the ship module to attach.</param>
        /// <returns>A ship enemy builder with the configured options.</returns>
        public SpaceObjectBuilder ShipModule(string shipModuleItemTag)
        {
            if (!Space.IsRegisteredShipModule(shipModuleItemTag))
            {
                _logger.Write<ErrorLogGroup>($"Failed to add {shipModuleItemTag} to ship enemy with tag {_creatureTag} as this module is not registered. Please ensure you entered the correct module tag.");
                return this;
            }

            var shipModule = Space.GetShipModuleDetailByItemTag(shipModuleItemTag);
            if (shipModule.PowerType == ShipModulePowerType.High)
            {
                _activeSpaceObject.HighPoweredModules.Add(shipModuleItemTag);
            }
            else
            {
                _activeSpaceObject.LowPowerModules.Add(shipModuleItemTag);
            }

            return this;
        }

        public Dictionary<string, SpaceObjectDetail> Build()
        {
            return _spaceObjects;
        }
    }
}
