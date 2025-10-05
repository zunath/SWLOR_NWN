using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.World.Events
{
    public class OnResourceUsed : BaseEvent
    {
        public override string Script => WorldScriptName.OnResourceUsed;
    }
}
