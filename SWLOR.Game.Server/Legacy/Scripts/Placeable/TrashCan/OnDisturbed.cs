using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable.TrashCan
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
