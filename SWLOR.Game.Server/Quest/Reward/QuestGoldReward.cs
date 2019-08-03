using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;
using static NWN._;

namespace SWLOR.Game.Server.Quest.Reward
{
    public class QuestGoldReward: IQuestReward
    {
        public int Amount { get; }

        public QuestGoldReward(int amount)
        {
            Amount = amount;
        }

        public string MenuName => Amount + " Credits";

        public void GiveReward(NWPlayer player)
        {
            GiveGoldToCreature(player, Amount);
        }
    }
}
