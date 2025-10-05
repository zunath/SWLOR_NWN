using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Inventory.Events
{
    public class OnCorpseClosed : BaseEvent
    {
        public override string Script => ScriptName.OnCorpseClosed;
    }
}
