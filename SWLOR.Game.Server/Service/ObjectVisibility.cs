using System.Collections.Generic;
using System.Linq;

using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Event;
using SWLOR.Shared.Core.Log;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Service
{
    public static class ObjectVisibility
    {
        private static ILogger _logger = ServiceContainer.GetService<ILogger>();
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        private static readonly Dictionary<string, uint> _visibilityObjects = new Dictionary<string, uint>();
        private static readonly List<uint> _defaultHiddenObjects = new List<uint>();

        /// <summary>
        /// When the module loads, cycle through every area and every object to identify the visibility objects.
        /// </summary>
        [ScriptHandler(ScriptName.OnModuleCacheBefore)]
        public static void LoadVisibilityObjects()
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
        [ScriptHandler(ScriptName.OnModuleEnter)]
        public static void LoadPlayerVisibilityObjects()
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
        public static void AdjustVisibility(uint player, uint target, VisibilityType type)
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
        public static void AdjustVisibilityByObjectId(uint player, string visibilityObjectId, VisibilityType type)
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
