using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.NWScript;

namespace SWLOR.Game.Server.Placeable.OverflowStorage
{
    public class OnClosed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable container = NWPlaceable.Wrap(Object.OBJECT_SELF);
            container.DestroyAllInventoryItems();
            container.Destroy();

            return true;
        }
    }
}
