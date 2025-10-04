using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScriptMocks.NWNXPluginMocks
{
    /// <summary>
    /// Mock implementation of the ItemPlugin for testing purposes.
    /// Provides comprehensive item management functionality including weight, value,
    /// appearance, armor class, and movement operations.
    /// </summary>
    public class ItemPluginMock: IItemPluginService
    {
        // Mock data storage
        private readonly Dictionary<uint, int> _itemWeights = new();
        private readonly Dictionary<uint, int> _baseGoldValues = new();
        private readonly Dictionary<uint, int> _addGoldValues = new();
        private readonly Dictionary<uint, BaseItemType> _baseItemTypes = new();
        private readonly Dictionary<uint, Dictionary<ItemModelColorType, Dictionary<int, int>>> _itemAppearances = new();
        private readonly Dictionary<uint, string> _entireItemAppearances = new();
        private readonly Dictionary<uint, int> _baseArmorClasses = new();
        private readonly Dictionary<uint, int> _minEquipLevels = new();
        private readonly Dictionary<uint, int> _minEquipLevelModifiers = new();
        private readonly Dictionary<uint, int> _minEquipLevelOverrides = new();
        private readonly Dictionary<uint, bool> _minEquipLevelModifierPersistence = new();
        private readonly Dictionary<uint, bool> _minEquipLevelOverridePersistence = new();
        private readonly Dictionary<uint, List<ItemMoveRecord>> _itemMoveHistory = new();

        /// <summary>
        /// Set an item's weight.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <param name="w">The weight, note this is in tenths of pounds.</param>
        /// <remarks>Will not persist through saving.</remarks>
        public void SetWeight(uint oItem, int w)
        {
            _itemWeights[oItem] = w;
        }

        /// <summary>
        /// Set an item's base value in gold pieces.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <param name="g">The base gold value.</param>
        /// <remarks>Total cost = base_value + additional_value. Will not persist through saving. This value will also revert if item is identified or player relogs into server. Equivalent to SetGoldPieceValue NWNX2 function.</remarks>
        public void SetBaseGoldPieceValue(uint oItem, int g)
        {
            _baseGoldValues[oItem] = g;
        }

        /// <summary>
        /// Set an item's additional value in gold pieces.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <param name="g">The additional gold value.</param>
        /// <remarks>Total cost = base_value + additional_value. Will persist through saving.</remarks>
        public void SetAddGoldPieceValue(uint oItem, int g)
        {
            _addGoldValues[oItem] = g;
        }

        /// <summary>
        /// Get an item's base value in gold pieces.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <returns>The base gold piece value for the item.</returns>
        public int GetBaseGoldPieceValue(uint oItem)
        {
            return _baseGoldValues.TryGetValue(oItem, out var value) ? value : 0;
        }

        /// <summary>
        /// Get an item's additional value in gold pieces.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <returns>The additional gold piece value for the item.</returns>
        public int GetAddGoldPieceValue(uint oItem)
        {
            return _addGoldValues.TryGetValue(oItem, out var value) ? value : 0;
        }

        /// <summary>
        /// Set an item's base item type.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <param name="baseitem">The new base item.</param>
        /// <remarks>This will not be visible until the item is refreshed (e.g. drop and take the item, or logging out and back in).</remarks>
        public void SetBaseItemType(uint oItem, BaseItemType baseitem)
        {
            _baseItemTypes[oItem] = baseitem;
        }

        /// <summary>
        /// Make a single change to the appearance of an item.
        /// </summary>
        /// <param name="oItem">The item</param>
        /// <param name="nType">The type</param>
        /// <param name="nIndex">The index</param>
        /// <param name="nValue">The value</param>
        /// <param name="updateCreatureAppearance">If true, also update the appearance of oItem's possessor. Only works for armor/helmets/cloaks. Will remove the item from the quickbar as side effect.</param>
        public void SetItemAppearance(uint oItem, ItemModelColorType nType, int nIndex, int nValue, bool updateCreatureAppearance = false)
        {
            if (!_itemAppearances.ContainsKey(oItem))
            {
                _itemAppearances[oItem] = new Dictionary<ItemModelColorType, Dictionary<int, int>>();
            }
            
            if (!_itemAppearances[oItem].ContainsKey(nType))
            {
                _itemAppearances[oItem][nType] = new Dictionary<int, int>();
            }
            
            _itemAppearances[oItem][nType][nIndex] = nValue;
        }

        /// <summary>
        /// Return a string containing the entire appearance for an item.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <returns>A string representing the item's appearance.</returns>
        public string GetEntireItemAppearance(uint oItem)
        {
            return _entireItemAppearances.TryGetValue(oItem, out var appearance) ? appearance : string.Empty;
        }

        /// <summary>
        /// Restores an item's appearance using the value retrieved through GetEntireItemAppearance().
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <param name="sApp">A string representing the item's appearance.</param>
        public void RestoreItemAppearance(uint oItem, string sApp)
        {
            _entireItemAppearances[oItem] = sApp;
        }

        /// <summary>
        /// Get an item's base armor class.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <returns>The base armor class.</returns>
        public int GetBaseArmorClass(uint oItem)
        {
            return _baseArmorClasses.TryGetValue(oItem, out var ac) ? ac : 0;
        }

        /// <summary>
        /// Get an item's minimum level required to equip.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <returns>The minimum level required to equip the item.</returns>
        public int GetMinEquipLevel(uint oItem)
        {
            return _minEquipLevels.TryGetValue(oItem, out var level) ? level : 1;
        }

        /// <summary>
        /// Move an item to a specific location.
        /// </summary>
        /// <param name="oItem">The item object to move.</param>
        /// <param name="oLocation">The destination location (area, container, or creature).</param>
        /// <param name="bHideAllFeedback">If true, hides all feedback messages generated by losing/acquiring items.</param>
        /// <returns>True if the item was successfully moved, false otherwise.</returns>
        public bool MoveTo(uint oItem, uint oLocation, bool bHideAllFeedback = false)
        {
            // Record the move for testing purposes
            if (!_itemMoveHistory.ContainsKey(oItem))
            {
                _itemMoveHistory[oItem] = new List<ItemMoveRecord>();
            }
            
            _itemMoveHistory[oItem].Add(new ItemMoveRecord
            {
                Destination = oLocation,
                HideFeedback = bHideAllFeedback,
                Timestamp = DateTime.UtcNow
            });
            
            // Mock successful move
            return true;
        }

        /// <summary>
        /// Set an item's minimum equip level modifier.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <param name="nModifier">The modifier value to add to the minimum equip level.</param>
        /// <param name="bPersist">Whether the modifier should persist to gff field. Strongly recommended to be true.</param>
        public void SetMinEquipLevelModifier(uint oItem, int nModifier, bool bPersist = true)
        {
            _minEquipLevelModifiers[oItem] = nModifier;
            _minEquipLevelModifierPersistence[oItem] = bPersist;
        }

        /// <summary>
        /// Get an item's minimum equip level modifier.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <returns>The current modifier value for the minimum equip level.</returns>
        public int GetMinEquipLevelModifier(uint oItem)
        {
            return _minEquipLevelModifiers.TryGetValue(oItem, out var modifier) ? modifier : 0;
        }

        /// <summary>
        /// Set an item's minimum equip level override.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <param name="nOverride">The override value for the minimum equip level.</param>
        /// <param name="bPersist">Whether the override should persist to gff field. Strongly recommended to be true.</param>
        public void SetMinEquipLevelOverride(uint oItem, int nOverride, bool bPersist = true)
        {
            _minEquipLevelOverrides[oItem] = nOverride;
            _minEquipLevelOverridePersistence[oItem] = bPersist;
        }

        /// <summary>
        /// Get an item's minimum equip level override.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <returns>The current override value for the minimum equip level, or -1 if no override is set.</returns>
        public int GetMinEquipLevelOverride(uint oItem)
        {
            return _minEquipLevelOverrides.TryGetValue(oItem, out var @override) ? @override : -1;
        }

        // Helper methods for testing
        /// <summary>
        /// Resets all mock data to default values for testing.
        /// </summary>
        public void Reset()
        {
            _itemWeights.Clear();
            _baseGoldValues.Clear();
            _addGoldValues.Clear();
            _baseItemTypes.Clear();
            _itemAppearances.Clear();
            _entireItemAppearances.Clear();
            _baseArmorClasses.Clear();
            _minEquipLevels.Clear();
            _minEquipLevelModifiers.Clear();
            _minEquipLevelOverrides.Clear();
            _minEquipLevelModifierPersistence.Clear();
            _minEquipLevelOverridePersistence.Clear();
            _itemMoveHistory.Clear();
        }

        /// <summary>
        /// Gets all item data for testing verification.
        /// </summary>
        /// <returns>An ItemData object containing all settings.</returns>
        public ItemData GetItemDataForTesting()
        {
            return new ItemData
            {
                ItemWeights = new Dictionary<uint, int>(_itemWeights),
                BaseGoldValues = new Dictionary<uint, int>(_baseGoldValues),
                AddGoldValues = new Dictionary<uint, int>(_addGoldValues),
                BaseItemTypes = new Dictionary<uint, BaseItemType>(_baseItemTypes),
                ItemAppearances = new Dictionary<uint, Dictionary<ItemModelColorType, Dictionary<int, int>>>(_itemAppearances),
                EntireItemAppearances = new Dictionary<uint, string>(_entireItemAppearances),
                BaseArmorClasses = new Dictionary<uint, int>(_baseArmorClasses),
                MinEquipLevels = new Dictionary<uint, int>(_minEquipLevels),
                MinEquipLevelModifiers = new Dictionary<uint, int>(_minEquipLevelModifiers),
                MinEquipLevelOverrides = new Dictionary<uint, int>(_minEquipLevelOverrides),
                MinEquipLevelModifierPersistence = new Dictionary<uint, bool>(_minEquipLevelModifierPersistence),
                MinEquipLevelOverridePersistence = new Dictionary<uint, bool>(_minEquipLevelOverridePersistence),
                ItemMoveHistory = new Dictionary<uint, List<ItemMoveRecord>>(_itemMoveHistory)
            };
        }

        /// <summary>
        /// Gets the move history for a specific item.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <returns>A list of move records for the item.</returns>
        public List<ItemMoveRecord> GetItemMoveHistory(uint oItem)
        {
            return _itemMoveHistory.TryGetValue(oItem, out var history) ? new List<ItemMoveRecord>(history) : new List<ItemMoveRecord>();
        }

        /// <summary>
        /// Gets the total gold value of an item (base + additional).
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <returns>The total gold value.</returns>
        public int GetTotalGoldValue(uint oItem)
        {
            return GetBaseGoldPieceValue(oItem) + GetAddGoldPieceValue(oItem);
        }

        /// <summary>
        /// Gets the effective minimum equip level (base + modifier, or override if set).
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <returns>The effective minimum equip level.</returns>
        public int GetEffectiveMinEquipLevel(uint oItem)
        {
            var @override = GetMinEquipLevelOverride(oItem);
            if (@override != -1)
            {
                return @override;
            }
            
            return GetMinEquipLevel(oItem) + GetMinEquipLevelModifier(oItem);
        }

        // Helper classes
        public class ItemMoveRecord
        {
            public uint Destination { get; set; }
            public bool HideFeedback { get; set; }
            public DateTime Timestamp { get; set; }
        }

        public class ItemData
        {
            public Dictionary<uint, int> ItemWeights { get; set; } = new();
            public Dictionary<uint, int> BaseGoldValues { get; set; } = new();
            public Dictionary<uint, int> AddGoldValues { get; set; } = new();
            public Dictionary<uint, BaseItemType> BaseItemTypes { get; set; } = new();
            public Dictionary<uint, Dictionary<ItemModelColorType, Dictionary<int, int>>> ItemAppearances { get; set; } = new();
            public Dictionary<uint, string> EntireItemAppearances { get; set; } = new();
            public Dictionary<uint, int> BaseArmorClasses { get; set; } = new();
            public Dictionary<uint, int> MinEquipLevels { get; set; } = new();
            public Dictionary<uint, int> MinEquipLevelModifiers { get; set; } = new();
            public Dictionary<uint, int> MinEquipLevelOverrides { get; set; } = new();
            public Dictionary<uint, bool> MinEquipLevelModifierPersistence { get; set; } = new();
            public Dictionary<uint, bool> MinEquipLevelOverridePersistence { get; set; } = new();
            public Dictionary<uint, List<ItemMoveRecord>> ItemMoveHistory { get; set; } = new();
        }
    }
}
