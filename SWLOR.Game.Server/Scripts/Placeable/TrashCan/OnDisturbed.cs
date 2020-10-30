using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Core.NWScript.Enum;

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
            NWItem oItem = (NWScript.GetInventoryDisturbItem());
            var type = NWScript.GetInventoryDisturbType();

            if (type == DisturbType.Added)
            {
                oItem.Destroy();
            }
        }
    }
}
