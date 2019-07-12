using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using NWN;

namespace SWLOR.Game.Server.Placeable.OverflowStorage
{
    public class OnClosed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable container = (NWGameObject.OBJECT_SELF);
            container.DestroyAllInventoryItems();
            container.Destroy();

            return true;
        }
    }
}
