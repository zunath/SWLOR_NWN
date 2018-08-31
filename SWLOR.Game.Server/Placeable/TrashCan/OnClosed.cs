using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.NWScript;

namespace SWLOR.Game.Server.Placeable.TrashCan
{
    public class OnClosed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable self = NWPlaceable.Wrap(Object.OBJECT_SELF);
            foreach (NWItem item in self.InventoryItems)
            {
                item.Destroy();
            }

            self.Destroy();
            return true;
        }
    }
}
