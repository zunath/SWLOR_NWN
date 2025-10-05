using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Associate.Events
{
    public class OnBeastDisturbed : BaseEvent
    {
        public override string Script => AssociateScriptName.OnBeastDisturbed;
    }
}
