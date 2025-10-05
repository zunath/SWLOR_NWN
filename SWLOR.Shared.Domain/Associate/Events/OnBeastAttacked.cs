using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Associate.Events
{
    public class OnBeastAttacked : BaseEvent
    {
        public override string Script => AssociateScriptName.OnBeastAttacked;
    }
}
