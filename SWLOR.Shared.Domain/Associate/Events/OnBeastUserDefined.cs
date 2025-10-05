using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Associate.Events
{
    public class OnBeastUserDefined : BaseEvent
    {
        public override string Script => AssociateScriptName.OnBeastUserDefined;
    }
}
