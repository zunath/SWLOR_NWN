using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Properties.Service;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Abstractions.Models;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Properties.Entities;
using SWLOR.Shared.Domain.Properties.Enums;
using SWLOR.Shared.Domain.Repositories;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Properties.UI.ViewModel
{
    public class ElectionViewModel: GuiViewModelBase<ElectionViewModel, IGuiPayload>
    {
        private readonly IWorldPropertyRepository _worldPropertyRepository;
        private readonly IElectionRepository _electionRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly IServiceProvider _serviceProvider;

        public ElectionViewModel(IGuiService guiService, IWorldPropertyRepository worldPropertyRepository, IElectionRepository electionRepository, IPlayerRepository playerRepository, IServiceProvider serviceProvider) : base(guiService)
        {
            _worldPropertyRepository = worldPropertyRepository;
            _electionRepository = electionRepository;
            _playerRepository = playerRepository;
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded service to break circular dependency
        private PropertyService Property => _serviceProvider.GetRequiredService<PropertyService>();
        
        private readonly List<string> _candidatePlayerIds = new();
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

        protected override void Initialize(IGuiPayload initialPayload)
        {
            var cdKey = GetPCPublicCDKey(Player);
            var playerId = GetObjectUUID(Player);
            var area = GetArea(TetherObject);
            var propertyId = Property.GetPropertyId(area);
            var dbProperty = _worldPropertyRepository.GetById(propertyId);
            var dbBuilding = _worldPropertyRepository.GetById(dbProperty.ParentPropertyId);
            var election = _electionRepository.GetSingleByPropertyId(dbBuilding.ParentPropertyId);

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
                ? election.CandidatePlayerIds.Select(id => _playerRepository.GetById(id)).Where(p => p != null).ToList()
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
            var dbElection = _electionRepository.GetById(_electionId);

            bool IsCitizen()
            {
                var dbPlayer = _playerRepository.GetById(playerId);
                if (dbPlayer.CitizenPropertyId != dbElection.PropertyId)
                {
                    SendMessageToPC(Player, "You must be a registered citizen of this city to run in the election.");
                    return false;
                }

                return true;
            }

            if (!IsCitizen())
                return;

            var dbCity = _worldPropertyRepository.GetById(dbElection.PropertyId);

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

                            dbElection = _electionRepository.GetById(_electionId);

                            if(dbElection.CandidatePlayerIds.Contains(playerId))
                                dbElection.CandidatePlayerIds.Remove(playerId);

                            foreach (var vote in dbElection.VoterSelections.ToList())
                            {
                                if (vote.Value.CandidatePlayerId != playerId)
                                    continue;

                                dbElection.VoterSelections.Remove(vote.Key);
                            }

                            _electionRepository.Save(dbElection);
                            SendMessageToPC(Player, "You have withdrawn from the race.");
                            _guiService.TogglePlayerWindow(Player, GuiWindowType.Election);
                        });
                }
                else
                {
                    ShowModal($"Are you sure you want to enter the race? If you win the vote, you will immediately become the mayor of {dbCity.CustomName}. This includes being responsible for paying maintenance and upkeep fees. Will you enter the race?",
                        () =>
                        {
                            if (!IsCitizen())
                                return;

                            dbElection = _electionRepository.GetById(_electionId);
                            if(!dbElection.CandidatePlayerIds.Contains(playerId))
                                dbElection.CandidatePlayerIds.Add(playerId);

                            _electionRepository.Save(dbElection);
                            SendMessageToPC(Player, "You have entered the race!");
                            _guiService.TogglePlayerWindow(Player, GuiWindowType.Election);
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
                    _guiService.TogglePlayerWindow(Player, GuiWindowType.Election);
                }
                // An actual player was selected.
                else
                {
                    var selectedCandidateId = _candidatePlayerIds[_selectedCandidateIndex];
                    var dbCandidate = _playerRepository.GetById(selectedCandidateId);

                    dbElection.VoterSelections[cdKey] = new ElectionVoter
                    {
                        CandidatePlayerId = selectedCandidateId,
                        VoterPlayerId = playerId
                    };

                    SendMessageToPC(Player, $"Your vote for {dbCandidate.Name} has been cast.");
                    _guiService.TogglePlayerWindow(Player, GuiWindowType.Election);
                }

                _electionRepository.Save(dbElection);
            }
        };
    }
}
