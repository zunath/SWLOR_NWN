using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Placeable.ResourceBay
{
    public class OnClosed : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable objSelf = (NWGameObject.OBJECT_SELF);
            NWObject parent = (objSelf.GetLocalObject("CONTROL_TOWER_PARENT"));
            parent.DeleteLocalObject("CONTROL_TOWER_RESOURCE_BAY");
            objSelf.DestroyAllInventoryItems();
            objSelf.Destroy();
            return true;
        }
    }
}
