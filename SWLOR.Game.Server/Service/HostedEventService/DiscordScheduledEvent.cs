namespace SWLOR.Game.Server.Service.HostedEventService
{
    /// <summary>
    /// One entry from the Discord server's Events tab, with its text already made safe for the game.
    /// </summary>
    public class DiscordScheduledEvent
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public DateTime StartUtc { get; set; }
    }
}
