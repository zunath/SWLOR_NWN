using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN;

namespace SWLOR.Game.Server.Scripts.Placeable.OverflowStorage
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
            NWPlaceable container = (_.OBJECT_SELF);
            container.DestroyAllInventoryItems();
            container.Destroy();
        }
    }
}
