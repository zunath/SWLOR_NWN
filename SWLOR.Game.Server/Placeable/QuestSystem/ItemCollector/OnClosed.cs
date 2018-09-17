using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Placeable.QuestSystem.ItemCollector
{
    public class OnClosed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWObject container = Object.OBJECT_SELF;
            container.DestroyAllInventoryItems();
            container.Destroy();

            return true;
        }
    }
}
