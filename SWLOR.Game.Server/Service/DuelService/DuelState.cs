namespace SWLOR.Game.Server.Service.DuelService
{
    /// <summary>
    /// Where a duel stands. A duel is Active until a third party's blow voids it; it is then Interrupted, and the
    /// duelists stay protected until <see cref="DuelRecord.ProtectedUntilUtc"/>.
    /// </summary>
    public enum DuelState
    {
        Active = 1,
        Interrupted = 2
    }
}
