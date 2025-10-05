using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Inventory.Events
{
    public class OnGetKeyItem : BaseEvent
    {
        public override string Script => InventoryScriptName.OnGetKeyItem;
    }
}
