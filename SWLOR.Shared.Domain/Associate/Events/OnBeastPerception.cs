using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Associate.Events
{
    public class OnBeastPerception : BaseEvent
    {
        public override string Script => AssociateScriptName.OnBeastPerception;
    }
}
