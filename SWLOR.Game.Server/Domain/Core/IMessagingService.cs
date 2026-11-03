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
    }
}
