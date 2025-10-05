using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Associate.Events
{
    public class OnDroidAssociateUsed : BaseEvent
    {
        public override string Script => ScriptName.OnDroidAssociateUsed;
    }
}
