using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable.FuelBay
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
            NWPlaceable objSelf = (NWScript.OBJECT_SELF);
            NWObject parent = (objSelf.GetLocalObject("CONTROL_TOWER_PARENT"));
            parent.DeleteLocalObject("CONTROL_TOWER_FUEL_BAY");
            objSelf.DestroyAllInventoryItems();
            objSelf.Destroy();
        }
    }
}
