using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Crafting.Events
{
    public class OnScavengeClosed : BaseEvent
    {
        public override string Script => ScriptName.OnScavengeClosed;
    }
}
