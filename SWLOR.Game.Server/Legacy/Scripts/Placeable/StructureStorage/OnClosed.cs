using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable.StructureStorage
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
            NWPlaceable objSelf = (NWScript.OBJECT_SELF);
            NWObject parent = (objSelf.GetLocalObject("STRUCTURE_TEMP_PARENT"));
            parent.DeleteLocalObject("STRUCTURE_TEMP_INVENTORY_OPENED");
            objSelf.DestroyAllInventoryItems();
            objSelf.Destroy();
        }
    }
}
