using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Quest.Contracts;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Legacy.Quest.Reward
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
