using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class GuildTasksViewModel: GuiViewModelBase<GuildTasksViewModel, GuildTasksPayload>
    {
        private readonly List<string> _questIds = new();
        private GuildType _guildType;
        private uint _guildMaster;
        private int _selectedQuestIndex;

        public string HeaderText
        {
            get => Get<string>();
            set => Set(value);
        }

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

        protected override void Initialize(GuildTasksPayload initialPayload)
        {
            _guildType = initialPayload.Guild;
            _guildMaster = initialPayload.GuildMaster;
            _selectedQuestIndex = -1;

            _selectedQuestIndex = -1;
            RefreshTasks();
            LoadSelectedTask();
        }

        private void RefreshTasks()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var guild = Guild.GetGuild(_guildType);
            var playerGuild = dbPlayer.Guilds.ContainsKey(_guildType)
                ? dbPlayer.Guilds[_guildType]
                : new PlayerGuild();

            HeaderText = $"{guild.Name} Tasks - Rank {playerGuild.Rank}";

            _questIds.Clear();
            var taskNames = new GuiBindingList<string>();
            var taskToggles = new GuiBindingList<bool>();
            var currentTasks = Guild.GetAllActiveGuildTasks(_guildType);

            foreach (var (questId, pcQuest) in dbPlayer.Quests)
            {
                var task = Quest.GetQuestById(questId);
                if (task.GuildType != _guildType || pcQuest.DateLastCompleted != null || currentTasks.ContainsKey(questId))
                    continue;

                _questIds.Add(questId);
                taskNames.Add($"{task.Name} [Rank {task.GuildRank + 1}] {{ACCEPTED}}");
                taskToggles.Add(false);
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

                var status = "{ACCEPTED}";
                if (!dbPlayer.Quests.ContainsKey(task.QuestId) ||
                    (dbPlayer.Quests[task.QuestId].DateLastCompleted != null && dbPlayer.Quests[task.QuestId].TimesCompleted > 0))
                {
                    status = "{AVAILABLE}";
                }

                _questIds.Add(task.QuestId);
                taskNames.Add($"{task.Name} [Rank {task.GuildRank + 1}] {status}");
                taskToggles.Add(false);
            }

            TaskNames = taskNames;
            TaskToggles = taskToggles;
        }

        private void LoadSelectedTask()
        {
            IsAcceptEnabled = false;
            IsGiveReportEnabled = false;

            if (_selectedQuestIndex < 0 || _selectedQuestIndex >= _questIds.Count)
            {
                TaskDetails = "Select a task to view details.";
                return;
            }

            var questId = _questIds[_selectedQuestIndex];
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var pcQuest = dbPlayer.Quests.ContainsKey(questId) ? dbPlayer.Quests[questId] : null;
            var task = Quest.GetQuestById(questId);

            var gpAmount = task.Rewards.OfType<GPReward>().Sum(x => Guild.CalculateGPReward(Player, _guildType, x.Amount));
            var creditAmount = task.Rewards.OfType<GoldReward>().Sum(x => Quest.CalculateQuestGoldReward(Player, true, x.Amount));

            TaskDetails = $"Task: {task.Name}\n\nRewards:\nCredits: {creditAmount}\nGuild Points: {gpAmount}";

            if (pcQuest == null || pcQuest.DateLastCompleted != null)
                IsAcceptEnabled = true;

            if (pcQuest != null && pcQuest.DateLastCompleted == null)
                IsGiveReportEnabled = true;
        }

        public Action OnClickTask() => () =>
        {
            if (_selectedQuestIndex > -1 && _selectedQuestIndex < TaskToggles.Count)
                TaskToggles[_selectedQuestIndex] = false;

            _selectedQuestIndex = NuiGetEventArrayIndex();
            TaskToggles[_selectedQuestIndex] = true;
            LoadSelectedTask();
        };

        public Action OnClickAcceptTask() => () =>
        {
            if (_selectedQuestIndex < 0) return;
            Quest.AcceptQuest(Player, _questIds[_selectedQuestIndex]);
            _selectedQuestIndex = -1;
            RefreshTasks();
            LoadSelectedTask();
        };

        public Action OnClickGiveReport() => () =>
        {
            if (_selectedQuestIndex < 0) return;

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
                SendMessageToPC(Player, ColorToken.Red("One or more task is incomplete. Refer to your journal for more information."));
            }

            _selectedQuestIndex = -1;
            RefreshTasks();
            LoadSelectedTask();
        };
    }
}
