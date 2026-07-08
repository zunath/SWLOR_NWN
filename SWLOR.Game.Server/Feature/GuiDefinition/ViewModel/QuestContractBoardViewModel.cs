using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.QuestContractService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class QuestContractBoardViewModel: GuiViewModelBase<QuestContractBoardViewModel, GuiPayloadBase>,
        IGuiRefreshable<QuestContractPublishedRefreshEvent>
    {
        private bool _isDM;
        private readonly List<QuestContract> _browseContracts = new();
        private int _selectedBrowseIndex = -1;

        private readonly List<QuestContract> _myContracts = new();
        private int _selectedMyContractIndex = -1;

        public string SearchText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsBrowseTabToggled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsMyContractsTabToggled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsBrowseVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsMyContractsVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public GuiBindingList<string> BrowseLabels
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> BrowseToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public string DetailText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string StatusText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsAcceptVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsAcceptEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsTurnInVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsAbandonVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsTakeDownVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public GuiBindingList<string> MyContractLabels
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> MyContractToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> MyContractColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public bool IsCancelEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            var authLevel = Authorization.GetAuthorizationLevel(Player);
            _isDM = authLevel == AuthorizationLevel.DM || authLevel == AuthorizationLevel.Admin;

            SearchText = string.Empty;
            StatusText = string.Empty;
            IsBrowseVisible = true;
            IsMyContractsVisible = false;
            IsBrowseTabToggled = true;
            IsMyContractsTabToggled = false;

            Search();

            WatchOnClient(model => model.SearchText);
        }

        private void Search()
        {
            _selectedBrowseIndex = -1;
            StatusText = string.Empty;

            var query = new DBQuery<QuestContract>()
                .AddFieldSearch(nameof(QuestContract.Status), (int)QuestContractStatus.Published)
                .OrderBy(nameof(QuestContract.DatePublished), false);
            var count = (int)DB.SearchCount(query);
            var results = count > 0 ? DB.Search(query.AddPaging(count, 0)) : Enumerable.Empty<QuestContract>();

            _browseContracts.Clear();
            var labels = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();

            foreach (var contract in results)
            {
                if (!string.IsNullOrWhiteSpace(SearchText) &&
                    !contract.Title.ToLower().Contains(SearchText.ToLower()))
                    continue;

                var authorName = PlayerName.GetDisplayNameByPlayerId(Player, contract.AuthorPlayerId, contract.AuthorName);

                _browseContracts.Add(contract);
                labels.Add($"{contract.Title} - {authorName} - {contract.RewardCredits} cr - {contract.CompletionsRemaining} left");
                toggles.Add(false);
            }

            BrowseLabels = labels;
            BrowseToggles = toggles;

            LoadBrowseDetail();
        }

        private void LoadBrowseDetail()
        {
            IsAcceptVisible = false;
            IsAcceptEnabled = false;
            IsTurnInVisible = false;
            IsAbandonVisible = false;
            IsTakeDownVisible = false;

            if (_selectedBrowseIndex < 0 || _selectedBrowseIndex >= _browseContracts.Count)
            {
                DetailText = "Select a contract to view details.";
                return;
            }

            var contract = _browseContracts[_selectedBrowseIndex];
            DetailText = BuildDetailText(contract);

            var questId = QuestContractFactory.BuildQuestId(contract.Id);
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var hasActive = dbPlayer.Quests.TryGetValue(questId, out var playerQuest) && playerQuest.DateLastCompleted == null;
            var isStillRegistered = Quest.GetQuestByIdOrDefault(questId) != null;

            IsAcceptVisible = !hasActive;
            IsAcceptEnabled = !_isDM && isStillRegistered && Quest.CanAcceptQuest(Player, questId);
            IsTurnInVisible = hasActive && isStillRegistered;
            IsAbandonVisible = hasActive && isStillRegistered;
            IsTakeDownVisible = _isDM;

            if (!isStillRegistered && !hasActive)
                DetailText += "\n\nThis contract is no longer available.";
        }

        private string BuildDetailText(QuestContract contract)
        {
            var authorName = PlayerName.GetDisplayNameByPlayerId(Player, contract.AuthorPlayerId, contract.AuthorName);
            var sb = new StringBuilder();

            sb.Append($"Title: {contract.Title}\n");
            sb.Append($"Author: {authorName}\n");
            sb.Append($"Completions Remaining: {contract.CompletionsRemaining}\n");
            sb.Append($"Expires: {contract.DateExpires:yyyy-MM-dd}\n\n");
            sb.Append(contract.Description);
            sb.Append("\n\nObjectives:\n");

            foreach (var objective in contract.Objectives)
            {
                sb.Append($"  {objective.Quantity}x {objective.ItemName}");

                if (objective.MustBePlayerProduced)
                    sb.Append(" (player-crafted)");

                sb.Append('\n');
            }

            sb.Append("\nRewards:\n");

            if (contract.RewardCredits > 0)
                sb.Append($"  {contract.RewardCredits} credits\n");

            foreach (var rewardItem in contract.RewardItems)
            {
                sb.Append($"  {rewardItem.Name}\n");
            }

            return sb.ToString();
        }

        private void SearchMyContracts()
        {
            _selectedMyContractIndex = -1;
            StatusText = string.Empty;

            var playerId = GetObjectUUID(Player);
            var query = new DBQuery<QuestContract>()
                .AddFieldSearch(nameof(QuestContract.AuthorPlayerId), playerId, false)
                .AddFieldSearch(nameof(QuestContract.Status), new[] { (int)QuestContractStatus.Draft, (int)QuestContractStatus.Published });
            var count = (int)DB.SearchCount(query);
            var results = count > 0 ? DB.Search(query.AddPaging(count, 0)) : Enumerable.Empty<QuestContract>();

            _myContracts.Clear();
            var labels = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();
            var colors = new GuiBindingList<GuiColor>();

            foreach (var contract in results.OrderByDescending(x => x.Status))
            {
                var statusLabel = contract.Status == QuestContractStatus.Draft ? "Draft" : "Published";

                _myContracts.Add(contract);
                labels.Add($"[{statusLabel}] {contract.Title} - {contract.CompletionsRemaining} completions remaining");
                toggles.Add(false);
                colors.Add(contract.Status == QuestContractStatus.Draft ? GuiColor.White : GuiColor.Green);
            }

            MyContractLabels = labels;
            MyContractToggles = toggles;
            MyContractColors = colors;
            IsCancelEnabled = false;
        }

        public Action OnClickBrowseTab() => () =>
        {
            IsBrowseVisible = true;
            IsMyContractsVisible = false;
            IsBrowseTabToggled = true;
            IsMyContractsTabToggled = false;

            Search();
        };

        public Action OnClickMyContractsTab() => () =>
        {
            IsBrowseVisible = false;
            IsMyContractsVisible = true;
            IsBrowseTabToggled = false;
            IsMyContractsTabToggled = true;

            SearchMyContracts();
        };

        public Action OnClickSearch() => Search;

        public Action OnClickClearSearch() => () =>
        {
            SearchText = string.Empty;
            Search();
        };

        public Action OnClickSelectContract() => () =>
        {
            if (_selectedBrowseIndex > -1 && _selectedBrowseIndex < BrowseToggles.Count)
                BrowseToggles[_selectedBrowseIndex] = false;

            var index = NuiGetEventArrayIndex();
            _selectedBrowseIndex = index;
            BrowseToggles[index] = true;
            StatusText = string.Empty;

            LoadBrowseDetail();
        };

        public Action OnClickAccept() => () =>
        {
            if (_selectedBrowseIndex < 0 || _selectedBrowseIndex >= _browseContracts.Count) return;

            var contract = _browseContracts[_selectedBrowseIndex];
            var questId = QuestContractFactory.BuildQuestId(contract.Id);

            if (Quest.GetQuestByIdOrDefault(questId) == null)
            {
                StatusText = "This contract is no longer available.";
                Search();
                return;
            }

            Quest.AcceptQuest(Player, questId);

            LoadBrowseDetail();
        };

        public Action OnClickTurnIn() => () =>
        {
            if (_selectedBrowseIndex < 0 || _selectedBrowseIndex >= _browseContracts.Count) return;

            var contract = _browseContracts[_selectedBrowseIndex];
            Quest.RequestItemsFromPlayer(Player, QuestContractFactory.BuildQuestId(contract.Id));
        };

        public Action OnClickAbandon() => () =>
        {
            if (_selectedBrowseIndex < 0 || _selectedBrowseIndex >= _browseContracts.Count) return;

            var contract = _browseContracts[_selectedBrowseIndex];

            ShowModal("Are you sure you wish to abandon this contract?", () =>
            {
                Quest.AbandonQuest(Player, QuestContractFactory.BuildQuestId(contract.Id));
                LoadBrowseDetail();
            });
        };

        public Action OnClickTakeDown() => () =>
        {
            if (_selectedBrowseIndex < 0 || _selectedBrowseIndex >= _browseContracts.Count) return;

            var contract = _browseContracts[_selectedBrowseIndex];

            ShowModal($"Take down the contract '{contract.Title}'? Remaining escrow will be refunded to its author.", () =>
            {
                var error = QuestContractBoard.CancelContract(Player, contract.Id);
                StatusText = error;
                Search();
            });
        };

        public Action OnClickSelectMyContract() => () =>
        {
            if (_selectedMyContractIndex > -1 && _selectedMyContractIndex < MyContractToggles.Count)
                MyContractToggles[_selectedMyContractIndex] = false;

            var index = NuiGetEventArrayIndex();
            _selectedMyContractIndex = index;
            MyContractToggles[index] = true;

            var contract = _myContracts[index];
            IsCancelEnabled = contract.Status == QuestContractStatus.Published;
        };

        public Action OnClickEditDraft() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.QuestContractEditor, null, TetherObject);
        };

        public Action OnClickCancelContract() => () =>
        {
            if (_selectedMyContractIndex < 0 || _selectedMyContractIndex >= _myContracts.Count) return;

            var contract = _myContracts[_selectedMyContractIndex];

            ShowModal($"Cancel the contract '{contract.Title}'? Remaining escrow will be refunded to you. The posting fee is not refunded.", () =>
            {
                var error = QuestContractBoard.CancelContract(Player, contract.Id);
                StatusText = error;
                SearchMyContracts();
            });
        };

        public Action OnClickClaimDeliveries() => () =>
        {
            QuestContractBoard.ClaimDeliveries(Player);
        };

        public void Refresh(QuestContractPublishedRefreshEvent payload)
        {
            if (IsMyContractsVisible)
                SearchMyContracts();
            else
                Search();
        }
    }
}
