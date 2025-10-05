using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Space.Events
{
    public class OnSpaceTarget : BaseEvent
    {
        public override string Script => ScriptName.OnSpaceTarget;
    }
}
