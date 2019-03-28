using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;

namespace SWLOR.Game.Server.Placeable.TrashCan
{
    public class OnDisturbed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWItem oItem = (_.GetInventoryDisturbItem());
            int type = _.GetInventoryDisturbType();

            if (type == _.INVENTORY_DISTURB_TYPE_ADDED)
            {
                oItem.Destroy();
            }
            return true;
        }
    }
}
