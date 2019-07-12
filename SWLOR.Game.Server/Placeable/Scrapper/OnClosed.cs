using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Placeable.Scrapper
{
    public class OnClosed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable container = NWGameObject.OBJECT_SELF;
            container.DestroyAllInventoryItems();
            container.Destroy();

            return true;
        }
    }
}
