using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Quest.Contracts
{
    public interface IQuestReward
    {
        void GiveReward(NWPlayer player);
    }
}
