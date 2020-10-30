using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Scripts.Placeable.StructureRubble
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
            NWPlaceable self = NWScript.OBJECT_SELF;
            NWItem item = NWScript.GetFirstItemInInventory(self);

            if (!item.IsValid)
            {
                self.Destroy();
            }
        }
    }
}
