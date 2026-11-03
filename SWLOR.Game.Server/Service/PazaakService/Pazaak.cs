using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Service.PazaakService
{
    /// <summary>
    /// Runs Pazaak matches between two players: invitations, turns, timeouts, and side deck storage.
    /// Both players see the same match through their own Pazaak window.
    /// </summary>
    public static class Pazaak
    {
        private const float ChallengeRange = 10f;
        private const int InviteSeconds = 60;
        private const float TurnTimeoutSeconds = 90f;

        private static readonly Dictionary<uint, PazaakInvite> _invitesByTarget = new();
        private static readonly Dictionary<uint, PazaakMatch> _matchesByPlayer = new();
        private static readonly Dictionary<PazaakMatch, PazaakSetResult> _announcedSetResults = new();

        public static IReadOnlyList<PazaakCardType> GetSideDeck(uint player)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer != null && PazaakCards.IsValidSideDeck(dbPlayer.PazaakSideDeck, dbPlayer.PazaakCardCollection))
                return dbPlayer.PazaakSideDeck;

            return PazaakCards.DefaultSideDeck;
        }

        /// <summary>
        /// Side cards the player collected from card items, by number of copies.
        /// </summary>
        public static IReadOnlyDictionary<PazaakCardType, int> GetCollection(uint player)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            return dbPlayer?.PazaakCardCollection ?? new Dictionary<PazaakCardType, int>();
        }

        public static bool SaveSideDeck(uint player, IReadOnlyCollection<PazaakCardType> deck)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null || !PazaakCards.IsValidSideDeck(deck, dbPlayer.PazaakCardCollection))
                return false;

            dbPlayer.PazaakSideDeck = deck.ToList();
            DB.Set(dbPlayer);
            return true;
        }

        /// <summary>
        /// Adds one copy of a side card to the player's collection and returns how many copies they now own.
        /// </summary>
        public static int AddCardToCollection(uint player, PazaakCardType type)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            dbPlayer.PazaakCardCollection.TryGetValue(type, out var count);
            dbPlayer.PazaakCardCollection[type] = count + 1;
            DB.Set(dbPlayer);

            if (Gui.IsWindowOpen(player, GuiWindowType.Pazaak))
                Gui.PublishRefreshEvent(player, new PazaakRefreshEvent());

            return PazaakCards.GetOwnedCopies(type, dbPlayer.PazaakCardCollection);
        }

        /// <summary>
        /// The player's current or most recent match, or null.
        /// </summary>
        public static PazaakMatch GetMatch(uint player) =>
            _matchesByPlayer.TryGetValue(player, out var match) ? match : null;

        public static bool IsInActiveMatch(uint player) =>
            GetMatch(player) is { IsFinished: false };

        public static int GetSeatIndex(PazaakMatch match, uint player) =>
            match.Seats[0].Player == player ? 0 : 1;

        /// <summary>
        /// The player who challenged this player, or OBJECT_INVALID when no challenge is waiting.
        /// </summary>
        public static uint GetPendingChallenger(uint target)
        {
            if (!_invitesByTarget.TryGetValue(target, out var invite))
                return OBJECT_INVALID;

            if (invite.ExpiresUtc < DateTime.UtcNow || !GetIsObjectValid(invite.Challenger))
            {
                _invitesByTarget.Remove(target);
                return OBJECT_INVALID;
            }

            return invite.Challenger;
        }

        public static void OpenWindow(uint player)
        {
            if (!Gui.IsWindowOpen(player, GuiWindowType.Pazaak))
                Gui.TogglePlayerWindow(player, GuiWindowType.Pazaak);
        }

        /// <summary>
        /// Sends a challenge to a nearby player. Returns an error to show the challenger, or an empty string.
        /// </summary>
        public static string Challenge(uint challenger, uint target)
        {
            if (!GetIsObjectValid(target) || !GetIsPC(target) || GetIsDM(target) || GetIsDMPossessed(target))
                return "You can only challenge another player character.";

            if (target == challenger)
                return "You cannot challenge yourself.";

            if (GetIsDM(challenger) || GetIsDMPossessed(challenger))
                return "Pazaak is only available to player characters.";

            if (GetArea(target) != GetArea(challenger) || GetDistanceBetween(challenger, target) > ChallengeRange)
                return "Your opponent must be within 10 meters of you.";

            if (IsInActiveMatch(challenger))
                return "Finish your current match first.";

            var targetName = PlayerName.GetDisplayName(challenger, target);
            if (IsInActiveMatch(target))
                return $"{targetName} is already playing a match.";

            if (GetPendingChallenger(target) != OBJECT_INVALID)
                return $"{targetName} is already considering another challenge.";

            _invitesByTarget[target] = new PazaakInvite(challenger, DateTime.UtcNow.AddSeconds(InviteSeconds));

            SendMessageToPC(challenger, ColorToken.Green($"You challenge {targetName} to a game of Pazaak."));
            SendMessageToPC(target, ColorToken.Green($"{PlayerName.GetDisplayName(target, challenger)} challenges you to a game of Pazaak."));

            // The window shows the challenge when it opens or refreshes.
            if (Gui.IsWindowOpen(target, GuiWindowType.Pazaak))
                Gui.PublishRefreshEvent(target, new PazaakRefreshEvent());
            else
                OpenWindow(target);

            return string.Empty;
        }

        public static void DeclineChallenge(uint target)
        {
            var challenger = GetPendingChallenger(target);
            _invitesByTarget.Remove(target);
            if (challenger == OBJECT_INVALID)
                return;

            SendMessageToPC(challenger, ColorToken.Red($"{PlayerName.GetDisplayName(challenger, target)} declines your Pazaak challenge."));
        }

        /// <summary>
        /// Starts a match from the waiting challenge. Returns an error to show the target, or an empty string.
        /// </summary>
        public static string AcceptChallenge(uint target)
        {
            var challenger = GetPendingChallenger(target);
            _invitesByTarget.Remove(target);

            if (challenger == OBJECT_INVALID)
                return "The challenge is no longer available.";

            if (IsInActiveMatch(challenger) || IsInActiveMatch(target))
                return "One of you is already playing a match.";

            if (GetArea(target) != GetArea(challenger) || GetDistanceBetween(challenger, target) > ChallengeRange)
                return "Your opponent must be within 10 meters of you.";

            var match = new PazaakMatch(
                new PazaakSeat(challenger, GetSideDeck(challenger)),
                new PazaakSeat(target, GetSideDeck(target)),
                Random.Next);
            _matchesByPlayer[challenger] = match;
            _matchesByPlayer[target] = match;
            match.Start(Random.Next(2));

            foreach (var seat in match.Seats)
            {
                var opponent = match.Seats[PazaakMatch.GetOpponent(GetSeatIndex(match, seat.Player))].Player;
                SendMessageToPC(seat.Player, ColorToken.Green($"Your Pazaak match with {PlayerName.GetDisplayName(seat.Player, opponent)} begins."));
                OpenWindow(seat.Player);
                Gui.PublishRefreshEvent(seat.Player, new PazaakRefreshEvent { MatchStarted = true });
            }

            ScheduleTurnTimeout(match);
            return string.Empty;
        }

        public static string PlaySideCard(uint player, int handIndex, bool negative)
        {
            var match = GetMatch(player);
            if (match == null)
                return "You are not playing a match.";

            return AfterMove(match, match.PlaySideCard(GetSeatIndex(match, player), handIndex, negative, out var error), error);
        }

        public static string EndTurn(uint player)
        {
            var match = GetMatch(player);
            if (match == null)
                return "You are not playing a match.";

            return AfterMove(match, match.EndTurn(GetSeatIndex(match, player), out var error), error);
        }

        public static string Stand(uint player)
        {
            var match = GetMatch(player);
            if (match == null)
                return "You are not playing a match.";

            return AfterMove(match, match.Stand(GetSeatIndex(match, player), out var error), error);
        }

        public static void Forfeit(uint player)
        {
            var match = GetMatch(player);
            if (match == null || match.IsFinished)
                return;

            match.Forfeit(GetSeatIndex(match, player));
            AnnounceMatchEnd(match);
            RefreshWindows(match);
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ForfeitOnExit()
        {
            var player = GetExitingObject();
            _invitesByTarget.Remove(player);

            Forfeit(player);
            // The opponent keeps the finished match so their window still shows the result.
            _matchesByPlayer.Remove(player);
        }

        private static string AfterMove(PazaakMatch match, bool succeeded, string error)
        {
            if (!succeeded)
                return error;

            AnnounceSetResult(match);

            if (match.IsFinished)
                AnnounceMatchEnd(match);
            else
                ScheduleTurnTimeout(match);

            RefreshWindows(match);
            return string.Empty;
        }

        private static void AnnounceSetResult(PazaakMatch match)
        {
            var result = match.LastSetResult;
            if (result == null)
                return;

            if (_announcedSetResults.TryGetValue(match, out var announced) && announced == result)
                return;

            _announcedSetResults[match] = result;
            for (var seatIndex = 0; seatIndex < match.Seats.Length; seatIndex++)
            {
                var player = match.Seats[seatIndex].Player;
                SendMessageToPC(player, ColorToken.Cyan("Pazaak: ") + DescribeSetResult(match, seatIndex, player));
            }
        }

        /// <summary>
        /// Text describing the last set from one seat's point of view.
        /// </summary>
        public static string DescribeSetResult(PazaakMatch match, int seatIndex, uint observer)
        {
            var result = match.LastSetResult;
            if (result == null)
                return string.Empty;

            var opponentIndex = PazaakMatch.GetOpponent(seatIndex);
            var opponentName = PlayerName.GetDisplayName(observer, match.Seats[opponentIndex].Player);
            var score = $"{result.Totals[seatIndex]} to {result.Totals[opponentIndex]}";

            if (result.WinnerSeat < 0)
                return $"Set {result.SetNumber} is a tie, {score}.";

            var won = result.WinnerSeat == seatIndex;
            if (result.WasBust)
                return won
                    ? $"{opponentName} busts. You win set {result.SetNumber}."
                    : $"You bust. {opponentName} wins set {result.SetNumber}.";

            if (result.WasFullTable)
                return won
                    ? $"You fill your table. You win set {result.SetNumber}."
                    : $"{opponentName} fills their table and wins set {result.SetNumber}.";

            return won
                ? $"You win set {result.SetNumber}, {score}."
                : $"{opponentName} wins set {result.SetNumber}, {score}.";
        }

        private static void AnnounceMatchEnd(PazaakMatch match)
        {
            _announcedSetResults.Remove(match);
            for (var seatIndex = 0; seatIndex < match.Seats.Length; seatIndex++)
            {
                var player = match.Seats[seatIndex].Player;
                if (!GetIsObjectValid(player))
                    continue;

                var opponentName = PlayerName.GetDisplayName(player, match.Seats[PazaakMatch.GetOpponent(seatIndex)].Player);
                var message = match.WinnerSeat == seatIndex
                    ? ColorToken.Green(match.WasForfeited
                        ? $"{opponentName} forfeits. You win the Pazaak match!"
                        : $"You win the Pazaak match against {opponentName}!")
                    : ColorToken.Red(match.WasForfeited
                        ? $"You forfeit the Pazaak match against {opponentName}."
                        : $"{opponentName} wins the Pazaak match.");
                SendMessageToPC(player, message);
            }
        }

        private static void ScheduleTurnTimeout(PazaakMatch match)
        {
            var turnNumber = match.TurnNumber;
            AssignCommand(GetModule(), () => DelayCommand(TurnTimeoutSeconds, () =>
            {
                if (match.IsFinished || match.TurnNumber != turnNumber)
                    return;

                var seatIndex = match.CurrentSeat;
                var player = match.Seats[seatIndex].Player;
                SendMessageToPC(player, ColorToken.Red("You took too long, so you stand."));
                AfterMove(match, match.Stand(seatIndex, out var error), error);
            }));
        }

        private static void RefreshWindows(PazaakMatch match)
        {
            foreach (var seat in match.Seats)
            {
                if (GetIsObjectValid(seat.Player))
                    Gui.PublishRefreshEvent(seat.Player, new PazaakRefreshEvent());
            }
        }
    }
}
