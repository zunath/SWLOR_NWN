using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Scripting.Scripts.Placeable.QuestSystem.ItemCollector
{
    public class OnClosed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWObject container = NWGameObject.OBJECT_SELF;
            container.DestroyAllInventoryItems();
            container.Destroy();

            return true;
        }
    }
}
