using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.MessagingService;

namespace SWLOR.Game.Server.Domain.Core
{
    /// <summary>
    /// Optional calculations and tester feedback can be hidden in Production.
    /// Gameplay outcomes, state changes, progression, and errors use the normal message APIs,
    /// even when they occur frequently. Frequency alone does not make a message diagnostic.
    /// The static facade <c>Service.PlayerFeedback</c> forwards to the container's instance of this interface.
    /// </summary>
    public interface IPlayerFeedbackService
    {
        /// <summary>True when the environment is explicitly Testing or Development, where the optional messages are sent.</summary>
        bool DiagnosticsEnabled { get; }

        /// <summary>
        /// Whether a server environment shows the optional messages: only an explicit Testing or Development one does.
        /// </summary>
        /// <param name="environment">The server environment.</param>
        /// <param name="isExplicit">True when the environment was named by the settings, not defaulted.</param>
        bool AreDiagnosticsEnabled(ServerEnvironmentType environment, bool isExplicit);

        /// <summary>Sends an optional message to a player. Nothing is sent outside Testing and Development.</summary>
        void SendDiagnosticToPlayer(uint player, string message);

        /// <summary>Shows optional floating text on a creature. Nothing is shown outside Testing and Development.</summary>
        void ShowDiagnosticFloatingText(string message, uint creature, bool displayToFaction = false);

        /// <summary>
        /// Optional, private cooldown feedback shares one throttle across every ability.
        /// The normal denial stays in chat; this notification never adds another chat entry.
        /// </summary>
        bool ShowCooldownFloatingText(uint player, string abilityName, string timeToWait);

        /// <summary>
        /// Automatic warnings must remain visible without repeating every hit/XP tick.
        /// Store the timestamp on the creature so it goes away with the object.
        /// </summary>
        void SendWarningToPlayer(uint player, string warningKey, string message, int intervalSeconds = 60);

        /// <summary>
        /// Reports a creature's state to nearby players, including state changes on NPCs.
        /// Repeated notices share a limit on the source creature rather than on each observer.
        /// </summary>
        void SendWarningNearby(uint creature, string warningKey, BuildMessageDelegate buildMessage,
            int intervalSeconds = 60, float range = 10f);

        /// <summary>
        /// Whether a warning last sent at <paramref name="lastSent"/> (ticks) may be sent again at <paramref name="now"/>.
        /// A warning never sent, or a clock that moved backwards, is due.
        /// </summary>
        bool IsWarningDue(long now, long lastSent, int intervalSeconds);

        /// <summary>The message for a restored resource, or null when nothing was restored.</summary>
        string BuildResourceRestoredMessage(int restored, string resource);

        /// <summary>Tells a player (or a companion's owner) how much of a resource was restored. Testing and Development only.</summary>
        void SendResourceRestored(uint creature, int restored, string resource);
    }
}
