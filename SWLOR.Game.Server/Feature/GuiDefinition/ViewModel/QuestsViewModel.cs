using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class QuestsViewModel: GuiViewModelBase<QuestsViewModel, GuiPayloadBase>,
        IGuiRefreshable<QuestAcquiredRefreshEvent>,
        IGuiRefreshable<QuestProgressedRefreshEvent>,
        IGuiRefreshable<QuestCompletedRefreshEvent>
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        
        public string SearchText
        {
            get => Get<string>();
            set => Set(value);
        }

        private readonly List<string> _questIds = new List<string>();
        private int SelectedQuestIndex { get; set; }

        public GuiBindingList<string> QuestNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> QuestToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public string ActiveQuestName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ActiveQuestDescription
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsAbandonQuestEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        private void LoadQuest()
        {
            if (SelectedQuestIndex > -1)
            {
                var questId = _questIds[SelectedQuestIndex];
                var playerId = GetObjectUUID(Player);
                var dbPlayer = _db.Get<Player>(playerId);


                var dbPlayerQuest = dbPlayer.Quests[questId];
                var questDetail = Quest.GetQuestById(questId);

                ActiveQuestName = questDetail.Name;
                ActiveQuestDescription = BuildDescription(questDetail, dbPlayerQuest.CurrentState);
                IsAbandonQuestEnabled = true;
            }
            else
            {
                ActiveQuestName = "[Select a Quest]";
                ActiveQuestDescription = "[Select a Quest]";
                IsAbandonQuestEnabled = false;
            }
        }

        private string BuildDescription(QuestDetail questDetail, int currentState)
        {
            var sb = new StringBuilder();
            var state = questDetail.States[currentState];
            var objectives = state.GetObjectives().ToList();
            sb.Append(state.JournalText);

            if (objectives.Count > 0)
            {
                sb.Append("\n\n");
                sb.Append("Objectives:\n\n");

                foreach (var objective in state.GetObjectives())
                {
                    sb.Append(objective.GetCurrentStateText(Player, questDetail.QuestId));
                    sb.Append("\n");
                }
            }

            return sb.ToString();
        }

        private void Search()
        {
            SelectedQuestIndex = -1;
            var playerId = GetObjectUUID(Player);
            var dbPlayer = _db.Get<Player>(playerId);

            _questIds.Clear();
            var questNames = new GuiBindingList<string>();
            var questToggles = new GuiBindingList<bool>();

            foreach (var (questId, quest) in dbPlayer.Quests)
            {
                // Ignore completed quests.
                if (quest.DateLastCompleted != null)
                    continue;

                var questDetail = Quest.GetQuestById(questId);
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    if (!questDetail.Name.ToLower().Contains(SearchText))
                        continue;
                }

                _questIds.Add(questId);
                questNames.Add(questDetail.Name);
                questToggles.Add(false);
            }

            QuestNames = questNames;
            QuestToggles = questToggles;

            LoadQuest();
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            SearchText = string.Empty;
            SelectedQuestIndex = -1;
            Search();
            LoadQuest();

            WatchOnClient(model => model.SearchText);
        }

        public Action OnClickClearSearch() => () =>
        {
            SearchText = string.Empty;
            Search();
        };

        public Action OnClickSearch() => Search;

        public Action OnClickQuest() => () =>
        {
            if (SelectedQuestIndex > -1)
                QuestToggles[SelectedQuestIndex] = false;

            var index = NuiGetEventArrayIndex();
            SelectedQuestIndex = index;
            QuestToggles[index] = true;

            LoadQuest();
        };

        public Action OnClickAbandonQuest() => () =>
        {
            var questId = _questIds[SelectedQuestIndex];

            ShowModal("Are you sure you wish to abandon this quest?", () =>
            {
                if (Activity.IsBusy(Player))
                {
                    SendMessageToPC(Player, "You are busy.");
                    return;
                }

                Quest.AbandonQuest(Player, questId);

                _questIds.RemoveAt(SelectedQuestIndex);
                QuestNames.RemoveAt(SelectedQuestIndex);
                QuestToggles.RemoveAt(SelectedQuestIndex);
                ActiveQuestName = "[Select a Quest]";
                ActiveQuestDescription = "[Select a Quest]";
                IsAbandonQuestEnabled = false;

                SelectedQuestIndex = -1;
            });
        };

        public void Refresh(QuestAcquiredRefreshEvent payload)
        {
            Search();
        }

        public void Refresh(QuestProgressedRefreshEvent payload)
        {
            Search();
        }

        public void Refresh(QuestCompletedRefreshEvent payload)
        {
            Search();
        }
    }
}
