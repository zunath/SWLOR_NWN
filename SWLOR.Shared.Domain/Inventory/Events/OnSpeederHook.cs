using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Inventory.Events
{
    public class OnSpeederHook : BaseEvent
    {
        public override string Script => InventoryScriptName.OnSpeederHook;
    }
}
