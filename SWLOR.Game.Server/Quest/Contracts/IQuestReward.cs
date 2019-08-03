using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Quest.Contracts
{
    public interface IQuestReward
    {
        string MenuName { get; }
        void GiveReward(NWPlayer player);
    }
}
