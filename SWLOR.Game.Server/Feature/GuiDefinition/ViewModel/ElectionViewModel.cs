using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// A city's election window (Player City Revamp, "Election changes"): the candidates and their statements, the
    /// statement editor during registration, and the vote. A lone candidate gets a Yes or No confidence vote.
    /// </summary>
    public class ElectionViewModel: GuiViewModelBase<ElectionViewModel, GuiPayloadBase>
    {
        public const string MainContentElement = "election_content";
        public const string MainContentPartial = "ELECTION_MAIN";

        private const string AbstainName = "[ABSTAIN]";
        private const string NoStatementText = "This candidate has not written a statement.";

        // Row i of the candidate list shows _candidatePlayerIds[i]. When the list starts with the abstain row,
        // the first entry is empty.
        private readonly List<string> _candidatePlayerIds = new();
        private readonly List<string> _statements = new();
        private int _selectedCandidateIndex;
        private string _electionId = string.Empty;
        private bool _hasAbstainRow;

        public GuiBindingList<string> CandidateNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> CandidateToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<bool> CandidateEnables
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public string Instructions
        {
            get => Get<string>();
            set => Set(value);
        }

        /// <summary>The current stage and when it closes.</summary>
        public string ScheduleText
        {
            get => Get<string>();
            set => Set(value);
        }

        /// <summary>The statement of the candidate selected in the list.</summary>
        public string SelectedStatement
        {
            get => Get<string>();
            set => Set(value);
        }

        /// <summary>The text in the candidate's statement editor.</summary>
        public string StatementInput
        {
            get => Get<string>();
            set => Set(value);
        }

        /// <summary>Who the player voted for, or that they have not voted.</summary>
        public string VoteStatusText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MainActionButtonText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor MainActionButtonColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        /// <summary>Enter or Exit Race during registration, Cast Vote in a contested vote.</summary>
        public bool IsMainActionVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        /// <summary>The statement editor and its Save button: registration, for a citizen.</summary>
        public bool IsStatementEditorVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        /// <summary>Save Statement is offered once the player is in the race.</summary>
        public bool IsSaveStatementVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        /// <summary>The Yes, No and Clear Vote buttons of a confidence vote.</summary>
        public bool IsConfidenceVoteVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsVoteStatusVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            LoadData(false);

            // The statement is the only value the client changes.
            WatchOnClient(model => model.StatementInput);

            ChangePartialView(MainContentElement, MainContentPartial);
        }

        private Election GetElection()
        {
            var area = GetArea(TetherObject);
            var propertyId = Property.GetPropertyId(area);
            var dbProperty = DB.Get<WorldProperty>(propertyId);
            var dbBuilding = DB.Get<WorldProperty>(dbProperty.ParentPropertyId);
            return ElectionBallot.GetCityElection(dbBuilding.ParentPropertyId);
        }

        /// <summary>
        /// Reads the election and assigns every bound property. Called when the window opens and after each action.
        /// </summary>
        /// <param name="keepStatementInput">True to keep what the player has typed in the statement editor.</param>
        private void LoadData(bool keepStatementInput)
        {
            var cdKey = GetPCPublicCDKey(Player);
            var playerId = GetObjectUUID(Player);
            var now = DateTime.UtcNow;
            var election = GetElection();
            var dbPlayer = DB.Get<Entity.Player>(playerId);

            var names = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();
            var enables = new GuiBindingList<bool>();
            _candidatePlayerIds.Clear();
            _statements.Clear();
            _selectedCandidateIndex = 0;
            _hasAbstainRow = false;

            if (election == null)
            {
                _electionId = string.Empty;
                Instructions = "There is no election open right now. Check the Manage Citizenship window for the date the next one opens.";
                ScheduleText = string.Empty;
                SelectedStatement = string.Empty;
                VoteStatusText = string.Empty;
                MainActionButtonText = string.Empty;
                MainActionButtonColor = GuiColor.White;
                IsMainActionVisible = false;
                IsStatementEditorVisible = false;
                IsSaveStatementVisible = false;
                IsConfidenceVoteVisible = false;
                IsVoteStatusVisible = false;
                if (!keepStatementInput)
                    StatementInput = string.Empty;

                CandidateNames = names;
                CandidateToggles = toggles;
                CandidateEnables = enables;
                return;
            }

            _electionId = election.Id;

            var isCitizen = dbPlayer != null &&
                            ElectionRules.IsCitizenOf(dbPlayer.CitizenPropertyId, election.PropertyId, dbPlayer.IsDeleted);
            var isCandidate = election.CandidatePlayerIds.Contains(playerId);
            var registrationOpen = ElectionRules.IsRegistrationOpen(election.Stage, now, election.RegistrationClosesUtc);
            var votingOpen = ElectionRules.IsVotingOpen(election.Stage, now, election.VotingClosesUtc);
            var canVote = ElectionBallot.IsEligibleVoter(dbPlayer, election);
            var isRegistration = election.Stage == ElectionStageType.Registration;
            var isConfidence = election.Stage == ElectionStageType.Voting && election.IsConfidenceVote;
            var isContested = election.Stage == ElectionStageType.Voting && !election.IsConfidenceVote;
            var currentVote = ElectionBallot.GetVote(election, cdKey);

            // The abstain row lets a voter in a contested vote take their vote back.
            if (isContested)
            {
                _hasAbstainRow = true;
                _candidatePlayerIds.Add(string.Empty);
                _statements.Add("Select this to vote for nobody. If you have already voted, pressing Cast Vote takes your vote back.");
                names.Add(AbstainName);
                toggles.Add(false);
                enables.Add(true);
            }

            var candidates = election.CandidatePlayerIds.Count > 0
                ? DB.Search(new DBQuery<Entity.Player>()
                        .AddFieldSearch(nameof(Entity.Player.Id), election.CandidatePlayerIds))
                    .ToDictionary(candidate => candidate.Id)
                : new Dictionary<string, Entity.Player>();

            foreach (var candidateId in election.CandidatePlayerIds)
            {
                if (!candidates.TryGetValue(candidateId, out var candidate))
                    continue;

                _candidatePlayerIds.Add(candidateId);
                names.Add(PlayerName.GetPlainDisplayNameByPlayerId(Player, candidateId, candidate.Name));
                toggles.Add(false);
                enables.Add(true);

                var statement = election.Platforms != null && election.Platforms.TryGetValue(candidateId, out var platform)
                    ? platform.Statement
                    : string.Empty;
                _statements.Add(string.IsNullOrWhiteSpace(statement) ? NoStatementText : statement);
            }

            // A contested voter starts on their current choice; otherwise the list starts on its first row.
            if (isContested && currentVote != null)
            {
                var votedIndex = _candidatePlayerIds.IndexOf(currentVote.CandidatePlayerId);
                _selectedCandidateIndex = votedIndex > 0 ? votedIndex : 0;
            }

            if (toggles.Count > 0)
                toggles[_selectedCandidateIndex] = true;

            CandidateNames = names;
            CandidateToggles = toggles;
            CandidateEnables = enables;
            SelectedStatement = _statements.Count > 0 ? _statements[_selectedCandidateIndex] : "No candidates have registered yet.";

            ScheduleText = BuildScheduleText(election, now, registrationOpen, votingOpen);
            Instructions = BuildInstructions(election, isCitizen, canVote, registrationOpen, votingOpen);

            IsStatementEditorVisible = isRegistration && registrationOpen && isCitizen;
            IsSaveStatementVisible = IsStatementEditorVisible && isCandidate;
            IsMainActionVisible = (isRegistration && registrationOpen && isCitizen) || (isContested && votingOpen && canVote);
            IsConfidenceVoteVisible = isConfidence && votingOpen && canVote;
            IsVoteStatusVisible = election.Stage == ElectionStageType.Voting;

            if (isRegistration)
            {
                MainActionButtonText = isCandidate ? "Exit Race" : "Enter Race";
                MainActionButtonColor = isCandidate ? GuiColor.Red : GuiColor.White;
            }
            else
            {
                MainActionButtonText = "Cast Vote";
                MainActionButtonColor = GuiColor.White;
            }

            VoteStatusText = BuildVoteStatusText(election, currentVote, candidates, canVote);

            if (!keepStatementInput)
            {
                StatementInput = isCandidate && election.Platforms != null &&
                                 election.Platforms.TryGetValue(playerId, out var ownPlatform)
                    ? ownPlatform.Statement
                    : string.Empty;
            }
        }

        private static string BuildScheduleText(Election election, DateTime now, bool registrationOpen, bool votingOpen)
        {
            if (election.Stage == ElectionStageType.Registration)
            {
                return registrationOpen
                    ? $"Registration closes {election.RegistrationClosesUtc:yyyy-MM-dd HH:mm} UTC ({ElectionRules.GetDaysLeft(now, election.RegistrationClosesUtc)} days left)."
                    : $"Registration closed {election.RegistrationClosesUtc:yyyy-MM-dd HH:mm} UTC.";
            }

            return votingOpen
                ? $"Voting closes {election.VotingClosesUtc:yyyy-MM-dd HH:mm} UTC ({ElectionRules.GetDaysLeft(now, election.VotingClosesUtc)} days left)."
                : $"Voting closed {election.VotingClosesUtc:yyyy-MM-dd HH:mm} UTC.";
        }

        private static string BuildInstructions(
            Election election,
            bool isCitizen,
            bool canVote,
            bool registrationOpen,
            bool votingOpen)
        {
            var kindNote = election.Kind switch
            {
                ElectionKind.Recall => "This is a recall election, opened because the city's politics were neglected. ",
                ElectionKind.SnapConfidence => "This is a snap election, opened because citizens voted No on the mayor. ",
                ElectionKind.SnapStepDown => "This is a snap election, opened because the mayor stepped down. ",
                _ => string.Empty
            };

            if (election.Stage == ElectionStageType.Registration)
            {
                if (!registrationOpen)
                {
                    return kindNote + "Registration has closed and the ballot is fixed. Voting opens when the server next restarts.";
                }

                var text = kindNote +
                           "Candidate registration is open. Any citizen may run for mayor and write a statement of up to " +
                           $"{ElectionRules.StatementMaxLength} characters. Candidates can edit their statement or withdraw until registration closes. " +
                           "Select a candidate to read their statement. If only one candidate registers, citizens vote Yes or No on them; otherwise citizens vote for one candidate.";
                return isCitizen ? text : text + " Only citizens of this city can run.";
            }

            if (!votingOpen)
                return kindNote + "Voting has closed. The result posts when the server next restarts.";

            var voteText = election.IsConfidenceVote
                ? kindNote + "Only one candidate registered, so citizens vote Yes or No on them. You have one vote and can change or clear it until voting closes."
                : kindNote + "Select a candidate and press Cast Vote. You have one vote and can change it until voting closes. Select [ABSTAIN] and press Cast Vote to take your vote back.";

            if (canVote)
            {
                return voteText + $" Casting a vote earns {VoterRewardRules.RewardPoints} RP points, once every {VoterRewardRules.IntervalDays} days.";
            }

            return voteText + (isCitizen
                ? " You joined the city after registration opened, so you cannot vote in this election."
                : " Only citizens of this city can vote.");
        }

        private static string BuildVoteStatusText(
            Election election,
            ElectionVoter currentVote,
            IReadOnlyDictionary<string, Entity.Player> candidates,
            bool canVote)
        {
            if (election.Stage != ElectionStageType.Voting)
                return string.Empty;

            if (!canVote)
                return "You cannot vote in this election.";

            if (currentVote == null || !candidates.ContainsKey(currentVote.CandidatePlayerId))
                return "You have not voted.";

            if (election.IsConfidenceVote)
                return currentVote.IsNo ? "Your vote: No." : "Your vote: Yes.";

            return "You have voted.";
        }

        public Action SelectCandidate() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= CandidateToggles.Count)
                return;

            if (_selectedCandidateIndex > -1 && _selectedCandidateIndex < CandidateToggles.Count)
                CandidateToggles[_selectedCandidateIndex] = false;

            _selectedCandidateIndex = index;
            CandidateToggles[_selectedCandidateIndex] = true;
            SelectedStatement = _statements[_selectedCandidateIndex];
        };

        /// <summary>
        /// Enters or leaves the race during registration, and casts the selected vote in a contested vote.
        /// </summary>
        public Action MainAction() => () =>
        {
            var playerId = GetObjectUUID(Player);
            var election = GetElection();
            if (election == null || election.Id != _electionId)
            {
                SendMessageToPC(Player, "This election has ended.");
                LoadData(true);
                return;
            }

            if (election.Stage == ElectionStageType.Registration)
            {
                var dbCity = DB.Get<WorldProperty>(election.PropertyId);
                if (election.CandidatePlayerIds.Contains(playerId))
                {
                    ShowModal("Are you sure you want to withdraw the race? Any votes cast for you will be lost should you decide to enter again.",
                        () =>
                        {
                            var error = ElectionBallot.WithdrawCandidate(_electionId, playerId);
                            SendMessageToPC(Player, string.IsNullOrEmpty(error) ? "You have withdrawn from the race." : error);
                            LoadData(true);
                        });
                }
                else
                {
                    ShowModal($"Are you sure you want to enter the race? If you win the vote, you will immediately become the mayor of {dbCity.CustomName}. This includes being responsible for paying maintenance and upkeep fees. Your statement is saved with your entry, and you can edit it until registration closes. Will you enter the race?",
                        () =>
                        {
                            var error = ElectionBallot.RegisterCandidate(_electionId, playerId, StatementInput);
                            SendMessageToPC(Player, string.IsNullOrEmpty(error) ? "You have entered the race!" : error);
                            LoadData(false);
                        });
                }

                return;
            }

            // A contested vote: the abstain row takes the vote back.
            if (_selectedCandidateIndex <= 0 && _hasAbstainRow)
            {
                ClearVoteAndReport();
                return;
            }

            if (_selectedCandidateIndex < 0 || _selectedCandidateIndex >= _candidatePlayerIds.Count)
                return;

            CastVoteAndReport(_candidatePlayerIds[_selectedCandidateIndex], false);
        };

        /// <summary>Saves the statement of a candidate who is already in the race.</summary>
        public Action SaveStatement() => () =>
        {
            var error = ElectionBallot.UpdateStatement(_electionId, GetObjectUUID(Player), StatementInput);
            SendMessageToPC(Player, string.IsNullOrEmpty(error) ? "Your statement has been saved." : error);
            LoadData(false);
        };

        public Action VoteYes() => () => CastConfidenceVote(false);

        public Action VoteNo() => () => CastConfidenceVote(true);

        public Action ClearVote() => () => ClearVoteAndReport();

        private void CastConfidenceVote(bool isNo)
        {
            var election = DB.Get<Election>(_electionId);
            if (election == null || election.CandidatePlayerIds.Count != 1)
            {
                SendMessageToPC(Player, "This election has ended.");
                LoadData(true);
                return;
            }

            CastVoteAndReport(election.CandidatePlayerIds[0], isNo);
        }

        private void CastVoteAndReport(string candidateId, bool isNo)
        {
            var playerId = GetObjectUUID(Player);
            var cdKey = GetPCPublicCDKey(Player);
            var error = ElectionBallot.CastVote(_electionId, playerId, cdKey, candidateId, isNo, out var rewardPoints);
            if (!string.IsNullOrEmpty(error))
            {
                SendMessageToPC(Player, error);
                LoadData(true);
                return;
            }

            var election = DB.Get<Election>(_electionId);
            var message = election != null && election.IsConfidenceVote
                ? $"Your {(isNo ? "No" : "Yes")} vote has been cast."
                : $"Your vote for {PlayerName.GetDisplayNameByPlayerId(Player, candidateId, DB.Get<Entity.Player>(candidateId)?.Name)} has been cast.";
            if (rewardPoints > 0)
                message += $" You earned {rewardPoints} RP points for voting.";

            SendMessageToPC(Player, message);
            LoadData(true);
        }

        private void ClearVoteAndReport()
        {
            var error = ElectionBallot.ClearVote(_electionId, GetObjectUUID(Player), GetPCPublicCDKey(Player));
            SendMessageToPC(Player, string.IsNullOrEmpty(error) ? "You have abstained from voting for this election." : error);
            LoadData(true);
        }
    }
}
