using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Quest.Contracts
{
    public interface IQuest
    {
        int QuestID { get; }
        string Name { get; }
        string JournalTag { get; }
        bool AllowRewardSelection { get; }
        GuildType Guild { get; }
        int RequiredGuildRank { get; }

        IQuestState GetState(int state);
        IEnumerable<IQuestState> GetStates();
        IEnumerable<IQuestReward> GetRewards();
        bool CanAccept(NWPlayer player);
        bool CanComplete(NWPlayer player);
        bool IsComplete(NWPlayer player);

        void Accept(NWPlayer player, NWObject questSource);
        void Advance(NWPlayer player, NWObject questSource);
        void Complete(NWPlayer player, NWObject questSource, IQuestReward selectedReward);
        void GiveRewards(NWPlayer player);
        
        IQuest OnAccepted(Action<NWPlayer, NWObject> action);
        IQuest OnAdvanced(Action<NWPlayer, NWObject, int> action);
        IQuest OnCompleted(Action<NWPlayer, NWObject> action);

        IQuest IsRepeatable();
        IQuest IsGuildTask(GuildType guild, int requiredRank);
        IQuest EnableRewardSelection();

        IQuest AddObjective(int state, IQuestObjective objective);
        IQuest AddReward(IQuestReward reward);
        IQuest AddPrerequisite(IQuestPrerequisite prerequisite);

        IQuest AddObjectiveKillTarget(int state, NPCGroup group, int amount);
        IQuest AddObjectiveCollectItem(int state, string resref, int quantity, bool mustBeCraftedByPlayer);
        IQuest AddObjectiveCollectKeyItem(int state, KeyItem keyItemID);
        IQuest AddObjectiveTalkToNPC(int state);
        IQuest AddObjectiveEnterTrigger(int state);
        IQuest AddObjectiveUseObject(int state);

        IQuest AddRewardGold(int amount, bool isSelectable = false);
        IQuest AddRewardItem(string resref, int quantity, bool isSelectable = true);
        IQuest AddRewardKeyItem(KeyItem keyItemID, bool isSelectable = true);
        IQuest AddRewardFame(FameRegion regionID, int amount, bool isSelectable = false);
        IQuest AddRewardGuildPoints(GuildType guild, int amount, bool isSelectable = false);

        IQuest AddPrerequisiteFame(FameRegion regionID, int amount);
        IQuest AddPrerequisiteKeyItem(KeyItem keyItemID);
        IQuest AddPrerequisiteQuest(int questID);
    }
}
