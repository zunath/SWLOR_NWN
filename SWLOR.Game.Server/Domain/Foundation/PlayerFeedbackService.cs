using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// Optional calculations and tester feedback can be hidden in Production.
    /// Gameplay outcomes, state changes, progression, and errors use the normal message APIs,
    /// even when they occur frequently. Frequency alone does not make a message diagnostic.
    /// The logic that used to be static in the <c>PlayerFeedback</c> facade, moved here unchanged. Static callers reach
    /// the container's instance through <c>GameServices.PlayerFeedback</c>.
    /// </summary>
    public class PlayerFeedbackService : IPlayerFeedbackService
    {
        private readonly IDBService _db;
        private readonly IMessagingService _messaging;
        private readonly IPlayerNameService _playerName;

        public PlayerFeedbackService(IDBService db, IMessagingService messaging, IPlayerNameService playerName)
        {
            _db = db;
            _messaging = messaging;
            _playerName = playerName;
        }

        public bool DiagnosticsEnabled => AreDiagnosticsEnabled(
            GetSettings().ServerEnvironment,
            GetSettings().ServerEnvironmentIsExplicit);

        public bool AreDiagnosticsEnabled(ServerEnvironmentType environment, bool isExplicit)
        {
            return isExplicit &&
                   (environment == ServerEnvironmentType.Test || environment == ServerEnvironmentType.Development);
        }

        public void SendDiagnosticToPlayer(uint player, string message)
        {
            if (DiagnosticsEnabled)
                SendMessageToPC(player, message);
        }

        public void ShowDiagnosticFloatingText(string message, uint creature, bool displayToFaction = false)
        {
            if (DiagnosticsEnabled)
                FloatingTextStringOnCreature(message, creature, displayToFaction);
        }

        /// <summary>
        /// Optional, private cooldown feedback shares one throttle across every ability.
        /// The normal denial stays in chat; this notification never adds another chat entry.
        /// </summary>
        public bool ShowCooldownFloatingText(uint player, string abilityName, string timeToWait)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return false;

            var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
            if (dbPlayer?.Settings?.DisplayCooldownFloatingText != true)
                return false;

            const string lastSentVariable = "FEEDBACK_COOLDOWN_FLOATING_LAST_SENT";
            var now = UtcNowTicks();
            long.TryParse(GetLocalString(player, lastSentVariable), out var lastSent);
            if (!IsWarningDue(now, lastSent, 2))
                return false;

            SetLocalString(player, lastSentVariable, now.ToString(System.Globalization.CultureInfo.InvariantCulture));
            PlayerPlugin.FloatingTextStringOnCreature(player, player, ColorToken.Yellow($"{abilityName}: {timeToWait}"), false);
            return true;
        }

        /// <summary>
        /// Automatic warnings must remain visible without repeating every hit/XP tick.
        /// Store the timestamp on the creature so it goes away with the object.
        /// </summary>
        public void SendWarningToPlayer(uint player, string warningKey, string message, int intervalSeconds = 60)
        {
            if (!GetIsPC(player) || !TryBeginWarning(player, warningKey, intervalSeconds))
                return;

            SendMessageToPC(player, message);
        }

        /// <summary>
        /// Reports a creature's state to nearby players, including state changes on NPCs.
        /// Repeated notices share a limit on the source creature rather than on each observer.
        /// </summary>
        public void SendWarningNearby(uint creature, string warningKey, BuildMessageDelegate buildMessage,
            int intervalSeconds = 60, float range = 10f)
        {
            if (!GetIsObjectValid(creature) || buildMessage == null || !TryBeginWarning(creature, warningKey, intervalSeconds))
                return;

            _messaging.SendMessageNearbyToPlayers(creature, buildMessage, range);
        }

        private bool TryBeginWarning(uint creature, string warningKey, int intervalSeconds)
        {
            var variable = "FEEDBACK_WARNING_" + warningKey;
            var now = UtcNowTicks();
            long.TryParse(GetLocalString(creature, variable), out var lastSent);
            if (!DiagnosticsEnabled && !IsWarningDue(now, lastSent, intervalSeconds))
                return false;

            SetLocalString(creature, variable, now.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return true;
        }

        public bool IsWarningDue(long now, long lastSent, int intervalSeconds)
        {
            return lastSent <= 0 || now < lastSent ||
                   now - lastSent >= TimeSpan.FromSeconds(Math.Max(0, intervalSeconds)).Ticks;
        }

        public string BuildResourceRestoredMessage(int restored, string resource)
        {
            return restored > 0 ? $"Restored {restored} {resource}." : null;
        }

        public void SendResourceRestored(uint creature, int restored, string resource)
        {
            if (!DiagnosticsEnabled || restored <= 0 || !GetIsObjectValid(creature))
                return;

            var receiver = GetIsPC(creature) ? creature : GetMaster(creature);
            if (!GetIsObjectValid(receiver) || !GetIsPC(receiver) || GetIsDM(receiver))
                return;

            var message = BuildResourceRestoredMessage(restored, resource);
            if (receiver != creature)
                message = $"{GetDisplayName(receiver, creature)}: {message}";
            SendMessageToPC(receiver, ColorToken.Combat(message));
        }

        /// <summary>The server settings. A test answers its own.</summary>
        protected virtual ApplicationSettings GetSettings()
        {
            return ApplicationSettings.Get();
        }

        /// <summary>The current time in ticks. A test answers its own.</summary>
        protected virtual long UtcNowTicks()
        {
            return DateTime.UtcNow.Ticks;
        }

        /// <summary>How <paramref name="viewer"/> sees <paramref name="target"/>. <c>PlayerName</c> reads the static database for a player.</summary>
        protected virtual string GetDisplayName(uint viewer, uint target)
        {
            return _playerName.GetDisplayName(viewer, target);
        }
    }
}
