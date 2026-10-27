using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.Administration
{
    /// <summary>
    /// Finds online and offline characters for the DM player search: the logic that used to be static in
    /// <c>Service.DMPlayerDirectory</c>, moved here unchanged. The facade forwards every call to the container's
    /// instance and repeats <see cref="MaxResults"/>.
    /// </summary>
    public class DMPlayerDirectoryService : IDMPlayerDirectoryService
    {
        /// <summary>Searches return at most this many characters. <c>Service.DMPlayerDirectory.MaxResults</c> repeats it.</summary>
        public const int MaxResults = 200;
        private const int CandidatePageSize = 200;
        // Keeps a search whose words are all common from loading most of the character records.
        private const int MaxOfflineCandidates = 1000;
        // Bounds the index count queries one search can make.
        private const int MaxCountedSeedWords = 3;

        private readonly string[] _offlineSearchFields =
        {
            nameof(Player.Name),
            nameof(Player.UnknownDisplayName),
            nameof(Player.AccountName),
            nameof(Player.CDKey)
        };

        private readonly IDBService _db;
        private readonly IDMPresenceService _presence;

        public DMPlayerDirectoryService(IDBService db, IDMPresenceService presence)
        {
            _db = db;
            _presence = presence;
        }

        public List<DMPlayerSearchResult> Search(string search, DMPlayerSearchScope scope, out bool isTruncated)
        {
            var terms = DMSearchText.GetTerms(search);
            var results = new List<DMPlayerSearchResult>();
            var onlineIds = new HashSet<string>();
            var offlineTruncated = false;
            isTruncated = false;

            // Text with no queryable words, such as only punctuation, matches nobody, online or offline.
            if (terms.Count > 0 && !PartialMatchSearchText.HasQueryableWords(search))
                return results;

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
                results.AddRange(SearchOffline(search.Trim(), terms, onlineIds, out offlineTruncated));

            isTruncated = results.Count > MaxResults || offlineTruncated;
            return results
                .OrderByDescending(result => result.IsOnline)
                .ThenBy(result => result.Name, StringComparer.OrdinalIgnoreCase)
                .Take(MaxResults)
                .ToList();
        }

        public uint GetOnlinePlayer(string playerId)
        {
            foreach (var player in GetOnlinePlayers())
            {
                if (GetObjectUUID(player) == playerId)
                    return player;
            }

            return OBJECT_INVALID;
        }

        public bool TryGetLastLocation(string playerId, out Location location)
        {
            location = default;
            var dbPlayer = _db.Get<Player>(playerId);
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

        private List<uint> GetOnlinePlayers()
        {
            var players = new List<uint>();
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (_presence.IsControlledByDM(player))
                    continue;

                players.Add(player);
            }

            return players;
        }

        // Player IDs are only found whole, because offline records are looked up by ID rather than searched.
        private bool Matches(string search, IReadOnlyList<string> terms, DMPlayerSearchResult result, string unknownDisplayName)
        {
            if (terms.Count == 1 && Guid.TryParse(terms[0], out var searchedId) &&
                Guid.TryParse(result.PlayerId, out var playerId) && searchedId == playerId)
                return true;

            return DMSearchText.MatchesWordStarts(search,
                result.Name,
                result.Descriptor,
                unknownDisplayName,
                result.AccountName,
                result.CDKey);
        }

        // A matching character has every search word in some field, so the word with the fewest index hits finds
        // them all. Its candidates are paged in until enough characters match or the candidate limit is reached.
        private List<DMPlayerSearchResult> SearchOffline(
            string search,
            IReadOnlyList<string> terms,
            HashSet<string> onlineIds,
            out bool isTruncated)
        {
            var results = new List<DMPlayerSearchResult>();
            var seen = new HashSet<string>(onlineIds);
            var loaded = 0;
            isTruncated = false;

            void Consider(Player dbPlayer)
            {
                if (dbPlayer == null || dbPlayer.IsDeleted || !seen.Add(dbPlayer.Id))
                    return;

                var result = BuildOfflineResult(dbPlayer);
                if (Matches(search, terms, result, dbPlayer.UnknownDisplayName))
                    results.Add(result);
            }

            // Returns true when every candidate was read, false when it stopped at a limit.
            bool ReadAll<T>(Func<DBQuery<T>> buildQuery, Func<T, Player> toPlayer)
                where T : EntityBase
            {
                for (var offset = 0; ; offset += CandidatePageSize)
                {
                    if (results.Count > MaxResults || loaded >= MaxOfflineCandidates)
                        return false;

                    var page = _db.Search(buildQuery().AddPaging(CandidatePageSize, offset)).ToList();
                    loaded += page.Count;
                    foreach (var row in page)
                    {
                        Consider(toPlayer(row));
                    }

                    if (page.Count < CandidatePageSize)
                        return true;
                }
            }

            // Player records are keyed by the lowercase GUID, whatever case or format the DM typed.
            if (terms.Count == 1 && Guid.TryParse(terms[0], out var playerId))
                Consider(_db.Get<Player>(playerId.ToString()));

            // A word the index cannot query matches everything, so it can never narrow the candidates. Counting
            // hits costs several queries per word, so only the longest few words, usually the rarest, are counted.
            var seed = PartialMatchSearchText.GetWords(search)
                .Where(PartialMatchSearchText.IsQueryableWord)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(word => word.Length)
                .Take(MaxCountedSeedWords)
                .OrderBy(CountOfflineHits)
                .FirstOrDefault();
            if (seed == null)
                return results;

            foreach (var field in _offlineSearchFields)
            {
                if (!ReadAll(() => PlayerQuery(field, seed), dbPlayer => dbPlayer))
                {
                    isTruncated = true;
                    return results;
                }
            }

            isTruncated = !ReadAll(() => DisguiseQuery(seed), GetDisguisedPlayer);
            return results;
        }

        private long CountOfflineHits(string word)
        {
            return _offlineSearchFields.Sum(field => _db.SearchCount(PlayerQuery(field, word))) +
                   _db.SearchCount(DisguiseQuery(word));
        }

        private DBQuery<Player> PlayerQuery(string field, string text)
        {
            return new DBQuery<Player>()
                .AddFieldSearch(nameof(Player.IsDeleted), false)
                .AddFieldSearch(field, text, true);
        }

        // An active disguise shows its own descriptor, which is stored with the disguise.
        private DBQuery<PlayerDisguise> DisguiseQuery(string text)
        {
            return new DBQuery<PlayerDisguise>()
                .AddFieldSearch(nameof(PlayerDisguise.IsRetired), false)
                .AddFieldSearch(nameof(PlayerDisguise.Descriptor), text, true);
        }

        private Player GetDisguisedPlayer(PlayerDisguise disguise)
        {
            var dbPlayer = _db.Get<Player>(disguise.PlayerId);
            return dbPlayer?.ActiveDisguiseId == disguise.Id ? dbPlayer : null;
        }

        private DMPlayerSearchResult BuildOnlineResult(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            var activeDisguise = Disguise.GetActiveDisguise(dbPlayer);

            return new DMPlayerSearchResult(
                playerId,
                true,
                GetName(player),
                Disguise.GetDisplayDescriptor(player),
                activeDisguise != null,
                DMDisguiseText.Describe(activeDisguise),
                PlayerDescriptor.GetUnknownDisplayName(player),
                GetPCPlayerName(player),
                GetPCPublicCDKey(player),
                GetName(GetArea(player)),
                dbPlayer?.LastLoginDate);
        }

        private DMPlayerSearchResult BuildOfflineResult(Player dbPlayer)
        {
            var area = string.IsNullOrWhiteSpace(dbPlayer.LocationAreaResref)
                ? OBJECT_INVALID
                : Area.GetAreaByResref(dbPlayer.LocationAreaResref);
            var locationName = GetIsObjectValid(area)
                ? GetName(area)
                : dbPlayer.LocationAreaResref ?? string.Empty;
            var activeDisguise = Disguise.GetActiveDisguise(dbPlayer);

            return new DMPlayerSearchResult(
                dbPlayer.Id,
                false,
                dbPlayer.Name,
                GetOfflineDescriptor(dbPlayer, activeDisguise),
                activeDisguise != null,
                DMDisguiseText.Describe(activeDisguise),
                PlayerDescriptor.GetUnknownDisplayName(dbPlayer),
                dbPlayer.AccountName ?? string.Empty,
                dbPlayer.CDKey ?? string.Empty,
                locationName,
                dbPlayer.LastLoginDate);
        }

        private string GetOfflineDescriptor(Player dbPlayer, PlayerDisguise activeDisguise)
        {
            var disguiseDescriptor = activeDisguise?.Descriptor;
            return string.IsNullOrWhiteSpace(disguiseDescriptor)
                ? PlayerDescriptor.GetUnknownDisplayName(dbPlayer)
                : disguiseDescriptor;
        }
    }
}
