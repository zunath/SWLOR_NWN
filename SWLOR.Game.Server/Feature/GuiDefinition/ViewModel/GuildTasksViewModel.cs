using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class GuildTasksViewModel: GuiViewModelBase<GuildTasksViewModel, GuildTasksPayload>,
        IGuiRefreshable<QuestAcquiredRefreshEvent>,
        IGuiRefreshable<QuestProgressedRefreshEvent>,
        IGuiRefreshable<QuestCompletedRefreshEvent>,
        IGuiRefreshable<QuestAbandonedRefreshEvent>
    {
        private readonly List<string> _questIds = new();
        private GuildType _guildType;
        private uint _guildMaster;
        private int _selectedQuestIndex;
        private int _selectedRankFilter;

        public GuiBindingList<string> TaskNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> TaskToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }
        
        public GuiBindingList<GuiColor> TaskColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public string Rank1Text { get => Get<string>(); set => Set(value); }
        public string Rank2Text { get => Get<string>(); set => Set(value); }
        public string Rank3Text { get => Get<string>(); set => Set(value); }
        public string Rank4Text { get => Get<string>(); set => Set(value); }
        public string Rank5Text { get => Get<string>(); set => Set(value); }
        public GuiColor Rank1Color { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor Rank2Color { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor Rank3Color { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor Rank4Color { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor Rank5Color { get => Get<GuiColor>(); set => Set(value); }
        public bool IsRank1Enabled { get => Get<bool>(); set => Set(value); }
        public bool IsRank2Enabled { get => Get<bool>(); set => Set(value); }
        public bool IsRank3Enabled { get => Get<bool>(); set => Set(value); }
        public bool IsRank4Enabled { get => Get<bool>(); set => Set(value); }
        public bool IsRank5Enabled { get => Get<bool>(); set => Set(value); }
        public bool IsRank1Toggled { get => Get<bool>(); set => Set(value); }
        public bool IsRank2Toggled { get => Get<bool>(); set => Set(value); }
        public bool IsRank3Toggled { get => Get<bool>(); set => Set(value); }
        public bool IsRank4Toggled { get => Get<bool>(); set => Set(value); }
        public bool IsRank5Toggled { get => Get<bool>(); set => Set(value); }

        public string TaskDetails
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsAcceptEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsGiveReportEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsAcceptAllEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        protected override void Initialize(GuildTasksPayload initialPayload)
        {
            _guildType = initialPayload.Guild;
            _guildMaster = initialPayload.GuildMaster;
            _selectedQuestIndex = -1;
            _selectedRankFilter = 1;

            Rank1Text = "Rank 1";
            Rank2Text = "Rank 2";
            Rank3Text = "Rank 3";
            Rank4Text = "Rank 4";
            Rank5Text = "Rank 5";

            RefreshTasks();
            LoadSelectedTask();
        }

        private void RefreshTasks()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var playerGuild = dbPlayer.Guilds.ContainsKey(_guildType)
                ? dbPlayer.Guilds[_guildType]
                : new PlayerGuild();

            _questIds.Clear();
            var taskNames = new GuiBindingList<string>();
            var taskToggles = new GuiBindingList<bool>();
            var taskColors = new GuiBindingList<GuiColor>();
            var currentTasks = Guild.GetAllActiveGuildTasks(_guildType);
            var rankHasTasks = new Dictionary<int, bool> {{1,false}, {2,false}, {3,false}, {4,false}, {5,false}};

            foreach (var (questId, pcQuest) in dbPlayer.Quests)
            {
                var task = Quest.GetQuestById(questId);
                if (task.GuildType != _guildType || pcQuest.DateLastCompleted != null || currentTasks.ContainsKey(questId))
                    continue;
                if (task.GuildRank + 1 != _selectedRankFilter)
                    continue;

                _questIds.Add(questId);
                taskNames.Add($"{task.Name} [Rank {task.GuildRank + 1}] [Expired]");
                taskToggles.Add(false);
                taskColors.Add(GuiColor.Red);
                rankHasTasks[task.GuildRank + 1] = true;
            }

            foreach (var (_, task) in currentTasks)
            {
                if (dbPlayer.Quests.ContainsKey(task.QuestId) &&
                    dbPlayer.Quests[task.QuestId].DateLastCompleted >= Guild.DateTasksLoaded)
                    continue;

                var playerRank = dbPlayer.Guilds.ContainsKey(task.GuildType)
                    ? dbPlayer.Guilds[task.GuildType].Rank
                    : 0;

                if (playerRank < task.GuildRank)
                    continue;
                rankHasTasks[task.GuildRank + 1] = true;
                if (task.GuildRank + 1 != _selectedRankFilter)
                    continue;

                var statusColor = GuiColor.Green;
                if (!dbPlayer.Quests.ContainsKey(task.QuestId) ||
                    (dbPlayer.Quests[task.QuestId].DateLastCompleted != null && dbPlayer.Quests[task.QuestId].TimesCompleted > 0))
                {
                    statusColor = new GuiColor(255, 255, 0);
                }

                _questIds.Add(task.QuestId);
                taskNames.Add($"{task.Name} [Rank {task.GuildRank + 1}]");
                taskToggles.Add(false);
                taskColors.Add(statusColor);
            }

            TaskNames = taskNames;
            TaskToggles = taskToggles;
            TaskColors = taskColors;
            IsRank1Enabled = rankHasTasks[1] && playerGuild.Rank >= 0;
            IsRank2Enabled = rankHasTasks[2] && playerGuild.Rank >= 1;
            IsRank3Enabled = rankHasTasks[3] && playerGuild.Rank >= 2;
            IsRank4Enabled = rankHasTasks[4] && playerGuild.Rank >= 3;
            IsRank5Enabled = rankHasTasks[5] && playerGuild.Rank >= 4;
            Rank1Color = _selectedRankFilter == 1 ? GuiColor.Cyan : GuiColor.White;
            Rank2Color = _selectedRankFilter == 2 ? GuiColor.Cyan : GuiColor.White;
            Rank3Color = _selectedRankFilter == 3 ? GuiColor.Cyan : GuiColor.White;
            Rank4Color = _selectedRankFilter == 4 ? GuiColor.Cyan : GuiColor.White;
            Rank5Color = _selectedRankFilter == 5 ? GuiColor.Cyan : GuiColor.White;
            IsRank1Toggled = _selectedRankFilter == 1;
            IsRank2Toggled = _selectedRankFilter == 2;
            IsRank3Toggled = _selectedRankFilter == 3;
            IsRank4Toggled = _selectedRankFilter == 4;
            IsRank5Toggled = _selectedRankFilter == 5;
            IsAcceptAllEnabled = _questIds.Any(questId => IsQuestAcceptable(dbPlayer, questId));
        }

        private void LoadSelectedTask()
        {
            IsAcceptEnabled = false;
            IsGiveReportEnabled = false;

            if (!HasSelectedQuest())
            {
                TaskDetails = "Select a task to view details.";
                return;
            }

            var questId = _questIds[_selectedQuestIndex];
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var pcQuest = dbPlayer.Quests.ContainsKey(questId) ? dbPlayer.Quests[questId] : null;
            var task = Quest.GetQuestById(questId);
            var currentTasks = Guild.GetAllActiveGuildTasks(_guildType);
            var isExpired = pcQuest != null &&
                            pcQuest.DateLastCompleted == null &&
                            task.GuildType == _guildType &&
                            !currentTasks.ContainsKey(questId);

            var gpAmount = task.Rewards.OfType<GPReward>().Sum(x => Guild.CalculateGPReward(Player, _guildType, x.Amount));
            var creditAmount = task.Rewards.OfType<GoldReward>().Sum(x => Quest.CalculateQuestGoldReward(Player, true, x.Amount));

            TaskDetails = $"Task: {task.Name}\n\nRewards:\nCredits: {creditAmount}\nGuild Points: {gpAmount}";
            if (isExpired)
            {
                TaskDetails += "\n\nStatus: Expired task. This task is no longer in the current guild task rotation.";
            }

            if (pcQuest == null || pcQuest.DateLastCompleted != null)
                IsAcceptEnabled = true;

            if (pcQuest != null && pcQuest.DateLastCompleted == null)
                IsGiveReportEnabled = true;
        }

        private bool HasSelectedQuest()
        {
            return _selectedQuestIndex >= 0 && _selectedQuestIndex < _questIds.Count;
        }

        private static bool IsQuestAcceptable(Player dbPlayer, string questId)
        {
            return !dbPlayer.Quests.ContainsKey(questId) || dbPlayer.Quests[questId].DateLastCompleted != null;
        }

        public Action OnClickTask() => () =>
        {
            if (_selectedQuestIndex > -1 && _selectedQuestIndex < TaskToggles.Count)
                TaskToggles[_selectedQuestIndex] = false;

            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= TaskToggles.Count || index >= _questIds.Count)
            {
                _selectedQuestIndex = -1;
                LoadSelectedTask();
                return;
            }

            _selectedQuestIndex = index;
            TaskToggles[_selectedQuestIndex] = true;
            LoadSelectedTask();
        };

        public Action OnClickAcceptTask() => () =>
        {
            if (!HasSelectedQuest()) return;
            Quest.AcceptQuest(Player, _questIds[_selectedQuestIndex]);
            _selectedQuestIndex = -1;
            RefreshTasks();
            LoadSelectedTask();
        };

        public Action OnClickAcceptAllTasks() => () =>
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var questIds = _questIds
                .Where(questId => IsQuestAcceptable(dbPlayer, questId))
                .ToList();

            if (questIds.Count <= 0)
                return;

            ShowModal($"Accept all {questIds.Count} available Rank {_selectedRankFilter} tasks?", () =>
            {
                var acceptedCount = 0;
                foreach (var questId in questIds)
                {
                    Quest.AcceptQuest(Player, questId);
                    acceptedCount++;
                }

                SendMessageToPC(Player, $"Accepted {acceptedCount} Rank {_selectedRankFilter} guild tasks.");

                _selectedQuestIndex = -1;
                RefreshTasks();
                LoadSelectedTask();
            });
        };

        public Action OnClickGiveReport() => () =>
        {
            if (!HasSelectedQuest()) return;

            var questId = _questIds[_selectedQuestIndex];
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (!dbPlayer.Quests.ContainsKey(questId)) return;

            var pcStatus = dbPlayer.Quests[questId];
            var quest = Quest.GetQuestById(questId);
            var state = quest.States[pcStatus.CurrentState];
            var hasItemObjective = state.GetObjectives().Any(x => x.GetType() == typeof(CollectItemObjective));

            if (hasItemObjective)
            {
                Quest.RequestItemsFromPlayer(Player, questId);
            }
            else if (quest.CanComplete(Player))
            {
                quest.Complete(Player, _guildMaster, null);
            }
            else
            {
                SendMessageToPC(Player, ColorToken.Red("One or more tasks are incomplete. Refer to your journal for more information."));
            }

            _selectedQuestIndex = -1;
            RefreshTasks();
            LoadSelectedTask();
        };

        public Action OnClickRankFilter(int rank) => () =>
        {
            _selectedRankFilter = rank;
            _selectedQuestIndex = -1;
            RefreshTasks();
            LoadSelectedTask();
        };

        private void ReloadFromQuestEvent(string questId)
        {
            if (_selectedQuestIndex >= 0 && _selectedQuestIndex < _questIds.Count && _questIds[_selectedQuestIndex] == questId)
                _selectedQuestIndex = -1;

            RefreshTasks();
            LoadSelectedTask();
        }

        public void Refresh(QuestAcquiredRefreshEvent payload)
        {
            ReloadFromQuestEvent(payload.QuestId);
        }

        public void Refresh(QuestProgressedRefreshEvent payload)
        {
            ReloadFromQuestEvent(payload.QuestId);
        }

        public void Refresh(QuestCompletedRefreshEvent payload)
        {
            ReloadFromQuestEvent(payload.QuestId);
        }

        public void Refresh(QuestAbandonedRefreshEvent payload)
        {
            ReloadFromQuestEvent(payload.QuestId);
        }
    }
}
