using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Crafting.Events
{
    public class OnRefineryUsed : BaseEvent
    {
        public override string Script => ScriptName.OnRefineryUsed;
    }
}
