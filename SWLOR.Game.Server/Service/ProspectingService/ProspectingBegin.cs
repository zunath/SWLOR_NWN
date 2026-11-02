namespace SWLOR.Game.Server.Service.ProspectingService
{
    /// <summary>What happened when a player used a harvester on a node.</summary>
    /// <param name="Status">Whether a session began, and if not why.</param>
    /// <param name="Session">The session, when one began.</param>
    public sealed record ProspectingBegin(ProspectingBeginStatus Status, ProspectingSession Session)
    {
        public bool Opened => Status == ProspectingBeginStatus.Opened;
    }
}
