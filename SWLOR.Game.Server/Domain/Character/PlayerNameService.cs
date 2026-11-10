using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// What players see as each other's names, the known names they assign, and the AFK flag: the logic and state
    /// that used to be static in <c>Service.PlayerName</c>, moved here unchanged. The event handlers run on the
    /// container's instance.
    /// </summary>
    /// <remarks>
    /// The descriptors call <c>PlayerName</c> back (through <see cref="IPlayerNameService"/>), so this service reaches
    /// them through the <c>PlayerDescriptor</c> facade in two one-line <c>protected virtual</c> methods. The
    /// script-context hook that clears the descriptor setting cache is registered the first time the cache is filled
    /// (the static class registered it when it was first used), not in the constructor.
    /// </remarks>
    public class PlayerNameService : IPlayerNameService
    {
        public const int MaxKnownNameLength = 64;
        public const string UnknownName = PlayerDescriptorService.DefaultUnknownDisplayName;
        // Set on the sender while a tell addressed to a specific character, not a typed name, is sent.
        public const string DirectTellVariable = "PLAYER_NAME_DIRECT_TELL";
        // Known names change only through DB writes, so entries last until a PlayerKnownName write or logout.
        private readonly Dictionary<string, PlayerKnownName> KnownNamesByObserverId = new();
        private long _knownNamesWriteVersion;
        private readonly Dictionary<string, bool> ShowDescriptorsForNamedPlayersByObserverId = new();
        private readonly Dictionary<uint, (uint Area, Vector3 Position)> AfkPlayers = new();
        private IDisposable _afkMovementCheck;
        private bool _scriptContextHooked;
        private readonly string UnknownNamePrefix = ColorToken.TokenStart(127, 127, 127);
        private readonly string UnknownNameSuffix = ColorToken.TokenEnd();
        private readonly string StaffDisguiseMarker = " " + ColorToken.Orange("[Disguised]");

        private readonly IDBService _db;
        private readonly IPlayerListService _playerList;
        private readonly IChatPortraitTellsService _chatPortraitTells;
        private readonly Lazy<IDisguiseService> _disguise;
        private readonly Lazy<IPlayerDescriptorService> _playerDescriptor;

        public PlayerNameService(
            IDBService db,
            IPlayerListService playerList,
            IChatPortraitTellsService chatPortraitTells,
            Lazy<IDisguiseService> disguise,
            Lazy<IPlayerDescriptorService> playerDescriptor)
        {
            _db = db;
            _playerList = playerList;
            _chatPortraitTells = chatPortraitTells;
            _disguise = disguise;
            _playerDescriptor = playerDescriptor;
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public void ReleaseKnownNamesOnExit()
        {
            var observerId = GetObjectUUID(GetExitingObject());
            if (!string.IsNullOrWhiteSpace(observerId))
                KnownNamesByObserverId.Remove(observerId);
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public void ApplyNameOverridesOnEnter()
        {
            var player = GetEnteringObject();

            if (!GetIsPC(player))
                return;

            RemoveAfk(player);
            EnsureUnknownDisplayName(player);

            if (GetIsDM(player))
            {
                ApplyNameOverridesForDMObserver(player);
                DelayCommand(1.0f, () => ApplyNameOverridesForDMObserver(player));
                return;
            }

            ApplyNameOverridesForPlayer(player);
            DelayCommand(1.0f, () => ApplyNameOverridesForPlayer(player));
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public void ClearAfkOnExit()
        {
            RemoveAfk(GetExitingObject());
        }

        public bool IsAfk(uint player) => AfkPlayers.ContainsKey(player);

        public bool ToggleAfk(uint player)
        {
            if (!GetIsObjectValid(player) || !GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return false;

            if (!RemoveAfk(player))
            {
                AfkPlayers[player] = (GetArea(player), GetPosition(player));
                _afkMovementCheck ??= ScheduleRepeating(CheckAfkMovement, TimeSpan.FromSeconds(1));
            }

            RefreshNameOverridesForPlayer(player);
            return IsAfk(player);
        }

        private bool RemoveAfk(uint player)
        {
            if (!AfkPlayers.Remove(player))
                return false;

            if (AfkPlayers.Count == 0)
            {
                _afkMovementCheck?.Dispose();
                _afkMovementCheck = null;
            }

            return true;
        }

        private void CheckAfkMovement()
        {
            foreach (var (player, start) in AfkPlayers.ToArray())
            {
                if (!GetIsObjectValid(player))
                {
                    RemoveAfk(player);
                    continue;
                }

                if (GetArea(player) == start.Area && GetPosition(player) == start.Position)
                    continue;

                RemoveAfk(player);
                RefreshNameOverridesForPlayer(player);
                SendMessageToPC(player, "You are no longer AFK.");
            }
        }

        // Keep status outside the name used for identity resolution and tell targeting.
        private string GetNameOverrideSuffix(uint player, bool isUnknown = false)
        {
            return (isUnknown ? UnknownNameSuffix : string.Empty) +
                   (IsAfk(player) ? " " + ColorToken.Purple("[AFK]") : string.Empty);
        }

        // Only DM observers receive this suffix, so neither the disguised player nor other players see the marker.
        private string GetStaffNameOverrideSuffix(uint player)
        {
            return GetNameOverrideSuffix(player) +
                   (GetActiveDisguise(player) != null ? StaffDisguiseMarker : string.Empty);
        }

        [NWNEventHandler(ScriptName.OnNWNXChat)]
        public void PreventAmbiguousTellTargets()
        {
            var channel = ChatPlugin.GetChannel();
            if (channel != ChatChannel.PlayerTell && channel != ChatChannel.DMTell)
                return;

            var sender = ChatPlugin.GetSender();
            var target = ChatPlugin.GetTarget();

            if (_chatPortraitTells.RefuseStalePortraitTell(sender, target, GetLocalBool(sender, DirectTellVariable), out var confirmedPortraitTarget))
            {
                ChatPlugin.SkipMessage();
                return;
            }

            if (channel != ChatChannel.PlayerTell ||
                !GetIsObjectValid(sender) ||
                !GetIsObjectValid(target) ||
                !GetIsPC(sender) ||
                !GetIsPC(target) ||
                GetIsDM(sender) ||
                GetIsDM(target) ||
                GetIsDMPossessed(target) ||
                GetIsDMPossessed(sender))
            {
                return;
            }

            var displayName = GetDisplayName(sender, target);
            // A Player List tell or a confirmed portrait tell names its target exactly, so a shared name is not ambiguous.
            if (GetLocalBool(sender, DirectTellVariable) ||
                confirmedPortraitTarget ||
                string.IsNullOrWhiteSpace(displayName) ||
                CountDisplayNameMatches(sender, displayName) <= 1)
            {
                ApplyChatNameOverride(target, sender);
                ApplyChatNameOverride(sender, target);
                DelayCommand(0.1f, () => RestoreNameOverride(target, sender));
                DelayCommand(0.1f, () => RestoreNameOverride(sender, target));
                return;
            }

            ChatPlugin.SkipMessage();
            SendMessageToPC(sender, ColorToken.Red($"'{displayName}' is ambiguous. Use /name on the intended player before sending tells."));
        }

        public string GetDisplayName(uint observer, uint target)
        {
            return ResolveDisplayName(observer, target, out _);
        }

        public string GetChatDisplayName(uint observer, uint target)
        {
            return ResolveChatDisplayName(observer, target, out _);
        }

        /// <summary>Canonical name and current public descriptor for server audit records.</summary>
        public string GetAuditName(uint target)
        {
            var canonicalName = GetName(target);
            if (!GetIsObjectValid(target) || !GetIsPC(target) || GetIsDM(target) || GetIsDMPossessed(target))
                return canonicalName;

            return $"{canonicalName} [{GetDisplayDescriptor(target)}]";
        }

        /// <summary>Server-only identity snapshot for correlating messages with disguise history.</summary>
        public string GetAuditIdentity(uint target)
        {
            var name = GetAuditName(target);
            if (!GetIsObjectValid(target) || !GetIsPC(target) || GetIsDM(target) || GetIsDMPossessed(target))
                return name;

            var playerId = GetObjectUUID(target);
            var disguise = GetActiveDisguise(target);
            var identityKey = disguise == null ? playerId : _disguise.Value.BuildIdentityKey(disguise.Id);
            return $"{name} [PlayerId={playerId} IdentityKey={identityKey} Disguised={disguise != null} DisguiseId={disguise?.Id ?? "none"} DisguiseName={disguise?.PrivateName ?? "none"}]";
        }

        public void SendChatMessageWithChatNameOverride(uint observer, uint target, Action sendMessage)
        {
            ApplyChatNameOverride(observer, target);

            try
            {
                sendMessage();
            }
            finally
            {
                RestoreNameOverride(observer, target);
            }
        }

        public string GetDisplayNameByPlayerId(uint observer, string targetPlayerId, string fallbackName)
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
                return BuildDisplayNameWithDescriptor(fallbackDisplayName, GetUnknownDisplayNameByPlayerId(targetPlayerId));
            }

            if (TryGetKnownName(observer, targetPlayerId, out var knownName))
                return ShouldShowDescriptorForNamedPlayers(observer)
                    ? BuildDisplayNameWithDescriptor(knownName, GetUnknownDisplayNameByPlayerId(targetPlayerId))
                    : knownName;

            return GetUnknownDisplayNameByPlayerId(targetPlayerId);
        }

        /// <summary>
        /// Retrieves the display name of an offline/persisted player record for plain-text UI surfaces
        /// such as NUI windows, which render chat color tokens literally. Unlike
        /// <see cref="GetDisplayNameByPlayerId"/>, targets the observer has named show only the assigned
        /// name (no bracketed descriptor), and no output contains color tokens. Staff observers still see
        /// the canonical name with the descriptor in brackets.
        /// </summary>
        /// <param name="observer">The player viewing the name.</param>
        /// <param name="targetPlayerId">The player Id of the record being displayed.</param>
        /// <param name="fallbackName">The persisted canonical name to fall back on.</param>
        /// <returns>The observer-appropriate display name, free of color tokens.</returns>
        public string GetPlainDisplayNameByPlayerId(uint observer, string targetPlayerId, string fallbackName)
        {
            if (string.IsNullOrWhiteSpace(targetPlayerId))
                return UnknownName;

            // Strip color tokens: persisted names can contain them and this surface renders tokens literally.
            var fallbackDisplayName = string.IsNullOrWhiteSpace(fallbackName)
                ? UnknownName
                : UtilPlugin.StripColors(fallbackName);

            if (!GetIsObjectValid(observer) || !GetIsPC(observer))
                return fallbackDisplayName;

            if (GetObjectUUID(observer) == targetPlayerId)
                return fallbackDisplayName;

            if (GetIsDM(observer) || GetIsDMPossessed(observer))
                return $"{fallbackDisplayName} [{GetUnknownDisplayNameByPlayerId(targetPlayerId)}]";

            if (TryGetKnownName(observer, targetPlayerId, out var knownName))
                return knownName;

            return GetUnknownDisplayNameByPlayerId(targetPlayerId);
        }

        /// <summary>
        /// Returns the observer's known name when present, otherwise the fallback name.
        /// Use for operational permission management surfaces that target persisted character records.
        /// </summary>
        public string GetKnownNameOrFallbackByPlayerId(uint observer, string targetPlayerId, string fallbackName)
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

            var sanitizedSearch = SanitizeKnownName(searchText).ToLower();
            if (string.IsNullOrWhiteSpace(sanitizedSearch))
                return new List<string>();

            var dbKnownNames = GetKnownNames(observer, false);
            if (dbKnownNames?.KnownNames == null)
                return new List<string>();

            return dbKnownNames.KnownNames
                .Where(x => !string.IsNullOrWhiteSpace(x.Value) &&
                            !_disguise.Value.IsDisguiseIdentityKey(x.Key) &&
                            x.Value.ToLower().Contains(sanitizedSearch))
                .Select(x => x.Key)
                .Take(maxResults)
                .ToList();
        }

        public string GetColoredDisplayName(uint observer, uint target)
        {
            if (!GetIsPC(target) || GetIsDM(target) || GetIsDMPossessed(target))
                return ColorToken.GetNameNPCColor(target);

            var displayName = ResolveDisplayName(observer, target, out var isUnknown);
            if (!isUnknown && CanUseKnownName(observer, target) && TryGetKnownName(observer, target, out var knownName))
                return ShouldShowDescriptorForNamedPlayers(observer)
                    ? BuildColoredDisplayNameWithDescriptor(knownName, GetDisplayDescriptor(target))
                    : ColorToken.GetPCColor(knownName);

            if (!isUnknown &&
                GetIsObjectValid(observer) &&
                (GetIsDM(observer) || GetIsDMPossessed(observer)))
            {
                return BuildColoredDisplayNameWithDescriptor(GetName(target), GetDisplayDescriptor(target));
            }

            return isUnknown
                ? ColorToken.Gray(displayName)
                : ColorToken.GetPCColor(displayName);
        }

        public string GetColoredChatDisplayName(uint observer, uint target)
        {
            if (!GetIsPC(target) || GetIsDM(target) || GetIsDMPossessed(target))
                return ColorToken.GetNameNPCColor(target);

            var displayName = ResolveChatDisplayName(observer, target, out var isUnknown);
            return isUnknown
                ? ColorToken.Gray(displayName)
                : ColorToken.GetPCColor(displayName);
        }

        public string SanitizeKnownName(string name)
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

            return ValidateKnownName(SanitizeKnownName(name));
        }

        public string ValidateKnownNameAssignment(uint observer, uint target, string name)
        {
            var targetValidationError = ValidateKnownNameTarget(observer, target);
            if (!string.IsNullOrWhiteSpace(targetValidationError))
                return targetValidationError;

            var validationError = ValidateKnownNameInput(name);
            if (!string.IsNullOrWhiteSpace(validationError))
                return validationError;

            var sanitizedName = SanitizeKnownName(name);
            var targetId = GetIdentityKey(target);
            if (string.IsNullOrWhiteSpace(targetId))
                return "Unable to identify that player.";

            return ValidateKnownNameIsUnique(observer, target, sanitizedName);
        }

        public void SetKnownName(uint observer, uint target, string name)
        {
            var validationError = ValidateKnownNameAssignment(observer, target, name);

            if (!string.IsNullOrWhiteSpace(validationError))
                throw new ArgumentException(validationError);

            var sanitizedName = SanitizeKnownName(name);
            var targetId = GetIdentityKey(target);

            var dbKnownNames = GetKnownNames(observer, true);

            dbKnownNames.KnownNames[targetId] = sanitizedName;
            _db.Set(dbKnownNames);

            ApplyNameOverride(observer, target);
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

                if (TryGetKnownName(observer, player, out var knownName) &&
                    string.Equals(SanitizeKnownName(knownName), sanitizedName, StringComparison.OrdinalIgnoreCase))
                {
                    return "You already use that name for another character who is online.";
                }
            }

            return string.Empty;
        }

        public void ForgetKnownName(uint observer, uint target)
        {
            var targetId = GetIdentityKey(target);
            var dbKnownNames = GetKnownNames(observer, false);

            if (dbKnownNames?.KnownNames == null ||
                !dbKnownNames.KnownNames.ContainsKey(targetId))
            {
                ApplyNameOverride(observer, target);
                return;
            }

            dbKnownNames.KnownNames.Remove(targetId);
            _db.Set(dbKnownNames);

            ApplyNameOverride(observer, target);
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
                    KnownNamesByObserverId.Remove(dbKnownNames.ObserverPlayerId);
                }

                if (page.Count < PageSize)
                    break;

                offset += PageSize;
            }

            return removed;
        }

        public void RefreshNameOverridesForObserver(uint observer)
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

            _playerList.QueueRefresh();
        }

        public void RefreshNameOverridesForPlayer(uint player)
        {
            ApplyNameOverridesForPlayer(player);
            _playerList.QueueRefresh();
        }

        private PlayerKnownName GetKnownNames(uint observer, bool createIfMissing)
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

        private PlayerKnownName FindKnownNames(string observerId)
        {
            var writeVersion = _db.GetWriteVersion<PlayerKnownName>();
            if (writeVersion != _knownNamesWriteVersion)
            {
                KnownNamesByObserverId.Clear();
                _knownNamesWriteVersion = writeVersion;
            }

            if (KnownNamesByObserverId.TryGetValue(observerId, out var dbKnownNames))
                return dbKnownNames;

            FrameProfiler.Count(FrameMetric.PlayerNameLookup);
            dbKnownNames = _db.Search(new DBQuery<PlayerKnownName>()
                    .AddFieldSearch(nameof(PlayerKnownName.ObserverPlayerId), observerId, false))
                .FirstOrDefault();

            KnownNamesByObserverId[observerId] = dbKnownNames;
            return dbKnownNames;
        }

        private void ApplyNameOverridesForPlayer(uint player)
        {
            if (!GetIsObjectValid(player) || !GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            var unknownDisplayName = GetDisplayDescriptor(player);
            var ownDisplayName = ShouldShowOwnDescriptor(player)
                ? BuildDisplayNameWithDescriptor(GetName(player), unknownDisplayName)
                : GetName(player);
            var shouldScrambleAccountName = ShouldScrambleAccountName(player);

            if (!shouldScrambleAccountName)
                RenamePlugin.ClearPCNameOverride(player);

            RenamePlugin.SetPCNameOverride(player, unknownDisplayName, UnknownNamePrefix, GetNameOverrideSuffix(player, true), PlayerNameOverrideType.Default);
            RenamePlugin.SetPCNameOverride(player, ownDisplayName, string.Empty, GetNameOverrideSuffix(player), PlayerNameOverrideType.Default, player);

            if (shouldScrambleAccountName)
                RenamePlugin.SetPCNameOverride(player, unknownDisplayName, UnknownNamePrefix, GetNameOverrideSuffix(player, true), PlayerNameOverrideType.Obfuscate);

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

        private void ApplyNameOverridesForDMObserver(uint dm)
        {
            if (!GetIsObjectValid(dm) || !GetIsPC(dm) || !GetIsDM(dm))
                return;

            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                    continue;

                ApplyTrueNameOverride(dm, player);
            }
        }

        private void ApplyTrueNameOverride(uint observer, uint target)
        {
            if (!GetIsObjectValid(observer) ||
                !GetIsObjectValid(target) ||
                !GetIsPC(observer) ||
                !GetIsPC(target) ||
                GetIsDM(target) ||
                GetIsDMPossessed(target))
            {
                return;
            }

            RenamePlugin.SetPCNameOverride(target, BuildStaffDisplayName(target), string.Empty, GetStaffNameOverrideSuffix(target), PlayerNameOverrideType.Default, observer);
        }

        private void ApplyNameOverride(uint observer, uint target)
        {
            // Possessed NPCs can be player-controlled. NWNX Rename restores their blueprint
            // name after an override, discarding the display name set by the DM's rename tool.
            if (!GetIsObjectValid(observer) ||
                !GetIsObjectValid(target) ||
                !GetIsPC(observer) ||
                !GetIsPC(target) ||
                GetIsDM(observer) ||
                GetIsDM(target) ||
                GetIsDMPossessed(target))
            {
                return;
            }

            var displayName = ResolveDisplayName(observer, target, out var isUnknown);
            var prefix = isUnknown ? UnknownNamePrefix : string.Empty;
            var suffix = GetNameOverrideSuffix(target, isUnknown);

            RenamePlugin.SetPCNameOverride(target, displayName, prefix, suffix, PlayerNameOverrideType.Default, observer);
        }

        private string BuildStaffDisplayName(uint target)
        {
            var trueName = GetName(target);
            var unknownDisplayName = GetDisplayDescriptor(target);

            return $"{trueName} [{ColorToken.Gray(unknownDisplayName)}]";
        }

        private bool CanApplyChatNameOverride(uint observer, uint target)
        {
            return GetIsObjectValid(observer) &&
                   GetIsObjectValid(target) &&
                   GetIsPC(observer) &&
                   GetIsPC(target) &&
                   !GetIsDM(target) &&
                   !GetIsDMPossessed(target);
        }

        private void ApplyChatNameOverride(uint observer, uint target)
        {
            if (!CanApplyChatNameOverride(observer, target))
                return;

            var displayName = ResolveChatDisplayName(observer, target, out var isUnknown);
            var prefix = isUnknown ? UnknownNamePrefix : string.Empty;
            var suffix = GetIsDM(observer) || GetIsDMPossessed(observer)
                ? GetStaffNameOverrideSuffix(target)
                : GetNameOverrideSuffix(target, isUnknown);

            RenamePlugin.SetPCNameOverride(target, displayName, prefix, suffix, PlayerNameOverrideType.Default, observer);
        }

        private void RestoreNameOverride(uint observer, uint target)
        {
            if (!CanApplyChatNameOverride(observer, target))
                return;

            if (observer == target)
            {
                var ownDisplayName = ShouldShowOwnDescriptor(target)
                    ? BuildDisplayNameWithDescriptor(GetName(target), GetDisplayDescriptor(target))
                    : GetName(target);

                RenamePlugin.SetPCNameOverride(target, ownDisplayName, string.Empty, GetNameOverrideSuffix(target), PlayerNameOverrideType.Default, observer);
                return;
            }

            if (GetIsDM(observer) || GetIsDMPossessed(observer))
                ApplyTrueNameOverride(observer, target);
            else
                ApplyNameOverride(observer, target);
        }

        private string ResolveChatDisplayName(uint observer, uint target, out bool isUnknown)
        {
            isUnknown = false;

            if (!GetIsObjectValid(target))
                return string.Empty;

            if (!GetIsPC(target) || GetIsDM(target) || GetIsDMPossessed(target))
                return GetName(target);

            if (!GetIsObjectValid(observer) ||
                observer == target ||
                !GetIsPC(observer))
            {
                return GetName(target);
            }

            if (GetIsDM(observer) || GetIsDMPossessed(observer))
                return BuildStaffDisplayName(target);

            if (TryGetKnownName(observer, target, out var knownName))
                return knownName;

            isUnknown = true;
            return GetDisplayDescriptor(target);
        }

        private string ResolveDisplayName(uint observer, uint target, out bool isUnknown)
        {
            isUnknown = false;

            if (!GetIsObjectValid(target))
                return string.Empty;

            if (!GetIsPC(target) || GetIsDM(target) || GetIsDMPossessed(target))
                return GetName(target);

            if (!GetIsObjectValid(observer) ||
                observer == target)
            {
                return GetName(target);
            }

            if (GetIsDM(observer) || GetIsDMPossessed(observer))
                return BuildDisplayNameWithDescriptor(GetName(target), GetDisplayDescriptor(target));

            if (!GetIsPC(observer))
                return GetName(target);

            if (TryGetKnownName(observer, target, out var knownName))
                return ShouldShowDescriptorForNamedPlayers(observer)
                    ? BuildDisplayNameWithDescriptor(knownName, GetDisplayDescriptor(target))
                    : knownName;

            isUnknown = true;
            return GetDisplayDescriptor(target);
        }

        private string BuildDisplayNameWithDescriptor(string primaryName, string descriptor)
        {
            return $"{primaryName} [{ColorToken.Gray(descriptor)}]";
        }

        private string BuildColoredDisplayNameWithDescriptor(string primaryName, string descriptor)
        {
            return $"{ColorToken.GetPCColor(primaryName)} [{ColorToken.Gray(descriptor)}]";
        }

        private bool CanUseKnownName(uint observer, uint target)
        {
            return GetIsObjectValid(observer) &&
                   GetIsPC(observer) &&
                   observer != target &&
                   !GetIsDM(observer) &&
                   !GetIsDMPossessed(observer);
        }

        public bool ShouldShowDescriptorForNamedPlayers(uint observer)
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

            var dbPlayer = _db.Get<Player>(observerId);
            setting = dbPlayer?.Settings?.ShowDescriptorsForNamedPlayers ?? true;
            if (!_scriptContextHooked)
            {
                _scriptContextHooked = true;
                SubscribeScriptContextEnd(ShowDescriptorsForNamedPlayersByObserverId.Clear);
            }

            ShowDescriptorsForNamedPlayersByObserverId[observerId] = setting;
            return setting;
        }

        public bool ShouldShowOwnDescriptor(uint player)
        {
            if (!GetIsObjectValid(player) ||
                !GetIsPC(player) ||
                GetIsDM(player) ||
                GetIsDMPossessed(player))
            {
                return false;
            }

            var playerId = GetObjectUUID(player);
            if (string.IsNullOrWhiteSpace(playerId))
                return true;

            var dbPlayer = _db.Get<Player>(playerId);
            return dbPlayer?.Settings?.ShowOwnDescriptor ?? true;
        }

        public bool TryGetKnownName(uint observer, uint target, out string knownName)
        {
            var targetId = GetIdentityKey(target);
            return TryGetKnownName(observer, targetId, out knownName);
        }

        private bool TryGetKnownName(uint observer, string targetId, out string knownName)
        {
            knownName = string.Empty;
            var dbKnownNames = GetKnownNames(observer, false);

            return dbKnownNames?.KnownNames != null &&
                   dbKnownNames.KnownNames.TryGetValue(targetId, out knownName) &&
                   !string.IsNullOrWhiteSpace(knownName);
        }

        private int CountDisplayNameMatches(uint observer, string displayName)
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

        /// <summary>Registers a handler for the end of every script context: <c>ServerManager.OnScriptContextEnd</c>.</summary>
        protected virtual void SubscribeScriptContextEnd(Action handler)
        {
            ServerManager.OnScriptContextEnd += handler;
        }

        /// <summary>Runs a task every <paramref name="schedule"/>: <c>Scheduler.ScheduleRepeating</c>.</summary>
        protected virtual IDisposable ScheduleRepeating(Action task, TimeSpan schedule)
        {
            return Scheduler.ScheduleRepeating(task, schedule);
        }

        /// <summary>The active disguise of a player: <c>Disguise.GetActiveDisguise</c>.</summary>
        protected virtual PlayerDisguise GetActiveDisguise(uint player)
        {
            return _disguise.Value.GetActiveDisguise(player);
        }

        /// <summary>The descriptor others see: <c>Disguise.GetDisplayDescriptor</c>.</summary>
        protected virtual string GetDisplayDescriptor(uint player)
        {
            return _disguise.Value.GetDisplayDescriptor(player);
        }

        /// <summary>The identity key of the player's active identity: <c>Disguise.GetIdentityKey</c>.</summary>
        protected virtual string GetIdentityKey(uint player)
        {
            return _disguise.Value.GetIdentityKey(player);
        }

        /// <summary>Whether the account name is scrambled: <c>Disguise.ShouldScrambleAccountName</c>.</summary>
        protected virtual bool ShouldScrambleAccountName(uint player)
        {
            return _disguise.Value.ShouldScrambleAccountName(player);
        }

        /// <summary>Generates a missing base descriptor: <c>PlayerDescriptor.EnsureUnknownDisplayName</c>.</summary>
        protected virtual bool EnsureUnknownDisplayName(uint player)
        {
            return _playerDescriptor.Value.EnsureUnknownDisplayName(player);
        }

        /// <summary>The base descriptor of a stored player: <c>PlayerDescriptor.GetUnknownDisplayNameByPlayerId</c>.</summary>
        protected virtual string GetUnknownDisplayNameByPlayerId(string targetPlayerId)
        {
            return _playerDescriptor.Value.GetUnknownDisplayNameByPlayerId(targetPlayerId);
        }
    }
}
