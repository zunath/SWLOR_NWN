using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Space.Events
{
    public class OnPlayerCapAdjusted : BaseEvent
    {
        public override string Script => ScriptName.OnPlayerCapAdjusted;
    }
}
