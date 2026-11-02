using System.Linq;
using System.Text;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CityTreasuryService;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.TournamentService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    /// <summary>
    /// The ring terminal's tournament pages: what is posted, signing up, and, for the mayor and officers, posting,
    /// beginning, calling matches, deciding interrupted ones and cancelling. <see cref="Tournaments"/> holds the rules and
    /// checks everything again when an action is taken.
    /// </summary>
    public partial class DuelingRingDialog
    {
        private const string TournamentPageId = "TOURNAMENT_PAGE";
        private const string PostPageId = "TOURNAMENT_POST_PAGE";

        /// <summary>What the poster has picked so far, and whether a cancellation is waiting for confirmation.</summary>
        private class TournamentDraft
        {
            public long? Prize { get; set; }
            public long? EntryFee { get; set; }
            public int? LeadHours { get; set; }
            public bool IsConfirmingCancel { get; set; }

            public void Reset()
            {
                Prize = null;
                EntryFee = null;
                LeadHours = null;
                IsConfirmingCancel = false;
            }
        }

        private void AddTournamentPages(ConversationMenuBuilder builder)
        {
            builder
                .AddPage(TournamentPageId, TournamentPageInit)
                .AddPage(PostPageId, PostPageInit);
        }

        /// <summary>Adds the way into the tournament pages, and a line about what the ring is holding.</summary>
        private void AddTournamentEntry(ConversationMenuPage page)
        {
            var tournament = Tournaments.GetAtRing(Owner);
            if (tournament != null)
            {
                page.Header += tournament.State == TournamentState.SignUp
                    ? "A tournament is taking sign-ups at this ring.\n\n"
                    : "A tournament is running at this ring.\n\n";
            }

            page.AddResponse("Tournament", () =>
            {
                Data<TournamentDraft>().Reset();
                GoToPage(TournamentPageId);
            });
        }

        // The tournament

        private void TournamentPageInit(ConversationMenuPage page)
        {
            var tournament = Tournaments.GetAtRing(Owner);
            var canRun = Tournaments.CanRun(Player, Owner);

            page.Header = "Tournament\n\n";
            if (tournament == null)
            {
                page.Header += "No tournament is scheduled at this ring.";
                if (canRun)
                {
                    page.AddResponse("Post a tournament", () =>
                    {
                        Data<TournamentDraft>().Reset();
                        GoToPage(PostPageId);
                    });
                }
                else
                {
                    page.Header += " The mayor or an officer can post one.";
                }

                return;
            }

            page.Header += BuildDetails(tournament);
            switch (tournament.State)
            {
                case TournamentState.SignUp:
                    AddSignUpResponses(page, tournament, canRun);
                    break;
                case TournamentState.Running:
                    page.Header += "\n\n" + BuildBracket(tournament);
                    AddRunningResponses(page, tournament, canRun);
                    break;
                default:
                    page.Header += "\n\nThe tournament is being settled.";
                    break;
            }
        }

        private string BuildDetails(CityTournament tournament)
        {
            var details = new StringBuilder();
            details.Append($"Prize: {TournamentRules.FormatCredits(tournament.Prize)} credits\n");
            details.Append(tournament.EntryFee > 0
                ? $"Entry fee: {TournamentRules.FormatCredits(tournament.EntryFee)} credits\n"
                : "Entry fee: free\n");

            if (tournament.State == TournamentState.SignUp)
            {
                var now = DateTime.UtcNow;
                details.Append($"Sign-ups close: {TournamentText.FormatUtc(tournament.StartsUtc)}");
                if (tournament.StartsUtc > now)
                    details.Append($" (in {Time.GetTimeLongIntervals(tournament.StartsUtc - now, false)})");
                details.Append('\n');
            }

            details.Append($"Entrants: {tournament.Entrants.Count}");
            foreach (var entrant in tournament.Entrants)
            {
                var name = Tournaments.GetName(Player, tournament, entrant.PlayerId);
                details.Append(entrant.PlayerId == GetObjectUUID(Player) ? $"\n  {name} (you)" : $"\n  {name}");
            }

            return details.ToString();
        }

        private void AddSignUpResponses(ConversationMenuPage page, CityTournament tournament, bool canRun)
        {
            var player = Player;
            var ring = Owner;
            var reason = TournamentRules.CheckSignUp(tournament, GetObjectUUID(player), GetGold(player), DateTime.UtcNow);
            if (string.IsNullOrEmpty(reason))
            {
                var fee = tournament.EntryFee > 0
                    ? $"Sign up and pay {TournamentRules.FormatCredits(tournament.EntryFee)} credits"
                    : "Sign up";
                page.AddResponse(ColorToken.Green(fee), () => Report(Tournaments.RequestSignUp(player, ring)));
                page.Header += "\n\nThe entry fee is returned only if the tournament is cancelled.";
            }
            else
            {
                page.Header += $"\n\n{reason}";
            }

            if (!canRun)
                return;

            page.AddResponse("Begin the tournament", () => Report(Tournaments.Begin(player, ring)));
            AddCancelResponses(page, tournament);
        }

        private void AddRunningResponses(ConversationMenuPage page, CityTournament tournament, bool canRun)
        {
            if (!canRun)
                return;

            var player = Player;
            var ring = Owner;
            if (TournamentRules.CheckCall(tournament).Length == 0)
                page.AddResponse("Call the next match", () => Report(Tournaments.CallNextMatch(player, ring)));

            var waiting = tournament.Matches.FirstOrDefault(x => x.State == TournamentMatchState.AwaitingOfficial);
            if (waiting != null)
            {
                page.Header += $"\n\nThis match was interrupted {TournamentRules.InterruptionsBeforeOfficial} times. Pick who advances.";
                foreach (var duelist in new[] { waiting.PlayerAId, waiting.PlayerBId })
                {
                    var winnerId = duelist;
                    page.AddResponse(
                        $"{Tournaments.GetName(player, tournament, winnerId)} advances",
                        () => Report(Tournaments.DecideMatch(player, ring, winnerId)));
                }
            }

            AddCancelResponses(page, tournament);
        }

        private void AddCancelResponses(ConversationMenuPage page, CityTournament tournament)
        {
            var draft = Data<TournamentDraft>();
            var player = Player;
            var ring = Owner;

            if (!draft.IsConfirmingCancel)
            {
                page.AddResponse("Cancel the tournament", () => draft.IsConfirmingCancel = true);
                return;
            }

            page.Header += "\n\nCancelling returns every entry fee to the entrants and the prize to the treasury. This cannot be undone.";
            page.AddResponse(ColorToken.Red("CONFIRM CANCEL THE TOURNAMENT"), () =>
            {
                draft.IsConfirmingCancel = false;
                Report(Tournaments.Cancel(player, ring));
            });
            page.AddResponse("Keep the tournament", () => draft.IsConfirmingCancel = false);
        }

        /// <summary>One line per match, round by round.</summary>
        private string BuildBracket(CityTournament tournament)
        {
            var rounds = TournamentRules.GetRoundCount(tournament.Matches);
            var bracket = new StringBuilder("Bracket:");
            foreach (var round in tournament.Matches.GroupBy(x => x.Round).OrderBy(x => x.Key))
            {
                bracket.Append($"\n{CapitalizeFirst(TournamentRules.GetRoundName(round.Key, rounds))}:");
                foreach (var match in round.OrderBy(x => x.Slot))
                {
                    bracket.Append($"\n  {DescribeMatch(tournament, match)}");
                }
            }

            return bracket.ToString();
        }

        private string DescribeMatch(CityTournament tournament, TournamentMatch match)
        {
            var first = string.IsNullOrEmpty(match.PlayerAId) ? "TBD" : Tournaments.GetName(Player, tournament, match.PlayerAId);
            var second = string.IsNullOrEmpty(match.PlayerBId) ? "TBD" : Tournaments.GetName(Player, tournament, match.PlayerBId);
            var status = match.State switch
            {
                TournamentMatchState.Called when match.CalledUtc.HasValue => $"called, {DescribeTimeLeft(match.CalledUtc.Value)}",
                TournamentMatchState.Called => "called",
                TournamentMatchState.Active => "in progress",
                TournamentMatchState.AwaitingOfficial => "waiting for an official",
                TournamentMatchState.Done => DescribeResult(tournament, match),
                _ => TournamentRules.IsReady(match) ? "ready to be called" : "waiting"
            };

            return $"{first} vs {second}: {status}";
        }

        private string DescribeResult(CityTournament tournament, TournamentMatch match)
        {
            if (string.IsNullOrEmpty(match.WinnerId))
                return "no one advanced";

            var winner = Tournaments.GetName(Player, tournament, match.WinnerId);
            return match.Result switch
            {
                TournamentMatchResult.Forfeit => $"{winner} advanced by forfeit",
                TournamentMatchResult.Official => $"{winner} advanced by an official's decision",
                TournamentMatchResult.Bye => $"{winner} advanced without a fight",
                _ => $"{winner} won"
            };
        }

        private static string DescribeTimeLeft(DateTime calledUtc)
        {
            var left = calledUtc.AddMinutes(TournamentRules.NoShowMinutes) - DateTime.UtcNow;
            return left <= TimeSpan.Zero ? "time is up" : $"{Time.GetTimeLongIntervals(left, false)} left to arrive";
        }

        private static string CapitalizeFirst(string text)
        {
            return text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];
        }

        // Posting

        private void PostPageInit(ConversationMenuPage page)
        {
            var draft = Data<TournamentDraft>();
            var city = Tournaments.GetRingCity(Owner);
            var spendable = city == null ? 0 : CityTreasuryRules.GetSpendable(city, restrictedFirst: false);

            page.Header = "Post a tournament\n\n" +
                          "The prize is held out of the city's treasury as soon as the tournament is posted, and nobody can spend it until the final ends. " +
                          "Restricted funds cannot pay a prize. Entrants pay the entry fee when they sign up, and the city keeps the fees once the final ends. " +
                          $"Available for a prize: {TournamentRules.FormatCredits(spendable)} credits.\n\n";

            if (draft.Prize == null)
            {
                var affordable = TournamentRules.PrizeChoices.Where(x => x <= spendable).ToList();
                if (affordable.Count == 0)
                {
                    page.Header += "The treasury cannot cover the smallest prize.";
                    return;
                }

                page.Header += "Choose the prize.";
                foreach (var prize in affordable)
                {
                    var choice = prize;
                    page.AddResponse($"{TournamentRules.FormatCredits(choice)} credits", () => draft.Prize = choice);
                }

                return;
            }

            page.Header += $"Prize: {TournamentRules.FormatCredits(draft.Prize.Value)} credits\n";
            if (draft.EntryFee == null)
            {
                page.Header += "\nChoose the entry fee.";
                foreach (var fee in TournamentRules.FeeChoices)
                {
                    var choice = fee;
                    page.AddResponse(choice == 0 ? "Free entry" : $"{TournamentRules.FormatCredits(choice)} credits", () => draft.EntryFee = choice);
                }

                AddStartOver(page, draft);
                return;
            }

            page.Header += draft.EntryFee.Value > 0
                ? $"Entry fee: {TournamentRules.FormatCredits(draft.EntryFee.Value)} credits\n"
                : "Entry fee: free\n";
            if (draft.LeadHours == null)
            {
                page.Header += "\nSign-ups close, and the tournament can begin, after:";
                foreach (var hours in TournamentRules.LeadHourChoices)
                {
                    var choice = hours;
                    page.AddResponse(DescribeLead(choice), () => draft.LeadHours = choice);
                }

                AddStartOver(page, draft);
                return;
            }

            page.Header += $"Sign-ups close in {DescribeLead(draft.LeadHours.Value)}.\n\nPost this tournament?";
            var player = Player;
            var ring = Owner;
            page.AddResponse(ColorToken.Green("Post the tournament"), () =>
            {
                var error = Tournaments.Post(player, ring, draft.Prize.Value, draft.EntryFee.Value, draft.LeadHours.Value);
                Report(error);
                if (string.IsNullOrEmpty(error))
                {
                    draft.Reset();
                    GoToPage(TournamentPageId, false);
                }
            });
            AddStartOver(page, draft);
        }

        private static void AddStartOver(ConversationMenuPage page, TournamentDraft draft)
        {
            page.AddResponse("Start over", draft.Reset);
        }

        private static string DescribeLead(int hours)
        {
            return hours % 24 == 0
                ? $"{hours / 24} day{(hours / 24 == 1 ? string.Empty : "s")}"
                : $"{hours} hour{(hours == 1 ? string.Empty : "s")}";
        }

        /// <summary>Tells the player why an action was refused. An empty string means it worked.</summary>
        private void Report(string error)
        {
            if (!string.IsNullOrEmpty(error))
                FloatingTextStringOnCreature(error, Player, false);
        }
    }
}
