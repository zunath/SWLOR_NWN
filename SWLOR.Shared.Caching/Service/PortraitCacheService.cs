using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Shared.Caching.Service
{
    /// <summary>
    /// This class is responsible for loading and retrieving portrait data which lives for the lifespan of the server.
    /// Nothing in here will be permanently stored, it's simply here to make queries quicker.
    /// </summary>
    public class PortraitCacheService : IPortraitCacheService
    {
        private Dictionary<int, int> PortraitIdsByInternalId { get; } = new();
        private Dictionary<int, int> PortraitInternalIdsByPortraitId { get; } = new();
        private Dictionary<int, string> PortraitResrefByInternalId { get; } = new();
        private Dictionary<string, int> PortraitInternalIdsByPortraitResref { get; } = new();

        /// <summary>
        /// Retrieves the number of portraits registered in the system.
        /// </summary>
        public int PortraitCount => PortraitIdsByInternalId.Count;

        [ScriptHandler<OnModuleCacheBefore>]
        public void CachePortraitsById()
        {
            const string Portraits2DA = "portraits";
            var twoDACount = Get2DARowCount(Portraits2DA);
            var internalId = 1;

            for (var row = 0; row < twoDACount; row++)
            {
                var baseResref = Get2DAString(Portraits2DA, "BaseResRef", row);
                var race = Get2DAString(Portraits2DA, "Race", row);

                if (!string.IsNullOrWhiteSpace(baseResref) &&
                    !string.IsNullOrWhiteSpace(race))
                {
                    PortraitIdsByInternalId[internalId] = row;
                    PortraitInternalIdsByPortraitId[row] = internalId;
                    PortraitResrefByInternalId[internalId] = "po_" + baseResref;
                    PortraitInternalIdsByPortraitResref["po_" + baseResref] = internalId;
                    internalId++;
                }
            }
        }

        /// <summary>
        /// Retrieves the portrait 2DA Id from the internal Id of the portrait.
        /// The value returned by this method can be used with NWScript.SetPortrait
        /// </summary>
        /// <param name="portraitInternalId">The internal portrait Id to retrieve.</param>
        /// <returns>The 2DA Id of the portrait.</returns>
        public int GetPortraitByInternalId(int portraitInternalId)
        {
            return PortraitIdsByInternalId[portraitInternalId];
        }

        /// <summary>
        /// Retrieves the internal Id of a portrait by its NWN 2DA Id.
        /// </summary>
        /// <param name="portraitId">The NWN portrait 2DA Id.</param>
        /// <returns>The internal Id of the portrait.</returns>
        public int GetPortraitInternalId(int portraitId)
        {
            return PortraitInternalIdsByPortraitId[portraitId];
        }

        /// <summary>
        /// Retrieves the resref of the portrait by the internal portrait Id.
        /// The size of the portrait needs to be appended to the end of this result.
        /// </summary>
        /// <param name="portraitInternalId">The internal portrait Id</param>
        /// <returns>The resref of the portrait, excluding the size.</returns>
        public string GetPortraitResrefByInternalId(int portraitInternalId)
        {
            return PortraitResrefByInternalId[portraitInternalId];
        }

        public int GetPortraitInternalIdByResref(string resref)
        {
            if (!PortraitInternalIdsByPortraitResref.ContainsKey(resref))
                return -1;

            return PortraitInternalIdsByPortraitResref[resref];
        }
    }
}
