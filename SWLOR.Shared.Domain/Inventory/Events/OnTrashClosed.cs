using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Inventory.Events
{
    public class OnTrashClosed : BaseEvent
    {
        public override string Script => InventoryScriptName.OnTrashClosed;
    }
}
