using System;
using System.Collections.Generic;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;
using SWLOR.Game.Server.Quest.Objective;
using SWLOR.Game.Server.Quest.Reward;

namespace SWLOR.Game.Server.Quest
{
    public class Quest: IQuest
    {
        private Dictionary<int, IQuestState> QuestStates { get; } = new Dictionary<int, IQuestState>();
        private List<IQuestReward> Rewards { get; } = new List<IQuestReward>();
        private List<Type> PrerequisiteTypes { get; } = new List<Type>();

        public string Name { get; }
        public string JournalTag { get; }
        private bool _repeatable;

        private Action _onAccept = null;
        private Action _onAdvance = null;
        private Action _onComplete = null;

        public Quest(string name, string journalTag)
        {
            Name = name;
            JournalTag = journalTag;
        }
        
        public IQuestState AddState()
        {
            int index = QuestStates.Count;
            QuestStates[index] = new QuestState();
            return QuestStates[index];
        }

        public bool CanAccept(NWPlayer player)
        {
            // todo: check persistence
            return false;
        }

        public bool IsComplete(NWPlayer player)
        {
            //todo: check persistence
            return false;
        }

        public void GiveRewards(NWPlayer player)
        {
            foreach (var reward in Rewards)
            {
                reward.GiveReward(player);
            }
        }

        public IQuest IsRepeatable()
        {
            _repeatable = true;
            return this;
        }

        public IQuest AddObjective(int state, IQuestObjective objective)
        {
            if (!QuestStates.ContainsKey(state - 1))
            {
                QuestStates[state-1] = new QuestState();
            }

            var questState = QuestStates[state - 1];
            questState.AddObjective(objective);

            return this;
        }

        public IQuest AddReward(IQuestReward reward)
        {
            Rewards.Add(reward);

            return this;
        }

        public IQuest AddPrerequisite<T>()
            where T: IQuest
        {
            // User tried to make this quest a prerequisite of itself.
            if (typeof(T) == GetType())
            {
                throw new Exception("This quest cannot be a prerequisite of itself.");
            }

            PrerequisiteTypes.Add(typeof(T));
            return this;
        }


        // Convenience functions for commonly used objectives
        public IQuest AddObjectiveKillTarget(int state, string resref, int amount)
        {
            AddObjective(state, new KillTargetObjective(resref, amount));
            return this;
        }

        public IQuest AddObjectiveCollectItem(int state, string resref, int quantity)
        {
            AddObjective(state, new CollectItemObjective(resref, quantity));
            return this;
        }

        // Convenience functions for commonly used rewards
        public IQuest AddRewardGold(int amount)
        {
            AddReward(new QuestGoldReward(amount));
            return this;
        }

        public IQuest AddRewardItem(string resref, int quantity)
        {
            AddReward(new QuestItemReward(resref, quantity));
            return this;
        }
    }
}
