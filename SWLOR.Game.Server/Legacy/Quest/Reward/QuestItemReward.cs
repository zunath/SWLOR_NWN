using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Quest.Contracts;
using SWLOR.Game.Server.Legacy.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Legacy.Quest.Reward
{
    public class QuestItemReward: IQuestReward
    {
        public bool IsSelectable { get; }
        public string MenuName { get; }
        private readonly string _resref;
        private readonly int _quantity;

        public QuestItemReward(string resref, int quantity, bool isSelectable)
        {
            _resref = resref;
            _quantity = quantity;
            IsSelectable = isSelectable;

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
