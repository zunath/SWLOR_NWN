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
        public const string UnknownName = PlayerDescriptor.DefaultUnknownDisplayName;
        private static readonly Dictionary<string, PlayerKnownName> KnownNamesByObserverId = new();
        private static readonly Dictionary<string, bool> ShowDescriptorsForNamedPlayersByObserverId = new();
        private static readonly string UnknownNamePrefix = ColorToken.TokenStart(127, 127, 127);
        private static readonly string UnknownNameSuffix = ColorToken.TokenEnd();

        static PlayerName()
        {
            ServerManager.OnScriptContextEnd += KnownNamesByObserverId.Clear;
            ServerManager.OnScriptContextEnd += ShowDescriptorsForNamedPlayersByObserverId.Clear;
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void ApplyNameOverridesOnEnter()
        {
            var player = GetEnteringObject();

            if (!GetIsPC(player))
                return;

            PlayerDescriptor.EnsureUnknownDisplayName(player);

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

            var fallbackDisplayName = string.IsNullOrWhiteSpace(fallbackName)
                ? UnknownName
                : fallbackName;

            if (!GetIsObjectValid(observer) || !GetIsPC(observer))
                return fallbackDisplayName;

            if (GetObjectUUID(observer) == targetPlayerId)
                return fallbackDisplayName;

            if (GetIsDM(observer) || GetIsDMPossessed(observer))
            {
                return BuildDisplayNameWithDescriptor(fallbackDisplayName, PlayerDescriptor.GetUnknownDisplayNameByPlayerId(targetPlayerId));
            }

            if (TryGetKnownName(observer, targetPlayerId, out var knownName))
                return ShouldShowDescriptorForNamedPlayers(observer)
                    ? BuildDisplayNameWithDescriptor(knownName, PlayerDescriptor.GetUnknownDisplayNameByPlayerId(targetPlayerId))
                    : knownName;

            return PlayerDescriptor.GetUnknownDisplayNameByPlayerId(targetPlayerId);
        }

        /// <summary>
        /// Returns the observer's known name when present, otherwise the fallback name.
        /// Use for operational permission management surfaces that target persisted character records.
        /// </summary>
        public static string GetKnownNameOrFallbackByPlayerId(uint observer, string targetPlayerId, string fallbackName)
        {
            if (string.IsNullOrWhiteSpace(targetPlayerId))
                return string.IsNullOrWhiteSpace(fallbackName)
                    ? UnknownName
                    : fallbackName;

            if (GetIsObjectValid(observer) &&
                GetIsPC(observer) &&
                !GetIsDM(observer) &&
                !GetIsDMPossessed(observer) &&
                GetObjectUUID(observer) != targetPlayerId &&
                TryGetKnownName(observer, targetPlayerId, out var knownName))
            {
                return knownName;
            }

            return string.IsNullOrWhiteSpace(fallbackName)
                ? UnknownName
                : fallbackName;
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
            if (!isUnknown && CanUseKnownName(observer, target) && TryGetKnownName(observer, target, out var knownName))
                return ShouldShowDescriptorForNamedPlayers(observer)
                    ? BuildColoredDisplayNameWithDescriptor(knownName, PlayerDescriptor.GetUnknownDisplayName(target))
                    : ColorToken.GetPCColor(knownName);

            if (!isUnknown &&
                GetIsObjectValid(observer) &&
                (GetIsDM(observer) || GetIsDMPossessed(observer)))
            {
                return BuildColoredDisplayNameWithDescriptor(GetName(target), PlayerDescriptor.GetUnknownDisplayName(target));
            }

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

        public static string ValidateKnownNameInput(string name)
        {
            if (ContainsColorToken(name))
                return "Names may not contain color codes.";

            return ValidateKnownName(SanitizeKnownName(name));
        }

        public static string ValidateKnownNameAssignment(uint observer, uint target, string name)
        {
            var targetValidationError = ValidateKnownNameTarget(observer, target);
            if (!string.IsNullOrWhiteSpace(targetValidationError))
                return targetValidationError;

            var validationError = ValidateKnownNameInput(name);
            if (!string.IsNullOrWhiteSpace(validationError))
                return validationError;

            var sanitizedName = SanitizeKnownName(name);
            var targetId = GetObjectUUID(target);
            if (string.IsNullOrWhiteSpace(targetId))
                return "Unable to identify that player.";

            var dbKnownNames = GetKnownNames(observer, false);
            return ValidateKnownNameIsUnique(dbKnownNames, targetId, sanitizedName);
        }

        public static void SetKnownName(uint observer, uint target, string name)
        {
            var validationError = ValidateKnownNameAssignment(observer, target, name);

            if (!string.IsNullOrWhiteSpace(validationError))
                throw new ArgumentException(validationError);

            var sanitizedName = SanitizeKnownName(name);
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

        private static bool ContainsColorToken(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return !string.Equals(name, UtilPlugin.StripColors(name), StringComparison.Ordinal);
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

        public static void RefreshNameOverridesForObserver(uint observer)
        {
            if (!GetIsObjectValid(observer) ||
                !GetIsPC(observer) ||
                GetIsDM(observer) ||
                GetIsDMPossessed(observer))
            {
                return;
            }

            var observerId = GetObjectUUID(observer);
            if (!string.IsNullOrWhiteSpace(observerId))
                ShowDescriptorsForNamedPlayersByObserverId.Remove(observerId);

            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (!GetIsPC(player) ||
                    player == observer)
                {
                    continue;
                }

                ApplyNameOverride(observer, player);
            }
        }

        public static void RefreshNameOverridesForPlayer(uint player)
        {
            ApplyNameOverridesForPlayer(player);
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

            var unknownDisplayName = PlayerDescriptor.GetUnknownDisplayName(player);
            RenamePlugin.SetPCNameOverride(player, unknownDisplayName, UnknownNamePrefix, UnknownNameSuffix, PlayerNameOverrideType.Default);
            RenamePlugin.SetPCNameOverride(player, GetName(player), string.Empty, string.Empty, PlayerNameOverrideType.Default, player);
            RenamePlugin.SetPCNameOverride(player, unknownDisplayName, UnknownNamePrefix, UnknownNameSuffix, PlayerNameOverrideType.Obfuscate);

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

            var trueName = GetName(target);
            var descriptor = PlayerDescriptor.GetUnknownDisplayName(target);

            RenamePlugin.SetPCNameOverride(target, BuildStaffDisplayName(target), string.Empty, string.Empty, PlayerNameOverrideType.Default, observer);
            PlayerPlugin.SetCreatureNameOverride(observer, target, BuildCreatureNameOverrideWithDescriptor(trueName, descriptor));
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
            PlayerPlugin.SetCreatureNameOverride(observer, target, BuildCreatureNameOverride(observer, target, isUnknown));
        }

        private static string BuildStaffDisplayName(uint target)
        {
            var trueName = GetName(target);
            var unknownDisplayName = PlayerDescriptor.GetUnknownDisplayName(target);

            return $"{trueName} [{ColorToken.Gray(unknownDisplayName)}]";
        }

        private static string BuildCreatureNameOverride(uint observer, uint target, bool isUnknown)
        {
            if (isUnknown)
                return string.Empty;

            var descriptor = PlayerDescriptor.GetUnknownDisplayName(target);

            if (GetIsDMPossessed(observer))
                return BuildCreatureNameOverrideWithDescriptor(GetName(target), descriptor);

            if (TryGetKnownName(observer, target, out var knownName) &&
                ShouldShowDescriptorForNamedPlayers(observer))
            {
                return BuildCreatureNameOverrideWithDescriptor(knownName, descriptor);
            }

            return string.Empty;
        }

        private static string BuildCreatureNameOverrideWithDescriptor(string primaryName, string descriptor)
        {
            return $"{primaryName}\n{ColorToken.Gray(descriptor)}";
        }

        private static string ResolveDisplayName(uint observer, uint target, out bool isUnknown)
        {
            isUnknown = false;

            if (!GetIsObjectValid(target))
                return string.Empty;

            if (!GetIsPC(target) || GetIsDM(target))
                return GetName(target);

            if (!GetIsObjectValid(observer) ||
                observer == target)
            {
                return GetName(target);
            }

            if (GetIsDM(observer) || GetIsDMPossessed(observer))
                return BuildDisplayNameWithDescriptor(GetName(target), PlayerDescriptor.GetUnknownDisplayName(target));

            if (!GetIsPC(observer))
                return GetName(target);

            if (TryGetKnownName(observer, target, out var knownName))
                return ShouldShowDescriptorForNamedPlayers(observer)
                    ? BuildDisplayNameWithDescriptor(knownName, PlayerDescriptor.GetUnknownDisplayName(target))
                    : knownName;

            isUnknown = true;
            return PlayerDescriptor.GetUnknownDisplayName(target);
        }

        private static string BuildDisplayNameWithDescriptor(string primaryName, string descriptor)
        {
            return $"{primaryName} [{ColorToken.Gray(descriptor)}]";
        }

        private static string BuildColoredDisplayNameWithDescriptor(string primaryName, string descriptor)
        {
            return $"{ColorToken.GetPCColor(primaryName)} [{ColorToken.Gray(descriptor)}]";
        }

        private static bool CanUseKnownName(uint observer, uint target)
        {
            return GetIsObjectValid(observer) &&
                   GetIsPC(observer) &&
                   observer != target &&
                   !GetIsDM(observer) &&
                   !GetIsDMPossessed(observer);
        }

        private static bool ShouldShowDescriptorForNamedPlayers(uint observer)
        {
            if (!GetIsObjectValid(observer) ||
                !GetIsPC(observer) ||
                GetIsDM(observer) ||
                GetIsDMPossessed(observer))
            {
                return true;
            }

            var observerId = GetObjectUUID(observer);
            if (string.IsNullOrWhiteSpace(observerId))
                return true;

            if (ShowDescriptorsForNamedPlayersByObserverId.TryGetValue(observerId, out var setting))
                return setting;

            var dbPlayer = DB.Get<Player>(observerId);
            setting = dbPlayer?.Settings?.ShowDescriptorsForNamedPlayers ?? true;
            ShowDescriptorsForNamedPlayersByObserverId[observerId] = setting;
            return setting;
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
