using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The names the engine shows players, written as name overrides, and what changes them: the overrides applied on entering and
    /// after a refresh, the chat display names and the temporary chat override, the AFK flag and its movement check, and the tell
    /// handler that keeps an ambiguous name from reaching the wrong player. The display names, the known-name caches and the
    /// disguise and descriptor reads are on <see cref="IPlayerNameService"/>. The event handlers run on the container's instance.
    /// </summary>
    public class PlayerNameOverrideService : IPlayerNameOverrideService, IReloadStateAdopter
    {
        [ReloadState(ReloadStateKind.Carried)]
        [ReloadCarriedFrom("SWLOR.Game.Server.Domain.Character.PlayerNameService")]
        private readonly Dictionary<uint, (uint Area, Vector3 Position)> AfkPlayers = new();
        private IDisposable _afkMovementCheck;
        private readonly string UnknownNamePrefix = ColorToken.TokenStart(127, 127, 127);
        private readonly string UnknownNameSuffix = ColorToken.TokenEnd();
        private readonly string StaffDisguiseMarker = " " + ColorToken.Orange("[Disguised]");

        private readonly IPlayerNameService _playerName;
        private readonly IPlayerListRefresh _playerListRefresh;
        private readonly IPortraitTellGuard _portraitTellGuard;

        public PlayerNameOverrideService(
            IPlayerNameService playerName,
            IPlayerListRefresh playerListRefresh,
            IPortraitTellGuard portraitTellGuard)
        {
            _playerName = playerName;
            _playerListRefresh = playerListRefresh;
            _portraitTellGuard = portraitTellGuard;
        }

        [PlayerSession(PlayerReload.LoginOnly,
            "The name overrides stay in the engine, the name caches refill from Redis on use, and the AFK flags are carried.")]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public void ApplyNameOverridesOnEnter()
        {
            var player = GetEnteringObject();

            if (!GetIsPC(player))
                return;

            RemoveAfk(player);
            _playerName.EnsureUnknownDisplayName(player);

            if (GetIsDM(player))
            {
                ApplyNameOverridesForDMObserver(player);
                DelayCommand(1.0f, () => ApplyNameOverridesForDMObserver(player));
                return;
            }

            ApplyNameOverridesForPlayer(player);
            DelayCommand(1.0f, () => ApplyNameOverridesForPlayer(player));
        }

        [PlayerSession(PlayerReload.LogoutOnly,
            "Clears the AFK flag; the flags are carried.")]
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public void ClearAfkOnExit()
        {
            RemoveAfk(GetExitingObject());
        }

        public bool IsAfk(uint player) => AfkPlayers.ContainsKey(player);

        /// <summary>
        /// A hot reload carried the AFK players; the movement check that clears them is started again. A tell's temporary chat name is
        /// put back by a closure the reload drops, so every online player's name overrides are applied again from Redis, as a refresh
        /// does (unchanged ones are rewritten with the same text).
        /// </summary>
        public void OnCarriedStateAdopted()
        {
            if (AfkPlayers.Count > 0)
                _afkMovementCheck ??= ScheduleRepeating(CheckAfkMovement, TimeSpan.FromSeconds(1));

            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (GetIsDM(player))
                    ApplyNameOverridesForDMObserver(player);
                else
                    ApplyNameOverridesForPlayer(player);
            }
        }

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
                   (_playerName.GetActiveDisguise(player) != null ? StaffDisguiseMarker : string.Empty);
        }

        [NWNEventHandler(ScriptName.OnNWNXChat)]
        public void PreventAmbiguousTellTargets()
        {
            var channel = ChatPlugin.GetChannel();
            if (channel != ChatChannel.PlayerTell && channel != ChatChannel.DMTell)
                return;

            var sender = ChatPlugin.GetSender();
            var target = ChatPlugin.GetTarget();

            if (_portraitTellGuard.RefuseStalePortraitTell(sender, target, GetLocalBool(sender, PlayerNameService.DirectTellVariable), out var confirmedPortraitTarget))
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

            var displayName = _playerName.GetDisplayName(sender, target);
            // A Player List tell or a confirmed portrait tell names its target exactly, so a shared name is not ambiguous.
            if (GetLocalBool(sender, PlayerNameService.DirectTellVariable) ||
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

        public string GetChatDisplayName(uint observer, uint target)
        {
            return ResolveChatDisplayName(observer, target, out _);
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

        public string GetColoredChatDisplayName(uint observer, uint target)
        {
            if (!GetIsPC(target) || GetIsDM(target) || GetIsDMPossessed(target))
                return ColorToken.GetNameNPCColor(target);

            var displayName = ResolveChatDisplayName(observer, target, out var isUnknown);
            return isUnknown
                ? ColorToken.Gray(displayName)
                : ColorToken.GetPCColor(displayName);
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
                _playerName.ForgetDescriptorSetting(observerId);

            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (!GetIsPC(player) ||
                    player == observer)
                {
                    continue;
                }

                ApplyNameOverride(observer, player);
            }

            _playerListRefresh.QueueRefresh();
        }

        public void RefreshNameOverridesForPlayer(uint player)
        {
            ApplyNameOverridesForPlayer(player);
            _playerListRefresh.QueueRefresh();
        }

        private void ApplyNameOverridesForPlayer(uint player)
        {
            if (!GetIsObjectValid(player) || !GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            var unknownDisplayName = _playerName.GetDisplayDescriptor(player);
            var ownDisplayName = _playerName.ShouldShowOwnDescriptor(player)
                ? _playerName.BuildDisplayNameWithDescriptor(GetName(player), unknownDisplayName)
                : GetName(player);
            var shouldScrambleAccountName = _playerName.ShouldScrambleAccountName(player);

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

            RenamePlugin.SetPCNameOverride(target, _playerName.BuildStaffDisplayName(target), string.Empty, GetStaffNameOverrideSuffix(target), PlayerNameOverrideType.Default, observer);
        }

        public void ApplyNameOverride(uint observer, uint target)
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

            var displayName = _playerName.ResolveDisplayName(observer, target, out var isUnknown);
            var prefix = isUnknown ? UnknownNamePrefix : string.Empty;
            var suffix = GetNameOverrideSuffix(target, isUnknown);

            RenamePlugin.SetPCNameOverride(target, displayName, prefix, suffix, PlayerNameOverrideType.Default, observer);
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
                var ownDisplayName = _playerName.ShouldShowOwnDescriptor(target)
                    ? _playerName.BuildDisplayNameWithDescriptor(GetName(target), _playerName.GetDisplayDescriptor(target))
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
                return _playerName.BuildStaffDisplayName(target);

            if (_playerName.TryGetKnownName(observer, target, out var knownName))
                return knownName;

            isUnknown = true;
            return _playerName.GetDisplayDescriptor(target);
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

                if (string.Equals(_playerName.GetDisplayName(observer, player), displayName, StringComparison.OrdinalIgnoreCase))
                    count++;
            }

            return count;
        }

        /// <summary>Runs a task every <paramref name="schedule"/>: <c>Scheduler.ScheduleRepeating</c>.</summary>
        protected virtual IDisposable ScheduleRepeating(Action task, TimeSpan schedule)
        {
            return Scheduler.ScheduleRepeating(task, schedule);
        }
    }
}
