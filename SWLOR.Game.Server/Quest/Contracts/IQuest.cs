using System;
using System.Collections.Generic;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Quest.Contracts
{
    public interface IQuest
    {
        int QuestID { get; }
        string Name { get; }
        string JournalTag { get; }
        bool AllowRewardSelection { get; }

        IQuestState GetState(int state);
        IEnumerable<IQuestState> GetStates();
        IEnumerable<IQuestReward> GetRewards();
        bool CanAccept(NWPlayer player);
        bool CanComplete(NWPlayer player);
        bool IsComplete(NWPlayer player);

        void Accept(NWPlayer player);
        void Advance(NWPlayer player);
        void Complete(NWPlayer player, IQuestReward selectedReward);
        void GiveRewards(NWPlayer player);
        
        IQuest OnAccepted(Action<NWPlayer> action);
        IQuest OnAdvanced(Action<NWPlayer> action);
        IQuest OnCompleted(Action<NWPlayer> action);

        IQuest IsRepeatable();
        IQuest EnableRewardSelection();

        IQuest AddObjective(int state, IQuestObjective objective);
        IQuest AddReward(IQuestReward reward);
        IQuest AddPrerequisite(IQuestPrerequisite prerequisite);

        IQuest AddObjectiveKillTarget(int state, NPCGroupType group, int amount);
        IQuest AddObjectiveCollectItem(int state, string resref, int quantity, bool mustBeCraftedByPlayer);
        IQuest AddObjectiveCollectKeyItem(int state, int keyItemID);

        IQuest AddRewardGold(int amount);
        IQuest AddRewardItem(string resref, int quantity);
        IQuest AddRewardKeyItem(int keyItemID);
        IQuest AddRewardFame(int regionID, int amount);

        IQuest AddPrerequisiteFame(int regionID, int amount);
        IQuest AddPrerequisiteKeyItem(int keyItemID);
        IQuest AddPrerequisiteQuest(int questID);
    }
}
