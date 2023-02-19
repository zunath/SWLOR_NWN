using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.FactionService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.NPCService;

namespace SWLOR.Game.Server.Service.QuestService
{
    public class QuestBuilder
    {
        private readonly Dictionary<string, QuestDetail> _quests = new Dictionary<string, QuestDetail>();
        private QuestDetail _activeQuest;
        private QuestStateDetail _activeState;

        /// <summary>
        /// Creates a new quest with a given questId, name, and journalTag.
        /// All arguments are required. Exceptions will be thrown if any are null or whitespace.
        /// </summary>
        /// <param name="questId">The quest Id to assign this quest.</param>
        /// <param name="name">The name of the quest.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        public QuestBuilder Create(string questId, string name)
        {
            if (string.IsNullOrWhiteSpace(questId))
                throw new ArgumentException($"{nameof(questId)} cannot be null or whitespace.");
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException($"{nameof(name)} cannot be null or whitespace.");

            _activeQuest = new QuestDetail
            {
                QuestId = questId,
                Name = name
            };

            _quests[questId] = _activeQuest;
            _activeState = null;

            return this;
        }

        /// <summary>
        /// Marks the quest as repeatable.
        /// </summary>
        /// <returns>A QuestBuilder with the configured options.</returns>
        public QuestBuilder IsRepeatable()
        {
            _activeQuest.IsRepeatable = true;

            return this;
        }

        /// <summary>
        /// Marks the quest as a guild task.
        /// </summary>
        /// <returns>A QuestBuilder with the configured options.</returns>
        public QuestBuilder IsGuildTask(GuildType guild, int rank)
        {
            _activeQuest.GuildType = guild;
            _activeQuest.GuildRank = rank;

            return this;
        }

        /// <summary>
        /// Marks that the quest allows the player to select a reward when completed.
        /// </summary>
        /// <returns>A QuestBuilder with the configured options.</returns>
        public QuestBuilder HasRewardSelection()
        {
            _activeQuest.AllowRewardSelection = true;

            return this;
        }

        /// <summary>
        /// Adds an item reward for completing this quest.
        /// </summary>
        /// <param name="itemResref">The resref of the item to create.</param>
        /// <param name="quantity">The number of items to create.</param>
        /// <param name="isSelectable">If true, player will have the option to select the item as a reward. If false, they will receive it no matter what.
        /// If IsRepeatable() has not been called, this argument is ignored and all items are given to the player.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        public QuestBuilder AddItemReward(string itemResref, int quantity, bool isSelectable = true)
        {
            var reward = new ItemReward(itemResref, quantity, isSelectable);
            _activeQuest.Rewards.Add(reward);

            return this;
        }

        /// <summary>
        /// Adds a gold reward for completing this quest.
        /// </summary>
        /// <param name="amount">The amount of gold to create.</param>
        /// <param name="isSelectable">If true, player will have the option to select the gold as a reward. If false, they will receive it no matter what.
        /// If IsRepeatable() has not been called, this argument is ignored and all gold is given to the player.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        public QuestBuilder AddGoldReward(int amount, bool isSelectable = true)
        {
            var reward = new GoldReward(amount, isSelectable, _activeQuest.GuildType != GuildType.Invalid);
            _activeQuest.Rewards.Add(reward);

            return this;
        }

        /// <summary>
        /// Adds an XP reward for completing this quest.
        /// </summary>
        /// <param name="amount">The amount of XP to give.</param>
        /// <param name="isSelectable">If true, player will have the option to select XP as a reward. If false, they will receive it no matter what.
        /// If IsRepeatable() has not been called, this argument is ignored and all XP is given to the player</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        public QuestBuilder AddXPReward(int amount, bool isSelectable = true)
        {
            var reward = new XPReward(amount, isSelectable);
            _activeQuest.Rewards.Add(reward);

            return this;
        }

        /// <summary>
        /// Adds a key item reward for completing this quest.
        /// </summary>
        /// <param name="keyItemType">The type of key item to award.</param>
        /// <param name="isSelectable">If true, player will have the option to select the key item as a reward. If false, they will receive it no matter what. If IsRepeatable() has not been called, this argument is ignored and all gold is given to the player.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        public QuestBuilder AddKeyItemReward(KeyItemType keyItemType, bool isSelectable = true)
        {
            var reward = new KeyItemReward(keyItemType, isSelectable);
            _activeQuest.Rewards.Add(reward);

            return this;
        }

        /// <summary>
        /// Adds a guild GP reward for completing this quest.
        /// </summary>
        /// <param name="guild">The type of guild GP to reward.</param>
        /// <param name="amount">The amount of GP to award</param>
        /// <param name="isSelectable">If true, player will have the option to select the GP as a reward. If false, they will receive it no matter what. If IsRepeatable() has not been called, this argument is ignored and all GP is given to the player.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        public QuestBuilder AddGPReward(GuildType guild, int amount, bool isSelectable = true)
        {
            var reward = new GPReward(guild, amount, isSelectable);
            _activeQuest.Rewards.Add(reward);

            return this;
        }

        /// <summary>
        /// Adds a faction standing reward for completing this quest.
        /// </summary>
        /// <param name="faction">The type of faction to use.</param>
        /// <param name="amount">Amount of standing to give</param>
        /// <param name="isSelectable">If true, player will have the option to select the Standing as a reward. If false, they will receive it no matter what. If IsRepeatable() has not been called, this argument is ignored and all Standing is given to the player.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        public QuestBuilder AddFactionStandingReward(FactionType faction, int amount, bool isSelectable = true)
        {
            var reward = new FactionStandingReward(faction, amount, isSelectable);
            _activeQuest.Rewards.Add(reward);

            return this;
        }

        /// <summary>
        /// Adds a faction point reward for completing this quest.
        /// </summary>
        /// <param name="faction">The type of faction to use.</param>
        /// <param name="amount">Amount of points to give</param>
        /// <param name="isSelectable">If true, player will have the option to select the Points as a reward. If false, they will receive it no matter what. If IsRepeatable() has not been called, this argument is ignored and all Standing is given to the player.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        public QuestBuilder AddFactionPointsReward(FactionType faction, int amount, bool isSelectable = true)
        {
            var reward = new FactionPointsReward(faction, amount, isSelectable);
            _activeQuest.Rewards.Add(reward);

            return this;
        }

        /// <summary>
        /// Adds a prerequisite to the quest. If the player has not completed this prerequisite quest, they will be unable to accept it.
        /// </summary>
        /// <param name="prerequisiteQuestId">The unique Id of the prerequisite quest. If this Id has not been registered, an exception will be thrown.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        public QuestBuilder PrerequisiteQuest(string prerequisiteQuestId)
        {
            var prereq = new RequiredQuestPrerequisite(prerequisiteQuestId);
            _activeQuest.Prerequisites.Add(prereq);

            return this;
        }

        /// <summary>
        /// Adds a prerequisite to the quest. If the player has not acquired this key item, they will be unable to accept it.
        /// </summary>
        /// <param name="keyItemType">The type of key item to require.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        public QuestBuilder PrerequisiteKeyItem(KeyItemType keyItemType)
        {
            var prereq = new RequiredKeyItemPrerequisite(keyItemType);
            _activeQuest.Prerequisites.Add(prereq);

            return this;
        }

        /// <summary>
        /// Adds an action to run when a player accepts a quest.
        /// </summary>
        /// <param name="action">The action to run when a player accepts a quest.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        public QuestBuilder OnAcceptAction(AcceptQuestDelegate action)
        {
            _activeQuest.OnAcceptActions.Add(action);

            return this;
        }

        /// <summary>
        /// Adds an action to run when a player abandons a quest.
        /// </summary>
        /// <param name="action">The action to run when a player abandons a quest.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        public QuestBuilder OnAbandonAction(AbandonQuestDelegate action)
        {
            _activeQuest.OnAbandonActions.Add(action);

            return this;
        }

        /// <summary>
        /// Adds an action to run when a player advances to a new quest state.
        /// </summary>
        /// <param name="action">The action to run when a player advances to a new quest state.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        public QuestBuilder OnAdvanceAction(AdvanceQuestDelegate action)
        {
            _activeQuest.OnAdvanceActions.Add(action);

            return this;
        }

        /// <summary>
        /// Adds an action to run when a player completes a quest.
        /// </summary>
        /// <param name="action">The action to run when a player completes the quest.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        public QuestBuilder OnCompleteAction(CompleteQuestDelegate action)
        {
            _activeQuest.OnCompleteActions.Add(action);

            return this;
        }

        /// <summary>
        /// Adds a new quest state to the quest.
        /// </summary>
        /// <returns>A QuestBuilder with the configured options.</returns>
        public QuestBuilder AddState()
        {
            _activeState = new QuestStateDetail();
            var index = _activeQuest.States.Count + 1;
            _activeQuest.States[index] = _activeState;

            return this;
        }

        /// <summary>
        /// Sets the journal text of this quest state.
        /// </summary>
        /// <param name="journalText">The journal text to set.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        public QuestBuilder SetStateJournalText(string journalText)
        {
            _activeState.JournalText = journalText;
            return this;
        }

        /// <summary>
        /// Adds a kill objective to this quest state.
        /// </summary>
        /// <param name="group">The NPC group Id</param>
        /// <param name="amount">The number of NPCs in this group required to kill to complete the objective.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        public QuestBuilder AddKillObjective(NPCGroupType group, int amount)
        {
            var killObjective = new KillTargetObjective(group, amount);
            _activeState.AddObjective(killObjective);

            return this;
        }

        /// <summary>
        /// Adds a collect item objective to this quest state.
        /// </summary>
        /// <param name="resref">The resref of the required item.</param>
        /// <param name="amount">The number of items needed to complete the objective.</param>
        /// <returns>A QuestBuilder with the configured options.</returns>
        public QuestBuilder AddCollectItemObjective(string resref, int amount)
        {
            var collectItemObjective = new CollectItemObjective(resref, amount);
            _activeState.AddObjective(collectItemObjective);

            return this;
        }

        /// <summary>
        /// Builds all of the configured quests.
        /// </summary>
        /// <returns>A dictionary containing all of the new quests.</returns>
        public Dictionary<string, QuestDetail> Build()
        {
            return _quests;
        }
    }
}
