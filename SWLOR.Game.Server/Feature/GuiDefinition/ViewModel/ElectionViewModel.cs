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
    public class ElectionViewModel: GuiViewModelBase<ElectionViewModel, GuiPayloadBase>
    {
        private List<string> _candidatePlayerIds = new List<string>();
        private int _selectedCandidateIndex;
        private string _electionId;

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

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            var cdKey = GetPCPublicCDKey(Player);
            var playerId = GetObjectUUID(Player);
            var area = GetArea(TetherObject);
            var propertyId = Property.GetPropertyId(area);
            var dbProperty = DB.Get<WorldProperty>(propertyId);
            var dbBuilding = DB.Get<WorldProperty>(dbProperty.ParentPropertyId);
            var election = DB.Search(new DBQuery<Election>()
                .AddFieldSearch(nameof(Election.PropertyId), dbBuilding.ParentPropertyId, false))
                .Single();

            if (election.Stage == ElectionStageType.Registration)
            {
                Instructions = "The election is currently in the 'Registration' period. During this time, any citizen may choose to run for mayor. This option is available for the first two weeks of the election. Below is the current list of candidates. You will be able to vote for ONE candidate during the last week of the election.";

                if (election.CandidatePlayerIds.Contains(playerId))
                {
                    MainActionButtonText = "Exit Race";
                    MainActionButtonColor = GuiColor.Red;
                }
                else
                {
                    MainActionButtonText = "Enter Race";
                    MainActionButtonColor = GuiColor.White;
                }
            }
            else if(election.Stage == ElectionStageType.Voting)
            {
                Instructions = "Please select from the list of candidates below. You may only vote for one candidate per election. This choice can be changed as many times as you'd like until the end of the election. Select the 'Abstain' option if you would prefer not to vote.";

                MainActionButtonText = "Cast Vote";
                MainActionButtonColor = GuiColor.White;
            }

            var candidates = election.CandidatePlayerIds.Count > 0
                ? DB.Search(new DBQuery<Player>()
                    .AddFieldSearch(nameof(Entity.Player.Id), election.CandidatePlayerIds))
                    .ToList()
                : new List<Player>();
            var candidateNames = new GuiBindingList<string>();
            var candidateToggles = new GuiBindingList<bool>();
            var candidateEnables = new GuiBindingList<bool>();
            var selectedCandidateId = election.VoterSelections.ContainsKey(cdKey)
                ? election.VoterSelections[cdKey].CandidatePlayerId
                : new ElectionVoter().CandidatePlayerId;
            
            _candidatePlayerIds.Clear();
            _candidatePlayerIds.Add(string.Empty);
            candidateNames.Add("[ABSTAIN]");
            candidateToggles.Add(false);
            candidateEnables.Add(election.Stage == ElectionStageType.Voting);
            _selectedCandidateIndex = 0;

            foreach (var candidate in candidates)
            {
                _candidatePlayerIds.Add(candidate.Id);
                candidateNames.Add(candidate.Name);
                if (selectedCandidateId == candidate.Id)
                {
                    candidateToggles.Add(true);
                    _selectedCandidateIndex = _candidatePlayerIds.Count - 1;
                }
                else
                {
                    candidateToggles.Add(false);
                }
                candidateEnables.Add(election.Stage == ElectionStageType.Voting);
            }

            if (_selectedCandidateIndex <= 0)
            {
                candidateToggles[0] = true;
            }

            CandidateNames = candidateNames;
            CandidateToggles = candidateToggles;
            CandidateEnables = candidateEnables;
            _electionId = election.Id;
        }

        public Action SelectCandidate() => () =>
        {
            if (_selectedCandidateIndex > -1)
                CandidateToggles[_selectedCandidateIndex] = false;

            _selectedCandidateIndex = NuiGetEventArrayIndex();
            CandidateToggles[_selectedCandidateIndex] = true;
        };

        public Action MainAction() => () =>
        {
            var playerId = GetObjectUUID(Player);
            var cdKey = GetPCPublicCDKey(Player);
            var dbElection = DB.Get<Election>(_electionId);

            bool IsCitizen()
            {
                var dbPlayer = DB.Get<Player>(playerId);
                if (dbPlayer.CitizenPropertyId != dbElection.PropertyId)
                {
                    SendMessageToPC(Player, "You must be a registered citizen of this city to run in the election.");
                    return false;
                }

                return true;
            }

            if (!IsCitizen())
                return;

            var dbCity = DB.Get<WorldProperty>(dbElection.PropertyId);

            // This button behaves differently depending on the mode the election is in.
            // If we're in the 'Registration' process, it will enter or exit the player from the race.
            // If we're in the 'Voting' process, it will cast the player's vote toward the selected candidate.
            if (dbElection.Stage == ElectionStageType.Registration)
            {
                // Player is currently in the race. 
                if (dbElection.CandidatePlayerIds.Contains(playerId))
                {
                    ShowModal("Are you sure you want to withdraw the race? Any votes cast for you will be lost should you decide to enter again.",
                        () =>
                        {
                            if (!IsCitizen())
                                return;

                            dbElection = DB.Get<Election>(_electionId);

                            if(dbElection.CandidatePlayerIds.Contains(playerId))
                                dbElection.CandidatePlayerIds.Remove(playerId);

                            foreach (var vote in dbElection.VoterSelections.ToList())
                            {
                                if (vote.Value.CandidatePlayerId != playerId)
                                    continue;

                                dbElection.VoterSelections.Remove(vote.Key);
                            }

                            DB.Set(dbElection);
                            SendMessageToPC(Player, "You have withdrawn from the race.");
                            Gui.TogglePlayerWindow(Player, GuiWindowType.Election);
                        });
                }
                else
                {
                    ShowModal($"Are you sure you want to enter the race? If you win the vote, you will immediately become the mayor of {dbCity.CustomName}. This includes being responsible for paying maintenance and upkeep fees. Will you enter the race?",
                        () =>
                        {
                            if (!IsCitizen())
                                return;

                            dbElection = DB.Get<Election>(_electionId);
                            if(!dbElection.CandidatePlayerIds.Contains(playerId))
                                dbElection.CandidatePlayerIds.Add(playerId);

                            DB.Set(dbElection);
                            SendMessageToPC(Player, "You have entered the race!");
                            Gui.TogglePlayerWindow(Player, GuiWindowType.Election);
                        });
                }
            }
            else if(dbElection.Stage == ElectionStageType.Voting)
            {
                // "Abstain" was selected.
                if (_selectedCandidateIndex <= 0)
                {
                    if (dbElection.VoterSelections.ContainsKey(cdKey))
                    {
                        dbElection.VoterSelections.Remove(cdKey);
                    }

                    SendMessageToPC(Player, "You have abstained from voting for this election.");
                    Gui.TogglePlayerWindow(Player, GuiWindowType.Election);
                }
                // An actual player was selected.
                else
                {
                    var selectedCandidateId = _candidatePlayerIds[_selectedCandidateIndex];
                    var dbCandidate = DB.Get<Player>(selectedCandidateId);

                    dbElection.VoterSelections[cdKey] = new ElectionVoter
                    {
                        CandidatePlayerId = selectedCandidateId,
                        VoterPlayerId = playerId
                    };

                    SendMessageToPC(Player, $"Your vote for {dbCandidate.Name} has been cast.");
                    Gui.TogglePlayerWindow(Player, GuiWindowType.Election);
                }

                DB.Set(dbElection);
            }
        };
    }
}
