using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Quest.Contracts
{
    public interface IQuest
    {
        string Name { get; }
        string JournalTag { get; }

        bool CanAccept(NWPlayer player);
        bool IsComplete(NWPlayer player);
        void GiveRewards(NWPlayer player);

        IQuest IsRepeatable();

        IQuest AddObjective(int state, IQuestObjective objective);
        IQuest AddReward(IQuestReward reward);
        IQuest AddPrerequisite<T>()
            where T: IQuest;

        IQuest AddObjectiveKillTarget(int state, string resref, int amount);
        IQuest AddObjectiveCollectItem(int state, string resref, int quantity);

        IQuest AddRewardGold(int amount);
        IQuest AddRewardItem(string resref, int quantity);
    }
}
