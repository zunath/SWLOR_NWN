using SWLOR.Component.World.Contracts;
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
    public class ObjectVisibilityService : IObjectVisibilityService
    {
        private readonly ILogger _logger;
        private readonly IDatabaseService _db;
        private readonly Dictionary<string, uint> _visibilityObjects = new();
        private readonly List<uint> _defaultHiddenObjects = new();

        public ObjectVisibilityService(ILogger logger, IDatabaseService db)
        {
            _logger = logger;
            _db = db;
        }

        /// <summary>
        /// When the module loads, cycle through every area and every object to identify the visibility objects.
        /// </summary>
        [ScriptHandler<OnModuleCacheBefore>]
        public void LoadVisibilityObjects()
        {
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
        }

        /// <summary>
        /// When a player enters the server, toggle visibility on all objects
        /// </summary>
        [ScriptHandler<OnModuleEnter>]
        public void LoadPlayerVisibilityObjects()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            // Toggle visibility of all hidden objects 
            foreach (var hiddenObject in _defaultHiddenObjects)
            {
                VisibilityPlugin.SetVisibilityOverride(player, hiddenObject, VisibilityType.Hidden);
            }

            // Now iterate over the player's objects and adjust visibility.
            var playerId = GetObjectUUID(player);
            var visibilities = (_db.Get<Player>(playerId) ?? new Player(playerId));
            for(var index = visibilities.ObjectVisibilities.Count-1; index >= 0; index--)
            {
                var (objectId, visibilityType) = visibilities.ObjectVisibilities.ElementAt(index);
                if (!_visibilityObjects.ContainsKey(objectId))
                {
                    // This object is no longer tracked. Remove it from the player's data.
                    visibilities.ObjectVisibilities.Remove(objectId);
                    continue;
                }

                var obj = _visibilityObjects[objectId];
                VisibilityPlugin.SetVisibilityOverride(player, obj, visibilityType);
            }
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
            var dbVisibility = _db.Get<Player>(playerId) ?? new Player(playerId);
            dbVisibility.ObjectVisibilities[visibilityObjectId] = type;
            _db.Set(dbVisibility);

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
            if (!_visibilityObjects.ContainsKey(visibilityObjectId))
            {
                _logger.Write<ErrorLogGroup>($"No object matching visibility object Id '{visibilityObjectId}' can be found. This is likely due to an object with an Id being created after module load.");
                return;
            }

            var obj = _visibilityObjects[visibilityObjectId];
            AdjustVisibility(player, obj, type);
        }

    }
}
