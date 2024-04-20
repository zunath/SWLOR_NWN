using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.PropertyService
{
    public class PropertyLayoutBuilder
    {
        private PropertyLayout _currentLayout;
        private Dictionary<PropertyLayoutType, PropertyLayout> _layouts = new();

        /// <summary>
        /// Creates a new layout definition.
        /// </summary>
        /// <param name="type">The type of layout to associate with this entry.</param>
        /// <returns>A PropertyLayoutBuilder with the configured options.</returns>
        public PropertyLayoutBuilder Create(PropertyLayoutType type)
        {
            _currentLayout = new PropertyLayout();
            _layouts[type] = _currentLayout;

            return this;
        }

        /// <summary>
        /// Specifies the property type associated with this layout.
        /// </summary>
        /// <param name="type">The type of property to associate.</param>
        /// <returns>A PropertyLayoutBuilder with the configured options.</returns>
        public PropertyLayoutBuilder PropertyType(PropertyType type)
        {
            _currentLayout.PropertyType = type;

            return this;
        }

        /// <summary>
        /// Specifies the default name to apply to the layout.
        /// This can be changed by the player.
        /// </summary>
        /// <param name="name">The default name to apply.</param>
        /// <returns>A PropertyLayoutBuilder with the configured options.</returns>
        public PropertyLayoutBuilder Name(string name)
        {
            _currentLayout.Name = name;

            return this;
        }

        /// <summary>
        /// Specifies the number of structures (non-buildings) that can be placed within this layout.
        /// </summary>
        /// <param name="structureLimit">The number of structures this layout allows.</param>
        /// <returns>A PropertyLayoutBuilder with the configured options.</returns>
        public PropertyLayoutBuilder StructureLimit(int structureLimit)
        {
            _currentLayout.StructureLimit = structureLimit;

            return this;
        }

        /// <summary>
        /// Specifies the number of items which can be stored within this type of layout.
        /// Only applicable for buildings, apartments, and starships.
        /// </summary>
        /// <param name="itemStorageLimit">The number of items this layout allows.</param>
        /// <returns>A PropertyLayoutBuilder with the configured options.</returns>
        public PropertyLayoutBuilder ItemStorageLimit(int itemStorageLimit)
        {
            _currentLayout.ItemStorageLimit = itemStorageLimit;

            return this;
        }

        /// <summary>
        /// Specifies the number of buildings which can be placed within a city.
        /// Only applicable for cities.
        /// </summary>
        /// <param name="buildingLimit">The number of buildings this layout allows.</param>
        /// <returns>A PropertyLayoutBuilder with the configured options.</returns>
        public PropertyLayoutBuilder BuildingLimit(int buildingLimit)
        {
            _currentLayout.BuildingLimit = buildingLimit;

            return this;
        }

        /// <summary>
        /// Specifies the number of research devices which can be placed within this type of layout.
        /// Only applicable for labs.
        /// </summary>
        /// <param name="deviceLimit"></param>
        /// <returns></returns>
        public PropertyLayoutBuilder ResearchDeviceLimit(int deviceLimit)
        {
            _currentLayout.ResearchDeviceLimit = deviceLimit;

            return this;
        }

        /// <summary>
        /// Specifies the initial price paid to purchase this layout.
        /// Only applicable for cities and apartments.
        /// </summary>
        /// <param name="initialPrice">The initial price to charge.</param>
        /// <returns>A PropertyLayoutBuilder with the configured options.</returns>
        public PropertyLayoutBuilder InitialPrice(int initialPrice)
        {
            _currentLayout.InitialPrice = initialPrice;

            return this;
        }

        /// <summary>
        /// Specifies the amount of credits required per day to maintain this layout.
        /// Only applicable for cities and apartments.
        /// </summary>
        /// <param name="pricePerDay">The price per day to specify.</param>
        /// <returns>A PropertyLayoutBuilder with the configured options.</returns>
        public PropertyLayoutBuilder PricePerDay(int pricePerDay)
        {
            _currentLayout.PricePerDay = pricePerDay;

            return this;
        }

        /// <summary>
        /// Specifies the resref of the area instance used for this layout.
        /// </summary>
        /// <param name="resref">The resref of the area instance template.</param>
        /// <returns>A PropertyLayoutBuilder with the configured options.</returns>
        public PropertyLayoutBuilder AreaInstance(string resref)
        {
            _currentLayout.AreaInstanceResref = resref;

            return this;
        }

        /// <summary>
        /// Runs an action when the layout is spawned into the world.
        /// </summary>
        /// <param name="action">The action to perform upon spawning into the world.</param>
        /// <returns>A PropertyLayoutBuilder with the configured options.</returns>
        public PropertyLayoutBuilder OnSpawn(Action<uint> action)
        {
            _currentLayout.OnSpawnAction = action;

            return this;
        }

        /// <summary>
        /// Runs an action when the layout's parent city is upgraded.
        /// </summary>
        /// <param name="action"></param>
        /// <returns>A PropertyLayoutBuilder with the configured options.</returns>
        public PropertyLayoutBuilder OnCityUpgraded(Action<uint, PropertyUpgradeType, int> action)
        {
            _currentLayout.OnCityUpgradeAction = action;

            return this;
        }

        /// <summary>
        /// Builds a dictionary of layouts.
        /// </summary>
        /// <returns>A dictionary of layouts.</returns>
        public Dictionary<PropertyLayoutType, PropertyLayout> Build()
        {
            return _layouts;
        }

    }
}
