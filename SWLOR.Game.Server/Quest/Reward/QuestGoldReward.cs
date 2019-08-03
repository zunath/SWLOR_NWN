using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;
using static NWN._;

namespace SWLOR.Game.Server.Quest.Reward
{
    public class QuestGoldReward: IQuestReward
    {
        private readonly int _amount;

        public QuestGoldReward(int amount)
        {
            _amount = amount;
        }

        public void GiveReward(NWPlayer player)
        {
            GiveGoldToCreature(player, _amount);
        }
    }
}
