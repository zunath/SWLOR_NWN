using SWLOR.Component.World.Contracts;
using SWLOR.Component.World.Entity;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entity;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Area;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.World.Service
{
    public class Area : IAreaService
    {
        private readonly IDatabaseService _db;
        private readonly Property _property;
        private Dictionary<string, uint> AreasByResref { get; } = new();
        private Dictionary<uint, List<uint>> PlayersByArea { get; } = new();

        public Area(IDatabaseService db, Property property)
        {
            _db = db;
            _property = property;
        }

        [ScriptHandler<OnModuleCacheBefore>]
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

        /// <summary>
        /// Remove instance templates from the area cache on module load.
        /// This ensures player locations are not updated in places they shouldn't be.
        /// </summary>
        [ScriptHandler<OnModuleCacheAfter>]
        public void RemoveInstancesFromCache()
        {
            var propertyLayouts = _property.GetAllLayoutsByPropertyType(PropertyType.Apartment);
            foreach (var type in propertyLayouts)
            {
                var layout = _property.GetLayoutByType(type);
                if (AreasByResref.ContainsKey(layout.AreaInstanceResref))
                    AreasByResref.Remove(layout.AreaInstanceResref);
            }
        }

        /// <summary>
        /// Retrieves an area by its resref. If the area does not exist, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="resref">The resref to use for the search.</param>
        /// <returns>The area ID or OBJECT_INVALID if area does not exist.</returns>
        public uint GetAreaByResref(string resref)
        {
            if (!AreasByResref.ContainsKey(resref))
                return OBJECT_INVALID;

            return AreasByResref[resref];
        }

        /// <summary>
        /// Retrieves list of all areas.
        /// </summary>
        /// <param> </param>
        /// <returns>AreasByResref cache.</returns>
        public Dictionary<string, uint> GetAreas()
        {
            return AreasByResref;
        }

        /// <summary>
        /// Retrieves all of the players currently in the specified area.
        /// If no players are in the area, an empty list will returned.
        /// </summary>
        /// <param name="area">The area to search by.</param>
        /// <returns>A list of player objects</returns>
        public List<uint> GetPlayersInArea(uint area)
        {
            if (!PlayersByArea.ContainsKey(area))
                return new List<uint>();

            return PlayersByArea[area].ToList();
        }

        /// <summary>
        /// When a player or DM enters an area, add them to the cache.
        /// </summary>
        [ScriptHandler<OnAreaEnter>]
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

        /// <summary>
        /// When a player or DM leaves an area, remove them from the cache.
        /// </summary>
        [ScriptHandler<OnAreaExit>]
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
