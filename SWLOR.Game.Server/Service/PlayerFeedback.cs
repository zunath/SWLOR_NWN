using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Optional calculations and tester feedback can be hidden in Production.
    /// Gameplay outcomes, state changes, progression, and errors use the normal message APIs,
    /// even when they occur frequently. Frequency alone does not make a message diagnostic.
    /// </summary>
    public static class PlayerFeedback
    {
        public static bool DiagnosticsEnabled => AreDiagnosticsEnabled(
            ApplicationSettings.Get().ServerEnvironment,
            ApplicationSettings.Get().ServerEnvironmentIsExplicit);

        public static bool AreDiagnosticsEnabled(ServerEnvironmentType environment, bool isExplicit)
        {
            return isExplicit &&
                   (environment == ServerEnvironmentType.Test || environment == ServerEnvironmentType.Development);
        }

        public static void SendDiagnosticToPlayer(uint player, string message)
        {
            if (DiagnosticsEnabled)
                SendMessageToPC(player, message);
        }

        public static void ShowDiagnosticFloatingText(string message, uint creature, bool displayToFaction = false)
        {
            if (DiagnosticsEnabled)
                FloatingTextStringOnCreature(message, creature, displayToFaction);
        }

        /// <summary>
        /// Automatic warnings must remain visible without repeating every hit/XP tick.
        /// Store the timestamp on the creature so it goes away with the object.
        /// </summary>
        public static void SendWarningToPlayer(uint player, string warningKey, string message, int intervalSeconds = 60)
        {
            if (!GetIsPC(player) || !TryBeginWarning(player, warningKey, intervalSeconds))
                return;

            SendMessageToPC(player, message);
        }

        /// <summary>
        /// Reports a creature's state to nearby players, including state changes on NPCs.
        /// Repeated notices share a limit on the source creature rather than on each observer.
        /// </summary>
        public static void SendWarningNearby(uint creature, string warningKey, Messaging.BuildMessageDelegate buildMessage,
            int intervalSeconds = 60, float range = 10f)
        {
            if (!GetIsObjectValid(creature) || buildMessage == null || !TryBeginWarning(creature, warningKey, intervalSeconds))
                return;

            Messaging.SendMessageNearbyToPlayers(creature, buildMessage, range);
        }

        private static bool TryBeginWarning(uint creature, string warningKey, int intervalSeconds)
        {
            var variable = "FEEDBACK_WARNING_" + warningKey;
            var now = DateTime.UtcNow.Ticks;
            long.TryParse(GetLocalString(creature, variable), out var lastSent);
            if (!DiagnosticsEnabled && !IsWarningDue(now, lastSent, intervalSeconds))
                return false;

            SetLocalString(creature, variable, now.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return true;
        }

        public static bool IsWarningDue(long now, long lastSent, int intervalSeconds)
        {
            return lastSent <= 0 || now < lastSent ||
                   now - lastSent >= TimeSpan.FromSeconds(Math.Max(0, intervalSeconds)).Ticks;
        }

        public static string BuildResourceRestoredMessage(int restored, string resource)
        {
            return restored > 0 ? $"Restored {restored} {resource}." : null;
        }

        public static void SendResourceRestored(uint creature, int restored, string resource)
        {
            if (!DiagnosticsEnabled || restored <= 0 || !GetIsObjectValid(creature))
                return;

            var receiver = GetIsPC(creature) ? creature : GetMaster(creature);
            if (!GetIsObjectValid(receiver) || !GetIsPC(receiver) || GetIsDM(receiver))
                return;

            var message = BuildResourceRestoredMessage(restored, resource);
            if (receiver != creature)
                message = $"{PlayerName.GetDisplayName(receiver, creature)}: {message}";
            SendMessageToPC(receiver, ColorToken.Combat(message));
        }
    }
}
