using SWLOR.Game.Server.Service.MessagingService;

namespace SWLOR.Game.Server.Domain.Core
{
    /// <summary>
    /// Messages to the players near a creature. The static facade <c>Service.Messaging</c> forwards to the container's
    /// instance of this interface.
    /// </summary>
    public interface IMessagingService
    {
        /// <summary>
        /// Whether a nearby broadcast may reach the players around the sender: neither the sender nor the actor the
        /// message names may be concealed.
        /// </summary>
        /// <param name="senderConcealed">Whether the sender is in stealth or invisible.</param>
        /// <param name="actorConcealed">Whether the actor the message names is in stealth or invisible.</param>
        bool ShouldDeliverNearby(bool senderConcealed, bool actorConcealed);

        /// <summary>
        /// Floats text over a creature for every player within range. A creature in stealth or invisible only shows it to itself.
        /// </summary>
        void FloatTextNearbyToPlayers(uint creature, string text, float range = 30f);

        /// <summary>
        /// Sends a message to all nearby players within a certain distance.
        /// </summary>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="message">The message to send to all nearby players.</param>
        /// <param name="range">The range, in meters, to deliver the message. Any creatures outside this range will not see the message.</param>
        void SendMessageNearbyToPlayers(uint sender, string message, float range = 10f);

        /// <summary>
        /// Sends each nearby player within a certain distance the message built for that player.
        /// </summary>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="buildMessage">Builds the message for one receiver. The sender gets the message built for the sender.</param>
        /// <param name="range">The range, in meters, to deliver the message. Any creatures outside this range will not see the message.</param>
        void SendMessageNearbyToPlayers(uint sender, BuildMessageDelegate buildMessage, float range = 10f);

        /// <summary>
        /// Sends a message to the sender and every player within range. While the sender, or the
        /// <paramref name="concealedActor"/> the message names, is in stealth or invisible, the message
        /// goes only to the sender and that actor instead, so it never tells nearby players a hidden
        /// creature is there.
        /// </summary>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="buildMessage">Builds the message for one receiver.</param>
        /// <param name="range">The range, in meters, to deliver the message.</param>
        /// <param name="concealedActor">The actor the message names, or OBJECT_INVALID.</param>
        void SendMessageNearbyToPlayers(uint sender, BuildMessageDelegate buildMessage, float range, uint concealedActor);
    }
}
