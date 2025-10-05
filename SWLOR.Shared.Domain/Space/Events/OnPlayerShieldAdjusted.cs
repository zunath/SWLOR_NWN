using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Space.Events
{
    public class OnPlayerShieldAdjusted : BaseEvent
    {
        public override string Script => ScriptName.OnPlayerShieldAdjusted;
    }
}
