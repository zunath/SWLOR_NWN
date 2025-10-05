using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScriptMocks.NWNXPluginMocks
{
    /// <summary>
    /// Mock implementation of the FeatPlugin for testing purposes.
    /// Provides feat modification functionality for customizing feat behavior and properties.
    /// </summary>
    public class FeatPluginMock: IFeatPluginService
    {
        // Mock data storage
        private readonly Dictionary<FeatType, Dictionary<FeatModifierType, FeatModifierData>> _featModifiers = new();

        /// <summary>
        /// Sets a feat modifier with custom parameters to modify feat behavior.
        /// </summary>
        /// <param name="featType">The feat type to modify. Must be a valid FeatType enum value or feat.2da index.</param>
        /// <param name="modifierType">The type of modifier to apply. See FeatModifierType enum for available modifiers.</param>
        /// <param name="param1">The first parameter for this feat modifier. Defaults to 0xDEADBEEF if not specified.</param>
        /// <param name="param2">The second parameter for this feat modifier. Defaults to 0xDEADBEEF if not specified.</param>
        /// <param name="param3">The third parameter for this feat modifier. Defaults to 0xDEADBEEF if not specified.</param>
        /// <param name="param4">The fourth parameter for this feat modifier. Defaults to 0xDEADBEEF if not specified.</param>
        public void SetFeatModifier(
            FeatType featType, 
            FeatModifierType modifierType, 
            uint param1 = 0xDEADBEEF, 
            uint param2 = 0xDEADBEEF,
            uint param3 = 0xDEADBEEF, 
            uint param4 = 0xDEADBEEF)
        {
            if (!_featModifiers.ContainsKey(featType))
            {
                _featModifiers[featType] = new Dictionary<FeatModifierType, FeatModifierData>();
            }

            _featModifiers[featType][modifierType] = new FeatModifierData
            {
                FeatType = featType,
                ModifierType = modifierType,
                Param1 = param1,
                Param2 = param2,
                Param3 = param3,
                Param4 = param4
            };
        }

        /// <summary>
        /// Gets a feat modifier for the specified feat and modifier type.
        /// </summary>
        /// <param name="featType">The feat type to query.</param>
        /// <param name="modifierType">The modifier type to query.</param>
        /// <returns>The feat modifier data, or null if not found.</returns>
        public FeatModifierData? GetFeatModifier(FeatType featType, FeatModifierType modifierType)
        {
            if (_featModifiers.TryGetValue(featType, out var modifiers) &&
                modifiers.TryGetValue(modifierType, out var modifier))
            {
                return modifier;
            }
            return null;
        }

        /// <summary>
        /// Removes a feat modifier for the specified feat and modifier type.
        /// </summary>
        /// <param name="featType">The feat type to modify.</param>
        /// <param name="modifierType">The modifier type to remove.</param>
        public void RemoveFeatModifier(FeatType featType, FeatModifierType modifierType)
        {
            if (_featModifiers.TryGetValue(featType, out var modifiers))
            {
                modifiers.Remove(modifierType);
                if (modifiers.Count == 0)
                {
                    _featModifiers.Remove(featType);
                }
            }
        }

        /// <summary>
        /// Gets all feat modifiers for a specific feat.
        /// </summary>
        /// <param name="featType">The feat type to query.</param>
        /// <returns>A dictionary of modifier types to their data.</returns>
        public Dictionary<FeatModifierType, FeatModifierData> GetFeatModifiers(FeatType featType)
        {
            return _featModifiers.TryGetValue(featType, out var modifiers) 
                ? new Dictionary<FeatModifierType, FeatModifierData>(modifiers) 
                : new Dictionary<FeatModifierType, FeatModifierData>();
        }

        /// <summary>
        /// Gets all feat modifiers across all feats.
        /// </summary>
        /// <returns>A dictionary of feat types to their modifier data.</returns>
        public Dictionary<FeatType, Dictionary<FeatModifierType, FeatModifierData>> GetAllFeatModifiers()
        {
            return new Dictionary<FeatType, Dictionary<FeatModifierType, FeatModifierData>>(_featModifiers);
        }

        /// <summary>
        /// Checks if a feat has a specific modifier type.
        /// </summary>
        /// <param name="featType">The feat type to check.</param>
        /// <param name="modifierType">The modifier type to check.</param>
        /// <returns>True if the feat has the modifier, false otherwise.</returns>
        public bool HasFeatModifier(FeatType featType, FeatModifierType modifierType)
        {
            return _featModifiers.TryGetValue(featType, out var modifiers) &&
                   modifiers.ContainsKey(modifierType);
        }

        /// <summary>
        /// Gets the count of modifiers for a specific feat.
        /// </summary>
        /// <param name="featType">The feat type to query.</param>
        /// <returns>The number of modifiers for the feat.</returns>
        public int GetFeatModifierCount(FeatType featType)
        {
            return _featModifiers.TryGetValue(featType, out var modifiers) ? modifiers.Count : 0;
        }

        /// <summary>
        /// Gets the count of all feat modifiers across all feats.
        /// </summary>
        /// <returns>The total number of feat modifiers.</returns>
        public int GetTotalFeatModifierCount()
        {
            return _featModifiers.Values.Sum(modifiers => modifiers.Count);
        }

        /// <summary>
        /// Gets the count of feats that have modifiers.
        /// </summary>
        /// <returns>The number of feats with modifiers.</returns>
        public int GetFeatCount()
        {
            return _featModifiers.Count;
        }

        /// <summary>
        /// Clears all feat modifiers for a specific feat.
        /// </summary>
        /// <param name="featType">The feat type to clear.</param>
        public void ClearFeatModifiers(FeatType featType)
        {
            _featModifiers.Remove(featType);
        }

        /// <summary>
        /// Clears all feat modifiers across all feats.
        /// </summary>
        public void ClearAllFeatModifiers()
        {
            _featModifiers.Clear();
        }

        // Helper methods for testing
        /// <summary>
        /// Resets all mock data to default values for testing.
        /// </summary>
        public void Reset()
        {
            _featModifiers.Clear();
        }

        /// <summary>
        /// Gets the feat modifiers for testing verification.
        /// </summary>
        /// <returns>A dictionary of all feat modifiers.</returns>
        public Dictionary<FeatType, Dictionary<FeatModifierType, FeatModifierData>> GetFeatModifiersForTesting()
        {
            return new Dictionary<FeatType, Dictionary<FeatModifierType, FeatModifierData>>(_featModifiers);
        }

        // Helper classes
        public class FeatModifierData
        {
            public FeatType FeatType { get; set; }
            public FeatModifierType ModifierType { get; set; }
            public uint Param1 { get; set; }
            public uint Param2 { get; set; }
            public uint Param3 { get; set; }
            public uint Param4 { get; set; }
        }
    }
}
