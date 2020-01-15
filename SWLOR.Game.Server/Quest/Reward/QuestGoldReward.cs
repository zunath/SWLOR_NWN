using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;
using static SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.Quest.Reward
{
    public class QuestGoldReward: IQuestReward
    {
        public int Amount { get; }

        public QuestGoldReward(int amount, bool isSelectable)
        {
            Amount = amount;
            IsSelectable = isSelectable;
        }

        public bool IsSelectable { get; }
        public string MenuName => Amount + " Credits";

        public void GiveReward(NWPlayer player)
        {
            GiveGoldToCreature(player, Amount);
        }
    }
}
