using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityTreasuryService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.TournamentService;

namespace SWLOR.Game.Server.Service
{
    public static partial class Tournaments
    {
        /// <summary>
        /// A Dueling Ring was taken down or deleted: the tournament held there is called off, its entry fees go back to
        /// the entrants and its prize back to the treasury.
        /// </summary>
        public static void OnRingDeleted(WorldProperty ring)
        {
            if (ring == null)
                return;

            var tournaments = Search(new DBQuery<CityTournament>()
                .AddFieldSearch(nameof(CityTournament.RingPropertyId), ring.Id, false));
            foreach (var tournament in tournaments)
            {
                Settle(tournament, TournamentOutcome.Cancelled, TournamentText.BuildCancelledText("The Dueling Ring was taken down."));
            }
        }

        /// <summary>
        /// A city is being removed: the entry fees of its tournaments become credits owed to the entrants, paid at their
        /// next login. The prize goes with the treasury.
        /// </summary>
        public static void OnCityDeleted(WorldProperty city)
        {
            if (city == null)
                return;

            var tournaments = Search(new DBQuery<CityTournament>()
                .AddFieldSearch(nameof(CityTournament.CityId), city.Id, false));
            foreach (var tournament in tournaments)
            {
                Settle(tournament, TournamentOutcome.CityRemoved);
            }
        }

        /// <summary>
        /// Pays out or returns a tournament's money and deletes it. The tournament is saved as Settling first and works as
        /// the journal: the owed credits are written under ids that never change, then the city's escrow entry is released
        /// and the fees credited in one save, then the tournament is deleted. A restart at any point repeats the steps
        /// that did not finish and none that did.
        /// </summary>
        /// <param name="tournament">The tournament to settle.</param>
        /// <param name="outcome">How it ended. Ignored when the tournament is already settling: it finishes as it began.</param>
        /// <param name="gazetteText">The Gazette line for a cancellation, or null for none.</param>
        /// <param name="officialPlayerId">The official who ended it, for the Gazette.</param>
        private static void Settle(CityTournament tournament, TournamentOutcome outcome, string gazetteText = null, string officialPlayerId = null)
        {
            var firstRun = tournament.State != TournamentState.Settling;
            if (firstRun)
            {
                // A prize can only be paid out of the escrow entry that holds it, and a removed city holds nothing.
                var holder = DB.Get<WorldProperty>(tournament.CityId);
                if (holder == null)
                    outcome = TournamentOutcome.CityRemoved;
                else if (outcome == TournamentOutcome.Completed && !holder.Escrow.ContainsKey(tournament.Id))
                {
                    Log.Write(LogGroup.Error, $"Tournament '{tournament.Id}' has no escrow entry to pay its prize from, so it is settled as cancelled.");
                    outcome = TournamentOutcome.Cancelled;
                }

                tournament.State = TournamentState.Settling;
                tournament.Outcome = outcome;
                DB.Set(tournament);
            }

            outcome = tournament.Outcome;
            var city = outcome == TournamentOutcome.CityRemoved ? null : DB.Get<WorldProperty>(tournament.CityId);
            if (city == null)
                outcome = TournamentOutcome.CityRemoved;

            CityEscrowEntry escrow = null;
            city?.Escrow.TryGetValue(tournament.Id, out escrow);

            var settlement = TournamentSettlementRules.Build(tournament, outcome, escrow);
            foreach (var payout in settlement.Payouts)
            {
                PendingCredits.Add(payout.Id, payout.PlayerId, payout.Credits, payout.Reason);
            }

            if (city != null && escrow != null)
            {
                if (outcome == TournamentOutcome.Completed)
                {
                    CityTreasuryRules.PayOutEscrow(city, tournament.Id);
                    city.Treasury += settlement.FeesToTreasury;
                }
                else
                {
                    CityTreasuryRules.RefundEscrow(city, tournament.Id);
                }

                DB.Set(city);
            }

            DB.Delete<CityTournament>(tournament.Id);
            Log.Write(LogGroup.Property, $"Tournament '{tournament.Id}' of city '{tournament.CityId}' settled as {outcome}: {settlement.Payouts.Count} payments owed, {settlement.FeesToTreasury} credits in fees kept by the city.");

            if (firstRun && city != null)
            {
                if (outcome == TournamentOutcome.Completed)
                {
                    RefreshLabel(tournament, tournament.ChampionPlayerId);
                    Announce(tournament, TournamentText.BuildChampionText(TournamentText.GetLabel(tournament, tournament.ChampionPlayerId), tournament.Prize), officialPlayerId);
                }
                else
                {
                    Announce(tournament, gazetteText, officialPlayerId);
                }
            }

            foreach (var payout in settlement.Payouts)
            {
                PendingCredits.DeliverIfOnline(payout.PlayerId);
            }

            if (firstRun && outcome != TournamentOutcome.Completed)
            {
                foreach (var entrant in tournament.Entrants.Where(x => settlement.Payouts.All(p => p.PlayerId != x.PlayerId)))
                {
                    var online = GetObjectByUUID(entrant.PlayerId);
                    if (GetIsObjectValid(online))
                        SendMessageToPC(online, ColorToken.Red("The tournament you entered was cancelled."));
                }
            }
        }
    }
}
