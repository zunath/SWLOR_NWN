using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.TournamentService
{
    /// <summary>
    /// What a finished tournament owes: the credits to pay players, and the entry fees that stay with the city.
    /// <paramref name="ReleasedEscrow"/> is true when the city's escrow entry was released, so the city record needs saving.
    /// </summary>
    public sealed record TournamentSettlement(IReadOnlyList<TournamentPayout> Payouts, long FeesToTreasury, bool ReleasedEscrow = false);
}
