using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The names a player gives other characters with <c>/name</c>: the validation, the writes that set and forget a known name, the search by
    /// known name and the cleanup when a disguise is deleted. They moved here from <see cref="PlayerNameService"/> unchanged; the caches, the
    /// descriptors and the name overrides stay there.
    /// </summary>
    public class PlayerKnownNameService : IPlayerKnownNameService
    {
        private const int MaxKnownNameLength = PlayerNameService.MaxKnownNameLength;

        private readonly IDBService _db;
        private readonly IPlayerNameService _playerName;
        private readonly IPlayerListRefresh _playerList;

        public PlayerKnownNameService(IDBService db, IPlayerNameService playerName, IPlayerListRefresh playerList)
        {
            _db = db;
            _playerName = playerName;
            _playerList = playerList;
        }

        public List<string> SearchKnownPlayerIdsByName(uint observer, string searchText, int maxResults)
        {
            if (!GetIsObjectValid(observer) ||
                !GetIsPC(observer) ||
                GetIsDM(observer) ||
                string.IsNullOrWhiteSpace(searchText) ||
                maxResults <= 0)
            {
                return new List<string>();
            }

            var sanitizedSearch = _playerName.SanitizeKnownName(searchText).ToLower();
            if (string.IsNullOrWhiteSpace(sanitizedSearch))
                return new List<string>();

            var dbKnownNames = _playerName.GetKnownNames(observer, false);
            if (dbKnownNames?.KnownNames == null)
                return new List<string>();

            return dbKnownNames.KnownNames
                .Where(x => !string.IsNullOrWhiteSpace(x.Value) &&
                            !_playerName.IsDisguiseIdentityKey(x.Key) &&
                            x.Value.ToLower().Contains(sanitizedSearch))
                .Select(x => x.Key)
                .Take(maxResults)
                .ToList();
        }

        public string ValidateKnownName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Please enter a name.";

            if (name.Length > MaxKnownNameLength)
                return $"Names may be no longer than {MaxKnownNameLength} characters.";

            return string.Empty;
        }

        public string ValidateKnownNameInput(string name)
        {
            if (ContainsColorToken(name))
                return "Names may not contain color codes.";

            return ValidateKnownName(_playerName.SanitizeKnownName(name));
        }

        public string ValidateKnownNameAssignment(uint observer, uint target, string name)
        {
            var targetValidationError = ValidateKnownNameTarget(observer, target);
            if (!string.IsNullOrWhiteSpace(targetValidationError))
                return targetValidationError;

            var validationError = ValidateKnownNameInput(name);
            if (!string.IsNullOrWhiteSpace(validationError))
                return validationError;

            var sanitizedName = _playerName.SanitizeKnownName(name);
            var targetId = _playerName.GetIdentityKey(target);
            if (string.IsNullOrWhiteSpace(targetId))
                return "Unable to identify that player.";

            return ValidateKnownNameIsUnique(observer, target, sanitizedName);
        }

        public void SetKnownName(uint observer, uint target, string name)
        {
            var validationError = ValidateKnownNameAssignment(observer, target, name);

            if (!string.IsNullOrWhiteSpace(validationError))
                throw new ArgumentException(validationError);

            var sanitizedName = _playerName.SanitizeKnownName(name);
            var targetId = _playerName.GetIdentityKey(target);

            var dbKnownNames = _playerName.GetKnownNames(observer, true);

            dbKnownNames.KnownNames[targetId] = sanitizedName;
            _db.Set(dbKnownNames);

            _playerName.ApplyNameOverride(observer, target);
            _playerList.QueueRefresh();
        }

        private string ValidateKnownNameTarget(uint observer, uint target)
        {
            if (!GetIsObjectValid(target) || !GetIsPC(target) || GetIsDM(target) || GetIsDMPossessed(target))
                return "Known names may only target player characters.";

            if (target == observer)
                return "Known names cannot target the observer.";

            return string.Empty;
        }

        private bool ContainsColorToken(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return !string.Equals(name, UtilPlugin.StripColors(name), StringComparison.Ordinal);
        }

        // Online identities only: stored labels include absent disguises, and any key-type rule would reveal them.
        private string ValidateKnownNameIsUnique(uint observer, uint target, string sanitizedName)
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (!GetIsPC(player) ||
                    GetIsDM(player) ||
                    player == observer ||
                    player == target)
                {
                    continue;
                }

                if (_playerName.TryGetKnownName(observer, player, out var knownName) &&
                    string.Equals(_playerName.SanitizeKnownName(knownName), sanitizedName, StringComparison.OrdinalIgnoreCase))
                {
                    return "You already use that name for another character who is online.";
                }
            }

            return string.Empty;
        }

        public void ForgetKnownName(uint observer, uint target)
        {
            var targetId = _playerName.GetIdentityKey(target);
            var dbKnownNames = _playerName.GetKnownNames(observer, false);

            if (dbKnownNames?.KnownNames == null ||
                !dbKnownNames.KnownNames.ContainsKey(targetId))
            {
                _playerName.ApplyNameOverride(observer, target);
                return;
            }

            dbKnownNames.KnownNames.Remove(targetId);
            _db.Set(dbKnownNames);

            _playerName.ApplyNameOverride(observer, target);
            _playerList.QueueRefresh();
        }

        public int DeleteKnownNameReferences(string targetIdentityKey)
        {
            if (string.IsNullOrWhiteSpace(targetIdentityKey))
                return 0;

            var removed = 0;
            var offset = 0;
            const int PageSize = 50;

            while (true)
            {
                var page = _db.Search(new DBQuery<PlayerKnownName>()
                        .AddPaging(PageSize, offset))
                    .ToList();

                foreach (var dbKnownNames in page)
                {
                    if (dbKnownNames.KnownNames == null ||
                        !dbKnownNames.KnownNames.Remove(targetIdentityKey))
                    {
                        continue;
                    }

                    removed++;
                    _db.Set(dbKnownNames);
                    _playerName.ForgetKnownNames(dbKnownNames.ObserverPlayerId);
                }

                if (page.Count < PageSize)
                    break;

                offset += PageSize;
            }

            return removed;
        }
    }
}
