using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityTreasuryService;

namespace SWLOR.Game.Server.Service.TournamentService
{
    /// <summary>
    /// Who is owed what when a tournament ends, with no database or engine calls. The prize and the fees are already
    /// out of the players' hands and the city's balances, held in escrow (<see cref="CityEscrowEntry"/>); this works out
    /// where they go. The prize is paid to the champion, and a prize that is not won goes back to the balance it came
    /// from through the escrow refund. Entry fees go back to the entrants when the tournament is cancelled or its city is
    /// removed, and to the city's ordinary treasury when the tournament is completed.
    /// </summary>
    public static class TournamentSettlementRules
    {
        /// <summary>
        /// The fee each player paid. The escrow entry and the entrant's own record each hold it; a restart can fall between
        /// the two writes, so the larger of the two stands, and a fee held for someone who is not an entrant is still theirs.
        /// </summary>
        public static Dictionary<string, long> GetFeesPaid(CityTournament tournament, CityEscrowEntry escrow)
        {
            var fees = new Dictionary<string, long>();
            foreach (var entrant in tournament.Entrants)
            {
                if (entrant.FeePaid > 0)
                    fees[entrant.PlayerId] = entrant.FeePaid;
            }

            if (escrow != null)
            {
                foreach (var (playerId, fee) in escrow.PlayerFees)
                {
                    if (fee > 0 && fee > fees.GetValueOrDefault(playerId))
                        fees[playerId] = fee;
                }
            }

            return fees;
        }

        /// <summary>
        /// Works out the settlement for how the tournament ended. A completed tournament pays the prize to the champion and
        /// keeps the fees for the city; any other ending returns every fee to the entrant who paid it.
        /// </summary>
        /// <param name="tournament">The tournament.</param>
        /// <param name="outcome">How it ended.</param>
        /// <param name="escrow">The city's escrow entry for it, or null when the city no longer holds one.</param>
        public static TournamentSettlement Build(CityTournament tournament, TournamentOutcome outcome, CityEscrowEntry escrow)
        {
            var fees = GetFeesPaid(tournament, escrow);
            var payouts = new List<TournamentPayout>();

            if (outcome == TournamentOutcome.Completed)
            {
                var champion = string.IsNullOrEmpty(tournament.ChampionPlayerId)
                    ? TournamentRules.GetChampionId(tournament)
                    : tournament.ChampionPlayerId;

                if (!string.IsNullOrEmpty(champion) && tournament.Prize > 0)
                {
                    payouts.Add(new TournamentPayout(
                        BuildPayoutId(tournament, outcome, champion),
                        champion,
                        tournament.Prize,
                        "You won the city tournament and were paid the prize."));
                }

                return new TournamentSettlement(payouts, fees.Values.Sum());
            }

            foreach (var (playerId, fee) in fees.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                payouts.Add(new TournamentPayout(
                    BuildPayoutId(tournament, outcome, playerId),
                    playerId,
                    fee,
                    "Your entry fee for a city tournament was returned."));
            }

            return new TournamentSettlement(payouts, 0);
        }

        /// <summary>
        /// Settles the tournament against the city's records in memory, without saving anything. The city's escrow entry is
        /// released: a completed tournament pays its prize out and credits the fees to the ordinary treasury, and any other
        /// ending returns each portion of the prize to the balance it came from. A city with no escrow entry for the
        /// tournament (it was released already, or the city is gone) is left alone, so repeating the settlement after a
        /// restart never pays or credits twice. The caller writes the payouts first, then saves the city when
        /// <see cref="TournamentSettlement.ReleasedEscrow"/> is set.
        /// </summary>
        /// <param name="city">The city that holds the tournament, or null when it is gone.</param>
        /// <param name="tournament">The tournament.</param>
        /// <param name="outcome">How it ended.</param>
        public static TournamentSettlement Apply(WorldProperty city, CityTournament tournament, TournamentOutcome outcome)
        {
            CityEscrowEntry escrow = null;
            city?.Escrow.TryGetValue(tournament.Id, out escrow);

            var settlement = Build(tournament, outcome, escrow);
            if (city == null || escrow == null)
                return settlement;

            if (outcome == TournamentOutcome.Completed)
            {
                CityTreasuryRules.PayOutEscrow(city, tournament.Id);
                city.Treasury += settlement.FeesToTreasury;
            }
            else
            {
                CityTreasuryRules.RefundEscrow(city, tournament.Id);
            }

            return settlement with { ReleasedEscrow = true };
        }

        /// <summary>The id of a player's payout from a tournament ending a given way.</summary>
        public static string BuildPayoutId(CityTournament tournament, TournamentOutcome outcome, string playerId)
        {
            return $"tournament:{tournament.Id}:{outcome}:{playerId}";
        }
    }
}
