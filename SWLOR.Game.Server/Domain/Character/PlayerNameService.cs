using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// What players see as each other's names and the known names they assign: the logic and state
    /// that used to be static in <c>Service.PlayerName</c>, moved here unchanged. The name overrides, the chat display
    /// names, the AFK flag and the tell handler are on <see cref="IPlayerNameOverrideService"/>. The event handlers run on the
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
        private bool _scriptContextHooked;

        private readonly IDBService _db;
        private readonly Lazy<IDisguiseService> _disguise;
        private readonly Lazy<IPlayerDescriptorService> _playerDescriptor;

        public PlayerNameService(
            IDBService db,
            Lazy<IDisguiseService> disguise,
            Lazy<IPlayerDescriptorService> playerDescriptor)
        {
            _db = db;
            _disguise = disguise;
            _playerDescriptor = playerDescriptor;
        }

        [PlayerSession(PlayerReload.LogoutOnly,
            "Drops the player's name cache, which goes with the old build.")]
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public void ReleaseKnownNamesOnExit()
        {
            var observerId = GetObjectUUID(GetExitingObject());
            if (!string.IsNullOrWhiteSpace(observerId))
                KnownNamesByObserverId.Remove(observerId);
        }

        public string GetDisplayName(uint observer, uint target)
        {
            return ResolveDisplayName(observer, target, out _);
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

        public PlayerKnownName GetKnownNames(uint observer, bool createIfMissing)
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

        /// <summary>Drops the cached known names of an observer, after another service rewrote the stored record.</summary>
        public void ForgetKnownNames(string observerId)
        {
            KnownNamesByObserverId.Remove(observerId);
        }

        /// <summary>Drops the cached descriptor setting of an observer, so the next read comes from the player record.</summary>
        public void ForgetDescriptorSetting(string observerId)
        {
            ShowDescriptorsForNamedPlayersByObserverId.Remove(observerId);
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

        public string BuildStaffDisplayName(uint target)
        {
            var trueName = GetName(target);
            var unknownDisplayName = GetDisplayDescriptor(target);

            return $"{trueName} [{ColorToken.Gray(unknownDisplayName)}]";
        }

        public string ResolveDisplayName(uint observer, uint target, out bool isUnknown)
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

        public string BuildDisplayNameWithDescriptor(string primaryName, string descriptor)
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

        /// <summary>Registers a handler for the end of every script context: <c>ServerManager.OnScriptContextEnd</c>.</summary>
        protected virtual void SubscribeScriptContextEnd(Action handler)
        {
            ServerManager.OnScriptContextEnd += handler;
        }

        /// <summary>The active disguise of a player: <c>Disguise.GetActiveDisguise</c>.</summary>
        public virtual PlayerDisguise GetActiveDisguise(uint player)
        {
            return _disguise.Value.GetActiveDisguise(player);
        }

        /// <summary>The descriptor others see: <c>Disguise.GetDisplayDescriptor</c>.</summary>
        public virtual string GetDisplayDescriptor(uint player)
        {
            return _disguise.Value.GetDisplayDescriptor(player);
        }

        /// <summary>The identity key of the player's active identity: <c>Disguise.GetIdentityKey</c>.</summary>
        public virtual string GetIdentityKey(uint player)
        {
            return _disguise.Value.GetIdentityKey(player);
        }

        /// <summary>Whether an identity key names a disguise: <c>Disguise.IsDisguiseIdentityKey</c>.</summary>
        public virtual bool IsDisguiseIdentityKey(string identityKey)
        {
            return _disguise.Value.IsDisguiseIdentityKey(identityKey);
        }

        /// <summary>Whether the account name is scrambled: <c>Disguise.ShouldScrambleAccountName</c>.</summary>
        public virtual bool ShouldScrambleAccountName(uint player)
        {
            return _disguise.Value.ShouldScrambleAccountName(player);
        }

        /// <summary>Generates a missing base descriptor: <c>PlayerDescriptor.EnsureUnknownDisplayName</c>.</summary>
        public virtual bool EnsureUnknownDisplayName(uint player)
        {
            return _playerDescriptor.Value.EnsureUnknownDisplayName(player);
        }

        /// <summary>The base descriptor of a stored player: <c>PlayerDescriptor.GetUnknownDisplayNameByPlayerId</c>.</summary>
        public virtual string GetUnknownDisplayNameByPlayerId(string targetPlayerId)
        {
            return _playerDescriptor.Value.GetUnknownDisplayNameByPlayerId(targetPlayerId);
        }
    }
}
