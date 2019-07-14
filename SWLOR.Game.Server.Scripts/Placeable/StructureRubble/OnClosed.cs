using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;

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
            NWPlaceable self = NWGameObject.OBJECT_SELF;
            NWItem item = _.GetFirstItemInInventory(self);

            if (!item.IsValid)
            {
                self.Destroy();
            }
        }
    }
}
