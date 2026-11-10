using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The area cache: the logic and state that used to be a static class, moved here unchanged. The event handlers
    /// run on the container's instance; static callers reach it through <c>GameServices.Area</c>.
    /// </summary>
    public class AreaService : IAreaService
    {
        private readonly IDBService _db;
        private readonly Lazy<IPropertyService> _property;

        private Dictionary<string, uint> AreasByResref { get; } = new();
        private Dictionary<uint, List<uint>> PlayersByArea { get; } = new();

        public AreaService(IDBService db, Lazy<IPropertyService> property)
        {
            _db = db;
            _property = property;
        }

        public uint CreateInstance(string sourceResref, string newTag = "", string newName = "")
        {
            var area = CreateArea(sourceResref, newTag, newName);
            if (GetIsObjectValid(area))
                RegisterEvents(area);

            return area;
        }

        public void RegisterEvents(uint area)
        {
            SetEventScript(area, EventScript.Area_OnEnter, ScriptName.OnAreaEnter);
            SetEventScript(area, EventScript.Area_OnExit, ScriptName.OnAreaExit);
            SetEventScript(area, EventScript.Area_OnHeartbeat, string.Empty); // Disabled for performance reasons
            SetEventScript(area, EventScript.Area_OnUserDefined, ScriptName.OnAreaUserDefined);
        }

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void CacheData()
        {
            CacheAreasByResref();

            Console.WriteLine($"Loaded {AreasByResref.Count} areas by resref.");
        }

        /// <summary>
        /// Caches all areas by their resref.
        /// </summary>
        private void CacheAreasByResref()
        {
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                var resref = GetResRef(area);
                AreasByResref[resref] = area;
            }
        }

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public void RemoveInstancesFromCache()
        {
            // Scheduled flights clone this template without a property layout definition.
            // Keep flight coordinates out of persistent player locations.
            AreasByResref.Remove(ShuttleService.ShuttleInteriorResref);

            foreach (var areaResref in _property.Value.GetAllInstanceAreaResrefs())
            {
                if (AreasByResref.ContainsKey(areaResref))
                    AreasByResref.Remove(areaResref);
            }
        }

        public uint GetAreaByResref(string resref)
        {
            if (!AreasByResref.ContainsKey(resref))
                return OBJECT_INVALID;

            return AreasByResref[resref];
        }

        public Dictionary<string, uint> GetAreas()
        {
            return AreasByResref;
        }

        public List<uint> GetPlayersInArea(uint area)
        {
            if (!PlayersByArea.ContainsKey(area))
                return new List<uint>();

            return PlayersByArea[area].ToList();
        }

        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public void EnterArea()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player))
                return;

            var area = OBJECT_SELF;
            if (!PlayersByArea.ContainsKey(area))
                PlayersByArea[area] = new List<uint>();

            if(!PlayersByArea[area].Contains(player))
                PlayersByArea[area].Add(player);

            // Handle DM created Area Notes
            var query = new DBQuery<AreaNote>()
                .AddFieldSearch(nameof(AreaNote.AreaResref), GetResRef(area), false)
                .OrderBy(nameof(AreaNote.AreaResref));
            var notes = _db.Search(query)
                .ToList();

            if (notes.Count > 0)
            {
                var prefix = GetName(area) + ": ";
                var message = string.Empty;
                foreach (var note in notes)
                {
                    message += note.PublicText;
                }

                if (!string.IsNullOrWhiteSpace(message.Trim()))
                {
                    SendMessageToPC(player, ColorToken.Purple(prefix + message));
                }
            }
        }

        [NWNEventHandler(ScriptName.OnAreaExit)]
        public void ExitArea()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player))
                return;

            var area = OBJECT_SELF;
            RemoveFromCache(player, area);
        }

        /// <summary>
        /// When a player or DM disconnects, remove them from the cache.
        /// </summary>
        [NWNEventHandler(ScriptName.OnClientDisconnectBefore)]
        public void DisconnectBefore()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player))
                return;

            var area = GetArea(player);
            RemoveFromCache(player, area);
        }

        private void RemoveFromCache(uint player, uint area)
        {
            if (!PlayersByArea.ContainsKey(area))
                PlayersByArea[area] = new List<uint>();

            if (PlayersByArea[area].Contains(player))
                PlayersByArea[area].Remove(player);
        }
    }
}
