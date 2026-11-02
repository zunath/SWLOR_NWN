namespace SWLOR.Game.Server.Service.TournamentService
{
    /// <summary>
    /// Credits a tournament owes a player when it ends: the prize, or a returned entry fee. The id is the same every
    /// time the settlement is worked out, so writing the debt twice leaves one record.
    /// </summary>
    public sealed record TournamentPayout(string Id, string PlayerId, long Credits, string Reason);
}
