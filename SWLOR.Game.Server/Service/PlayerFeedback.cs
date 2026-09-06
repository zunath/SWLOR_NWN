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
        /// Store the timestamp on the player so it goes away with the session.
        /// </summary>
        public static void SendWarningToPlayer(uint player, string warningKey, string message, int intervalSeconds = 60)
        {
            if (!GetIsPC(player))
                return;

            var variable = "PLAYER_WARNING_" + warningKey;
            var now = DateTime.UtcNow.Ticks;
            long.TryParse(GetLocalString(player, variable), out var lastSent);
            if (!DiagnosticsEnabled && !IsWarningDue(now, lastSent, intervalSeconds))
                return;

            SetLocalString(player, variable, now.ToString(System.Globalization.CultureInfo.InvariantCulture));
            SendMessageToPC(player, message);
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
            if (!DiagnosticsEnabled || restored <= 0 || !GetIsPC(creature) || GetIsDM(creature))
                return;

            SendMessageToPC(creature, ColorToken.Combat(BuildResourceRestoredMessage(restored, resource)));
        }
    }
}
