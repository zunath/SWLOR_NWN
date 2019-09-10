using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;

namespace SWLOR.Game.Server.Scripts.Placeable.TrashCan
{
    public class OnDisturbed: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWItem oItem = (_.GetInventoryDisturbItem());
            int type = _.GetInventoryDisturbType();

            if (type == _.INVENTORY_DISTURB_TYPE_ADDED)
            {
                oItem.Destroy();
            }
        }
    }
}
