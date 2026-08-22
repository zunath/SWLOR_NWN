using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.QuestContractService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class QuestContractEditorViewModel: GuiViewModelBase<QuestContractEditorViewModel, GuiPayloadBase>
    {
        private int _selectedObjectiveIndex = -1;
        private int _selectedSearchIndex = -1;
        private readonly List<(string Resref, string Name)> _searchResults = new();
        private float _appliedContentWidth = -1f;

        /// <summary>
        /// Regenerates the form layout for the current window width and swaps it in. NUI layout
        /// widths cannot be bound, so this is how the content stretches with the window.
        /// </summary>
        private void RefreshContentLayout()
        {
            var contentWidth = QuestContractEditorDefinition.CalculateContentWidth(Geometry.Width);

            // Resizing fires a stream of geometry updates - only rebuild when the width
            // meaningfully changed.
            if (_appliedContentWidth > 0f && Math.Abs(contentWidth - _appliedContentWidth) < 8f)
                return;

            _appliedContentWidth = contentWidth;
            SetGroupLayout(QuestContractEditorDefinition.ContentElement, QuestContractEditorDefinition.BuildContentLayout(contentWidth));
        }

        protected override void OnClientPropertyUpdated(string propertyName)
        {
            if (propertyName == nameof(Geometry))
                RefreshContentLayout();
        }

        private void ReapplyContentLayout()
        {
            _appliedContentWidth = -1f;
            RefreshContentLayout();
        }

        protected override void OnMainViewRestored()
        {
            // Restoring the main view (e.g. when a modal closes) re-renders the static window
            // template, whose content placeholder is empty - the generated form must be swapped
            // back in. NUI can drop nested layouts while the parent is being redrawn, so reapply
            // again on the next tick (same workaround as the character sheet's tab swaps).
            ReapplyContentLayout();
            DelayCommand(0.0f, ReapplyContentLayout);
        }

        public string Title
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Description
        {
            get => Get<string>();
            set => Set(value);
        }

        public string StatusText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ItemSearchText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> SearchResultLabels
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> SearchResultIconResrefs
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> SearchResultToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<string> ObjectiveLabels
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> ObjectiveIconResrefs
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> ObjectiveToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public bool IsAddObjectiveEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string NewObjectiveQuantityText
        {
            get => Get<string>();
            set
            {
                var sanitized = SanitizeNumber(value, 1, QuestContractBoard.MaxObjectiveQuantity);
                Set(sanitized);

                if (sanitized != value)
                    OnPropertyChanged();
            }
        }

        public bool IsObjectiveDetailVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string ObjectiveQuantityText
        {
            get => Get<string>();
            set
            {
                var sanitized = SanitizeNumber(value, 1, QuestContractBoard.MaxObjectiveQuantity);
                Set(sanitized);

                // See RewardCreditsText - push clamped values back to the client's text box.
                if (sanitized != value)
                    OnPropertyChanged();
            }
        }

        public string RewardCreditsText
        {
            get => Get<string>();
            set
            {
                var sanitized = SanitizeNumber(value, 0, 999999);
                Set(sanitized);

                // Client-watched updates suppress the change notification, so when the typed value
                // was clamped, explicitly push the corrected value back to the client's text box.
                if (sanitized != value)
                    OnPropertyChanged();

                UpdateCostSummary();
            }
        }

        public GuiBindingList<string> RewardItemIconResrefs
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> RewardItemLabels
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public bool IsAddRewardItemEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string PostingFeeText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string EscrowText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string TotalCostText
        {
            get => Get<string>();
            set => Set(value);
        }

        private static string SanitizeNumber(string value, int min, int max)
        {
            var digits = Regex.Replace(value ?? string.Empty, "[^0-9]", string.Empty).TrimStart('0');

            if (digits.Length < 1)
                digits = "0";

            // The digits are guaranteed numeric at this point, so a failed parse means the value
            // overflowed int - clamp to the maximum rather than zeroing it out.
            if (!int.TryParse(digits, out var result))
                result = max;

            if (result < min)
                result = min;

            if (result > max)
                result = max;

            return result.ToString();
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            _selectedObjectiveIndex = -1;
            _selectedSearchIndex = -1;
            _searchResults.Clear();
            _appliedContentWidth = -1f;
            RefreshContentLayout();
            StatusText = string.Empty;
            ItemSearchText = string.Empty;
            SearchResultLabels = new GuiBindingList<string>();
            SearchResultToggles = new GuiBindingList<bool>();
            SearchResultIconResrefs = new GuiBindingList<string>();
            NewObjectiveQuantityText = "1";
            ObjectiveQuantityText = "1";
            IsObjectiveDetailVisible = false;

            var draft = QuestContractBoard.GetDraft(Player);
            Title = draft?.Title ?? string.Empty;
            Description = draft?.Description ?? string.Empty;
            RewardCreditsText = (draft?.RewardCredits ?? 0).ToString();

            LoadObjectives();
            LoadRewardItems();

            WatchOnClient(model => model.Title);
            WatchOnClient(model => model.Description);
            WatchOnClient(model => model.RewardCreditsText);
            WatchOnClient(model => model.ItemSearchText);
            WatchOnClient(model => model.NewObjectiveQuantityText);
            WatchOnClient(model => model.ObjectiveQuantityText);
        }

        private void LoadObjectives()
        {
            var draft = QuestContractBoard.GetDraft(Player);
            var labels = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();
            var icons = new GuiBindingList<string>();

            var objectives = draft?.Objectives ?? new List<QuestContractObjective>();

            for (var index = 0; index < objectives.Count; index++)
            {
                var objective = objectives[index];

                labels.Add($"{objective.Quantity}x {objective.ItemName}");
                toggles.Add(index == _selectedObjectiveIndex);
                icons.Add(Cache.GetItemIconByResref(objective.ItemResref));
            }

            ObjectiveLabels = labels;
            ObjectiveToggles = toggles;
            ObjectiveIconResrefs = icons;

            UpdateAddObjectiveEnabled();
            LoadObjectiveDetail(draft);
        }

        private void UpdateAddObjectiveEnabled()
        {
            IsAddObjectiveEnabled = _selectedSearchIndex >= 0
                && _selectedSearchIndex < _searchResults.Count
                && (ObjectiveLabels?.Count ?? 0) < QuestContractBoard.MaxObjectives;
        }

        private void LoadObjectiveDetail(QuestContract draft)
        {
            if (draft == null || _selectedObjectiveIndex < 0 || _selectedObjectiveIndex >= draft.Objectives.Count)
            {
                _selectedObjectiveIndex = -1;
                IsObjectiveDetailVisible = false;
                return;
            }

            var objective = draft.Objectives[_selectedObjectiveIndex];
            ObjectiveQuantityText = objective.Quantity.ToString();
            IsObjectiveDetailVisible = true;
        }

        private void LoadRewardItems()
        {
            var draft = QuestContractBoard.GetDraft(Player);
            var icons = new GuiBindingList<string>();
            var labels = new GuiBindingList<string>();

            var rewardItems = draft?.RewardItems ?? new List<QuestContractItem>();

            foreach (var rewardItem in rewardItems)
            {
                icons.Add(QuestContractBoard.ResolveContractItemIcon(rewardItem));
                labels.Add(rewardItem.StackSize > 1 ? $"{rewardItem.StackSize}x {rewardItem.Name}" : rewardItem.Name);
            }

            RewardItemIconResrefs = icons;
            RewardItemLabels = labels;
            IsAddRewardItemEnabled = rewardItems.Count < QuestContractBoard.MaxRewardItems;
        }

        private void UpdateCostSummary()
        {
            if (!int.TryParse(RewardCreditsText, out var credits) || credits < 0)
                credits = 0;

            var fee = QuestContractBoard.CalculatePostingFee(credits);
            var totalCost = credits + fee;

            EscrowText = $"Reward Escrow: {credits} cr (paid to whoever completes the contract)";
            PostingFeeText = $"Posting Fee: {fee} cr (non-refundable)";
            TotalCostText = $"Total to Publish: {totalCost} cr";
        }

        private QuestContract SaveDetails()
        {
            var draft = QuestContractBoard.GetOrCreateDraft(Player);
            draft.Title = Title;
            draft.Description = Description;

            if (!int.TryParse(RewardCreditsText, out var credits) || credits < 0)
                credits = 0;
            draft.RewardCredits = credits;
            draft.CompletionsRemaining = 1;

            DB.Set(draft);

            return draft;
        }

        public Action OnClickSaveDetails() => () =>
        {
            SaveDetails();
            StatusText = "Draft details saved.";
            LoadRewardItems();
            Gui.PublishRefreshEvent(Player, new QuestContractPublishedRefreshEvent());
        };

        public Action OnClickSelectObjective() => () =>
        {
            if (_selectedObjectiveIndex > -1 && _selectedObjectiveIndex < ObjectiveToggles.Count)
                ObjectiveToggles[_selectedObjectiveIndex] = false;

            var index = NuiGetEventArrayIndex();
            _selectedObjectiveIndex = index;
            ObjectiveToggles[index] = true;

            var draft = QuestContractBoard.GetDraft(Player);
            LoadObjectiveDetail(draft);
        };

        public Action OnClickSearchItems() => () =>
        {
            _selectedSearchIndex = -1;

            var results = Cache.SearchItemsByName(ItemSearchText, QuestContractBoard.MaxItemSearchResults);

            _searchResults.Clear();
            var labels = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();
            var icons = new GuiBindingList<string>();

            foreach (var result in results)
            {
                _searchResults.Add(result);
                labels.Add(result.Name);
                toggles.Add(false);
                icons.Add(Cache.GetItemIconByResref(result.Resref));
            }

            SearchResultLabels = labels;
            SearchResultToggles = toggles;
            SearchResultIconResrefs = icons;

            if (results.Count == 0)
                StatusText = "No items found. Try a different search.";
            else if (results.Count >= QuestContractBoard.MaxItemSearchResults)
                StatusText = $"Showing the first {QuestContractBoard.MaxItemSearchResults} matches. Refine your search to narrow it down.";
            else
                StatusText = string.Empty;

            UpdateAddObjectiveEnabled();
        };

        public Action OnClickSelectSearchResult() => () =>
        {
            if (_selectedSearchIndex > -1 && _selectedSearchIndex < SearchResultToggles.Count)
                SearchResultToggles[_selectedSearchIndex] = false;

            var index = NuiGetEventArrayIndex();
            _selectedSearchIndex = index;
            SearchResultToggles[index] = true;

            UpdateAddObjectiveEnabled();
        };

        public Action OnClickAddObjective() => () =>
        {
            if (_selectedSearchIndex < 0 || _selectedSearchIndex >= _searchResults.Count) return;

            var draft = QuestContractBoard.GetOrCreateDraft(Player);

            if (draft.Objectives.Count >= QuestContractBoard.MaxObjectives)
            {
                StatusText = $"A contract can have at most {QuestContractBoard.MaxObjectives} objectives.";
                return;
            }

            var selected = _searchResults[_selectedSearchIndex];

            if (draft.Objectives.Any(x => x.ItemResref == selected.Resref))
            {
                StatusText = "That item is already an objective.";
                return;
            }

            if (!int.TryParse(NewObjectiveQuantityText, out var quantity) || quantity < 1)
                quantity = 1;
            if (quantity > QuestContractBoard.MaxObjectiveQuantity)
                quantity = QuestContractBoard.MaxObjectiveQuantity;

            draft.Objectives.Add(new QuestContractObjective
            {
                ItemResref = selected.Resref,
                ItemName = selected.Name,
                Quantity = quantity
            });
            DB.Set(draft);

            NewObjectiveQuantityText = "1";
            StatusText = string.Empty;
            LoadObjectives();
        };

        public Action OnClickRemoveObjective() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var draft = QuestContractBoard.GetDraft(Player);

            if (draft == null || index < 0 || index >= draft.Objectives.Count) return;

            draft.Objectives.RemoveAt(index);
            DB.Set(draft);

            if (_selectedObjectiveIndex == index)
                _selectedObjectiveIndex = -1;
            else if (_selectedObjectiveIndex > index)
                _selectedObjectiveIndex--;

            LoadObjectives();
        };

        public Action OnClickApplyObjective() => () =>
        {
            if (_selectedObjectiveIndex < 0) return;

            var draft = QuestContractBoard.GetOrCreateDraft(Player);
            if (_selectedObjectiveIndex >= draft.Objectives.Count) return;

            if (!int.TryParse(ObjectiveQuantityText, out var quantity) || quantity < 1)
                quantity = 1;
            if (quantity > QuestContractBoard.MaxObjectiveQuantity)
                quantity = QuestContractBoard.MaxObjectiveQuantity;

            draft.Objectives[_selectedObjectiveIndex].Quantity = quantity;
            DB.Set(draft);

            ObjectiveQuantityText = quantity.ToString();
            LoadObjectives();
        };

        public Action OnClickAddRewardItem() => () =>
        {
            Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please click on an item within your inventory to escrow as a contract reward.", AddRewardItem);
            EnterTargetingMode(Player, ObjectType.Item);
        };

        private void AddRewardItem(uint item)
        {
            var error = QuestContractBoard.AddRewardItem(Player, item);
            StatusText = error;
            LoadRewardItems();
        }

        public Action OnClickRemoveRewardItem() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var error = QuestContractBoard.RemoveRewardItem(Player, index);
            StatusText = error;
            LoadRewardItems();
        };

        public Action OnClickPublish() => () =>
        {
            var existingDraft = QuestContractBoard.GetDraft(Player);

            if (existingDraft == null || existingDraft.Objectives.Count < 1)
            {
                StatusText = "Add at least one objective before publishing.";
                return;
            }

            var draft = SaveDetails();

            var title = QuestContractBoard.SanitizeContractText(draft.Title, QuestContractBoard.MaxTitleLength);
            var description = QuestContractBoard.SanitizeContractText(draft.Description, QuestContractBoard.MaxDescriptionLength);
            var validationError = QuestContractBoard.ValidateDraft(draft, title, description, Cache.GetItemNameByResref);

            if (!string.IsNullOrWhiteSpace(validationError))
            {
                StatusText = validationError;
                return;
            }

            var escrowCredits = draft.RewardCredits;
            var fee = QuestContractBoard.CalculatePostingFee(escrowCredits);
            var totalCost = escrowCredits + fee;

            ShowModal($"Publish this contract?\n\nReward Escrow: {escrowCredits} cr\nPosting Fee: {fee} cr\nTotal: {totalCost} cr\n\nThe escrow is paid to whoever completes the contract. The posting fee is non-refundable.", () =>
            {
                var error = QuestContractBoard.PublishContract(Player);

                if (!string.IsNullOrWhiteSpace(error))
                {
                    StatusText = error;
                    return;
                }

                FloatingTextStringOnCreature("Your quest contract has been published.", Player, false);
                Gui.PublishRefreshEvent(Player, new QuestContractPublishedRefreshEvent());
                Gui.TogglePlayerWindow(Player, GuiWindowType.QuestContractEditor);
            });
        };

        public Action OnClickClose() => () =>
        {
            ShowModal("Close the contract editor? Any unsaved changes to the title, description, or reward credits will be lost.", () =>
            {
                Gui.TogglePlayerWindow(Player, GuiWindowType.QuestContractEditor);
            });
        };
    }
}
