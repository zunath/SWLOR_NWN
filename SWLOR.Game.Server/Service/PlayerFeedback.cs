using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.MessagingService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Optional calculations and tester feedback can be hidden in Production.
    /// Gameplay outcomes, state changes, progression, and errors use the normal message APIs,
    /// even when they occur frequently. Frequency alone does not make a message diagnostic.
    /// The logic lives in <see cref="IPlayerFeedbackService"/>; every member here forwards to the container's instance.
    /// </summary>
    public static class PlayerFeedback
    {
        private static IPlayerFeedbackService Instance => FacadeAccessor.Resolve<IPlayerFeedbackService>();

        public static bool DiagnosticsEnabled => Instance.DiagnosticsEnabled;

        public static bool AreDiagnosticsEnabled(ServerEnvironmentType environment, bool isExplicit)
        {
            return Instance.AreDiagnosticsEnabled(environment, isExplicit);
        }

        public static void SendDiagnosticToPlayer(uint player, string message)
        {
            Instance.SendDiagnosticToPlayer(player, message);
        }

        public static void ShowDiagnosticFloatingText(string message, uint creature, bool displayToFaction = false)
        {
            Instance.ShowDiagnosticFloatingText(message, creature, displayToFaction);
        }

        /// <summary>
        /// Optional, private cooldown feedback shares one throttle across every ability.
        /// The normal denial stays in chat; this notification never adds another chat entry.
        /// </summary>
        public static bool ShowCooldownFloatingText(uint player, string abilityName, string timeToWait)
        {
            return Instance.ShowCooldownFloatingText(player, abilityName, timeToWait);
        }

        /// <summary>
        /// Automatic warnings must remain visible without repeating every hit/XP tick.
        /// Store the timestamp on the creature so it goes away with the object.
        /// </summary>
        public static void SendWarningToPlayer(uint player, string warningKey, string message, int intervalSeconds = 60)
        {
            Instance.SendWarningToPlayer(player, warningKey, message, intervalSeconds);
        }

        /// <summary>
        /// Reports a creature's state to nearby players, including state changes on NPCs.
        /// Repeated notices share a limit on the source creature rather than on each observer.
        /// </summary>
        public static void SendWarningNearby(uint creature, string warningKey, BuildMessageDelegate buildMessage,
            int intervalSeconds = 60, float range = 10f)
        {
            Instance.SendWarningNearby(creature, warningKey, buildMessage, intervalSeconds, range);
        }

        public static bool IsWarningDue(long now, long lastSent, int intervalSeconds)
        {
            return Instance.IsWarningDue(now, lastSent, intervalSeconds);
        }

        public static string BuildResourceRestoredMessage(int restored, string resource)
        {
            return Instance.BuildResourceRestoredMessage(restored, resource);
        }

        public static void SendResourceRestored(uint creature, int restored, string resource)
        {
            Instance.SendResourceRestored(creature, restored, resource);
        }
    }
}
