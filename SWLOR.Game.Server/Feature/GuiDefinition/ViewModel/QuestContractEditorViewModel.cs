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

        public GuiBindingList<string> ObjectiveLabels
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

        public bool IsObjectiveDetailVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string ObjectiveQuantityText
        {
            get => Get<string>();
            set => Set(SanitizeNumber(value, 1, QuestContractBoard.MaxObjectiveQuantity));
        }

        public bool ObjectiveIsPlayerCrafted
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string RewardCreditsText
        {
            get => Get<string>();
            set
            {
                Set(SanitizeNumber(value, 0, 999999));
                UpdateCostSummary();
            }
        }

        public string CompletionsText
        {
            get => Get<string>();
            set
            {
                Set(SanitizeNumber(value, 1, QuestContractBoard.MaxCompletions));
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

        public string RewardItemHint
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CostSummaryText
        {
            get => Get<string>();
            set => Set(value);
        }

        private static string SanitizeNumber(string value, int min, int max)
        {
            var digits = Regex.Replace(value ?? string.Empty, "[^0-9]", string.Empty).TrimStart('0');

            if (digits.Length < 1)
                digits = "0";

            if (!int.TryParse(digits, out var result))
                result = 0;

            if (result < min)
                result = min;

            if (result > max)
                result = max;

            return result.ToString();
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            _selectedObjectiveIndex = -1;
            StatusText = string.Empty;
            ObjectiveQuantityText = "1";
            ObjectiveIsPlayerCrafted = false;
            IsObjectiveDetailVisible = false;

            var draft = QuestContractBoard.GetOrCreateDraft(Player);
            Title = draft.Title;
            Description = draft.Description;
            RewardCreditsText = draft.RewardCredits.ToString();
            CompletionsText = draft.CompletionsRemaining.ToString();

            LoadObjectives();
            LoadRewardItems();

            WatchOnClient(model => model.Title);
            WatchOnClient(model => model.Description);
            WatchOnClient(model => model.RewardCreditsText);
            WatchOnClient(model => model.CompletionsText);
            WatchOnClient(model => model.ObjectiveQuantityText);
            WatchOnClient(model => model.ObjectiveIsPlayerCrafted);
        }

        private void LoadObjectives()
        {
            var draft = QuestContractBoard.GetOrCreateDraft(Player);
            var labels = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();

            for (var index = 0; index < draft.Objectives.Count; index++)
            {
                var objective = draft.Objectives[index];
                var label = $"{objective.Quantity}x {objective.ItemName}";

                if (objective.MustBePlayerProduced)
                    label += " (player-crafted)";

                labels.Add(label);
                toggles.Add(index == _selectedObjectiveIndex);
            }

            ObjectiveLabels = labels;
            ObjectiveToggles = toggles;
            IsAddObjectiveEnabled = draft.Objectives.Count < QuestContractBoard.MaxObjectives;

            LoadObjectiveDetail(draft);
        }

        private void LoadObjectiveDetail(QuestContract draft)
        {
            if (_selectedObjectiveIndex < 0 || _selectedObjectiveIndex >= draft.Objectives.Count)
            {
                _selectedObjectiveIndex = -1;
                IsObjectiveDetailVisible = false;
                return;
            }

            var objective = draft.Objectives[_selectedObjectiveIndex];
            ObjectiveQuantityText = objective.Quantity.ToString();
            ObjectiveIsPlayerCrafted = objective.MustBePlayerProduced;
            IsObjectiveDetailVisible = true;
        }

        private void LoadRewardItems()
        {
            var draft = QuestContractBoard.GetOrCreateDraft(Player);
            var icons = new GuiBindingList<string>();
            var labels = new GuiBindingList<string>();

            foreach (var rewardItem in draft.RewardItems)
            {
                icons.Add(rewardItem.IconResref);
                labels.Add(rewardItem.StackSize > 1 ? $"{rewardItem.StackSize}x {rewardItem.Name}" : rewardItem.Name);
            }

            RewardItemIconResrefs = icons;
            RewardItemLabels = labels;
            IsAddRewardItemEnabled = draft.RewardItems.Count < QuestContractBoard.MaxRewardItems;

            RewardItemHint = draft.RewardItems.Count > 0 && draft.CompletionsRemaining != 1
                ? "Item rewards can only be offered on single-completion contracts."
                : string.Empty;
        }

        private void UpdateCostSummary()
        {
            if (!int.TryParse(RewardCreditsText, out var credits) || credits < 0)
                credits = 0;

            if (!int.TryParse(CompletionsText, out var completions) || completions < 1)
                completions = 1;

            var totalRewardCredits = credits * completions;
            var fee = Math.Max(QuestContractBoard.MinimumPostingFee, totalRewardCredits * QuestContractBoard.PostingFeePercent / 100);
            var totalCost = totalRewardCredits + fee;

            CostSummaryText = $"Escrow: {totalRewardCredits} cr + Posting Fee: {fee} cr = Total: {totalCost} cr";
        }

        private QuestContract SaveDetails()
        {
            var draft = QuestContractBoard.GetOrCreateDraft(Player);
            draft.Title = Title;
            draft.Description = Description;

            if (!int.TryParse(RewardCreditsText, out var credits) || credits < 0)
                credits = 0;
            draft.RewardCredits = credits;

            if (!int.TryParse(CompletionsText, out var completions) || completions < 1)
                completions = 1;
            if (completions > QuestContractBoard.MaxCompletions)
                completions = QuestContractBoard.MaxCompletions;
            draft.CompletionsRemaining = completions;

            DB.Set(draft);

            return draft;
        }

        public Action OnClickSaveDetails() => () =>
        {
            SaveDetails();
            StatusText = "Draft details saved.";
            LoadRewardItems();
        };

        public Action OnClickSelectObjective() => () =>
        {
            if (_selectedObjectiveIndex > -1 && _selectedObjectiveIndex < ObjectiveToggles.Count)
                ObjectiveToggles[_selectedObjectiveIndex] = false;

            var index = NuiGetEventArrayIndex();
            _selectedObjectiveIndex = index;
            ObjectiveToggles[index] = true;

            var draft = QuestContractBoard.GetOrCreateDraft(Player);
            LoadObjectiveDetail(draft);
        };

        public Action OnClickAddObjective() => () =>
        {
            Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please click on an item within your inventory to use as this objective's required item.", AddObjective);
            EnterTargetingMode(Player, ObjectType.Item);
        };

        private void AddObjective(uint item)
        {
            if (GetItemPossessor(item) != Player)
            {
                FloatingTextStringOnCreature("Item must be in your inventory.", Player, false);
                return;
            }

            var draft = QuestContractBoard.GetOrCreateDraft(Player);

            if (draft.Objectives.Count >= QuestContractBoard.MaxObjectives)
            {
                FloatingTextStringOnCreature($"A contract can have at most {QuestContractBoard.MaxObjectives} objectives.", Player, false);
                return;
            }

            var objective = new QuestContractObjective
            {
                ItemResref = GetResRef(item),
                ItemName = GetName(item),
                Quantity = 1,
                MustBePlayerProduced = false
            };

            draft.Objectives.Add(objective);
            DB.Set(draft);

            LoadObjectives();
        }

        public Action OnClickRemoveObjective() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var draft = QuestContractBoard.GetOrCreateDraft(Player);

            if (index < 0 || index >= draft.Objectives.Count) return;

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
            draft.Objectives[_selectedObjectiveIndex].MustBePlayerProduced = ObjectiveIsPlayerCrafted;
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
            var draft = SaveDetails();
            var totalRewardCredits = draft.RewardCredits * draft.CompletionsRemaining;
            var fee = Math.Max(QuestContractBoard.MinimumPostingFee, totalRewardCredits * QuestContractBoard.PostingFeePercent / 100);
            var totalCost = totalRewardCredits + fee;

            ShowModal($"Publish this contract for a total cost of {totalCost} credits ({totalRewardCredits} escrowed reward + {fee} posting fee)? This cannot be undone.", () =>
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
            Gui.TogglePlayerWindow(Player, GuiWindowType.QuestContractEditor);
        };
    }
}
