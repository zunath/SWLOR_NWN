using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.QuestContractService;
using SWLOR.NWN.API.NWNX;

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

        public bool IsContractSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public GuiBindingList<string> ObjectiveIconResrefs
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> ObjectiveLabels
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> RewardIconResrefs
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> RewardLabels
        {
            get => Get<GuiBindingList<string>>();
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

        public string CancelButtonText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsNewContractEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsEditDraftEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsClaimDeliveriesEnabled
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
            IsSearchVisible = true;
            IsBrowseActionsVisible = false;
            IsMyActionsVisible = true;

            LoadMyContracts();
        }

        private void LoadBrowse()
        {
            _selectedIndex = -1;
            StatusText = string.Empty;

            // Sorted in memory: DatePublished is not an indexed field, so RediSearch cannot sort on it.
            var query = new DBQuery<QuestContract>()
                .AddFieldSearch(nameof(QuestContract.Status), (int)QuestContractStatus.Published);
            var count = (int)DB.SearchCount(query);
            var results = count > 0
                ? DB.Search(query.AddPaging(count, 0)).OrderByDescending(x => x.DatePublished)
                : Enumerable.Empty<QuestContract>();

            _rows.Clear();
            var labels = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();
            var colors = new GuiBindingList<GuiColor>();

            foreach (var contract in results)
            {
                if (!string.IsNullOrWhiteSpace(SearchText) &&
                    !contract.Title.ToLower().Contains(SearchText.ToLower()))
                    continue;

                var authorName = PlayerName.GetPlainDisplayNameByPlayerId(Player, contract.AuthorPlayerId, contract.AuthorName);

                _rows.Add(contract);
                labels.Add($"{contract.Title} - {authorName} - {contract.RewardCredits} cr");
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
                if (!string.IsNullOrWhiteSpace(SearchText) &&
                    !contract.Title.ToLower().Contains(SearchText.ToLower()))
                    continue;

                var statusLabel = contract.Status == QuestContractStatus.Draft ? "Draft" : "Published";

                _rows.Add(contract);
                labels.Add($"[{statusLabel}] {contract.Title}");
                toggles.Add(false);
                colors.Add(contract.Status == QuestContractStatus.Draft ? GuiColor.White : GuiColor.Green);
            }

            RowLabels = labels;
            RowToggles = toggles;
            RowColors = colors;

            var hasDraft = QuestContractBoard.GetDraft(Player) != null;
            IsNewContractEnabled = !hasDraft;
            IsEditDraftEnabled = hasDraft;

            UpdateClaimDeliveriesEnabled();

            LoadDetail();
        }

        private void UpdateClaimDeliveriesEnabled()
        {
            var playerId = GetObjectUUID(Player);
            var query = new DBQuery<QuestContractDelivery>()
                .AddFieldSearch(nameof(QuestContractDelivery.PlayerId), playerId, false);

            IsClaimDeliveriesEnabled = DB.SearchCount(query) > 0;
        }

        private void LoadDetail()
        {
            IsAcceptVisible = false;
            IsAcceptEnabled = false;
            IsTurnInVisible = false;
            IsAbandonVisible = false;
            IsTakeDownVisible = false;
            IsCancelEnabled = false;
            CancelButtonText = "Cancel";

            if (_selectedIndex < 0 || _selectedIndex >= _rows.Count)
            {
                DetailText = _isBrowseTab
                    ? "Select a contract to view details."
                    : "Select one of your contracts to view details, or create a new one below.";
                IsContractSelected = false;
                ObjectiveIconResrefs = new GuiBindingList<string>();
                ObjectiveLabels = new GuiBindingList<string>();
                RewardIconResrefs = new GuiBindingList<string>();
                RewardLabels = new GuiBindingList<string>();
                return;
            }

            var contract = _rows[_selectedIndex];
            DetailText = BuildDetailText(contract);
            IsContractSelected = true;
            LoadDetailItems(contract);

            if (_isBrowseTab)
            {
                var questId = QuestContractFactory.BuildQuestId(contract.Id);
                var playerId = GetObjectUUID(Player);
                var dbPlayer = DB.Get<Player>(playerId);
                var hasActive = dbPlayer.Quests.TryGetValue(questId, out var playerQuest) && playerQuest.DateLastCompleted == null;
                var isStillRegistered = Quest.GetQuestByIdOrDefault(questId) != null;

                IsAcceptVisible = !hasActive;
                // Gate on the actual DM avatar rather than authorization level: DM clients cannot
                // hold quests, but staff playing a normal character can accept contracts like anyone.
                IsAcceptEnabled = !GetIsDM(Player) && isStillRegistered && Quest.CanAcceptQuest(Player, questId);
                IsTurnInVisible = hasActive && isStillRegistered;
                IsAbandonVisible = hasActive && isStillRegistered;
                IsTakeDownVisible = _isDM;

                if (!isStillRegistered && !hasActive)
                    DetailText += "\n\nThis contract is no longer available.";
            }
            else
            {
                if (contract.Status == QuestContractStatus.Draft)
                {
                    IsCancelEnabled = true;
                    CancelButtonText = "Delete Draft";
                }
                else
                {
                    IsCancelEnabled = contract.Status == QuestContractStatus.Published;
                    CancelButtonText = "Cancel";
                }
            }
        }

        private string BuildDetailText(QuestContract contract)
        {
            var sb = new StringBuilder();

            sb.Append($"Title: {contract.Title}\n");

            if (_isBrowseTab)
            {
                var authorName = PlayerName.GetPlainDisplayNameByPlayerId(Player, contract.AuthorPlayerId, contract.AuthorName);
                sb.Append($"Author: {authorName}\n");
            }
            else
            {
                var statusLabel = contract.Status == QuestContractStatus.Draft ? "Draft" : "Published";
                sb.Append($"Status: {statusLabel}\n");
            }

            if (contract.Status == QuestContractStatus.Published)
                sb.Append($"Expires: {contract.DateExpires:yyyy-MM-dd}\n");

            if (contract.RewardCredits > 0)
                sb.Append($"Reward: {contract.RewardCredits} credits\n");

            sb.Append('\n');
            sb.Append(string.IsNullOrWhiteSpace(contract.Description) ? "(No description)" : contract.Description);

            return sb.ToString();
        }

        private void LoadDetailItems(QuestContract contract)
        {
            var objectiveIcons = new GuiBindingList<string>();
            var objectiveLabels = new GuiBindingList<string>();
            var rewardIcons = new GuiBindingList<string>();
            var rewardLabels = new GuiBindingList<string>();

            foreach (var objective in contract.Objectives)
            {
                objectiveIcons.Add(Cache.GetItemIconByResref(objective.ItemResref));
                objectiveLabels.Add($"{objective.Quantity}x {objective.ItemName}");
            }

            foreach (var rewardItem in contract.RewardItems)
            {
                rewardIcons.Add(QuestContractBoard.ResolveContractItemIcon(rewardItem));
                rewardLabels.Add(rewardItem.StackSize > 1 ? $"{rewardItem.StackSize}x {rewardItem.Name}" : rewardItem.Name);
            }

            ObjectiveIconResrefs = objectiveIcons;
            ObjectiveLabels = objectiveLabels;
            RewardIconResrefs = rewardIcons;
            RewardLabels = rewardLabels;
        }

        public Action OnClickBrowseTab() => ShowBrowse;

        public Action OnClickMyContractsTab() => ShowMyContracts;

        private void ReloadCurrentTab()
        {
            if (_isBrowseTab)
                LoadBrowse();
            else
                LoadMyContracts();
        }

        public Action OnClickSearch() => ReloadCurrentTab;

        public Action OnClickClearSearch() => () =>
        {
            SearchText = string.Empty;
            ReloadCurrentTab();
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

            // Re-check availability at turn-in time: another player may have completed the contract
            // since this list was loaded.
            var contract = DB.Get<QuestContract>(_rows[_selectedIndex].Id);

            if (contract == null ||
                contract.Status != QuestContractStatus.Published ||
                contract.CompletionsRemaining <= 0)
            {
                StatusText = "This contract has already been completed or is no longer available.";
                LoadBrowse();
                return;
            }

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

        public Action OnClickNewContract() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.QuestContractEditor, null, TetherObject);
        };

        public Action OnClickEditDraft() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.QuestContractEditor, null, TetherObject);
        };

        public Action OnClickCancelContract() => () =>
        {
            if (_isBrowseTab || _selectedIndex < 0 || _selectedIndex >= _rows.Count) return;

            var contract = _rows[_selectedIndex];

            if (contract.Status == QuestContractStatus.Draft)
            {
                var prompt = contract.RewardItems.Count > 0
                    ? "Delete this draft? Your escrowed reward items will be returned to you as a delivery."
                    : "Delete this draft?";

                ShowModal(prompt, () =>
                {
                    var error = QuestContractBoard.DeleteDraft(Player);
                    StatusText = error;
                    LoadMyContracts();
                });
                return;
            }

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

            // A claim may fully or partially empty the pending deliveries (items can remain if the
            // player's inventory is full), so recompute rather than assume.
            UpdateClaimDeliveriesEnabled();
        };

        public Action OnClickExamineObjective() => () =>
        {
            if (_selectedIndex < 0 || _selectedIndex >= _rows.Count) return;

            var contract = _rows[_selectedIndex];
            var index = NuiGetEventArrayIndex();

            if (index < 0 || index >= contract.Objectives.Count) return;

            var storageContainer = GetObjectByTag("TEMP_ITEM_STORAGE");
            var item = CreateItemOnObject(contract.Objectives[index].ItemResref, storageContainer);
            var payload = new ExamineItemPayload(GetName(item), GetDescription(item), Item.BuildItemPropertyString(item));
            Gui.TogglePlayerWindow(Player, GuiWindowType.ExamineItem, payload);
            DestroyObject(item);
        };

        public Action OnClickExamineReward() => () =>
        {
            if (_selectedIndex < 0 || _selectedIndex >= _rows.Count) return;

            var contract = _rows[_selectedIndex];
            var index = NuiGetEventArrayIndex();

            if (index < 0 || index >= contract.RewardItems.Count) return;

            var item = ObjectPlugin.Deserialize(contract.RewardItems[index].Data);
            var payload = new ExamineItemPayload(GetName(item), GetDescription(item), Item.BuildItemPropertyString(item));
            Gui.TogglePlayerWindow(Player, GuiWindowType.ExamineItem, payload);
            DestroyObject(item);
        };

        public void Refresh(QuestContractPublishedRefreshEvent payload)
        {
            ReloadCurrentTab();
        }
    }
}
