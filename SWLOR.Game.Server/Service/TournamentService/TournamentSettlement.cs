using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.TournamentService
{
    /// <summary>
    /// What a finished tournament owes: the credits to pay players, and the entry fees that stay with the city.
    /// </summary>
    public sealed record TournamentSettlement(IReadOnlyList<TournamentPayout> Payouts, long FeesToTreasury);
}
