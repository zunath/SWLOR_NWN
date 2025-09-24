namespace SWLOR.Shared.Domain.Social.Contracts
{
    public interface IMessagingService
    {
        /// <summary>
        /// Sends a message to all nearby players within a certain distance.
        /// </summary>
        /// <param name="sender">The sender of the message.</param>
        /// <param name="message">The message to send to all nearby players.</param>
        /// <param name="range">The range, in meters, to deliver the message. Any creatures outside this range will not see the message.</param>
        void SendMessageNearbyToPlayers(uint sender, string message, float range = 10f);
    }
}
