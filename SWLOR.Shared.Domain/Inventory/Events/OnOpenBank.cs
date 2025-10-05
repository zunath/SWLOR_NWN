using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Inventory.Events
{
    public class OnOpenBank : BaseEvent
    {
        public override string Script => InventoryScriptName.OnOpenBank;
    }
}
