using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Scripts.Placeable.ResourceBay
{
    public class OnClosed : IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlaceable objSelf = (_.OBJECT_SELF);
            NWObject parent = (objSelf.GetLocalObject("CONTROL_TOWER_PARENT"));
            parent.DeleteLocalObject("CONTROL_TOWER_RESOURCE_BAY");
            objSelf.DestroyAllInventoryItems();
            objSelf.Destroy();
        }
    }
}
