namespace SWLOR.Game.Server.Service.ScavengeService
{
    /// <summary>One attempt of a Quick Search: the d20 it rolled against its DC, and the XP it paid.</summary>
    public sealed record QuickSearchAttempt(int Roll, int Dc, bool Success, int Xp);
}
