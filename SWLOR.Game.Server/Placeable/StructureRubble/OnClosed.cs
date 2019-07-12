using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Placeable.StructureRubble
{
    public class OnClosed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable self = NWGameObject.OBJECT_SELF;
            NWItem item = _.GetFirstItemInInventory(self);

            if (!item.IsValid)
            {
                self.Destroy();
            }

            return true;
        }
    }
}
