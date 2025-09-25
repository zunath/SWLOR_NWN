using SWLOR.Component.Space.Contracts;
using SWLOR.Component.Space.Model;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.Space.Enums;

namespace SWLOR.Component.Space.Service
{
    public class SpaceObjectBuilder : ISpaceObjectBuilder
    {
        private readonly ILogger _logger;
        private readonly ISpaceService _spaceService;
        private readonly Dictionary<string, SpaceObjectDetail> _spaceObjects = new();
        private SpaceObjectDetail _activeSpaceObject;
        private string _creatureTag;

        public SpaceObjectBuilder(
            ILogger logger,
            ISpaceService spaceService)
        {
            _logger = logger;
            _spaceService = spaceService;
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
            if (!_spaceService.IsRegisteredShipModule(shipModuleItemTag))
            {
                _logger.Write<ErrorLogGroup>($"Failed to add {shipModuleItemTag} to ship enemy with tag {_creatureTag} as this module is not registered. Please ensure you entered the correct module tag.");
                return this;
            }

            var shipModule = _spaceService.GetShipModuleDetailByItemTag(shipModuleItemTag);
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
