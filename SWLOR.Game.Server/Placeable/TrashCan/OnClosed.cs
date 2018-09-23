using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using NWN;

namespace SWLOR.Game.Server.Placeable.TrashCan
{
    public class OnClosed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable self = (Object.OBJECT_SELF);
            foreach (NWItem item in self.InventoryItems)
            {
                item.Destroy();
            }

            self.Destroy();
            return true;
        }
    }
}
