using System.Collections.Generic;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.Administration
{
    /// <summary>
    /// Finds online and offline characters for the DM player search.
    /// This is a staff surface, so it shows canonical names, account names, and CD keys.
    /// The static facade <c>Service.DMPlayerDirectory</c> forwards to the container's instance of this interface.
    /// </summary>
    public interface IDMPlayerDirectoryService
    {
        /// <summary>
        /// Finds characters by name, descriptor, account name, or CD key, matching the start of each word, or by
        /// their whole player ID. Online and offline characters match alike: offline characters are found through
        /// the database index, which only matches word starts.
        /// Offline characters are only searched when search text is given.
        /// </summary>
        /// <param name="search">The search text.</param>
        /// <param name="scope">Which characters to include.</param>
        /// <param name="isTruncated">Set when more matches existed than the maximum number of results.</param>
        List<DMPlayerSearchResult> Search(string search, DMPlayerSearchScope scope, out bool isTruncated);

        /// <summary>
        /// Returns the online character with the given player ID, or OBJECT_INVALID if they are offline.
        /// </summary>
        /// <param name="playerId">The character's player ID.</param>
        uint GetOnlinePlayer(string playerId);

        /// <summary>
        /// Returns where an offline character was last saved, or false if that area no longer exists.
        /// </summary>
        /// <param name="playerId">The character's player ID.</param>
        /// <param name="location">The saved location.</param>
        bool TryGetLastLocation(string playerId, out Location location);
    }
}
