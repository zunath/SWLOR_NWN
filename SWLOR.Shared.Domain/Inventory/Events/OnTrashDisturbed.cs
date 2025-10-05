using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Inventory.Events
{
    public class OnTrashDisturbed : BaseEvent
    {
        public override string Script => ScriptName.OnTrashDisturbed;
    }
}
