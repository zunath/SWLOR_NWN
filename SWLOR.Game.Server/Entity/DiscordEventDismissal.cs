namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A Discord event a DM cancelled or ended in game, keyed by its Discord ID, so the next sync does not list it again.
    /// It covers one occurrence: a recurring Discord event that moves to its next start time is listed again.
    /// </summary>
    public class DiscordEventDismissal : EntityBase
    {
        public DateTime ScheduledStartUtc { get; set; }
    }
}
