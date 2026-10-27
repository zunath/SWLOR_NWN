using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API for finding online and offline characters for the DM player search.
    /// This is a staff surface, so it shows canonical names, account names, and CD keys.
    /// The logic lives in <see cref="IDMPlayerDirectoryService"/>; every member here forwards to the container's instance.
    /// </summary>
    public static class DMPlayerDirectory
    {
        // A compile-time constant: callers get the value inlined, as before.
        public const int MaxResults = Domain.Administration.DMPlayerDirectoryService.MaxResults;

        private static IDMPlayerDirectoryService Instance => FacadeAccessor.Resolve<IDMPlayerDirectoryService>();

        /// <summary>
        /// Finds characters by name, descriptor, account name, or CD key, matching the start of each word, or by
        /// their whole player ID. Online and offline characters match alike: offline characters are found through
        /// the database index, which only matches word starts.
        /// Offline characters are only searched when search text is given.
        /// </summary>
        /// <param name="search">The search text.</param>
        /// <param name="scope">Which characters to include.</param>
        /// <param name="isTruncated">Set when more matches existed than <see cref="MaxResults"/>.</param>
        public static List<DMPlayerSearchResult> Search(string search, DMPlayerSearchScope scope, out bool isTruncated)
        {
            return Instance.Search(search, scope, out isTruncated);
        }

        /// <summary>
        /// Returns the online character with the given player ID, or OBJECT_INVALID if they are offline.
        /// </summary>
        /// <param name="playerId">The character's player ID.</param>
        public static uint GetOnlinePlayer(string playerId)
        {
            return Instance.GetOnlinePlayer(playerId);
        }

        /// <summary>
        /// Returns where an offline character was last saved, or false if that area no longer exists.
        /// </summary>
        /// <param name="playerId">The character's player ID.</param>
        /// <param name="location">The saved location.</param>
        public static bool TryGetLastLocation(string playerId, out Location location)
        {
            return Instance.TryGetLastLocation(playerId, out location);
        }
    }
}
