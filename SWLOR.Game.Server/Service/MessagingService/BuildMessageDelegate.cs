namespace SWLOR.Game.Server.Service.MessagingService
{
    /// <summary>
    /// Builds the text one receiver gets from a nearby broadcast (<c>Messaging.SendMessageNearbyToPlayers</c>), so each
    /// receiver can be shown a different message.
    /// </summary>
    /// <param name="receiver">The player the message is for.</param>
    public delegate string BuildMessageDelegate(uint receiver);
}
