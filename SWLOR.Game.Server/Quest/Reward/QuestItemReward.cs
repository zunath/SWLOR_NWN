using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;
using static NWN._;

namespace SWLOR.Game.Server.Quest.Reward
{
    public class QuestItemReward: IQuestReward
    {
        private readonly string _resref;
        private readonly int _quantity;

        public QuestItemReward(string resref, int quantity)
        {
            _resref = resref;
            _quantity = quantity;
        }

        public void GiveReward(NWPlayer player)
        {
            CreateItemOnObject(_resref, player, _quantity);
        }
    }
}
