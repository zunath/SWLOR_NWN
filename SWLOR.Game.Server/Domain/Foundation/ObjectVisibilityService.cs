using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// Object visibility: the logic that used to be static in the <c>ObjectVisibility</c> facade, moved here unchanged.
    /// The event handlers run on the container's instance; static callers reach it through
    /// <c>GameServices.ObjectVisibility</c>.
    /// </summary>
    public class ObjectVisibilityService : IObjectVisibilityService
    {
        private readonly IDBService _db;
        private readonly Dictionary<string, uint> _visibilityObjects = new Dictionary<string, uint>();
        private readonly List<uint> _defaultHiddenObjects = new List<uint>();

        public ObjectVisibilityService(IDBService db)
        {
            _db = db;
        }

        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
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

        [NWNEventHandler(ScriptName.OnModuleEnter)]
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

        public void AdjustVisibility(uint player, uint target, VisibilityType type)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;
            if (GetIsPC(target)) return;

            var visibilityObjectId = GetLocalString(target, "VISIBILITY_OBJECT_ID");
            if (string.IsNullOrWhiteSpace(visibilityObjectId))
            {
                WriteLog(LogGroup.Error, $"{GetName(target)} is missing the local variable VISIBILITY_OBJECT_ID. The visibility of this object cannot be modified for player {GetName(player)}", true);
                return;
            }

            var playerId = GetObjectUUID(player);
            var dbVisibility = _db.Get<Player>(playerId) ?? new Player(playerId);
            dbVisibility.ObjectVisibilities[visibilityObjectId] = type;
            _db.Set(dbVisibility);

            VisibilityPlugin.SetVisibilityOverride(player, target, type);
        }

        public void AdjustVisibilityByObjectId(uint player, string visibilityObjectId, VisibilityType type)
        {
            if (!_visibilityObjects.ContainsKey(visibilityObjectId))
            {
                WriteLog(LogGroup.Error, $"No object matching visibility object Id '{visibilityObjectId}' can be found. This is likely due to an object with an Id being created after module load.");
                return;
            }

            var obj = _visibilityObjects[visibilityObjectId];
            AdjustVisibility(player, obj, type);
        }

        /// <summary>
        /// Writes to the server log. One line so a test can see what was logged; the body is the original
        /// <c>Log.Write</c> call (<c>Log</c> stays static in Phase 2).
        /// </summary>
        protected virtual void WriteLog(LogGroup group, string details, bool printToConsole = false)
        {
            Log.Write(group, details, printToConsole);
        }
    }
}
