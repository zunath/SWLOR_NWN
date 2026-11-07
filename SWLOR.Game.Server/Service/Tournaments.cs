using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityGazetteService;
using SWLOR.Game.Server.Service.CityTreasuryService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.TournamentService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// City tournaments at a Dueling Ring (Player City Revamp, "Conflict and PvP"). The mayor or an officer posts one; its
    /// prize is held in the city's escrow from the ordinary treasury, and entrants pay their fee into the same escrow when
    /// they sign up. The matches are fought as duels at the ring (<see cref="Duels"/>), and the prize is paid when the final
    /// ends. The rules live in <see cref="TournamentRules"/> and <see cref="TournamentSettlementRules"/>; this class reads
    /// and saves the records and acts on the game. The matches and the monitor are in Tournaments.Matches.cs and the
    /// payouts in Tournaments.Settlement.cs.
    /// </summary>
    public static partial class Tournaments
    {
        // Lookups

        public static CityTournament Get(string tournamentId)
        {
            return string.IsNullOrWhiteSpace(tournamentId) ? null : DB.Get<CityTournament>(tournamentId);
        }

        /// <summary>The tournament held at a ring, in whatever state, or null. A ring holds one at a time.</summary>
        public static CityTournament GetAtRing(uint ring)
        {
            var ringId = Property.GetPropertyId(ring);
            if (string.IsNullOrWhiteSpace(ringId))
                return null;

            return Search(new DBQuery<CityTournament>()
                    .AddFieldSearch(nameof(CityTournament.RingPropertyId), ringId, false))
                .FirstOrDefault();
        }

        /// <summary>The tournaments that are taking sign-ups, in any city.</summary>
        public static List<CityTournament> GetOpenForSignUp()
        {
            var now = DateTime.UtcNow;
            return Search(new DBQuery<CityTournament>()
                    .AddFieldSearch(nameof(CityTournament.State), (int)TournamentState.SignUp))
                .Where(x => x.StartsUtc > now)
                .OrderBy(x => x.StartsUtc)
                .ToList();
        }

        public static bool IsRunningAt(uint ring)
        {
            return GetAtRing(ring) is { State: TournamentState.Running };
        }

        /// <summary>Whether a player is barred from a ring: they interrupted a match of the tournament held there.</summary>
        public static bool IsBarred(uint ring, string playerId)
        {
            var tournament = GetAtRing(ring);
            return tournament != null && TournamentRules.IsBarred(tournament, playerId);
        }

        /// <summary>The city or station that owns a ring, or null.</summary>
        public static WorldProperty GetRingCity(uint ring)
        {
            var cityId = Duels.GetRingCityId(ring);
            return string.IsNullOrWhiteSpace(cityId) ? null : DB.Get<WorldProperty>(cityId);
        }

        /// <summary>Whether the player is the mayor or an officer of the city that owns the ring.</summary>
        public static bool CanRun(uint player, uint ring)
        {
            var city = GetRingCity(ring);
            return city != null && PvpCombat.IsPlayer(player) && CityOfficers.CanHandle(city, GetObjectUUID(player));
        }

        /// <summary>
        /// How a player appears in a list: the name they would see in the world while the entrant is online, and the
        /// label strangers saw at sign-up otherwise.
        /// </summary>
        public static string GetName(uint viewer, CityTournament tournament, string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
                return "no one";

            var online = GetObjectByUUID(playerId);
            if (GetIsObjectValid(online) && PvpCombat.IsPlayer(online))
                return PlayerName.GetDisplayName(viewer, online);

            return TournamentText.GetLabel(tournament, playerId);
        }

        // Posting

        /// <summary>
        /// Posts a tournament at the ring. The prize moves from the city's ordinary treasury into escrow at once. The
        /// record is saved first, so a restart before the prize is held finds an unfunded tournament and drops it.
        /// </summary>
        /// <returns>The reason it was refused, or an empty string.</returns>
        public static string Post(uint player, uint ring, long prize, long entryFee, int leadHours)
        {
            var city = GetRingCity(ring);
            if (city == null)
                return "This ring does not belong to a city.";

            var playerId = GetObjectUUID(player);
            var now = DateTime.UtcNow;
            var startsUtc = now.AddHours(leadHours);
            var error = TournamentRules.CheckPost(
                CityOfficers.CanHandle(city, playerId),
                GetAtRing(ring) != null,
                prize,
                entryFee,
                startsUtc,
                now,
                CityTreasuryRules.GetSpendable(city, restrictedFirst: false));
            if (!string.IsNullOrEmpty(error))
                return error;

            var tournament = new CityTournament
            {
                CityId = city.Id,
                RingPropertyId = Property.GetPropertyId(ring),
                State = TournamentState.SignUp,
                PostedByPlayerId = playerId,
                PostedUtc = now,
                StartsUtc = startsUtc,
                Prize = prize,
                EntryFee = entryFee
            };
            DB.Set(tournament);

            if (!CityTreasury.PostEscrow(city, tournament.Id, prize, restrictedFirst: false).Succeeded)
            {
                DB.Delete<CityTournament>(tournament.Id);
                return "The prize could not be held. Try again.";
            }

            Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} posted a tournament ({tournament.Id}) for city '{city.CustomName}' ({city.Id}): prize {prize}, entry fee {entryFee}, starts {startsUtc:u}.");
            Announce(tournament, TournamentText.BuildPostedText(prize, entryFee, startsUtc), playerId);
            return string.Empty;
        }

        // Signing up

        /// <summary>
        /// A player asks to sign up. The fee is taken on the player's own queue, as the engine requires, and saved with a
        /// credit payment mark so a restart in the middle neither loses the fee nor enters the player unpaid.
        /// </summary>
        /// <returns>The reason the request was refused, or an empty string when it was accepted.</returns>
        public static string RequestSignUp(uint player, uint ring)
        {
            var tournament = GetAtRing(ring);
            var error = TournamentRules.CheckSignUp(tournament, GetObjectUUID(player), GetGold(player), DateTime.UtcNow);
            if (!string.IsNullOrEmpty(error))
                return error;

            var tournamentId = tournament.Id;
            AssignCommand(player, () => CompleteSignUp(player, tournamentId));
            return string.Empty;
        }

        private static void CompleteSignUp(uint player, string tournamentId)
        {
            var tournament = Get(tournamentId);
            var playerId = GetObjectUUID(player);
            var error = TournamentRules.CheckSignUp(tournament, playerId, GetGold(player), DateTime.UtcNow);
            var city = tournament == null ? null : DB.Get<WorldProperty>(tournament.CityId);
            if (string.IsNullOrEmpty(error) && city == null)
                error = "The city that holds this tournament is gone.";

            if (!string.IsNullOrEmpty(error))
            {
                SendMessageToPC(player, ColorToken.Red(error));
                return;
            }

            var fee = tournament.EntryFee;
            var entrant = new TournamentEntrant
            {
                PlayerId = playerId,
                Label = Disguise.GetDisplayDescriptor(player),
                FeePaid = fee,
                SignedUpUtc = DateTime.UtcNow
            };

            CreditPaymentMark mark = null;
            if (fee > 0)
            {
                mark = CreditPayments.NewMark(CreditPaymentKind.TournamentEntry, tournament.Id, (int)fee);

                // The fee reaches the character file before the entrant and the escrow are saved, so a restart can't refund a fee
                // the restored character still holds.
                if (!CreditPayments.TryTakeSaved(player, mark))
                {
                    SendMessageToPC(player, ColorToken.Red("Your entry fee could not be saved, so you were not signed up. Please try again."));
                    return;
                }

                entrant.PaymentId = mark.Id;
            }

            // The entrant is saved before the fee is held in escrow. A restart between the two leaves a fee that only the
            // entrant's record names, and the settlement counts it (TournamentSettlementRules.GetFeesPaid).
            tournament.Entrants.Add(entrant);
            DB.Set(tournament);

            if (fee > 0)
            {
                if (!CityTreasury.AddEntryFee(city, tournament.Id, playerId, fee))
                    Log.Write(LogGroup.Error, $"Tournament '{tournament.Id}' held no escrow entry for the entry fee of {PlayerName.GetAuditName(player)}.");

                CreditPayments.Retire(player);
            }

            Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} signed up for tournament '{tournament.Id}' of city '{city.CustomName}' ({city.Id}) and paid {fee} credits.");
            var paid = fee > 0 ? $" and paid the entry fee of {TournamentRules.FormatCredits(fee)} credits" : string.Empty;
            SendMessageToPC(player, ColorToken.Green($"You signed up for the tournament{paid}. The entry fee is returned only if the tournament is cancelled."));
        }

        // Beginning, calling, deciding and cancelling

        /// <summary>
        /// An official begins the tournament once it is due: the entrants are put in a random order, the bracket is built
        /// and the official is recorded as the one running it.
        /// </summary>
        /// <returns>The reason it was refused, or an empty string.</returns>
        public static string Begin(uint player, uint ring)
        {
            var tournament = GetAtRing(ring);
            var error = CheckOfficial(player, ring, tournament);
            if (!string.IsNullOrEmpty(error))
                return error;

            error = TournamentRules.CheckBegin(tournament, DateTime.UtcNow);
            if (!string.IsNullOrEmpty(error))
                return error;

            var order = TournamentRules.Shuffle(tournament.Entrants.Select(x => x.PlayerId), Random.Next);
            tournament.Matches = TournamentRules.BuildBracket(order);
            tournament.State = TournamentState.Running;
            tournament.OfficialPlayerId = GetObjectUUID(player);
            DB.Set(tournament);
            EnsureMonitor();

            Announce(tournament, TournamentText.BuildBeganText(tournament.Entrants.Count), tournament.OfficialPlayerId);
            foreach (var entrant in tournament.Entrants)
            {
                var online = GetObjectByUUID(entrant.PlayerId);
                if (GetIsObjectValid(online))
                    SendMessageToPC(online, ColorToken.Green("The tournament has begun. Stay near the Dueling Ring: you have 5 minutes to reach it once your match is called."));
            }

            return string.Empty;
        }

        /// <summary>An official calls the next ready match. Its duelists have five minutes to stand in the ring.</summary>
        /// <returns>The reason it was refused, or an empty string.</returns>
        public static string CallNextMatch(uint player, uint ring)
        {
            var tournament = GetAtRing(ring);
            var error = CheckOfficial(player, ring, tournament);
            if (!string.IsNullOrEmpty(error))
                return error;

            error = TournamentRules.CheckCall(tournament);
            if (!string.IsNullOrEmpty(error))
                return error;

            var match = TournamentRules.GetNextReady(tournament.Matches);
            TournamentRules.Call(match, DateTime.UtcNow);
            DB.Set(tournament);
            TellMatchCalled(tournament, match);
            return string.Empty;
        }

        /// <summary>
        /// An official picks the winner of a match that was interrupted three times. The Gazette says an official
        /// decided it.
        /// </summary>
        /// <returns>The reason it was refused, or an empty string.</returns>
        public static string DecideMatch(uint player, uint ring, string winnerPlayerId)
        {
            var tournament = GetAtRing(ring);
            var error = CheckOfficial(player, ring, tournament);
            if (!string.IsNullOrEmpty(error))
                return error;

            var match = tournament.Matches.FirstOrDefault(x => x.State == TournamentMatchState.AwaitingOfficial);
            if (match == null || !TournamentRules.CanDecide(match, winnerPlayerId))
                return "No interrupted match is waiting for that decision.";

            var officialId = GetObjectUUID(player);
            TournamentRules.Decide(match, winnerPlayerId, officialId);
            RefreshLabel(tournament, match.PlayerAId);
            RefreshLabel(tournament, match.PlayerBId);
            FinishMatch(tournament, match, officialId);
            return string.Empty;
        }

        /// <summary>An official calls the tournament off. Entry fees go back to the entrants and the prize to the treasury.</summary>
        /// <returns>The reason it was refused, or an empty string.</returns>
        public static string Cancel(uint player, uint ring)
        {
            var tournament = GetAtRing(ring);
            var error = CheckOfficial(player, ring, tournament);
            if (!string.IsNullOrEmpty(error))
                return error;

            if (tournament.State == TournamentState.Settling)
                return "The tournament is already being settled.";

            Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} cancelled tournament '{tournament.Id}' of city '{tournament.CityId}'.");
            Settle(tournament, TournamentOutcome.Cancelled, "An official called it off.", GetObjectUUID(player));
            return string.Empty;
        }

        private static string CheckOfficial(uint player, uint ring, CityTournament tournament)
        {
            if (tournament == null)
                return "There is no tournament at this ring.";
            if (!CanRun(player, ring))
                return "Only the mayor or an officer can do that.";

            return string.Empty;
        }

        // Records

        private static List<CityTournament> Search(DBQuery<CityTournament> query)
        {
            var count = (int)DB.SearchCount(query);
            if (count <= 0)
                return new List<CityTournament>();

            return DB.Search(query.AddPaging(count, 0))
                .Where(x => x != null)
                .ToList();
        }

        /// <summary>Writes a line in the city's Gazette, under the official's title when one acted.</summary>
        private static void Announce(CityTournament tournament, string text, string officialPlayerId = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            string title = null;
            if (!string.IsNullOrWhiteSpace(officialPlayerId))
            {
                var city = DB.Get<WorldProperty>(tournament.CityId);
                title = city == null ? null : CityOfficers.GetTitle(city, officialPlayerId);
            }

            CityGazette.Post(tournament.CityId, CityGazetteEntryType.Notice, text, officialPlayerId, title);
        }
    }
}
