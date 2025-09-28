using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Domain.World.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.World.Service
{
    /// <summary>
    /// Service responsible for caching and managing visibility objects.
    /// </summary>
    public class VisibilityObjectCacheService : IVisibilityObjectCacheService
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, uint> _visibilityObjects = new();
        private readonly List<uint> _defaultHiddenObjects = new();

        public VisibilityObjectCacheService(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Loads all visibility objects from areas and caches them.
        /// </summary>
        public void LoadVisibilityObjects()
        {
            _visibilityObjects.Clear();
            _defaultHiddenObjects.Clear();

            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
                {
                    var visibilityObjectId = GetLocalString(obj, "VISIBILITY_OBJECT_ID");
                    if (!string.IsNullOrWhiteSpace(visibilityObjectId))
                    {
                        _visibilityObjects[visibilityObjectId] = obj;

                        if (GetLocalBool(obj, "VISIBILITY_HIDDEN_DEFAULT"))
                        {
                            _defaultHiddenObjects.Add(obj);
                        }
                    }
                }
            }

            _logger.Write<ServerLogGroup>($"Loaded {_visibilityObjects.Count} visibility objects, {_defaultHiddenObjects.Count} default hidden");
        }

        /// <summary>
        /// Gets a visibility object by its ID.
        /// </summary>
        /// <param name="visibilityObjectId">The visibility object ID</param>
        /// <returns>The object ID if found, null otherwise</returns>
        public uint? GetVisibilityObject(string visibilityObjectId)
        {
            return _visibilityObjects.TryGetValue(visibilityObjectId, out var obj) ? obj : null;
        }

        /// <summary>
        /// Gets all default hidden objects.
        /// </summary>
        /// <returns>Collection of default hidden object IDs</returns>
        public IEnumerable<uint> GetDefaultHiddenObjects()
        {
            return _defaultHiddenObjects.AsReadOnly();
        }

        /// <summary>
        /// Checks if a visibility object ID exists in the cache.
        /// </summary>
        /// <param name="visibilityObjectId">The visibility object ID to check</param>
        /// <returns>True if the object exists, false otherwise</returns>
        public bool HasVisibilityObject(string visibilityObjectId)
        {
            return _visibilityObjects.ContainsKey(visibilityObjectId);
        }
    }
}
