using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Scripts.Placeable.TrashCan
{
    public class OnClosed: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlaceable self = (NWScript.OBJECT_SELF);
            foreach (var item in self.InventoryItems)
            {
                item.Destroy();
            }

            self.Destroy();
        }
    }
}
