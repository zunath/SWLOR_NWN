using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.PropertyService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public class Area
    {
        private static Dictionary<string, uint> AreasByResref { get; } = new();

        public static void CacheData()
        {
            CacheAreasByResref();

            Console.WriteLine($"Loaded {AreasByResref.Count} areas by resref.");
        }

        /// <summary>
        /// Caches all areas by their resref.
        /// </summary>
        private static void CacheAreasByResref()
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
        [NWNEventHandler("mod_load")]
        public static void RemoveInstancesFromCache()
        {
            var propertyLayouts = Property.GetAllLayoutsByPropertyType(PropertyType.Apartment);
            foreach (var type in propertyLayouts)
            {
                var layout = Property.GetLayoutByType(type);
                if (AreasByResref.ContainsKey(layout.AreaInstanceResref))
                    AreasByResref.Remove(layout.AreaInstanceResref);
            }
        }

        /// <summary>
        /// Retrieves an area by its resref. If the area does not exist, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="resref">The resref to use for the search.</param>
        /// <returns>The area ID or OBJECT_INVALID if area does not exist.</returns>
        public static uint GetAreaByResref(string resref)
        {
            if (!AreasByResref.ContainsKey(resref))
                return OBJECT_INVALID;

            return AreasByResref[resref];
        }

    }
}
