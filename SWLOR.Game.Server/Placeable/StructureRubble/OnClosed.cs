using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Placeable.StructureRubble
{
    public class OnClosed: IRegisteredEvent
    {
        private readonly INWScript _;

        public OnClosed(INWScript script)
        {
            _ = script;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable self = Object.OBJECT_SELF;
            NWItem item = _.GetFirstItemInInventory(self);

            if (!item.IsValid)
            {
                self.Destroy();
            }

            return true;
        }
    }
}
