using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Crafting.Events
{
    public class OnCraftSuccess : BaseEvent
    {
        public override string Script => ScriptName.OnCraftSuccess;
    }
}
