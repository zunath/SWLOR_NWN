using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The area cache: the logic and state that used to be static in <c>Service.Area</c>, moved here unchanged. The
    /// facade still carries the event handlers and forwards every call to the container's instance.
    /// </summary>
    public class AreaService : IAreaService
    {
        private readonly IDBService _db;

        private Dictionary<string, uint> AreasByResref { get; } = new();
        private Dictionary<uint, List<uint>> PlayersByArea { get; } = new();

        public AreaService(IDBService db)
        {
            _db = db;
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

        public void RemoveInstancesFromCache()
        {
            // Scheduled flights clone this template without a property layout definition.
            // Keep flight coordinates out of persistent player locations.
            AreasByResref.Remove(Shuttle.ShuttleInteriorResref);

            foreach (var areaResref in Property.GetAllInstanceAreaResrefs())
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

        public void ExitArea()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player))
                return;

            var area = OBJECT_SELF;
            if (!PlayersByArea.ContainsKey(area))
                PlayersByArea[area] = new List<uint>();

            if (PlayersByArea[area].Contains(player))
                PlayersByArea[area].Remove(player);
        }
    }
}
