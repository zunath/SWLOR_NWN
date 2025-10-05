using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Combat.Events
{
    public class OnEnmityAcquired : BaseEvent
    {
        public override string Script => ScriptName.OnEnmityAcquired;
    }
}
