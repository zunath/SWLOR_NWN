using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.World.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.World.Service
{
    /// <summary>
    /// Service responsible for managing player-specific visibility state.
    /// </summary>
    public class PlayerVisibilityService : IPlayerVisibilityService
    {
        private readonly ILogger _logger;
        private readonly IDatabaseService _db;
        private readonly IVisibilityObjectCacheService _visibilityObjectCache;

        public PlayerVisibilityService(
            ILogger logger, 
            IDatabaseService db, 
            IVisibilityObjectCacheService visibilityObjectCache)
        {
            _logger = logger;
            _db = db;
            _visibilityObjectCache = visibilityObjectCache;
        }

        /// <summary>
        /// Loads visibility objects for a player when they enter the server.
        /// </summary>
        [ScriptHandler<OnModuleEnter>]
        public void LoadPlayerVisibilityObjects()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            // Toggle visibility of all hidden objects 
            foreach (var hiddenObject in _visibilityObjectCache.GetDefaultHiddenObjects())
            {
                VisibilityPlugin.SetVisibilityOverride(player, hiddenObject, VisibilityType.Hidden);
            }

            // Now iterate over the player's objects and adjust visibility.
            var playerId = GetObjectUUID(player);
            var visibilities = (_db.Get<Player>(playerId) ?? new Player(playerId));
            
            for (var index = visibilities.ObjectVisibilities.Count - 1; index >= 0; index--)
            {
                var (objectId, visibilityType) = visibilities.ObjectVisibilities.ElementAt(index);
                if (!_visibilityObjectCache.HasVisibilityObject(objectId))
                {
                    // This object is no longer tracked. Remove it from the player's data.
                    visibilities.ObjectVisibilities.Remove(objectId);
                    continue;
                }

                var obj = _visibilityObjectCache.GetVisibilityObject(objectId);
                if (obj.HasValue)
                {
                    VisibilityPlugin.SetVisibilityOverride(player, obj.Value, visibilityType);
                }
            }
        }

        /// <summary>
        /// Gets the visibility type for a specific object for a player.
        /// </summary>
        /// <param name="playerId">The player's UUID</param>
        /// <param name="visibilityObjectId">The visibility object ID</param>
        /// <returns>The visibility type, or null if not set</returns>
        public VisibilityType? GetPlayerObjectVisibility(string playerId, string visibilityObjectId)
        {
            var player = _db.Get<Player>(playerId);
            if (player?.ObjectVisibilities == null)
                return null;

            return player.ObjectVisibilities.TryGetValue(visibilityObjectId, out var visibilityType) ? visibilityType : null;
        }

        /// <summary>
        /// Sets the visibility type for a specific object for a player.
        /// </summary>
        /// <param name="playerId">The player's UUID</param>
        /// <param name="visibilityObjectId">The visibility object ID</param>
        /// <param name="visibilityType">The visibility type to set</param>
        public void SetPlayerObjectVisibility(string playerId, string visibilityObjectId, VisibilityType visibilityType)
        {
            var player = _db.Get<Player>(playerId) ?? new Player(playerId);
            player.ObjectVisibilities[visibilityObjectId] = visibilityType;
            _db.Set(player);
        }

        /// <summary>
        /// Removes visibility settings for a specific object for a player.
        /// </summary>
        /// <param name="playerId">The player's UUID</param>
        /// <param name="visibilityObjectId">The visibility object ID</param>
        public void RemovePlayerObjectVisibility(string playerId, string visibilityObjectId)
        {
            var player = _db.Get<Player>(playerId);
            if (player?.ObjectVisibilities != null)
            {
                player.ObjectVisibilities.Remove(visibilityObjectId);
                _db.Set(player);
            }
        }
    }
}
