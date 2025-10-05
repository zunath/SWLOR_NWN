using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX.Model;

namespace SWLOR.Test.Shared.NWScriptMocks.NWNXPluginMocks
{
    /// <summary>
    /// Mock implementation of the ItemPropertyPlugin for testing purposes.
    /// Provides comprehensive item property management functionality including
    /// packing, unpacking, and retrieving active properties.
    /// </summary>
    public class ItemPropertyPluginMock: IItemPropertyPluginService
    {
        // Mock data storage
        private readonly Dictionary<ItemProperty, ItemPropertyUnpacked> _unpackedProperties = new();
        private readonly Dictionary<ItemPropertyUnpacked, ItemProperty> _packedProperties = new();
        private readonly Dictionary<uint, Dictionary<int, ItemPropertyUnpacked>> _activeProperties = new();
        private int _nextPropertyId = 1;

        /// <summary>
        /// Convert native itemproperty type to unpacked structure.
        /// </summary>
        /// <param name="ip">The itemproperty to convert.</param>
        /// <returns>A constructed ItemPropertyUnpacked.</returns>
        public ItemPropertyUnpacked UnpackIP(ItemProperty ip)
        {
            if (_unpackedProperties.TryGetValue(ip, out var existing))
            {
                return existing;
            }

            // Create a mock unpacked property
            var unpacked = new ItemPropertyUnpacked
            {
                Id = $"prop_{_nextPropertyId++}",
                Property = 1, // Mock property type
                SubType = 0,
                CostTable = 0,
                CostTableValue = 0,
                Param1 = 0,
                Param1Value = 0,
                UsesPerDay = 0,
                ChanceToAppear = 100,
                IsUseable = true,
                SpellId = 0,
                Creator = 0,
                Tag = string.Empty
            };

            _unpackedProperties[ip] = unpacked;
            return unpacked;
        }

        /// <summary>
        /// Convert unpacked itemproperty structure to native type.
        /// </summary>
        /// <param name="itemProperty">The ItemPropertyUnpacked structure to convert.</param>
        /// <returns>The itemproperty.</returns>
        public ItemProperty PackIP(ItemPropertyUnpacked itemProperty)
        {
            if (_packedProperties.TryGetValue(itemProperty, out var existing))
            {
                return existing;
            }

            // Create a mock property handle
            var mockHandle = _nextPropertyId++;
            var packedProperty = new ItemProperty(mockHandle);
            
            _packedProperties[itemProperty] = packedProperty;
            _unpackedProperties[packedProperty] = itemProperty;
            
            return packedProperty;
        }

        /// <summary>
        /// Gets the active item property at the index.
        /// </summary>
        /// <param name="oItem">The item with the property.</param>
        /// <param name="nIndex">The index such as returned by some Item Events.</param>
        /// <returns>A constructed ItemPropertyUnpacked, except for creator, and spell id.</returns>
        public ItemPropertyUnpacked GetActiveProperty(uint oItem, int nIndex)
        {
            if (_activeProperties.TryGetValue(oItem, out var itemProperties) &&
                itemProperties.TryGetValue(nIndex, out var property))
            {
                return property;
            }

            // Return a default mock property
            return new ItemPropertyUnpacked
            {
                Id = string.Empty, // Not returned by GetActiveProperty
                Property = 0,
                SubType = 0,
                CostTable = 0,
                CostTableValue = 0,
                Param1 = 0,
                Param1Value = 0,
                UsesPerDay = 0,
                ChanceToAppear = 100,
                IsUseable = false,
                SpellId = 0, // Not returned by GetActiveProperty
                Creator = 0, // Not returned by GetActiveProperty
                Tag = string.Empty
            };
        }

        /// <summary>
        /// Sets an active property for an item at a specific index.
        /// This is a helper method for testing purposes.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <param name="nIndex">The property index.</param>
        /// <param name="property">The property to set.</param>
        public void SetActiveProperty(uint oItem, int nIndex, ItemPropertyUnpacked property)
        {
            if (!_activeProperties.ContainsKey(oItem))
            {
                _activeProperties[oItem] = new Dictionary<int, ItemPropertyUnpacked>();
            }
            
            _activeProperties[oItem][nIndex] = property;
        }

        /// <summary>
        /// Removes an active property from an item at a specific index.
        /// This is a helper method for testing purposes.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <param name="nIndex">The property index to remove.</param>
        public void RemoveActiveProperty(uint oItem, int nIndex)
        {
            if (_activeProperties.TryGetValue(oItem, out var itemProperties))
            {
                itemProperties.Remove(nIndex);
            }
        }

        /// <summary>
        /// Gets all active properties for an item.
        /// This is a helper method for testing purposes.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <returns>A dictionary of index to property mappings.</returns>
        public Dictionary<int, ItemPropertyUnpacked> GetAllActiveProperties(uint oItem)
        {
            return _activeProperties.TryGetValue(oItem, out var properties) 
                ? new Dictionary<int, ItemPropertyUnpacked>(properties) 
                : new Dictionary<int, ItemPropertyUnpacked>();
        }

        /// <summary>
        /// Gets the count of active properties for an item.
        /// This is a helper method for testing purposes.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <returns>The number of active properties.</returns>
        public int GetActivePropertyCount(uint oItem)
        {
            return _activeProperties.TryGetValue(oItem, out var properties) ? properties.Count : 0;
        }

        /// <summary>
        /// Clears all active properties for an item.
        /// This is a helper method for testing purposes.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        public void ClearActiveProperties(uint oItem)
        {
            if (_activeProperties.ContainsKey(oItem))
            {
                _activeProperties[oItem].Clear();
            }
        }

        /// <summary>
        /// Gets all unpacked properties that have been created.
        /// This is a helper method for testing purposes.
        /// </summary>
        /// <returns>A dictionary of property to unpacked property mappings.</returns>
        public Dictionary<ItemProperty, ItemPropertyUnpacked> GetAllUnpackedProperties()
        {
            return new Dictionary<ItemProperty, ItemPropertyUnpacked>(_unpackedProperties);
        }

        /// <summary>
        /// Gets all packed properties that have been created.
        /// This is a helper method for testing purposes.
        /// </summary>
        /// <returns>A dictionary of unpacked property to property mappings.</returns>
        public Dictionary<ItemPropertyUnpacked, ItemProperty> GetAllPackedProperties()
        {
            return new Dictionary<ItemPropertyUnpacked, ItemProperty>(_packedProperties);
        }

        /// <summary>
        /// Resets all mock data to default values for testing.
        /// </summary>
        public void Reset()
        {
            _unpackedProperties.Clear();
            _packedProperties.Clear();
            _activeProperties.Clear();
            _nextPropertyId = 1;
        }

        /// <summary>
        /// Gets all item property data for testing verification.
        /// </summary>
        /// <returns>An ItemPropertyData object containing all settings.</returns>
        public ItemPropertyData GetItemPropertyDataForTesting()
        {
            return new ItemPropertyData
            {
                UnpackedProperties = new Dictionary<ItemProperty, ItemPropertyUnpacked>(_unpackedProperties),
                PackedProperties = new Dictionary<ItemPropertyUnpacked, ItemProperty>(_packedProperties),
                ActiveProperties = new Dictionary<uint, Dictionary<int, ItemPropertyUnpacked>>(_activeProperties),
                NextPropertyId = _nextPropertyId
            };
        }

        // Helper classes
        public class ItemPropertyData
        {
            public Dictionary<ItemProperty, ItemPropertyUnpacked> UnpackedProperties { get; set; } = new();
            public Dictionary<ItemPropertyUnpacked, ItemProperty> PackedProperties { get; set; } = new();
            public Dictionary<uint, Dictionary<int, ItemPropertyUnpacked>> ActiveProperties { get; set; } = new();
            public int NextPropertyId { get; set; }
        }
    }
}
