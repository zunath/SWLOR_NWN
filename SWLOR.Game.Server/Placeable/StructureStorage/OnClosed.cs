using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.StructureStorage
{
    public class OnClosed : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable objSelf = NWPlaceable.Wrap(Object.OBJECT_SELF);
            NWObject parent = NWObject.Wrap(objSelf.GetLocalObject("STRUCTURE_TEMP_PARENT"));
            parent.DeleteLocalObject("STRUCTURE_TEMP_INVENTORY_OPENED");
            objSelf.DestroyAllInventoryItems();
            objSelf.Destroy();
            return true;
        }
    }
}
