using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;
using SWLOR.Game.Server.Service;
using static NWN._;

namespace SWLOR.Game.Server.Quest.Reward
{
    public class QuestItemReward: IQuestReward
    {
        public string MenuName { get; }
        private readonly string _resref;
        private readonly int _quantity;

        public QuestItemReward(string resref, int quantity)
        {
            _resref = resref;
            _quantity = quantity;

            var itemVO = QuestService.GetTempItemInformation(resref, quantity);

            if (_quantity > 1)
                MenuName = _quantity + "x " + itemVO.Name;
            else
                MenuName = itemVO.Name;
        }


        public void GiveReward(NWPlayer player)
        {
            CreateItemOnObject(_resref, player, _quantity);
        }
    }
}
