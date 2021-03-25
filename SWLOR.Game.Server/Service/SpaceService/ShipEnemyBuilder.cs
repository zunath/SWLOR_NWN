using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public class ShipEnemyBuilder
    {
        private readonly Dictionary<string, ShipEnemyDetail> _shipEnemies = new Dictionary<string, ShipEnemyDetail>();
        private ShipEnemyDetail _activeShipEnemy;
        private string _creatureTag;

        /// <summary>
        /// Creates a new ship enemy.
        /// </summary>
        /// <param name="creatureTag">The tag of the creature to associate with this ship detail.</param>
        /// <returns>A ship enemy builder with the configured options.</returns>
        public ShipEnemyBuilder Create(string creatureTag)
        {
            _creatureTag = creatureTag;
            _activeShipEnemy = new ShipEnemyDetail();
            _shipEnemies[creatureTag] = _activeShipEnemy;

            return this;
        }

        /// <summary>
        /// Sets the item tag of the ship being used by the enemy.
        /// </summary>
        /// <param name="itemTag"></param>
        /// <returns></returns>
        public ShipEnemyBuilder ItemTag(string itemTag)
        {
            _activeShipEnemy.ShipItemTag = itemTag;

            return this;
        }

        /// <summary>
        /// Adds the specified ship module to the enemy's loadout.
        /// </summary>
        /// <param name="shipModuleItemTag">Item tag of the ship module to attach.</param>
        /// <returns>A ship enemy builder with the configured options.</returns>
        public ShipEnemyBuilder ShipModule(string shipModuleItemTag)
        {
            if (!Space.IsRegisteredShipModule(shipModuleItemTag))
            {
                Log.Write(LogGroup.Error, $"Failed to add {shipModuleItemTag} to ship enemy with tag {_creatureTag} as this module is not registered. Please ensure you entered the correct module tag.");
                return this;
            }

            var shipModule = Space.GetShipModuleDetailByItemTag(shipModuleItemTag);
            if (shipModule.PowerType == ShipModulePowerType.High)
            {
                _activeShipEnemy.HighPoweredModules.Add(shipModuleItemTag);
            }
            else
            {
                _activeShipEnemy.LowPowerModules.Add(shipModuleItemTag);
            }

            return this;
        }

        public Dictionary<string, ShipEnemyDetail> Build()
        {
            return _shipEnemies;
        }
    }
}
