using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Placeable.FuelBay
{
    public class OnClosed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable objSelf = NWPlaceable.Wrap(Object.OBJECT_SELF);
            NWObject parent = NWObject.Wrap(objSelf.GetLocalObject("CONTROL_TOWER_PARENT"));
            parent.DeleteLocalObject("CONTROL_TOWER_FUEL_BAY_USER");
            objSelf.DestroyAllInventoryItems();
            objSelf.Destroy();
            return true;
        }
    }
}
