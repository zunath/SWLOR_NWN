using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;

namespace SWLOR.Game.Server.Quest.Objective
{
    public class CollectItemObjective: IQuestObjective
    {
        private string _resref;
        private int _quantity;

        public CollectItemObjective(string resref, int quantity)
        {
            _resref = resref;
            _quantity = quantity;
        }

        public bool IsComplete(NWPlayer player)
        {
            // todo: check persistence
            return false;
        }
    }
}
