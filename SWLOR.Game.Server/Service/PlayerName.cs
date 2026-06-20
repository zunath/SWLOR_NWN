using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
{
    public static class PlayerName
    {
        private const int MaxKnownNameLength = 64;
        public const string UnknownName = "Someone";
        private static readonly Dictionary<string, PlayerKnownName> KnownNamesByObserverId = new();
        private static readonly string UnknownNamePrefix = ColorToken.TokenStart(127, 127, 127);
        private static readonly string UnknownNameSuffix = ColorToken.TokenEnd();

        static PlayerName()
        {
            ServerManager.OnScriptContextEnd += KnownNamesByObserverId.Clear;
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void ApplyNameOverridesOnEnter()
        {
            var player = GetEnteringObject();

            if (!GetIsPC(player))
                return;

            if (GetIsDM(player))
            {
                ApplyNameOverridesForDMObserver(player);
                DelayCommand(1.0f, () => ApplyNameOverridesForDMObserver(player));
                return;
            }

            ApplyNameOverridesForPlayer(player);
            DelayCommand(1.0f, () => ApplyNameOverridesForPlayer(player));
        }

        [NWNEventHandler(ScriptName.OnNWNXChat)]
        public static void PreventAmbiguousTellTargets()
        {
            if (ChatPlugin.GetChannel() != ChatChannel.PlayerTell)
                return;

            var sender = ChatPlugin.GetSender();
            var target = ChatPlugin.GetTarget();

            if (!GetIsObjectValid(sender) ||
                !GetIsObjectValid(target) ||
                !GetIsPC(sender) ||
                !GetIsPC(target) ||
                GetIsDM(sender) ||
                GetIsDM(target) ||
                GetIsDMPossessed(sender))
            {
                return;
            }

            var displayName = GetDisplayName(sender, target);
            if (string.IsNullOrWhiteSpace(displayName) ||
                CountDisplayNameMatches(sender, displayName) <= 1)
            {
                return;
            }

            ChatPlugin.SkipMessage();
            SendMessageToPC(sender, ColorToken.Red($"'{displayName}' is ambiguous. Use /name on the intended player before sending tells."));
        }

        public static string GetDisplayName(uint observer, uint target)
        {
            return ResolveDisplayName(observer, target, out _);
        }

        public static string GetDisplayNameByPlayerId(uint observer, string targetPlayerId, string fallbackName)
        {
            if (string.IsNullOrWhiteSpace(targetPlayerId))
                return UnknownName;

            if (!GetIsObjectValid(observer) ||
                !GetIsPC(observer) ||
                GetIsDM(observer) ||
                GetIsDMPossessed(observer) ||
                GetObjectUUID(observer) == targetPlayerId)
            {
                return string.IsNullOrWhiteSpace(fallbackName)
                    ? UnknownName
                    : fallbackName;
            }

            if (TryGetKnownName(observer, targetPlayerId, out var knownName))
                return knownName;

            return UnknownName;
        }

        public static List<string> SearchKnownPlayerIdsByName(uint observer, string searchText, int maxResults)
        {
            if (!GetIsObjectValid(observer) ||
                !GetIsPC(observer) ||
                GetIsDM(observer) ||
                string.IsNullOrWhiteSpace(searchText) ||
                maxResults <= 0)
            {
                return new List<string>();
            }

            var sanitizedSearch = SanitizeKnownName(searchText).ToLower();
            if (string.IsNullOrWhiteSpace(sanitizedSearch))
                return new List<string>();

            var dbKnownNames = GetKnownNames(observer, false);
            if (dbKnownNames?.KnownNames == null)
                return new List<string>();

            return dbKnownNames.KnownNames
                .Where(x => !string.IsNullOrWhiteSpace(x.Value) &&
                            x.Value.ToLower().Contains(sanitizedSearch))
                .Select(x => x.Key)
                .Take(maxResults)
                .ToList();
        }

        public static string GetColoredDisplayName(uint observer, uint target)
        {
            if (!GetIsPC(target) || GetIsDM(target))
                return ColorToken.GetNameNPCColor(target);

            var displayName = ResolveDisplayName(observer, target, out var isUnknown);
            return isUnknown
                ? ColorToken.Gray(displayName)
                : ColorToken.GetPCColor(displayName);
        }

        public static string SanitizeKnownName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            var strippedName = UtilPlugin.StripColors(name).Trim();
            var builder = new StringBuilder();

            foreach (var character in strippedName)
            {
                if (char.IsControl(character))
                    continue;

                builder.Append(character);
            }

            var sanitizedName = builder.ToString().Trim();

            while (sanitizedName.Contains("  "))
            {
                sanitizedName = sanitizedName.Replace("  ", " ");
            }

            return sanitizedName;
        }

        public static string ValidateKnownName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Please enter a name.";

            if (name.Length > MaxKnownNameLength)
                return $"Names may be no longer than {MaxKnownNameLength} characters.";

            return string.Empty;
        }

        public static string ValidateKnownNameAssignment(uint observer, uint target, string name)
        {
            var sanitizedName = SanitizeKnownName(name);
            var targetValidationError = ValidateKnownNameTarget(observer, target);
            if (!string.IsNullOrWhiteSpace(targetValidationError))
                return targetValidationError;

            var validationError = ValidateKnownName(sanitizedName);
            if (!string.IsNullOrWhiteSpace(validationError))
                return validationError;

            var targetId = GetObjectUUID(target);
            if (string.IsNullOrWhiteSpace(targetId))
                return "Unable to identify that player.";

            var dbKnownNames = GetKnownNames(observer, false);
            return ValidateKnownNameIsUnique(dbKnownNames, targetId, sanitizedName);
        }

        public static void SetKnownName(uint observer, uint target, string name)
        {
            var sanitizedName = SanitizeKnownName(name);
            var validationError = ValidateKnownNameAssignment(observer, target, sanitizedName);

            if (!string.IsNullOrWhiteSpace(validationError))
                throw new ArgumentException(validationError);

            var targetId = GetObjectUUID(target);

            var dbKnownNames = GetKnownNames(observer, true);

            dbKnownNames.KnownNames[targetId] = sanitizedName;
            DB.Set(dbKnownNames);

            ApplyNameOverride(observer, target);
        }

        private static string ValidateKnownNameTarget(uint observer, uint target)
        {
            if (!GetIsObjectValid(target) || !GetIsPC(target) || GetIsDM(target))
                return "Known names may only target player characters.";

            if (target == observer)
                return "Known names cannot target the observer.";

            return string.Empty;
        }

        private static string ValidateKnownNameIsUnique(PlayerKnownName dbKnownNames, string targetId, string sanitizedName)
        {
            if (dbKnownNames?.KnownNames == null)
                return string.Empty;

            var isDuplicate = dbKnownNames.KnownNames.Any(entry =>
                entry.Key != targetId &&
                string.Equals(SanitizeKnownName(entry.Value), sanitizedName, StringComparison.OrdinalIgnoreCase));

            if (isDuplicate)
                return "You already use that name for another character.";

            return string.Empty;
        }

        public static void ForgetKnownName(uint observer, uint target)
        {
            var targetId = GetObjectUUID(target);
            var dbKnownNames = GetKnownNames(observer, false);

            if (dbKnownNames?.KnownNames == null ||
                !dbKnownNames.KnownNames.ContainsKey(targetId))
            {
                ApplyNameOverride(observer, target);
                return;
            }

            dbKnownNames.KnownNames.Remove(targetId);
            DB.Set(dbKnownNames);

            ApplyNameOverride(observer, target);
        }

        private static PlayerKnownName GetKnownNames(uint observer, bool createIfMissing)
        {
            var observerId = GetObjectUUID(observer);
            var dbKnownNames = FindKnownNames(observerId);

            if (dbKnownNames == null && createIfMissing)
            {
                dbKnownNames = new PlayerKnownName(observerId);
            }

            if (dbKnownNames != null && dbKnownNames.KnownNames == null)
            {
                dbKnownNames.KnownNames = new Dictionary<string, string>();
            }

            KnownNamesByObserverId[observerId] = dbKnownNames;
            return dbKnownNames;
        }

        private static PlayerKnownName FindKnownNames(string observerId)
        {
            if (KnownNamesByObserverId.TryGetValue(observerId, out var dbKnownNames))
                return dbKnownNames;

            dbKnownNames = DB.Search(new DBQuery<PlayerKnownName>()
                    .AddFieldSearch(nameof(PlayerKnownName.ObserverPlayerId), observerId, false))
                .FirstOrDefault();

            KnownNamesByObserverId[observerId] = dbKnownNames;
            return dbKnownNames;
        }

        private static void ApplyNameOverridesForPlayer(uint player)
        {
            if (!GetIsObjectValid(player) || !GetIsPC(player) || GetIsDM(player))
                return;

            RenamePlugin.SetPCNameOverride(player, UnknownName, UnknownNamePrefix, UnknownNameSuffix, PlayerNameOverrideType.Default);
            RenamePlugin.SetPCNameOverride(player, GetName(player), string.Empty, string.Empty, PlayerNameOverrideType.Default, player);
            RenamePlugin.SetPCNameOverride(player, UnknownName, UnknownNamePrefix, UnknownNameSuffix, PlayerNameOverrideType.Obfuscate);

            for (var otherPlayer = GetFirstPC(); GetIsObjectValid(otherPlayer); otherPlayer = GetNextPC())
            {
                if (!GetIsPC(otherPlayer))
                    continue;

                if (otherPlayer == player)
                    continue;

                if (GetIsDM(otherPlayer))
                {
                    ApplyTrueNameOverride(otherPlayer, player);
                    continue;
                }

                ApplyNameOverride(player, otherPlayer);
                ApplyNameOverride(otherPlayer, player);
            }
        }

        private static void ApplyNameOverridesForDMObserver(uint dm)
        {
            if (!GetIsObjectValid(dm) || !GetIsPC(dm) || !GetIsDM(dm))
                return;

            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (!GetIsPC(player) || GetIsDM(player))
                    continue;

                ApplyTrueNameOverride(dm, player);
            }
        }

        private static void ApplyTrueNameOverride(uint observer, uint target)
        {
            if (!GetIsObjectValid(observer) ||
                !GetIsObjectValid(target) ||
                !GetIsPC(observer) ||
                !GetIsPC(target) ||
                GetIsDM(target))
            {
                return;
            }

            RenamePlugin.SetPCNameOverride(target, GetName(target), string.Empty, string.Empty, PlayerNameOverrideType.Default, observer);
        }

        private static void ApplyNameOverride(uint observer, uint target)
        {
            if (!GetIsObjectValid(observer) ||
                !GetIsObjectValid(target) ||
                !GetIsPC(observer) ||
                !GetIsPC(target) ||
                GetIsDM(observer) ||
                GetIsDM(target))
            {
                return;
            }

            var displayName = ResolveDisplayName(observer, target, out var isUnknown);
            var prefix = isUnknown ? UnknownNamePrefix : string.Empty;
            var suffix = isUnknown ? UnknownNameSuffix : string.Empty;

            RenamePlugin.SetPCNameOverride(target, displayName, prefix, suffix, PlayerNameOverrideType.Default, observer);
        }

        private static string ResolveDisplayName(uint observer, uint target, out bool isUnknown)
        {
            isUnknown = false;

            if (!GetIsObjectValid(target))
                return string.Empty;

            if (!GetIsPC(target) || GetIsDM(target))
                return GetName(target);

            if (!GetIsObjectValid(observer) ||
                observer == target ||
                GetIsDM(observer) ||
                GetIsDMPossessed(observer))
            {
                return GetName(target);
            }

            if (!GetIsPC(observer))
                return GetName(target);

            if (TryGetKnownName(observer, target, out var knownName))
                return knownName;

            isUnknown = true;
            return UnknownName;
        }

        private static bool TryGetKnownName(uint observer, uint target, out string knownName)
        {
            var targetId = GetObjectUUID(target);
            return TryGetKnownName(observer, targetId, out knownName);
        }

        private static bool TryGetKnownName(uint observer, string targetId, out string knownName)
        {
            knownName = string.Empty;
            var dbKnownNames = GetKnownNames(observer, false);

            return dbKnownNames?.KnownNames != null &&
                   dbKnownNames.KnownNames.TryGetValue(targetId, out knownName) &&
                   !string.IsNullOrWhiteSpace(knownName);
        }

        private static int CountDisplayNameMatches(uint observer, string displayName)
        {
            var count = 0;

            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (!GetIsPC(player) ||
                    GetIsDM(player) ||
                    player == observer)
                {
                    continue;
                }

                if (string.Equals(GetDisplayName(observer, player), displayName, StringComparison.OrdinalIgnoreCase))
                    count++;
            }

            return count;
        }
    }
}
