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
        private bool _isBrowseTab;
        private readonly List<QuestContract> _rows = new();
        private int _selectedIndex = -1;

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

        public bool IsSearchVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsBrowseActionsVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsMyActionsVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public GuiBindingList<string> RowLabels
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> RowToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> RowColors
        {
            get => Get<GuiBindingList<GuiColor>>();
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

            ShowBrowse();

            WatchOnClient(model => model.SearchText);
        }

        private void ShowBrowse()
        {
            _isBrowseTab = true;
            IsBrowseTabToggled = true;
            IsMyContractsTabToggled = false;
            IsSearchVisible = true;
            IsBrowseActionsVisible = true;
            IsMyActionsVisible = false;

            LoadBrowse();
        }

        private void ShowMyContracts()
        {
            _isBrowseTab = false;
            IsBrowseTabToggled = false;
            IsMyContractsTabToggled = true;
            IsSearchVisible = false;
            IsBrowseActionsVisible = false;
            IsMyActionsVisible = true;

            LoadMyContracts();
        }

        private void LoadBrowse()
        {
            _selectedIndex = -1;
            StatusText = string.Empty;

            var query = new DBQuery<QuestContract>()
                .AddFieldSearch(nameof(QuestContract.Status), (int)QuestContractStatus.Published)
                .OrderBy(nameof(QuestContract.DatePublished), false);
            var count = (int)DB.SearchCount(query);
            var results = count > 0 ? DB.Search(query.AddPaging(count, 0)) : Enumerable.Empty<QuestContract>();

            _rows.Clear();
            var labels = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();
            var colors = new GuiBindingList<GuiColor>();

            foreach (var contract in results)
            {
                if (!string.IsNullOrWhiteSpace(SearchText) &&
                    !contract.Title.ToLower().Contains(SearchText.ToLower()))
                    continue;

                var authorName = PlayerName.GetDisplayNameByPlayerId(Player, contract.AuthorPlayerId, contract.AuthorName);

                _rows.Add(contract);
                labels.Add($"{contract.Title} - {authorName} - {contract.RewardCredits} cr - {contract.CompletionsRemaining} left");
                toggles.Add(false);
                colors.Add(GuiColor.White);
            }

            RowLabels = labels;
            RowToggles = toggles;
            RowColors = colors;

            LoadDetail();
        }

        private void LoadMyContracts()
        {
            _selectedIndex = -1;
            StatusText = string.Empty;

            var playerId = GetObjectUUID(Player);
            var query = new DBQuery<QuestContract>()
                .AddFieldSearch(nameof(QuestContract.AuthorPlayerId), playerId, false)
                .AddFieldSearch(nameof(QuestContract.Status), new[] { (int)QuestContractStatus.Draft, (int)QuestContractStatus.Published });
            var count = (int)DB.SearchCount(query);
            var results = count > 0 ? DB.Search(query.AddPaging(count, 0)) : Enumerable.Empty<QuestContract>();

            _rows.Clear();
            var labels = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();
            var colors = new GuiBindingList<GuiColor>();

            foreach (var contract in results.OrderByDescending(x => x.Status))
            {
                var statusLabel = contract.Status == QuestContractStatus.Draft ? "Draft" : "Published";

                _rows.Add(contract);
                labels.Add($"[{statusLabel}] {contract.Title} - {contract.CompletionsRemaining} completions remaining");
                toggles.Add(false);
                colors.Add(contract.Status == QuestContractStatus.Draft ? GuiColor.White : GuiColor.Green);
            }

            RowLabels = labels;
            RowToggles = toggles;
            RowColors = colors;

            LoadDetail();
        }

        private void LoadDetail()
        {
            IsAcceptVisible = false;
            IsAcceptEnabled = false;
            IsTurnInVisible = false;
            IsAbandonVisible = false;
            IsTakeDownVisible = false;
            IsCancelEnabled = false;

            if (_selectedIndex < 0 || _selectedIndex >= _rows.Count)
            {
                DetailText = _isBrowseTab
                    ? "Select a contract to view details."
                    : "Select one of your contracts to view details, or create a new one below.";
                return;
            }

            var contract = _rows[_selectedIndex];
            DetailText = BuildDetailText(contract);

            if (_isBrowseTab)
            {
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
            else
            {
                IsCancelEnabled = contract.Status == QuestContractStatus.Published;
            }
        }

        private string BuildDetailText(QuestContract contract)
        {
            var sb = new StringBuilder();

            sb.Append($"Title: {contract.Title}\n");

            if (_isBrowseTab)
            {
                var authorName = PlayerName.GetDisplayNameByPlayerId(Player, contract.AuthorPlayerId, contract.AuthorName);
                sb.Append($"Author: {authorName}\n");
            }
            else
            {
                var statusLabel = contract.Status == QuestContractStatus.Draft ? "Draft" : "Published";
                sb.Append($"Status: {statusLabel}\n");
            }

            sb.Append($"Completions Remaining: {contract.CompletionsRemaining}\n");

            if (contract.Status == QuestContractStatus.Published)
                sb.Append($"Expires: {contract.DateExpires:yyyy-MM-dd}\n");

            sb.Append('\n');
            sb.Append(string.IsNullOrWhiteSpace(contract.Description) ? "(No description)" : contract.Description);
            sb.Append("\n\nObjectives:\n");

            if (contract.Objectives.Count == 0)
            {
                sb.Append("  (None)\n");
            }
            else
            {
                foreach (var objective in contract.Objectives)
                {
                    sb.Append($"  {objective.Quantity}x {objective.ItemName}");

                    if (objective.MustBePlayerProduced)
                        sb.Append(" (player-crafted)");

                    sb.Append('\n');
                }
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

        public Action OnClickBrowseTab() => ShowBrowse;

        public Action OnClickMyContractsTab() => ShowMyContracts;

        public Action OnClickSearch() => LoadBrowse;

        public Action OnClickClearSearch() => () =>
        {
            SearchText = string.Empty;
            LoadBrowse();
        };

        public Action OnClickSelectRow() => () =>
        {
            if (_selectedIndex > -1 && _selectedIndex < RowToggles.Count)
                RowToggles[_selectedIndex] = false;

            var index = NuiGetEventArrayIndex();
            _selectedIndex = index;
            RowToggles[index] = true;
            StatusText = string.Empty;

            LoadDetail();
        };

        public Action OnClickAccept() => () =>
        {
            if (!_isBrowseTab || _selectedIndex < 0 || _selectedIndex >= _rows.Count) return;

            var contract = _rows[_selectedIndex];
            var questId = QuestContractFactory.BuildQuestId(contract.Id);

            if (Quest.GetQuestByIdOrDefault(questId) == null)
            {
                StatusText = "This contract is no longer available.";
                LoadBrowse();
                return;
            }

            Quest.AcceptQuest(Player, questId);

            LoadDetail();
        };

        public Action OnClickTurnIn() => () =>
        {
            if (!_isBrowseTab || _selectedIndex < 0 || _selectedIndex >= _rows.Count) return;

            var contract = _rows[_selectedIndex];
            Quest.RequestItemsFromPlayer(Player, QuestContractFactory.BuildQuestId(contract.Id));
        };

        public Action OnClickAbandon() => () =>
        {
            if (!_isBrowseTab || _selectedIndex < 0 || _selectedIndex >= _rows.Count) return;

            var contract = _rows[_selectedIndex];

            ShowModal("Are you sure you wish to abandon this contract?", () =>
            {
                Quest.AbandonQuest(Player, QuestContractFactory.BuildQuestId(contract.Id));
                LoadDetail();
            });
        };

        public Action OnClickTakeDown() => () =>
        {
            if (!_isBrowseTab || _selectedIndex < 0 || _selectedIndex >= _rows.Count) return;

            var contract = _rows[_selectedIndex];

            ShowModal($"Take down the contract '{contract.Title}'? Remaining escrow will be refunded to its author.", () =>
            {
                var error = QuestContractBoard.CancelContract(Player, contract.Id);
                StatusText = error;
                LoadBrowse();
            });
        };

        public Action OnClickEditDraft() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.QuestContractEditor, null, TetherObject);
        };

        public Action OnClickCancelContract() => () =>
        {
            if (_isBrowseTab || _selectedIndex < 0 || _selectedIndex >= _rows.Count) return;

            var contract = _rows[_selectedIndex];

            ShowModal($"Cancel the contract '{contract.Title}'? Remaining escrow will be refunded to you. The posting fee is not refunded.", () =>
            {
                var error = QuestContractBoard.CancelContract(Player, contract.Id);
                StatusText = error;
                LoadMyContracts();
            });
        };

        public Action OnClickClaimDeliveries() => () =>
        {
            QuestContractBoard.ClaimDeliveries(Player);
        };

        public void Refresh(QuestContractPublishedRefreshEvent payload)
        {
            if (_isBrowseTab)
                LoadBrowse();
            else
                LoadMyContracts();
        }
    }
}
