using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.NWN.API.Service;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Domain.World.Contracts;

namespace SWLOR.Component.World.Service
{
    /// <summary>
    /// Service responsible for core visibility management operations.
    /// </summary>
    public class ObjectVisibilityService : IObjectVisibilityService
    {
        private readonly ILogger _logger;
        private readonly IVisibilityObjectCacheService _visibilityObjectCache;
        private readonly IPlayerVisibilityService _playerVisibilityService;

        public ObjectVisibilityService(
            ILogger logger, 
            IVisibilityObjectCacheService visibilityObjectCache,
            IPlayerVisibilityService playerVisibilityService)
        {
            _logger = logger;
            _visibilityObjectCache = visibilityObjectCache;
            _playerVisibilityService = playerVisibilityService;
        }

        /// <summary>
        /// Modifies the visibility of an object for a specific player.
        /// </summary>
        /// <param name="player">The player to adjust.</param>
        /// <param name="target">The target object to adjust.</param>
        /// <param name="type">The new type of visibility to use.</param>
        public void AdjustVisibility(uint player, uint target, VisibilityType type)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;
            if (GetIsPC(target)) return;

            var visibilityObjectId = GetLocalString(target, "VISIBILITY_OBJECT_ID");
            if (string.IsNullOrWhiteSpace(visibilityObjectId))
            {
                _logger.Write<ErrorLogGroup>($"{GetName(target)} is missing the local variable VISIBILITY_OBJECT_ID. The visibility of this object cannot be modified for player {GetName(player)}");
                return;
            }

            var playerId = GetObjectUUID(player);
            _playerVisibilityService.SetPlayerObjectVisibility(playerId, visibilityObjectId, type);

            VisibilityPlugin.SetVisibilityOverride(player, target, type);
        }

        /// <summary>
        /// Adjusts the visibility of a given object for a given player.
        /// </summary>
        /// <param name="player">The player to adjust.</param>
        /// <param name="visibilityObjectId">The visibility object Id of the object to adjust.</param>
        /// <param name="type">The new visibility type to adjust to.</param>
        public void AdjustVisibilityByObjectId(uint player, string visibilityObjectId, VisibilityType type)
        {
            if (!_visibilityObjectCache.HasVisibilityObject(visibilityObjectId))
            {
                _logger.Write<ErrorLogGroup>($"No object matching visibility object Id '{visibilityObjectId}' can be found. This is likely due to an object with an Id being created after module load.");
                return;
            }

            var obj = _visibilityObjectCache.GetVisibilityObject(visibilityObjectId);
            if (obj.HasValue)
            {
                AdjustVisibility(player, obj.Value, type);
            }
        }

    }
}
