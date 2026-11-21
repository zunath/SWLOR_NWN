using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PlayerListService;
using SWLOR.Game.Server.Service.PvpCombatService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The PvP combat window (Player City Revamp, "Conflict and PvP"). Any PvP damage a player deals or takes holds their
    /// window open for <see cref="PvpCombatWindowRules.WindowSeconds"/>. While it is open a request to turn the PvP flag off
    /// or to change the identity shown is queued (the requested state) and takes effect when the window ends (the effective
    /// state), so nobody escapes a bounty or gives away a disguise mid-fight. Windows are held in memory by player id, so
    /// logging out and back in does not close one; a restart closes them all. The queued requests are saved on the player
    /// and applied at the next login if the player was away when the window ended. The rules live in
    /// <see cref="PvpCombatWindowRules"/>.
    /// </summary>
    public static class PvpCombatWindow
    {
        private static readonly Dictionary<string, DateTime> _openUntil = new();
        private static IDisposable _schedule;

        /// <summary>Delay after entering before a login message shows: the client needs a moment before chat does.</summary>
        private const float LoginDelaySeconds = 5f;

        // The window

        /// <summary>
        /// Holds a player's window open after PvP damage dealt or taken. Only records the time, since it runs while damage is
        /// being calculated.
        /// </summary>
        public static void Open(uint player)
        {
            if (!PvpCombat.IsPlayer(player))
                return;

            var playerId = GetObjectUUID(player);
            if (string.IsNullOrWhiteSpace(playerId))
                return;

            var now = DateTime.UtcNow;
            _openUntil[playerId] = PvpCombatWindowRules.Extend(_openUntil.GetValueOrDefault(playerId), now);
            _schedule ??= Scheduler.ScheduleRepeating(Tick, TimeSpan.FromSeconds(1));
        }

        public static bool IsOpen(string playerId)
        {
            return !string.IsNullOrWhiteSpace(playerId) &&
                   _openUntil.TryGetValue(playerId, out var until) &&
                   PvpCombatWindowRules.IsOpen(until, DateTime.UtcNow);
        }

        public static bool IsOpen(uint player)
        {
            return IsOpen(GetObjectUUID(player));
        }

        /// <summary>Whole seconds until the player's window closes, or 0 when it is closed.</summary>
        public static int GetSecondsLeft(string playerId)
        {
            return !string.IsNullOrWhiteSpace(playerId) && _openUntil.TryGetValue(playerId, out var until)
                ? PvpCombatWindowRules.GetSecondsLeft(until, DateTime.UtcNow)
                : 0;
        }

        /// <summary>Closes a player's window at once. For tests only.</summary>
        public static void CloseForTesting(string playerId)
        {
            _openUntil.Remove(playerId);
        }

        // Requests

        /// <summary>
        /// Handles a request to turn the PvP flag on or off for the identity the player shows. A turn off while the window
        /// is open is queued, and a turn on undoes a queued turn off.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="dbPlayer">The player's record, which this changes and saves when a request is queued or undone.</param>
        /// <param name="flagIsOn">Whether the flag is in effect for the shown identity.</param>
        /// <param name="wantOn">Whether the player asked for it on.</param>
        /// <returns>True when the request was dealt with here and nothing more should change now. False when the change goes ahead.</returns>
        public static bool HoldFlagChange(uint player, Player dbPlayer, bool flagIsOn, bool wantOn)
        {
            var requests = dbPlayer.PvpCombatRequests ??= new PvpCombatRequests();
            var wasQueued = requests.IsFlagOffQueued;
            var outcome = PvpCombatWindowRules.RequestFlag(IsOpen(dbPlayer.Id), flagIsOn, wantOn, requests);
            switch (outcome)
            {
                case PvpCombatChangeOutcome.Queued:
                    DB.Set(dbPlayer);
                    SendMessageToPC(player, ColorToken.Combat(
                        $"You are in PvP combat, so your PvP flag stays on. It turns off when PvP combat ends, in about {GetSecondsLeft(dbPlayer.Id)} seconds."));
                    return true;
                case PvpCombatChangeOutcome.Cancelled:
                    DB.Set(dbPlayer);
                    SendMessageToPC(player, "Your request to turn the PvP flag off was cancelled. It stays on.");
                    return true;
                case PvpCombatChangeOutcome.Applied:
                    return false;
                default:
                    if (wasQueued && !wantOn)
                        SendMessageToPC(player, ColorToken.Combat("Your request to turn the PvP flag off is already waiting for PvP combat to end."));
                    return true;
            }
        }

        /// <summary>
        /// Handles a request to show another identity: a disguise id, or empty for the player's own character. During the
        /// window the change is queued.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="dbPlayer">The player's record, which this changes and saves when a request is queued or undone.</param>
        /// <param name="wantedDisguiseId">The disguise asked for, or empty.</param>
        /// <param name="message">What to tell the player when the change is queued or undone.</param>
        /// <returns>What the request amounts to. Applied means the caller makes the change now.</returns>
        public static PvpCombatChangeOutcome RequestIdentity(uint player, Player dbPlayer, string wantedDisguiseId, out string message)
        {
            var requests = dbPlayer.PvpCombatRequests ??= new PvpCombatRequests();
            var outcome = PvpCombatWindowRules.RequestIdentity(
                IsOpen(dbPlayer.Id), dbPlayer.ActiveDisguiseId, wantedDisguiseId, requests);

            message = string.Empty;
            switch (outcome)
            {
                case PvpCombatChangeOutcome.Queued:
                    DB.Set(dbPlayer);
                    message = $"You are in PvP combat, so you keep your current identity. It changes when PvP combat ends, in about {GetSecondsLeft(dbPlayer.Id)} seconds.";
                    break;
                case PvpCombatChangeOutcome.Cancelled:
                    DB.Set(dbPlayer);
                    message = "Your request to change identity was cancelled. You keep your current identity.";
                    break;
            }

            return outcome;
        }

        /// <summary>Undoes a waiting identity change, such as when the player asks for the identity they already show.</summary>
        public static void CancelQueuedIdentity(uint player, Player dbPlayer)
        {
            if (RequestIdentity(player, dbPlayer, dbPlayer.ActiveDisguiseId, out var message) == PvpCombatChangeOutcome.Cancelled)
                SendMessageToPC(player, message);
        }

        // Applying

        /// <summary>
        /// Applies the requests a player made during the window once it has closed: the PvP flag goes off first, while the
        /// identity that asked for it is still the one shown, then the identity changes.
        /// </summary>
        public static void ApplyQueued(uint player)
        {
            if (!PvpCombat.IsPlayer(player))
                return;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            var requests = dbPlayer?.PvpCombatRequests;
            if (requests == null || !PvpCombatWindowRules.ShouldApplyQueued(requests, IsOpen(dbPlayer.Id)))
                return;

            var turnFlagOff = requests.IsFlagOffQueued;
            var changeIdentity = requests.IsIdentityChangeQueued;
            var disguiseId = requests.QueuedDisguiseId;

            // Cleared and saved first, so the changes below find nothing waiting and go ahead.
            requests.Clear();
            DB.Set(dbPlayer);

            if (turnFlagOff)
            {
                PlayerList.SetFlag(player, PlayerListFlagType.PvP, false);
                SendMessageToPC(player, "PvP combat has ended. Your PvP flag is now off.");
            }

            if (!changeIdentity)
                return;

            if (string.IsNullOrWhiteSpace(disguiseId))
            {
                if (Disguise.Deactivate(player))
                    SendMessageToPC(player, "PvP combat has ended. Your disguise is now off.");
                return;
            }

            var result = Disguise.Activate(player, disguiseId);
            SendMessageToPC(player, result.IsSuccessful
                ? "PvP combat has ended. Your disguise is now on."
                : result.ErrorMessage);
        }

        private static void Tick()
        {
            var now = DateTime.UtcNow;
            foreach (var (playerId, until) in _openUntil.ToList())
            {
                if (PvpCombatWindowRules.IsOpen(until, now))
                    continue;

                _openUntil.Remove(playerId);
                var player = GetObjectByUUID(playerId);
                if (GetIsObjectValid(player))
                    ApplyQueued(player);
            }

            if (_openUntil.Count == 0)
            {
                _schedule?.Dispose();
                _schedule = null;
            }
        }

        /// <summary>A player who was away when their window ended, or when the server restarted, gets their waiting requests now.</summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void ApplyQueuedOnLogin()
        {
            var player = GetEnteringObject();
            if (!PvpCombat.IsPlayer(player))
                return;

            DelayCommand(LoginDelaySeconds, () =>
            {
                if (GetIsObjectValid(player))
                    ApplyQueued(player);
            });
        }
    }
}
