using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Finds online and offline characters for the DM player search.
    /// This is a staff surface, so it shows canonical names, account names, and CD keys.
    /// </summary>
    public static class DMPlayerDirectory
    {
        public const int MaxResults = 200;

        private static readonly string[] OfflineSearchFields =
        {
            nameof(Player.Name),
            nameof(Player.UnknownDisplayName),
            nameof(Player.AccountName),
            nameof(Player.CDKey)
        };

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
            var terms = DMSearchText.GetTerms(search);
            var results = new List<DMPlayerSearchResult>();
            var onlineIds = new HashSet<string>();
            var offlineTruncated = false;

            foreach (var player in GetOnlinePlayers())
            {
                var result = BuildOnlineResult(player);
                onlineIds.Add(result.PlayerId);
                if (scope == DMPlayerSearchScope.Offline)
                    continue;

                if (Matches(search, terms, result, PlayerDescriptor.GetUnknownDisplayName(player)))
                    results.Add(result);
            }

            if (scope != DMPlayerSearchScope.Online && terms.Count > 0)
            {
                foreach (var dbPlayer in SearchOfflineRecords(search.Trim(), terms, out offlineTruncated))
                {
                    if (onlineIds.Contains(dbPlayer.Id))
                        continue;

                    var result = BuildOfflineResult(dbPlayer);
                    if (Matches(search, terms, result, dbPlayer.UnknownDisplayName))
                        results.Add(result);
                }
            }

            isTruncated = results.Count > MaxResults || offlineTruncated;
            return results
                .OrderByDescending(result => result.IsOnline)
                .ThenBy(result => result.Name, StringComparer.OrdinalIgnoreCase)
                .Take(MaxResults)
                .ToList();
        }

        /// <summary>
        /// Returns the online character with the given player ID, or OBJECT_INVALID if they are offline.
        /// </summary>
        /// <param name="playerId">The character's player ID.</param>
        public static uint GetOnlinePlayer(string playerId)
        {
            foreach (var player in GetOnlinePlayers())
            {
                if (GetObjectUUID(player) == playerId)
                    return player;
            }

            return OBJECT_INVALID;
        }

        /// <summary>
        /// Returns where an offline character was last saved, or false if that area no longer exists.
        /// </summary>
        /// <param name="playerId">The character's player ID.</param>
        /// <param name="location">The saved location.</param>
        public static bool TryGetLastLocation(string playerId, out Location location)
        {
            location = default;
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null || string.IsNullOrWhiteSpace(dbPlayer.LocationAreaResref))
                return false;

            var area = Area.GetAreaByResref(dbPlayer.LocationAreaResref);
            if (!GetIsObjectValid(area))
                return false;

            location = Location(area,
                new Vector3(dbPlayer.LocationX, dbPlayer.LocationY, dbPlayer.LocationZ),
                dbPlayer.LocationOrientation);
            return true;
        }

        private static List<uint> GetOnlinePlayers()
        {
            var players = new List<uint>();
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (GetIsDM(player) || GetIsDMPossessed(player))
                    continue;

                players.Add(player);
            }

            return players;
        }

        // Player IDs are only found whole, because offline records are looked up by ID rather than searched.
        private static bool Matches(string search, IReadOnlyList<string> terms, DMPlayerSearchResult result, string unknownDisplayName)
        {
            if (terms.Count == 1 && string.Equals(terms[0], result.PlayerId, StringComparison.OrdinalIgnoreCase))
                return true;

            return DMSearchText.MatchesWordStarts(search,
                result.Name,
                result.Descriptor,
                unknownDisplayName,
                result.AccountName,
                result.CDKey);
        }

        // Returns candidates for Search to filter. The index only matches when every word of a query is in one
        // field, so the longest word is also searched alone to find characters whose words span several fields.
        private static List<Player> SearchOfflineRecords(string search, IReadOnlyList<string> terms, out bool isTruncated)
        {
            var players = new Dictionary<string, Player>();
            isTruncated = false;

            if (terms.Count == 1 && Guid.TryParse(terms[0], out _))
            {
                var dbPlayer = DB.Get<Player>(terms[0]);
                if (dbPlayer != null && !dbPlayer.IsDeleted)
                    players[dbPlayer.Id] = dbPlayer;
            }

            var words = PartialMatchSearchText.GetWords(search);
            var queryTexts = words.Count > 1
                ? new[] { search, words.OrderByDescending(word => word.Length).First() }
                : new[] { search };

            foreach (var queryText in queryTexts)
            {
                foreach (var field in OfflineSearchFields)
                {
                    var query = new DBQuery<Player>()
                        .AddFieldSearch(nameof(Player.IsDeleted), false)
                        .AddFieldSearch(field, queryText, true)
                        .AddPaging(MaxResults + 1, 0);

                    var matches = DB.Search(query).ToList();
                    isTruncated |= matches.Count > MaxResults;
                    foreach (var dbPlayer in matches)
                    {
                        players[dbPlayer.Id] = dbPlayer;
                    }
                }

                // An active disguise shows its own descriptor, which is stored with the disguise.
                var disguiseQuery = new DBQuery<PlayerDisguise>()
                    .AddFieldSearch(nameof(PlayerDisguise.IsRetired), false)
                    .AddFieldSearch(nameof(PlayerDisguise.Descriptor), queryText, true)
                    .AddPaging(MaxResults + 1, 0);

                var disguises = DB.Search(disguiseQuery).ToList();
                isTruncated |= disguises.Count > MaxResults;
                foreach (var disguise in disguises)
                {
                    var dbPlayer = DB.Get<Player>(disguise.PlayerId);
                    if (dbPlayer != null && !dbPlayer.IsDeleted && dbPlayer.ActiveDisguiseId == disguise.Id)
                        players[dbPlayer.Id] = dbPlayer;
                }
            }

            return players.Values.ToList();
        }

        private static DMPlayerSearchResult BuildOnlineResult(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            return new DMPlayerSearchResult(
                playerId,
                true,
                GetName(player),
                Disguise.GetDisplayDescriptor(player),
                GetPCPlayerName(player),
                GetPCPublicCDKey(player),
                GetName(GetArea(player)),
                dbPlayer?.LastLoginDate);
        }

        private static DMPlayerSearchResult BuildOfflineResult(Player dbPlayer)
        {
            var area = string.IsNullOrWhiteSpace(dbPlayer.LocationAreaResref)
                ? OBJECT_INVALID
                : Area.GetAreaByResref(dbPlayer.LocationAreaResref);
            var locationName = GetIsObjectValid(area)
                ? GetName(area)
                : dbPlayer.LocationAreaResref ?? string.Empty;

            return new DMPlayerSearchResult(
                dbPlayer.Id,
                false,
                dbPlayer.Name,
                GetOfflineDescriptor(dbPlayer),
                dbPlayer.AccountName ?? string.Empty,
                dbPlayer.CDKey ?? string.Empty,
                locationName,
                dbPlayer.LastLoginDate);
        }

        private static string GetOfflineDescriptor(Player dbPlayer)
        {
            var disguiseDescriptor = Disguise.GetActiveDisguise(dbPlayer)?.Descriptor;
            return string.IsNullOrWhiteSpace(disguiseDescriptor)
                ? PlayerDescriptor.GetUnknownDisplayNameByPlayerId(dbPlayer.Id)
                : disguiseDescriptor;
        }
    }
}
