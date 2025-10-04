using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.Test.Shared.NWScriptMocks.NWNXPluginMocks
{
    /// <summary>
    /// Mock implementation of the VisibilityPlugin for testing purposes.
    /// Provides visibility management functionality for controlling object visibility
    /// for specific players or globally.
    /// </summary>
    public class VisibilityPluginMock: IVisibilityPluginService
    {
        // Mock data storage
        private readonly Dictionary<string, VisibilityType> _playerOverrides = new();
        private readonly Dictionary<string, VisibilityType> _globalOverrides = new();
        private readonly Dictionary<uint, List<uint>> _visibleObjects = new();
        private readonly Dictionary<uint, List<uint>> _hiddenObjects = new();

        /// <summary>
        /// Queries the existing visibility override for given (player, target) pair.
        /// If player is OBJECT_INVALID, the global visibility override will be returned.
        /// </summary>
        /// <param name="player">The PC Object or OBJECT_INVALID.</param>
        /// <param name="target">The object for which we're querying the visibility override.</param>
        /// <returns>The VisibilityType.</returns>
        public VisibilityType GetVisibilityOverride(uint player, uint target)
        {
            if (player == OBJECT_INVALID)
            {
                var globalKey = $"global:{target}";
                return _globalOverrides.TryGetValue(globalKey, out var globalOverride) ? globalOverride : VisibilityType.Default;
            }
            else
            {
                var playerKey = $"{player}:{target}";
                return _playerOverrides.TryGetValue(playerKey, out var playerOverride) ? playerOverride : VisibilityType.Default;
            }
        }

        /// <summary>
        /// Overrides the default visibility rules about how player perceives the target object.
        /// If player is OBJECT_INVALID, the global visibility override will be set.
        /// </summary>
        /// <param name="player">The PC Object or OBJECT_INVALID.</param>
        /// <param name="target">The object for which we're altering the visibility.</param>
        /// <param name="override">The visibility type.</param>
        public void SetVisibilityOverride(uint player, uint target, VisibilityType @override)
        {
            if (player == OBJECT_INVALID)
            {
                var globalKey = $"global:{target}";
                if (@override == VisibilityType.Default)
                {
                    _globalOverrides.Remove(globalKey);
                }
                else
                {
                    _globalOverrides[globalKey] = @override;
                }
            }
            else
            {
                var playerKey = $"{player}:{target}";
                if (@override == VisibilityType.Default)
                {
                    _playerOverrides.Remove(playerKey);
                }
                else
                {
                    _playerOverrides[playerKey] = @override;
                }
            }

            // Update visible/hidden object lists for testing
            UpdateObjectLists(player, target, @override);
        }

        /// <summary>
        /// Gets all visibility overrides for a specific player.
        /// This is a helper method for testing purposes.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>A dictionary of target objects to their visibility types.</returns>
        public Dictionary<uint, VisibilityType> GetPlayerOverrides(uint player)
        {
            var result = new Dictionary<uint, VisibilityType>();
            var prefix = $"{player}:";
            
            foreach (var kvp in _playerOverrides)
            {
                if (kvp.Key.StartsWith(prefix))
                {
                    var targetStr = kvp.Key.Substring(prefix.Length);
                    if (uint.TryParse(targetStr, out var target))
                    {
                        result[target] = kvp.Value;
                    }
                }
            }
            
            return result;
        }

        /// <summary>
        /// Gets all global visibility overrides.
        /// This is a helper method for testing purposes.
        /// </summary>
        /// <returns>A dictionary of target objects to their visibility types.</returns>
        public Dictionary<uint, VisibilityType> GetGlobalOverrides()
        {
            var result = new Dictionary<uint, VisibilityType>();
            
            foreach (var kvp in _globalOverrides)
            {
                if (kvp.Key.StartsWith("global:") && uint.TryParse(kvp.Key.Substring(7), out var target))
                {
                    result[target] = kvp.Value;
                }
            }
            
            return result;
        }

        /// <summary>
        /// Gets all objects that are visible to a specific player.
        /// This is a helper method for testing purposes.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>A list of visible object IDs.</returns>
        public List<uint> GetVisibleObjects(uint player)
        {
            return _visibleObjects.TryGetValue(player, out var objects) ? new List<uint>(objects) : new List<uint>();
        }

        /// <summary>
        /// Gets all objects that are hidden from a specific player.
        /// This is a helper method for testing purposes.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>A list of hidden object IDs.</returns>
        public List<uint> GetHiddenObjects(uint player)
        {
            return _hiddenObjects.TryGetValue(player, out var objects) ? new List<uint>(objects) : new List<uint>();
        }

        /// <summary>
        /// Gets the count of visibility overrides for a specific player.
        /// This is a helper method for testing purposes.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The number of overrides.</returns>
        public int GetOverrideCount(uint player)
        {
            return GetPlayerOverrides(player).Count;
        }

        /// <summary>
        /// Gets the count of global visibility overrides.
        /// This is a helper method for testing purposes.
        /// </summary>
        /// <returns>The number of global overrides.</returns>
        public int GetGlobalOverrideCount()
        {
            return GetGlobalOverrides().Count;
        }

        /// <summary>
        /// Clears all visibility overrides for a specific player.
        /// This is a helper method for testing purposes.
        /// </summary>
        /// <param name="player">The player object.</param>
        public void ClearPlayerOverrides(uint player)
        {
            var prefix = $"{player}:";
            var keysToRemove = _playerOverrides.Keys.Where(k => k.StartsWith(prefix)).ToList();
            
            foreach (var key in keysToRemove)
            {
                _playerOverrides.Remove(key);
            }
            
            _visibleObjects.Remove(player);
            _hiddenObjects.Remove(player);
        }

        /// <summary>
        /// Clears all global visibility overrides.
        /// This is a helper method for testing purposes.
        /// </summary>
        public void ClearGlobalOverrides()
        {
            _globalOverrides.Clear();
        }

        /// <summary>
        /// Checks if an object is visible to a player.
        /// This is a helper method for testing purposes.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="target">The target object.</param>
        /// <returns>True if the object is visible, false otherwise.</returns>
        public bool IsObjectVisible(uint player, uint target)
        {
            var @override = GetVisibilityOverride(player, target);
            return @override == VisibilityType.Visible || @override == VisibilityType.Default;
        }

        /// <summary>
        /// Checks if an object is hidden from a player.
        /// This is a helper method for testing purposes.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="target">The target object.</param>
        /// <returns>True if the object is hidden, false otherwise.</returns>
        public bool IsObjectHidden(uint player, uint target)
        {
            var @override = GetVisibilityOverride(player, target);
            return @override == VisibilityType.Hidden;
        }

        /// <summary>
        /// Resets all mock data to default values for testing.
        /// </summary>
        public void Reset()
        {
            _playerOverrides.Clear();
            _globalOverrides.Clear();
            _visibleObjects.Clear();
            _hiddenObjects.Clear();
        }

        /// <summary>
        /// Gets all visibility data for testing verification.
        /// </summary>
        /// <returns>A VisibilityData object containing all settings.</returns>
        public VisibilityData GetVisibilityDataForTesting()
        {
            return new VisibilityData
            {
                PlayerOverrides = new Dictionary<string, VisibilityType>(_playerOverrides),
                GlobalOverrides = new Dictionary<string, VisibilityType>(_globalOverrides),
                VisibleObjects = new Dictionary<uint, List<uint>>(_visibleObjects),
                HiddenObjects = new Dictionary<uint, List<uint>>(_hiddenObjects)
            };
        }

        // Helper methods
        private void UpdateObjectLists(uint player, uint target, VisibilityType @override)
        {
            if (player == OBJECT_INVALID) return;

            // Remove from both lists first
            if (_visibleObjects.ContainsKey(player))
            {
                _visibleObjects[player].Remove(target);
            }
            if (_hiddenObjects.ContainsKey(player))
            {
                _hiddenObjects[player].Remove(target);
            }

            // Add to appropriate list
            switch (@override)
            {
                case VisibilityType.Visible:
                    if (!_visibleObjects.ContainsKey(player))
                    {
                        _visibleObjects[player] = new List<uint>();
                    }
                    _visibleObjects[player].Add(target);
                    break;
                case VisibilityType.Hidden:
                    if (!_hiddenObjects.ContainsKey(player))
                    {
                        _hiddenObjects[player] = new List<uint>();
                    }
                    _hiddenObjects[player].Add(target);
                    break;
            }
        }

        // Constants
        private const uint OBJECT_INVALID = 0x7F000000;

        // Helper classes
        public class VisibilityData
        {
            public Dictionary<string, VisibilityType> PlayerOverrides { get; set; } = new();
            public Dictionary<string, VisibilityType> GlobalOverrides { get; set; } = new();
            public Dictionary<uint, List<uint>> VisibleObjects { get; set; } = new();
            public Dictionary<uint, List<uint>> HiddenObjects { get; set; } = new();
        }
    }
}
